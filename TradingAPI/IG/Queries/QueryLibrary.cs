using dto.endpoint.prices.v2;
using System;
using System.Collections.Generic;
using System.Text;
using Skender.Stock.Indicators;
using System.Linq;

namespace TradingAPI.IG.Queries
{
    public static class QueryLibrary
    {

        // This method will see if the CCI has gone above given value within the last given candle count - note will check from newest candles to oldest
        public static bool HasCCIGoneAboveValueWithinCandleCount(List<Quote> candles, int cciIndictorLookbackPeriod, decimal value, int lookbackCount)
        {

            var CCIvalues = Indicator.GetCci(candles, cciIndictorLookbackPeriod).ToArray();

            for (int i = candles.Count-1; i > CCIvalues.Count() - lookbackCount; i--)
            {
                if((decimal)CCIvalues[i].Cci > value)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasCCIGoneBelowValueWithinCandleCount(List<Quote> candles, int cciIndictorLookbackPeriod, decimal value, int lookbackCount)
        {

            var CCIvalues = Indicator.GetCci(candles, cciIndictorLookbackPeriod).ToArray();

            for (int i = candles.Count-1; i > CCIvalues.Count() - lookbackCount; i--)
            {
                if ((decimal)CCIvalues[i].Cci < value)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HaveAllCandlesClosedAboveEMA(List<Quote> candles, int EMAIndictorLookbackPeriod, int lookbackCount)
        {

            var EMAvalues = Indicator.GetEma(candles, EMAIndictorLookbackPeriod).ToArray();

            for (int i = candles.Count-1; i > EMAvalues.Count() - lookbackCount; i--)
            {
                if (candles[i].Close > EMAvalues[i].Ema)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // should not reach this point
            return false;
            
        }



        public static bool HaveAllCandlesClosedBelowEMA(List<Quote> candles, int EMAIndictorLookbackPeriod, int lookbackCount)
        {

            var EMAvalues = Indicator.GetEma(candles, EMAIndictorLookbackPeriod).ToArray();

            for (int i = candles.Count-1; i > EMAvalues.Count() - lookbackCount; i--)
            {


                if (candles[i].Close < EMAvalues[i].Ema)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // should not reach this point
            return false;

        }


        public static bool HaveAllCandlesCompletelyBeenAboveEMA(List<Quote> candles, int EMAIndictorLookbackPeriod, int lookbackCount)
        {

            var EMAvalues = Indicator.GetEma(candles, EMAIndictorLookbackPeriod).ToArray();

            for (int i = candles.Count - 1; i > EMAvalues.Count() - lookbackCount; i--)
            {
                if (candles[i].Close < EMAvalues[i].Ema || candles[i].Open < EMAvalues[i].Ema || candles[i].High < EMAvalues[i].Ema || candles[i].Low < EMAvalues[i].Ema)
                {
                    return false;
                }
                
            }

            return true;

        }

        public static bool HaveAllCandlesCompletelyBeenBelowEMA(List<Quote> candles, int EMAIndictorLookbackPeriod, int lookbackCount)
        {

            var EMAvalues = Indicator.GetEma(candles, EMAIndictorLookbackPeriod).ToArray();

            for (int i = candles.Count - 1; i > EMAvalues.Count() - lookbackCount; i--)
            {
                if (candles[i].Close > EMAvalues[i].Ema || candles[i].Open > EMAvalues[i].Ema || candles[i].High > EMAvalues[i].Ema || candles[i].Low > EMAvalues[i].Ema)
                {
                    return false;
                }

            }

            return true;

        }

    }
}
