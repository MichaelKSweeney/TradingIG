using System;
using System.Threading;
using TradingAPI.IG.Algorithms;
using TradingAPI.IG.Algorithms.AlgorithmLibrary;
using TradingAPI.IG;
using IGWebApiClient;
using System.Collections.Generic;
using System.Linq;
using TradingAPI.IG.Utils;

namespace TradingAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            // logs you into both the streaming and the API system 
            AccountManager.Login();

            //CSVExporter.ExportTransactions(AccountManager.GetTransactions("ALL", DateTime.Now, DateTime.Now.AddDays(-7)).Result.Response.transactions);

            //delete existing candle data if its old or just wanna clean up a bit
            FileManager.DeleteData("IX.D.SPTRD.DAILY.IP", ChartScale.FiveMinute.ToString());
            FileManager.DeleteData("CS.D.USCGC.TODAY.IP", ChartScale.FiveMinute.ToString());

            // create an algorithm, future algorithms may have more constructor parameters at some point i guess
            var SandP500SimpleCCI = new SimpleCCI("IX.D.SPTRD.DAILY.IP", ChartScale.FiveMinute, AccountManager.GetMarketCandles("IX.D.SPTRD.DAILY.IP", "MINUTE_5", 50));
            var GoldSimpleCCI = new SimpleCCI("CS.D.USCGC.TODAY.IP", ChartScale.FiveMinute, AccountManager.GetMarketCandles("CS.D.USCGC.TODAY.IP", "MINUTE_5", 50));

            // download some prior candles for the indicators to use inside the algorithm
            //var SandP500PrerequisteCandles = AccountManager.GetMarketCandles(SandP500SimpleCCI.algorithmEpic, "MINUTE_5", 50);
            //var GoldPrerequisteCandles = AccountManager.GetMarketCandles(GoldSimpleCCI.algorithmEpic, "MINUTE_5", 50);

            // serialise the prerequite candles
            //FileManager.SerialiseCandles(SandP500PrerequisteCandles, "IX.D.SPTRD.DAILY.IP", "MINUTE_5");
            //FileManager.SerialiseCandles(GoldPrerequisteCandles, "CS.D.USCGC.TODAY.IP", "MINUTE_5");

            // setup a subscription to one minute candle but DONT serialise the 1 minute candles
            AccountManager.SubscribeToCandleStream(SandP500SimpleCCI.algorithmEpic, ChartScale.OneMinute, false);
            AccountManager.SubscribeToCandleStream(GoldSimpleCCI.algorithmEpic, ChartScale.OneMinute, false);

            // setup a subscription to five minute candle and DO serialise the 5 minute candles
            AccountManager.SubscribeToCandleStream(SandP500SimpleCCI.algorithmEpic, ChartScale.FiveMinute, false);
            AccountManager.SubscribeToCandleStream(GoldSimpleCCI.algorithmEpic, ChartScale.FiveMinute, false);

            // find our subscription to the US500 one minute candle stream
            var SandP500SubcriptionOneMinute = AccountManager.GetCandleStreamUpdateBinding(SandP500SimpleCCI.algorithmEpic, ChartScale.OneMinute);
            var GoldSubcriptionOneMinute = AccountManager.GetCandleStreamUpdateBinding(GoldSimpleCCI.algorithmEpic, ChartScale.OneMinute);

            var SandP500SubcriptionFiveMinute = AccountManager.GetCandleStreamUpdateBinding(SandP500SimpleCCI.algorithmEpic, ChartScale.FiveMinute);
            var GoldSubcriptionFiveMinute = AccountManager.GetCandleStreamUpdateBinding(GoldSimpleCCI.algorithmEpic, ChartScale.FiveMinute);

            // attach out algorithm's run method to the US500's OneMinute candle stream event
            SandP500SubcriptionOneMinute.CandleUpdate += SandP500SimpleCCI.Run;
            GoldSubcriptionOneMinute.CandleUpdate += GoldSimpleCCI.Run;

            SandP500SubcriptionFiveMinute.CandleUpdate += SandP500SimpleCCI.FiveMinuteCandle;
            GoldSubcriptionFiveMinute.CandleUpdate += GoldSimpleCCI.FiveMinuteCandle;
            // just to keep process from ending
            Thread.Sleep(int.MaxValue);
            
        }
    }
}
