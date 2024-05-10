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
using TradingAPI.IG.RestAPI.Models;

namespace TradingAPI.IG.Algorithms.AlgorithmLibrary
{
    public class EMACrossOver : AlgorithmBase
    {
        List<PriceSnapshot> candles = new List<PriceSnapshot>();
        public EMACrossOver(string epic, ChartScale chartScale, List<PriceSnapshot> prerequisteCandles) : base(epic, chartScale)
        {
            candles.AddRange(prerequisteCandles);
        }

       


        protected override void AlgorithmStrategy(IgPublicApiData.ChartModel candle)
        {
            candles.Add(FileManager.ConvertStreamToPriceSnapshot(candle));



            //var candles = FileManager.GetCandles(algorithmEpic, algorithmChartScale.ToString()); // get candles currently downloaded 

            //candles.Add(inProgressCandle); // add the streamed candle thats just come through to our list of predownloaded candles

            BuyStrategy(candles);
            SellStrategy(candles);
        }

        public override void BuyStrategy(List<PriceSnapshot> candles, FileManager.TradeType tradeType = FileManager.TradeType.Bid)
        {
            var bidCandles = FileManager.ConvertDataSet(candles, TradeType.Bid); // get the BID prices

            // Check we have been above 100 CCI recently
            bool HaveLast5CandlesBeenCompletelyAboveEMA50 = QueryLibrary.HaveAllCandlesCompletelyBeenAboveEMA(bidCandles, 50, 4); // have the last 4 candles all been above 50 ema

            if (HaveLast5CandlesBeenCompletelyAboveEMA50)
            {
                Alert();
                if (AccountManager.GetOpenPositions().Result.Response.positions.Any(position => position.market.epic == algorithmEpic) == false)
                {
                    var marketDetails = AccountManager.GetMarketDetails(algorithmEpic).Result.Response;
                    var currentEMA50 = Indicator.GetEma(bidCandles, 50).Last().Ema;
                    OpenPosition("BUY", (decimal)0.5, (decimal)currentEMA50);
                }
            }
        }

        public override void SellStrategy(List<PriceSnapshot> candles, FileManager.TradeType tradeType = FileManager.TradeType.Ask)
        {
            throw new NotImplementedException();
        }


        public void ClosePosition()
        {
            var currentPositions = AccountManager.GetOpenPositions().Result.Response.positions;

            for (int i = 0; i < currentPositions.Count; i++)
            {
                if(currentPositions[i].market.epic == algorithmEpic)
                {

                    if(currentPositions[i].position.direction == "BUY")
                    {
                        var bidCandles = FileManager.ConvertDataSet(candles, TradeType.Bid); // get the BID prices

                        if(currentPositions[i].position.level < Indicator.GetEma(bidCandles, 50).Last().Ema)
                        {
                            AccountManager.ClosePosition(currentPositions[i]);

                        }
                    }

                }
            }
        }

        public void OpenPosition(string direction, decimal size, decimal stopLevel)
        {
            CreatePositionRequest orderRequest = new CreatePositionRequest();



            orderRequest.epic = algorithmEpic;
            orderRequest.expiry = "DFB";
            orderRequest.direction = direction;
            orderRequest.size = size;
            orderRequest.level = null;
            orderRequest.orderType = "MARKET";
            orderRequest.guaranteedStop = false;
            orderRequest.stopLevel = stopLevel;
            orderRequest.stopDistance = null;
            orderRequest.trailingStop = false;
            orderRequest.trailingStopIncrement = null;
            orderRequest.forceOpen = true;
            orderRequest.limitLevel = null;
            orderRequest.limitDistance = null;
            orderRequest.quoteId = null;
            orderRequest.currencyCode = "GBP";

            AccountManager.PlaceOrder(orderRequest);
            
        }
    }
}
