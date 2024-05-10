using dto.endpoint.prices.v2;
using IGWebApiClient;
using System;
using System.Collections.Generic;
using System.Text;
using static TradingAPI.IG.FileManager;

namespace TradingAPI.IG.Algorithms.AlgorithmLibrary
{
    class TestAlgorithm :  AlgorithmBase 
    {
        TestAlgorithm(string epic, ChartScale chartScale) : base(epic, chartScale)
        {

        }
       

        protected override void AlgorithmStrategy(RestAPI.Models.IgPublicApiData.ChartModel candle)
        {
            Console.WriteLine($"{candle.ChartEpic}: {candle.Bid.Open}");
        }

        public override void BuyStrategy(List<PriceSnapshot> candles, TradeType tradeType = TradeType.Bid)
        {

        }

        public override void SellStrategy(List<PriceSnapshot> candles, TradeType tradeType = TradeType.Bid)
        {

        }

    }
}
