using cAlgo.API;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Vindicator.Service.Models;
using Vindicator.Service.Services.Trader;

namespace Vindicator.Service.Reporting
{
    public class BacktestReport
    {
        private Robot robot;
        private List<RecoveryTraderResults> results;
        private VindicatorSettings config;
        private ReportData data;
        private Dictionary<(string, TradeType), IRecoveryTrader> traders;



        public BacktestReport(Robot _robot, List<RecoveryTraderResults> _results, Dictionary<(string, TradeType), IRecoveryTrader> _traders, VindicatorSettings _config)
        {
            robot = _robot;
            results = _results;
            config = _config;
            traders = _traders;
            data = new ReportData();
        }

        public void GenerateReport()
        {
            var firsResults = results.FirstOrDefault();
            if (firsResults == null)
                return;

            var symbol = firsResults.Symbol;
            config.Symbol = symbol;

            data.Balance = robot.Account.Balance;
            data.Equity = robot.Account.Equity;
            data.TimeFrame = robot.TimeFrame.ToString();
            data.StartDate = results.Min(r => r.StartDate).ToShortDateString();
            data.EndDate = results.Max(r => r.EndDate).ToShortDateString();
            data.Recoveries = results.ToArray();
            data.Config = config;


            SaveReportToMongoDB();
        }

        private void SaveReportToMongoDB()
        {
            // Connection string to MongoDB
            //var connectionString = "mongodb+srv://admin:Jkuo8501.M@cluster0.plzfekr.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0\r\n\r\n";
            var connectionString = "mongodb://localhost:27017";
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("backtests");
            var collection = database.GetCollection<BsonDocument>("vindicator");

            //Insert data into MongoDB
            var jsonData = JsonSerializer.Serialize(data);
            var document = BsonDocument.Parse(jsonData);
            collection.InsertOne(document);

            robot.Print("Backtest report saved to MongoDB");
        }

        private void SaveReportToJson()
        {
            var json = JsonSerializer.Serialize(data);
            var randomGuid = Guid.NewGuid().ToString();
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Vindicator", $"Vindicator_Backtest_Report_{randomGuid}.json");
            System.IO.File.WriteAllText(path, json);

            robot.Print("Backtest report saved to JSON");
        }
    }
}
