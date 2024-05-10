using dto.endpoint.accountactivity.transaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TradingAPI.IG.Utils
{
    public static class CSVExporter
    {
        public static void ExportTransactions(List<Transaction> transactions)
        {
            if (transactions.Count == 0) return;

            string csvdata = "";

            foreach (var field in typeof(Transaction).GetProperties().Select(f => f.Name).ToList())
            {

                csvdata += field;
                csvdata += ",";
            }

            csvdata = csvdata.Remove(csvdata.Length - 1);


            foreach (var transaction in transactions)
            {
                csvdata += System.Environment.NewLine;

                foreach (PropertyInfo property in typeof(Transaction).GetProperties())
                {
                    object propValue = property.GetValue(transaction, null);

                    var value = propValue.ToString();
                    csvdata += value;
                    csvdata += ",";
                }

                
                
            }
            csvdata = csvdata.Remove(csvdata.Length - 1);

            var path = $@"Exports\TransactionHistory\"+ DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");

            System.IO.Directory.CreateDirectory(@"Exports\TransactionHistory");
            File.WriteAllText(path, csvdata);
        }

        
    }
}
