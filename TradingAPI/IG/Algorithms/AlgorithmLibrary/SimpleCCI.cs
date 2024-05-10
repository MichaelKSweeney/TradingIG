using System;
using System.Collections.Generic;
using System.Text;
using Skender.Stock.Indicators;
using System.Linq;
using dto.endpoint.positions.create.otc.v2;
using IGWebApiClient;
using TradingAPI.IG.Queries;
using static TradingAPI.IG.FileManager;
using dto.endpoint.prices.v2;
using static TradingAPI.IG.RestAPI.Models.IgPublicApiData;
using System.Timers;

namespace TradingAPI.IG.Algorithms.AlgorithmLibrary
{
    class SimpleCCI : AlgorithmBase
    {

        List<PriceSnapshot> candles = new List<PriceSnapshot>();

        private Timer coolDownTimer = new Timer(60000);

        private bool CanOpenPosition = false;

        public SimpleCCI(string epic, ChartScale chartScale, List<PriceSnapshot> prerequisteCandles) : base (epic, chartScale)
        {
            candles.AddRange(prerequisteCandles);
            coolDownTimer.Elapsed += AllowTrade;
            coolDownTimer.AutoReset = false;
            coolDownTimer.Start();
        }

        public void AllowTrade(Object source, ElapsedEventArgs e)
        {
            CanOpenPosition = true;
        }

        public void FiveMinuteCandle(ChartModel candle)
        {
            candles.Add(FileManager.ConvertStreamToPriceSnapshot(candle));

            Console.WriteLine("candle count: " + candles.Count);
        }


        protected override void AlgorithmStrategy(ChartModel candle)
        {
            //Console.WriteLine($"{candle.ChartEpic} time: {candle.UpdateTime}"); // output market name and time every time incoming candle comes through - just to see i can see the stream hasnt died (happens sometimes)

            var inProgressCandle = FileManager.ConvertStreamToPriceSnapshot(candle); // stream candle is a different object to API candle if i recall so do some conversion


            var tempCandles = candles.ToList();

            tempCandles.Add(inProgressCandle);

            //var candles = FileManager.GetCandles(algorithmEpic, algorithmChartScale.ToString()); // get candles currently downloaded 

            //candles.Add(inProgressCandle); // add the streamed candle thats just come through to our list of predownloaded candles

            BuyStrategy(tempCandles);
            SellStrategy(tempCandles);
        }

        public override void BuyStrategy(List<PriceSnapshot> candles,  TradeType tradeType = TradeType.Bid)
        {
            //////////////////// BUY //////////////////

            var bidCandles = FileManager.ConvertDataSet(candles, TradeType.Bid); // get the BID prices
            var bidCCI = Indicator.GetCci(bidCandles); // get the CCI values

            // Check we have been above 100 CCI recently
            bool HasCCIBeenAbove100Recently_BID = QueryLibrary.HasCCIGoneAboveValueWithinCandleCount(bidCandles, 20, 100, 15); // has the 20 cci been above 100 in the last 10 candles

            // Check is we have been below -100 CCI
            bool HasCCIBeenBelowMinus100Recently_BID = QueryLibrary.HasCCIGoneBelowValueWithinCandleCount(bidCandles, 20, -100, 15);

            // Check we are now above 0 CCI
            bool IsCCINowAbove0_BID = bidCCI.Last().Cci > 0; // is the cci now above 0

            // Was the previous candle below 0
            bool WasCCIPreviouslyBelow0_BID = bidCCI.ToArray()[bidCCI.Count()-2].Cci < 0;

            bool HaveCandlesClosedAbove50EMA_BID = candles.Last().closePrice.bid > Indicator.GetEma(bidCandles, 50).Last().Ema; // is close price of last candle greater than 50EMA

            bool HaveAllCandlesBeenCompletelyAbove50EMA_BID = QueryLibrary.HaveAllCandlesCompletelyBeenAboveEMA(bidCandles, 50, 5);

            if (IsCCINowAbove0_BID && 
                WasCCIPreviouslyBelow0_BID && 
                HasCCIBeenBelowMinus100Recently_BID == false && // if we have gone below -100 recently, this is bad
                HasCCIBeenAbove100Recently_BID && 
                HaveCandlesClosedAbove50EMA_BID &&
                HaveAllCandlesBeenCompletelyAbove50EMA_BID)
            {
                //Console.WriteLine($"{algorithmEpic} - SimpleCCI Algorithm : BUY" );
                Alert();
                if(CanOpenPosition && AccountManager.GetOpenPositions().Result.Response.positions.Any(position => position.market.epic == algorithmEpic) == false)
                {
                    var marketDetails = AccountManager.GetMarketDetails(algorithmEpic).Result.Response;
                    var stopAndLimitDistance = (decimal)marketDetails.dealingRules.minNormalStopOrLimitDistance.value;
                    var atr = (decimal)Indicator.GetAtr(bidCandles).Last().Atr;
                    OpenPosition("BUY", (decimal)0.5, atr * (decimal)1, atr * 2);
                }
            }
        }

        public override void SellStrategy(List<PriceSnapshot> candles, TradeType tradeType = TradeType.Ask)
        {
            ////////////////// SELL ///////////////////

            var askCandles = FileManager.ConvertDataSet(candles, TradeType.Ask);

            var askCCI = Indicator.GetCci(askCandles);
            var askEMA50 = Indicator.GetEma(askCandles, 50);

            // Check we have been below 100 CCI recently
            bool HasCCIBeenBelow100Recently_Ask = QueryLibrary.HasCCIGoneBelowValueWithinCandleCount(askCandles, 20, -100, 15);

            // Check we have been above 100 cci
            bool HasCCIBeenAbove100Recently_Ask = QueryLibrary.HasCCIGoneAboveValueWithinCandleCount(askCandles, 20, 100, 15);

            // Check we are now below 0 CCI
            bool IsCCINowBelow0_Ask = askCCI.Last().Cci < 0;

            bool WasCCIPreviouslyBelow0_Ask = askCCI.ToArray()[askCCI.Count() - 2].Cci > 0;

            bool HaveCandlesClosedAbove50EMA_Ask = candles.Last().closePrice.ask < Indicator.GetEma(askCandles, 50).Last().Ema;

            bool HaveAllCandlesBeenCompletelyBelow50EMA_BID = QueryLibrary.HaveAllCandlesCompletelyBeenBelowEMA(askCandles, 50, 5);

            if (IsCCINowBelow0_Ask && 
                WasCCIPreviouslyBelow0_Ask && 
                HasCCIBeenAbove100Recently_Ask == false && // if we have gone below -100 recently, this is bad
                HasCCIBeenBelow100Recently_Ask && 
                HaveCandlesClosedAbove50EMA_Ask &&
                HaveAllCandlesBeenCompletelyBelow50EMA_BID)
            {
                //Console.WriteLine($"{algorithmEpic} -SimpleCCI Algorithm : SELL");
                Alert();
                if (CanOpenPosition && AccountManager.GetOpenPositions().Result.Response.positions.Any(position => position.market.epic == algorithmEpic) == false)
                {
                    var marketDetails = AccountManager.GetMarketDetails(algorithmEpic).Result.Response;
                    var stopAndLimitDistance = (decimal)marketDetails.dealingRules.minNormalStopOrLimitDistance.value;
                    var atr = (decimal)Indicator.GetAtr(askCandles).Last().Atr;
                    OpenPosition("SELL", (decimal)0.5, atr*(decimal)1.5, atr * (decimal)1.5);
                }
            }
        }

        public void OpenPosition( string direction, decimal size, decimal stopDistance, decimal profitDistance)
        {
            CreatePositionRequest orderRequest = new CreatePositionRequest();

            

            orderRequest.epic = algorithmEpic;
            orderRequest.expiry = "DFB";
            orderRequest.direction = direction;
            orderRequest.size = size;
            orderRequest.level = null;
            orderRequest.orderType = "MARKET";
            orderRequest.guaranteedStop = false;
            orderRequest.stopLevel = null;
            orderRequest.stopDistance = stopDistance;
            orderRequest.trailingStop = false;
            orderRequest.trailingStopIncrement = null;
            orderRequest.forceOpen = true;
            orderRequest.limitLevel = null;
            orderRequest.limitDistance = profitDistance;
            orderRequest.quoteId = null;
            orderRequest.currencyCode = "GBP";

            AccountManager.PlaceOrder(orderRequest);
            CanOpenPosition = false;
            coolDownTimer.Stop();
            coolDownTimer.Start();
        }
    }
}
