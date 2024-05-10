using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using dto.endpoint.prices.v2;
using IGWebApiClient;
using Skender.Stock.Indicators;

namespace TradingAPI.IG
{
    public static class FileManager
    {




        public static readonly string CandleRoot = $@"{Directory.GetCurrentDirectory()}\CandleData";

        // Check if the candle folder for the market epic and time exists
        private static void EnsureFolderExists(string epicName, string timeResolution)
        {

            try
            {
                // ensure candle data root exists folder name exists
                if (!Directory.Exists($@"{CandleRoot}"))
                {
                    Directory.CreateDirectory($@"{CandleRoot}");
                }

                // ensure epic folder name exists
                if (!Directory.Exists($@"{CandleRoot}\{epicName}"))
                {
                    Directory.CreateDirectory($@"{CandleRoot}\{epicName}");
                }

                // ensure time resolution folder exists
                if (!Directory.Exists($@"{CandleRoot}\{epicName}\{timeResolution}"))
                {
                    Directory.CreateDirectory($@"{CandleRoot}\{epicName}\{timeResolution}");
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine($"Ensure Folder Exists Failed : {e}");
            }
        }


        /// <summary>
        /// Serialise Candle
        /// timeResolution (MINUTE, MINUTE_2, MINUTE_3, MINUTE_5, MINUTE_10, MINUTE_15, MINUTE_30, HOUR, HOUR_2, HOUR_3, HOUR_4, DAY, WEEK, MONTH)
        /// </summary>
        public static void SerialiseCandle(PriceSnapshot candle, string epicName, string timeResolution)
        {
            try
            {
                var time = ConvertTimeScaleToString(timeResolution.ToString());
                var serialiser = new XmlSerializer(typeof(PriceSnapshot));

                if (candle == null)
                {
                    return;
                }

                EnsureFolderExists(epicName, time);

                var path = $@"{CandleRoot}\{epicName}\{time}";

                var filePath = Path.Combine(path, candle.snapshotTime.Replace(":", "_").Replace(@"/", "-") + ".xml");

                using (var writer = new StreamWriter(filePath))
                {
                    serialiser.Serialize(writer, candle);
                }


            }
            catch (Exception e)
            {
                Debug.WriteLine($"Serialise Candle Failed : {e}");
            }
        }

        //resolution Price resolution(MINUTE, MINUTE_2, MINUTE_3, MINUTE_5, MINUTE_10, MINUTE_15, MINUTE_30, HOUR, HOUR_2, HOUR_3, HOUR_4, DAY, WEEK, MONTH
        //conversion for streaming candle enum back to API styled time frames
        private static string ConvertTimeScaleToString(string time)
        {
            switch (time)
            {
                case "OneMinute":
                    return "MINUTE";
                case "FiveMinute":
                    return "MINUTE_5";
                case "OneHour":
                    return "HOUR";
                default:
                    return time;
            }
        }

        public static void SerialiseCandle(PriceSnapshot candle, string epicName, ChartScale timeResolution)
        {
            try
            {

                var time = ConvertTimeScaleToString(timeResolution.ToString());


                var serialiser = new XmlSerializer(typeof(PriceSnapshot));

                if (candle == null)
                {
                    return;
                }

                EnsureFolderExists(epicName, time);

                var path = $@"{CandleRoot}\{epicName}\{time}";

                var filePath = Path.Combine(path, candle.snapshotTime.Replace(":", "_").Replace(@"/", "-") + ".xml");

                using (var writer = new StreamWriter(filePath))
                {
                    serialiser.Serialize(writer, candle);
                }


            }
            catch (Exception e)
            {
                Debug.WriteLine($"Serialise Candle Failed : {e}");
            }
        }

        public static void SerialiseCandles(List<PriceSnapshot> candles, string epicName, string timeResolution)
        {

            try
            {
                var time = ConvertTimeScaleToString(timeResolution.ToString());

                var serialiser = new XmlSerializer(typeof(PriceSnapshot));

                if (candles == null)
                {
                    return;
                }

                EnsureFolderExists(epicName, time);

                var path = $@"{CandleRoot}\{epicName}\{time}";

                for (int i = 0; i < candles.Count; i++)
                {

                    var filePath = Path.Combine(path, candles[i].snapshotTime.Replace(":", "_").Replace(@"/", "-") + ".xml");

                    using (var writer = new StreamWriter(filePath))
                    {
                        serialiser.Serialize(writer, candles[i]);
                    }
                }


            }
            catch (Exception e)
            {
                Debug.WriteLine($"Serialise Candles Failed : {e}");
            }
        }

        public static void DeleteData(string epicName, string timeResolution)
        {
            var time = ConvertTimeScaleToString(timeResolution.ToString());

            var path = $@"{CandleRoot}\{epicName}\{time}\";
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                Directory.Delete(path);
            }
        }

        public static List<PriceSnapshot> GetCandles(string epicName, string timeResolution)
        {
            try
            {
                var time = ConvertTimeScaleToString(timeResolution.ToString());
                var serialiser = new XmlSerializer(typeof(PriceSnapshot));

                EnsureFolderExists(epicName, time);

                var path = $@"{CandleRoot}\{epicName}\{time}\";

                DirectoryInfo dir = new DirectoryInfo(path);
                List<PriceSnapshot> candles = new List<PriceSnapshot>();

                var files = dir.GetFiles("*.xml");

                for (int i = 0; i < files.Length; i++)
                {
                    XmlSerializer ser = new XmlSerializer(typeof(PriceSnapshot));
                    using (XmlReader reader = XmlReader.Create(files[i].FullName))
                    {
                        PriceSnapshot MarketData = (PriceSnapshot)ser.Deserialize(reader);
                        candles.Add(MarketData);
                    }
                }
                return candles;


            }
            catch (Exception e)
            {
                Debug.WriteLine($"Get Candles Failed : {e}");
                return null;
            }


        }

        public static PriceSnapshot ConvertStreamToPriceSnapshot(RestAPI.Models.IgPublicApiData.ChartModel streamData)
        {
            PriceSnapshot quote = new PriceSnapshot();
            try
            {
                bool isNull = streamData.GetType().GetProperties()
                           .All(p => p.GetValue(streamData) == null);

                if (isNull)
                {

                }

                quote.closePrice = new Price();
                quote.closePrice.bid = (decimal)streamData.Bid.Close;
                quote.closePrice.ask = (decimal)streamData.Offer.Close;

                quote.openPrice = new Price();
                quote.openPrice.bid = (decimal)streamData.Bid.Open;
                quote.openPrice.ask = (decimal)streamData.Offer.Open;

                quote.highPrice = new Price();
                quote.highPrice.bid = (decimal)streamData.Bid.High;
                quote.highPrice.ask = (decimal)streamData.Offer.High;



                quote.lowPrice = new Price();
                quote.lowPrice.bid = (decimal)streamData.Bid.Low;
                quote.lowPrice.ask = (decimal)streamData.Offer.Low;


                quote.lastTradedVolume = (decimal)streamData.LastTradedVolume;

                var date = streamData.UpdateTime.ToString();
                //date = date.Substring(0, 10);

                string year = date.Substring(6, 4);
                string month = date.Substring(3, 2);
                string day = date.Substring(0, 2);
                string hour = (int.Parse(date.Substring(11, 2)) + 1).ToString();
                string minute = date.Substring(14, 2);
                string second = date.Substring(17, 2);


                date = $"{year}_{month}_{day}-{hour}_{minute}_{second}";
                quote.snapshotTime = date.ToString();


            }
            catch (Exception e)
            {

            }

            return quote;

        }


        public enum TradeType
        {
            Bid, // The bid price refers to the highest price a buyer will pay for a security.
            Ask // The ask price refers to the lowest price a seller will accept for a security.
        }
        public static List<Quote> ConvertDataSet(List<PriceSnapshot> prices, TradeType tradeType)
        {
            List<Quote> quotes = new List<Quote>();
            try
            {
                for (int i = 0; i < prices.Count; i++)
                {


                    try
                    {
                        Quote quote = new Quote();


                        quote.Close = (decimal)(tradeType == TradeType.Bid ? prices[i].closePrice.bid : prices[i].closePrice.ask);
                        quote.Open = (decimal)(tradeType == TradeType.Bid ? prices[i].openPrice.bid : prices[i].openPrice.ask);
                        quote.High = (decimal)(tradeType == TradeType.Bid ? prices[i].highPrice.bid : prices[i].highPrice.ask);
                        quote.Low = (decimal)(tradeType == TradeType.Bid ? prices[i].lowPrice.bid : prices[i].lowPrice.ask);
                        quote.Volume = (decimal)prices[i].lastTradedVolume;

                        var date = prices[i].snapshotTime;
                        date = date.Substring(0, 10);
                        var time = prices[i].snapshotTime;
                        time = time.Substring(11, 8);
                        if (i == 209)
                        {

                        }
                        DateTime mydate = DateTime.Parse(date.Replace(":", "-").Replace("_", "-"));
                        DateTime mytime = DateTime.Parse(time.Replace("_", ":"));
                        mydate = mydate.AddHours(mytime.Hour);
                        mydate = mydate.AddMilliseconds(mytime.Minute);
                        mydate = mydate.AddSeconds(mytime.Second);

                        quote.Date = mydate;
                        quotes.Add(quote);
                    }
                    catch (Exception e)

                    {
                        Debug.WriteLine(e.ToString());
                    }



                }
            }
            catch (Exception e)
            {

            }

            return quotes;
        }
    }
}
