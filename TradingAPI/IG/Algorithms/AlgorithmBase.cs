using dto.endpoint.prices.v2;
using IGWebApiClient;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static TradingAPI.IG.FileManager;

namespace TradingAPI.IG.Algorithms
{
    abstract public class AlgorithmBase
    {
        

        protected abstract void AlgorithmStrategy(RestAPI.Models.IgPublicApiData.ChartModel candle);

        public readonly ChartScale algorithmChartScale;
        public readonly string algorithmEpic;

        protected AlgorithmBase(string epic, ChartScale chartScale)
        {
            algorithmChartScale = chartScale;
            algorithmEpic = epic;
        }

        public void Run(RestAPI.Models.IgPublicApiData.ChartModel candle)
        {
            AlgorithmStrategy(candle);
        }

        public void Alert()
        {
            Flash();
            Console.Beep(1000, 1000);
        }

        public abstract void BuyStrategy(List<PriceSnapshot> candles, TradeType tradeType = TradeType.Bid);


        public abstract void SellStrategy(List<PriceSnapshot> candles, TradeType tradeType = TradeType.Ask);
        
        


       

        //Alert 

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        public const UInt32 FLASHW_ALL = 3;

        // documentation console flash
        //https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-flashwinfo

        public void Flash()
        {
            FLASHWINFO fInfo = new FLASHWINFO();
            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = GetConsoleWindow();
            fInfo.dwFlags = FLASHW_ALL;
            fInfo.uCount = 60; // The number of times to flash the window.
            fInfo.dwTimeout = 0;// The rate at which the window is to be flashed, in milliseconds. If dwTimeout is zero, the function uses the default cursor blink rate.

            FlashWindowEx(ref fInfo);
        }
    }
}
