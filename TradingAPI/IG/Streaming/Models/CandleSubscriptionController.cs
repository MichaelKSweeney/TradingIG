using IGWebApiClient;
using Lightstreamer.DotNet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TradingAPI.IG.RestAPI.Models;

namespace TradingAPI.IG.Streaming.Models
{
    public class CandleSubscriptionController
    {

        public SubscribedTableKey marketSubscriptionStk;
        public ChartCandleTableListerner ActiveCandleSubscription = new ChartCandleTableListerner();

        public delegate void ChartUpdateUpdate(IgPublicApiData.ChartModel candle);
        public ChartUpdateUpdate CandleUpdate;
    
        IgPublicApiData.ChartModel candleData = new IgPublicApiData.ChartModel();

        public bool serialiseIncomingCandles;
        public ChartScale candleTimeFrame;
        public string chartEpic;

        public CandleSubscriptionController(string epic, ChartScale timeframe, bool serialiseStreamingCandles)
        {
            chartEpic = epic;
            candleTimeFrame = timeframe;
            serialiseIncomingCandles = serialiseStreamingCandles;
            ActiveCandleSubscription.Update += OnChartCandleDataUpdate;
        }

        public void OnChartCandleDataUpdate(object sender, UpdateArgs<ChartCandelData> e)
        {
            var candleUpdate = e.UpdateData;
            
            //Console.WriteLine($": bid {candleUpdate.Bid.Open}");
            bool isNull = candleUpdate.GetType().GetProperties()
                            .Any(p => p.GetValue(candleUpdate) == null);

            if (isNull)
            {

            }

            var tempEpic = e.ItemName.Replace("CHART:", "");
            var tempArray = tempEpic.Split(':');
            var epic = tempArray[0];
            var time = tempArray[1];




            candleData.ChartEpic = chartEpic;
            candleData.Bid = new IgPublicApiData.ChartHlocModel();
            candleData.Bid.Close = candleUpdate.Bid.Close;
            candleData.Bid.High = candleUpdate.Bid.High;
            candleData.Bid.Low = candleUpdate.Bid.Low;
            candleData.Bid.Open = candleUpdate.Bid.Open;

            candleData.DayChange = candleUpdate.DayChange;
            candleData.DayChangePct = candleUpdate.DayChangePct;
            candleData.DayHigh = candleUpdate.DayHigh;
            candleData.DayLow = candleUpdate.DayLow;
            candleData.DayMidOpenPrice = candleUpdate.DayMidOpenPrice;
            candleData.EndOfConsolidation = candleUpdate.EndOfConsolidation;
            candleData.IncrimetalTradingVolume = candleUpdate.IncrimetalTradingVolume;

            if (candleUpdate.LastTradedVolume != null)
            {
                candleData.LastTradedVolume = candleUpdate.LastTradedVolume;
                candleData.LastTradedPrice = new IgPublicApiData.ChartHlocModel();
                candleData.LastTradedPrice.Close = candleUpdate.LastTradedPrice.Close;
                candleData.LastTradedPrice.High = candleUpdate.LastTradedPrice.High;
                candleData.LastTradedPrice.Low = candleUpdate.LastTradedPrice.Low;
                candleData.LastTradedPrice.Open = candleUpdate.LastTradedPrice.Open;
            }

            candleData.Offer = new IgPublicApiData.ChartHlocModel();
            candleData.Offer.Close = candleUpdate.Offer.Close;
            candleData.Offer.Open = candleUpdate.Offer.Open;
            candleData.Offer.High = candleUpdate.Offer.High;
            candleData.Offer.Low = candleUpdate.Offer.Low;

            candleData.TickCount = candleUpdate.TickCount;
            candleData.UpdateTime = candleUpdate.UpdateTime;

            if (candleData.EndOfConsolidation != null && (bool)candleUpdate.EndOfConsolidation)
            {
                if(serialiseIncomingCandles)
                {
                    FileManager.SerialiseCandle(FileManager.ConvertStreamToPriceSnapshot(candleData), chartEpic, candleTimeFrame.ToString());
                }
                CandleUpdate?.Invoke(candleData);
            }
            
        }
    }
}
