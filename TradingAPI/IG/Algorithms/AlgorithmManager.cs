using IGWebApiClient;
using System.Collections.Generic;


namespace TradingAPI.IG.Algorithms
{

    // NOTE - this class is no longer used but im keeping it for future use incase we want to have some over-arching algorithm manager
    public static class AlgorithmManager
    {
        private static List<AlgorithmBase> ActiveAlgorithms = new List<AlgorithmBase>();

        public static void AttachAlgorithmToCandleStream(AlgorithmBase algorithm, string epicName, ChartScale timeScale) 
        {
            List<string> epic = new List<string>();
            epic.Add(epicName);
            
            ActiveAlgorithms.Add(algorithm);
            //AccountManager.SubscribeToCandleStream(epic, timeScale, algorithm, true);
            
        }

        public static void DettachAlgorithmToCandleStream<T>(AlgorithmBase algorithm, string epicName, ChartScale timeScale) where T : AlgorithmBase, new()
        {
            List<string> epic = new List<string>();
            epic.Add(epicName);
            
            ActiveAlgorithms.Add(algorithm);
            //AccountManager.SubscribeToCandleStream(epic, timeScale, algorithm, true);

        }


    }
}
