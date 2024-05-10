using dto.endpoint.auth.session.v2;
using IGWebApiClient;
using IGWebApiClient.Common;
using Lightstreamer.DotNet.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Linq;
using TradingAPI.IG.Algorithms;
using TradingAPI.IG.RestAPI.Models;
using TradingAPI.IG.Streaming.Models;
using dto.endpoint.prices.v2;
using dto.endpoint.positions.create.otc.v2;
using System.Xml.Serialization;
using System.IO;
using dto.endpoint.watchlists.retrieve;
using System.Threading.Tasks;
using dto.endpoint.accountactivity.activity;
using dto.endpoint.positions.get.otc.v2;
using dto.endpoint.positions.close.v1;
using dto.endpoint.marketdetails.v2;
using dto.endpoint.accountactivity.transaction;

namespace TradingAPI.IG
{
    public static class AccountManager
    {
        // deal with handling account details at some point
        private static string userName = "YOUR USERNAME";
        private static string password = "PASSWORD";
        private static string apiKey = "YOUR API KEY";


        private static bool LoggedIn;
        private static string CurrentAccountId;


        private static SmartDispatcher smartDispatcher;
        private static IGStreamingApiClient igStreamApiClient = new IGStreamingApiClient();

        private static string env = "demo";
        private static IgRestApiClient igRestApiClient;

        public static AccountDetailsTableListerner _accountBalanceSubscription;
        private static TradeSubscription _tradeSubscription;
        public static SubscribedTableKey _accountBalanceStk;
        public static SubscribedTableKey _tradeSubscriptionStk;


        public static List<CandleSubscriptionController> CandleSubscriptions = new List<CandleSubscriptionController>();

        public static ObservableCollection<IgPublicApiData.AccountModel> Accounts = new ObservableCollection<IgPublicApiData.AccountModel>();
        public static async void Login()
        {


            RegisterLightStreamerSubscriptions();
            var ar = new AuthenticationRequest { identifier = userName, password = password };

            smartDispatcher = (SmartDispatcher)SmartDispatcher.getInstance();
            igRestApiClient = new IgRestApiClient(env, smartDispatcher);
            try
            {
                var response = await igRestApiClient.SecureAuthenticate(ar, apiKey);
                if (response && (response.Response != null) && (response.Response.accounts.Count > 0))
                {
                    Accounts.Clear();

                    foreach (var account in response.Response.accounts)
                    {
                        var igAccount = new IgPublicApiData.AccountModel
                        {
                            ClientId = response.Response.clientId,
                            ProfitLoss = response.Response.accountInfo.profitLoss,
                            AvailableCash = response.Response.accountInfo.available,
                            Deposit = response.Response.accountInfo.deposit,
                            Balance = response.Response.accountInfo.balance,
                            LsEndpoint = response.Response.lightstreamerEndpoint,
                            AccountId = account.accountId,
                            AccountName = account.accountName,
                            AccountType = account.accountType
                        };

                        Accounts.Add(igAccount);
                    }
                    await igRestApiClient.accountSwitch(accountSwitchRequest: new dto.endpoint.accountswitch.AccountSwitchRequest() { accountId = "YOUR ACCOUNT ID", defaultAccount = true });
                    LoggedIn = true;

                    Console.WriteLine("Logged in, current account: " + response.Response.currentAccountId);

                    ConversationContext context = igRestApiClient.GetConversationContext();

                    Console.WriteLine("establishing datastream connection");

                    if ((context != null) && (response.Response.lightstreamerEndpoint != null) &&
                        (context.apiKey != null) && (context.xSecurityToken != null) && (context.cst != null))
                    {
                        try
                        {
                            CurrentAccountId = response.Response.currentAccountId;

                            var connectionEstablished =
                                igStreamApiClient.Connect(response.Response.currentAccountId,
                                                          context.cst,
                                                          context.xSecurityToken, context.apiKey,
                                                            response.Response.lightstreamerEndpoint);
                            if (connectionEstablished)
                            {
                                //UpdateDebugMessage(String.Format("Connecting to Lightstreamer. Endpoint ={0}",
                                //                                    response.Response.lightstreamerEndpoint));

                                //// Subscribe to Account Details and Trade Subscriptions...
                                SubscribeToAccountDetails();
                                SubscribeToTradeSubscription();
                            }
                            else
                            {
                                igStreamApiClient = null;
                                Console.WriteLine(String.Format("Could NOT connect to Lightstreamer. Endpoint ={0}", response.Response.lightstreamerEndpoint));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                else
                {
                    //UpdateDebugMessage("Failed to login. HttpResponse StatusCode = " + response.StatusCode);
                    Console.WriteLine("Failed to login. HttpResponse StatusCode = " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ApplicationViewModel exception : " + ex.Message);
                //UpdateDebugMessage("ApplicationViewModel exception : " + ex.Message);
            }

            //PlaceOrder("IX.D.SPTRD.DAILY.IP", 1, "BUY", null, null);
        }

        public static IgRestApiClient GetAPIClient()
        {
            return igRestApiClient;
        }

        public static  List<PriceSnapshot> GetMarketCandles(string marketName, string timeFrame, int intervalCount)
        {

            var marketData = igRestApiClient.priceSearchByNum(marketName, timeFrame, intervalCount.ToString());
            if (marketData.Result.Response != null)
            {

                Console.WriteLine(marketData.Result.Response.allowance.remainingAllowance);
                return marketData.Result.Response.prices;
            }
            return null;
        }

        private static void RegisterLightStreamerSubscriptions()
        {
            _accountBalanceSubscription = new AccountDetailsTableListerner();
            _accountBalanceSubscription.Update += OnAccountUpdate;
            _tradeSubscription = new TradeSubscription();
        }


        public static void SubscribeToCandleStream(string epic, ChartScale timeframe, bool serialiseCandles)
        {
            try
            {
                if (CurrentAccountId != null)
                {
                    //insists on epics being sent as a list of strings even if you only want 1 epic
                    List<string> epics = new List<string> { epic };

                    // check if we already have a subscription to this market and time frame, if not then make a new subscription
                    var existingSubscription = CandleSubscriptions.Where(f => f.chartEpic == epics.First() && f.candleTimeFrame == timeframe).FirstOrDefault();
                    
                    if(existingSubscription == null)
                    {
                        CandleSubscriptionController newSubscription = new CandleSubscriptionController(epics.First(), timeframe, serialiseCandles);
                        CandleSubscriptions.Add(newSubscription);
                        newSubscription.marketSubscriptionStk = igStreamApiClient.SubscribeToChartCandleData(epics, timeframe, newSubscription.ActiveCandleSubscription);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SubscribeToTradeSubscription" + ex.Message);
            }
        }

        public static CandleSubscriptionController GetCandleStreamUpdateBinding(string epic, ChartScale timeframe)
        {
            try
            {
                if (CurrentAccountId != null)
                {
                    return CandleSubscriptions.Where(f => f.chartEpic == epic && f.candleTimeFrame == timeframe).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SubscribeToTradeSubscription" + ex.Message);
            }
            return null;
        }

        //public static void SubscribeAlgorithmToCandleStream(List<string> epics, ChartScale timeframe, bool serialiseCandles)
        //{
        //    try
        //    {
        //        if (CurrentAccountId != null)
        //        {

        //            var existingSubscription = CandleSubscriptions.Where(f => f.chartEpic == epics.First() && f.candleTimeFrame == timeframe).FirstOrDefault();

        //            // check if we already have a subscription to this market and time frame, if not then make a new subscription
        //            if (existingSubscription == null)
        //            {
        //                CandleSubscriptionController newSubscription = new CandleSubscriptionController(epics.First(), timeframe, serialiseCandles);
        //                CandleSubscriptions.Add(newSubscription);
        //                newSubscription.marketSubscriptionStk = igStreamApiClient.SubscribeToChartCandleData(epics, timeframe, newSubscription.ActiveCandleSubscription);

        //            }
        //            else
        //            {
        //                Console.WriteLine($"subscription already active for {epic[0]}");
        //            }

        //            Console.WriteLine($"Lightstreamer - Subscribing to market: {epics[0]} timeframe: {timeframe} algorithm: {algo.GetType().ToString()}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("SubscribeToTradeSubscription" + ex.Message);
        //    }

        //}

        public static void UnSubscribeAlgorithmToCandleStream(List<string> epics, ChartScale timeframe, AlgorithmBase algo)
        {
            try
            {
                if (CurrentAccountId != null)
                {
                    //CandleSubscriptionController newSubscription = new CandleSubscriptionController(timeframe, false);
                    //CandleSubscriptionContainer.Add(newSubscription);

                   
                    //newSubscription.CandleUpdateMinutes += algo.Run;
                    //igStreamApiClient.UnsubscribeTableKey(newSubscription.tradeSubscriptionStk);

                    //CandleSubscriptionContainer.Where(sub => sub.)

                    Console.WriteLine($"Lightstreamer - UnSubscribing to market: {epics[0]} timeframe: {timeframe} algorithm: {algo.GetType().ToString()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SubscribeToTradeSubscription" + ex.Message);
            }

        }

        

        public static void SubscribeToTradeSubscription()
        {
            try
            {
                if (CurrentAccountId != null)
                {
                    _tradeSubscriptionStk = igStreamApiClient.SubscribeToTradeSubscription(CurrentAccountId, _tradeSubscription);
                    Console.WriteLine("Lightstreamer - Subscribing to CONFIRMS, Working order updates and open position updates");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ApplicationViewModel - SubscribeToTradeSubscription" + ex.Message);
            }
           

        }

        public static void tradeupdate()
        {
            //_tradeSubscription.OnUpdate += 
        }

        private static void OnAccountUpdate(object sender, UpdateArgs<StreamingAccountData> e)
        {
            var accountUpdates = e.UpdateData;

            if ((e.ItemPosition == 0) || (e.ItemPosition > Accounts.Count))
            {
                return;
            }
            var index = e.ItemPosition - 1; // we are subscription to the current account ( which will be account index 0 ).                                     

            if (accountUpdates.AmountDue.HasValue)
                Accounts[index].AmountDue = accountUpdates.AmountDue.Value;
            if (accountUpdates.AvailableCash != null)
                Accounts[index].AvailableCash = accountUpdates.AvailableCash.Value;
            if (accountUpdates.Deposit != null)
                Accounts[index].Deposit = accountUpdates.Deposit.Value;
            if (accountUpdates.ProfitAndLoss != null)
                Accounts[index].ProfitLoss = accountUpdates.ProfitAndLoss.Value;
            if (accountUpdates.UsedMargin != null)
                Accounts[index].UsedMargin = accountUpdates.UsedMargin.Value;

        }


        public static void SubscribeToAccountDetails()
        {
            try
            {
                if (CurrentAccountId != null)
                {
                    _accountBalanceStk = igStreamApiClient.SubscribeToAccountDetails(CurrentAccountId, _accountBalanceSubscription);
                    Console.WriteLine("Lightstreamer - Subscribing to Account Details");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ApplicationViewModel - SubscribeToAccountDetails" + ex.Message);
            }
        }


        public static async void PlaceOrder(string marketName, decimal size, string direction, decimal? profitLevel, decimal? lossStop, bool trailingStop = false, decimal? trailingStopIncrement = null)
        {

            CreatePositionRequest orderRequest = new CreatePositionRequest();

            orderRequest.epic = marketName;
            orderRequest.expiry = "DFB";
            orderRequest.direction = direction;
            orderRequest.size = size;
            orderRequest.level = null;
            orderRequest.orderType = "MARKET";
            orderRequest.guaranteedStop = false;
            orderRequest.stopLevel = null;
            orderRequest.stopDistance = lossStop;
            orderRequest.trailingStop = trailingStop;
            orderRequest.trailingStopIncrement = trailingStopIncrement;
            orderRequest.forceOpen = true;
            orderRequest.limitLevel = null;
            orderRequest.limitDistance = profitLevel;
            orderRequest.quoteId = null;
            orderRequest.currencyCode = "GBP";

            var response = await igRestApiClient.createPositionV2(orderRequest);

            if(response.StatusCode != System.Net.HttpStatusCode.Accepted || response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"Failed to place order - {response.StatusCode} : {response.Response.dealReference}");
            }

            //SerialisePosition(orderRequest);

        }

        public static async void PlaceOrder(CreatePositionRequest orderRequest)
        {

            

            
            var response = await igRestApiClient.createPositionV2(orderRequest);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted && response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"Failed to place order - {response.StatusCode} : {response.Response.dealReference}");
            }
            else
            {
                Console.WriteLine($"Order placed at {DateTime.Now} -{response.Response.dealReference} - {orderRequest.epic}");
            }

            //SerialisePosition(orderRequest);

        }


        public static void SerialisePosition(CreatePositionRequest position)
        {


            var serialiser = new XmlSerializer(typeof(CreatePositionRequest));




            var root = $@"D:\Gits\IG\TradingIG\MarketData\OpenPositions\";
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            var path = System.IO.Path.Combine(root, position.epic + " " + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".xml");
            using (var writer = new StreamWriter(path))
            {
                serialiser.Serialize(writer, position);
            }



        }

        public static async void ClosePosition(OpenPosition position)
        {

            try
            {
                var closePositionRequest = new ClosePositionRequest();

                closePositionRequest.dealId = position.position.dealId;
                closePositionRequest.direction = position.position.direction == "BUY" ? "SELL" : "BUY";
                closePositionRequest.size = 1;
                closePositionRequest.orderType = "MARKET";


                await igRestApiClient.closePosition(closePositionRequest);
            }
            catch
            {
            }


        }





        public static async Task<IgResponse<WatchlistInstrumentsResponse>> GetWatchListMarkets(string watchlistID)
        {


            var watchlists = await igRestApiClient.instrumentsForWatchlist(watchlistID);
            return watchlists;

        }

        public static async Task<IgResponse<ActivityHistoryResponse>> GetHistory()
        {


            var history = await igRestApiClient.lastActivityPeriod("99999999");
            return history;

        }

        public static async Task<IgResponse<MarketDetailsResponse>> GetMarketDetails(string marketEpic)
        {


            var marketDetails = await igRestApiClient.marketDetailsV2(marketEpic);
            return marketDetails;

        }

        public static async Task<IgResponse<PositionsResponse>> GetOpenPositions()
        {


            var openPositions = await igRestApiClient.getOTCOpenPositionsV2();
            return openPositions;

        }

        public static async Task<IgResponse<TransactionHistoryResponse>> GetTransactions(string type, DateTime from, DateTime to)
        {
            //DateTime epoch = DateTime.Today;
            //long ms = (long)(DateTime.UtcNow - epoch).TotalMilliseconds;

            double milliseconds = Convert.ToInt32((from - to).TotalMilliseconds);

            var transactionHistory = await igRestApiClient.lastTransactionPeriod(type, milliseconds.ToString());
            return transactionHistory;

        }

    }

    

    public class TradeSubscription : HandyTableListenerAdapter
    {
        //private readonly ApplicationViewModel _applicationViewModel;
        //public TradeSubscription(ApplicationViewModel avm)
        //{
        //    _applicationViewModel = avm;
        //}
        public IgPublicApiData.TradeSubscriptionModel tsm = new IgPublicApiData.TradeSubscriptionModel();
        public enum TradeSubscriptionType
        {
            Opu = 0,
            Wou = 1,
            Confirm = 2
        }

        public IgPublicApiData.TradeSubscriptionModel UpdateTs(int itemPos, string itemName, IUpdateInfo update, string inputData, TradeSubscriptionType updateType)
        {
            //var tsm = new IgPublicApiData.TradeSubscriptionModel();
            
            try
            {
                var tradeSubUpdate = JsonConvert.DeserializeObject<LsTradeSubscriptionData>(inputData);
                tsm.DealId = tradeSubUpdate.dealId;
                tsm.AffectedDealId = tradeSubUpdate.affectedDealId;
                tsm.DealReference = tradeSubUpdate.dealReference;
                tsm.DealStatus = tradeSubUpdate.dealStatus.ToString();
                tsm.Direction = tradeSubUpdate.direction.ToString();
                tsm.ItemName = itemName;
                tsm.Epic = tradeSubUpdate.epic;
                tsm.Expiry = tradeSubUpdate.expiry;
                tsm.GuaranteedStop = tradeSubUpdate.guaranteedStop;
                tsm.Level = tradeSubUpdate.level;
                tsm.Limitlevel = tradeSubUpdate.limitLevel;
                tsm.Size = tradeSubUpdate.size;
                tsm.Status = tradeSubUpdate.status.ToString();
                tsm.StopLevel = tradeSubUpdate.stopLevel;

                switch (updateType)
                {
                    case TradeSubscriptionType.Opu:
                        tsm.TradeType = "OPU";
                        break;
                    case TradeSubscriptionType.Wou:
                        tsm.TradeType = "WOU";
                        break;
                    case TradeSubscriptionType.Confirm:
                        tsm.TradeType = "CONFIRM";
                        break;
                }
                
                SmartDispatcher.getInstance().BeginInvoke(() =>
                {
                    //if (_applicationViewModel != null)
                    //{
                    //    _applicationViewModel.UpdateDebugMessage("TradeSubscription received : " + tsm.TradeType);
                    //    _applicationViewModel.TradeSubscriptions.Add(tsm);

                        if ((tradeSubUpdate.affectedDeals != null) && (tradeSubUpdate.affectedDeals.Count > 0))
                        {
                            foreach (var ad in tradeSubUpdate.affectedDeals)
                            {
                                var adm = new IgPublicApiData.AffectedDealModel
                                {
                                    AffectedDealId = ad.dealId,
                                    AffectedDealStatus = ad.status
                                };
                                //_applicationViewModel.AffectedDeals.Add(adm);
                            }
                        }

                    //}
                });
            }
            catch (Exception ex)
            {
                //_applicationViewModel.ApplicationDebugData += ex.Message;
            }
            return tsm;
        }

        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Trade Subscription Update");

            try
            {
                var confirms = update.GetNewValue("CONFIRMS");
                var opu = update.GetNewValue("OPU");
                var wou = update.GetNewValue("WOU");

                if (!(String.IsNullOrEmpty(opu)))
                {
                    UpdateTs(itemPos, itemName, update, opu, TradeSubscriptionType.Opu);
                }
                if (!(String.IsNullOrEmpty(wou)))
                {
                    UpdateTs(itemPos, itemName, update, wou, TradeSubscriptionType.Wou);
                }
                if (!(String.IsNullOrEmpty(confirms)))
                {
                    UpdateTs(itemPos, itemName, update, confirms, TradeSubscriptionType.Confirm);
                }

            }
            catch (Exception ex)
            {
                //_applicationViewModel.ApplicationDebugData += "Exception thrown in TradeSubscription Lightstreamer update" + ex.Message;
            }
        }


    }

    public class SmartDispatcher : PropertyEventDispatcher
    {
        private static PropertyEventDispatcher instance = new SmartDispatcher();

        private static bool _designer = false;
        //private static Dispatcher _instance;
        //private ViewModelBase viewModel;

        public static PropertyEventDispatcher getInstance()
        {
            return instance;
        }

        //public void setViewModel(ViewModelBase viewModel)
        //{
        //    this.viewModel = viewModel;
        //}

        public void addEventMessage(string message)
        {
            //viewModel.AddStatusMessage(message);
        }

        public void BeginInvoke(Action a)
        {
            BeginInvoke(a, false);
        }

        public void BeginInvoke(Action a, bool forceInvoke)
        {
            a();
            Console.Write("BeginInvoke");
            //if (_instance == null)
            //{
            //    RequireInstance();
            //}

            //// If the current thread is the user interface thread, skip the
            //// dispatcher and directly invoke the Action.
            //if (_instance != null)
            //{
            //    if (((forceInvoke && _instance != null) || !_instance.CheckAccess()) && !_designer)
            //    {
            //        _instance.BeginInvoke(a);
            //    }
            //    else
            //    {
            //        a();
            //    }
            //}
            //else
            //{
            //    if (_designer || Application.Current == null)
            //    {
            //        a();
            //    }
            //}
        }

        //private void RequireInstance()
        //{
        //    // Design-time is more of a no-op, won't be able to resolve the
        //    // dispatcher if it isn't already set in these situations.
        //    if (_designer || Application.Current == null)
        //    {
        //        return;
        //    }

        //    // Attempt to use the RootVisual of the plugin to retrieve a
        //    // dispatcher instance. This call will only succeed if the current
        //    // thread is the UI thread.
        //    try
        //    {
        //        _instance = Application.Current.Dispatcher;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new InvalidOperationException("The first time SmartDispatcher is used must be from a user interface thread. Consider having the application call Initialize, with or without an instance.", e);
        //    }

        //    if (_instance == null)
        //    {
        //        throw new InvalidOperationException("Unable to find a suitable Dispatcher instance.");
        //    }
        //}

        ///// <summary>
        ///// Initializes the SmartDispatcher system with the dispatcher
        ///// instance and logger
        ///// </summary>
        ///// <param name="dispatcher">The dispatcher instance.</param>
        //public static void Initialize(Dispatcher dispatcher)
        //{
        //    if (dispatcher == null)
        //    {
        //        throw new ArgumentNullException("dispatcher");
        //    }

        //    _instance = dispatcher;
        //}

    }
}
