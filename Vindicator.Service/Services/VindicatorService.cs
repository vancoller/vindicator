
using Algolib.Shared.Interfaces;
using cAlgo.API;
using cAlgo.API.Internals;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Vindicator.Service.Models;
using Vindicator.Service.Reporting;
using Vindicator.Service.Services.Receiver;
using Vindicator.Service.Services.Trader;

namespace Vindicator.Service.Services
{
    public class VindicatorService : IVindicatorService
    {
        private IVindicatorReceiver receiver;
        private Dictionary<(string, TradeType), IRecoveryTrader> traders;
        private readonly IBaseRobot _;
        private ServiceProvider serviceProvider;
        private VindicatorSettings config;
        private List<RecoveryTraderResults> results;
        private int traderIndex = 0;
        private double lastClose;

        public VindicatorService(IBaseRobot _robot, VindicatorSettings _config)
        {
            _ = _robot;
            traders = new Dictionary<(string, TradeType), IRecoveryTrader>();
            results = new List<RecoveryTraderResults>();
            config = _config;

            Configure();
            _.Symbol.Tick += OnTick;

            var oneMinBars = _.MarketData.GetBars(TimeFrame.Minute, _.Symbol.Name);
            oneMinBars.BarClosed += OnOneMinBarClosed;
        }

        private void Configure()
        {
            // Setup DI container
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            serviceProvider = serviceCollection.BuildServiceProvider();

            // Setup VindicatorReceiver
            receiver = serviceProvider.GetRequiredService<IVindicatorReceiver>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IVindicatorService>(this);
            services.AddSingleton(_);
            services.AddTransient<IRecoveryTrader, RecoveryTrader>();
            services.AddSingleton<IVindicatorReceiver, VindicatorReceiver>();
            services.AddSingleton(config);
            services.AddSingleton<IBaseRobot>(_);
        }

        private void OnOneMinBarClosed(BarClosedEventArgs args)
        {
            foreach (var trader in traders)
            {
                trader.Value.OnOneMinBarClosed();
            }
        }

        private void OnTick(SymbolTickEventArgs args)
        {
            FindInactiveTraders();

            //Tick all traders
            foreach (var trader in traders)
            {
                trader.Value.OnTick();
            }

            //Onbar
            var lastBarPrice = _.Bars.ClosePrices.Last(1);
            if (lastBarPrice != lastClose)
            {
                //Bar changed
                lastClose = lastBarPrice;

                foreach (var trader in traders)
                {
                    trader.Value.OnBar();
                }
            }
        }

        private void FindInactiveTraders()
        {
            //Remove any traders without work
            var tradesToRemove = traders.Where(x => !x.Value.Positions.Any()).ToList();
            foreach (var trader in tradesToRemove)
            {
                results.Add(trader.Value.GetResults());
                traders.Remove(trader.Key);
            }
        }

        public double GetFitness(GetFitnessArgs args)
        {
            var averageRecoveryDays = results.Select(x => x.TotalDays).Average();
            var maxDays = results.Select(x => x.TotalDays).Max();

            //if (maxDays > 200)
            //return 0;

            return 1000 - averageRecoveryDays;
        }

        public bool RecoverTrades(IEnumerable<Position> positions, string botLabel)
        {
            var trader = GetTrader(positions.First().Symbol.Name, positions.First().TradeType);
            return trader.AddPositions(positions, botLabel, true);
        }

        public bool AddTradesToRecovery(IEnumerable<Position> positions, string botLabel)
        {
            var trader = GetTrader(positions.First().Symbol.Name, positions.First().TradeType);
            return trader.AddPositions(positions, botLabel, false);
        }

        private IRecoveryTrader GetTrader(string symbol, TradeType tradeType)
        {
            if (!traders.ContainsKey((symbol, tradeType)))
            {
                var trader = serviceProvider.GetRequiredService<IRecoveryTrader>();
                trader.Configure(symbol, tradeType, traderIndex);

                traders.Add((symbol, tradeType), trader);
                traderIndex++;
            }

            return traders[(symbol, tradeType)];
        }

        public IEnumerable<int> GetPositionsInRecovery(string symbol)
        {
            return traders.SelectMany(x => x.Value.Positions).Where(x => x.Position.Symbol.Name == symbol).Select(x => x.Position.Id);
        }

        public void UpdatePipsBetweenTrades(int pipsBetweenTrades)
        {
            foreach (var trader in traders)
            {
                trader.Value.UpdatePipsBetweenTrades(pipsBetweenTrades);
            }
        }

        public void Stop()
        {
            try
            {
                //STOP ALL TRADERS
                var tradesToRemove = traders.Where(x => !x.Value.Positions.Any()).ToList();
                foreach (var trader in traders)
                {
                    results.Add(trader.Value.GetResults());
                    traders.Remove(trader.Key);
                }

                //REPORT
                if (config.GenerateBacktestReport)
                {
                    var report = new BacktestReport(_, results, traders, config);
                    report.GenerateReport();
                }

                var startDate = _.History.First().EntryTime;
                var endDate = _.History.Last().ClosingTime; 

                FindInactiveTraders();

                _.Print("--------------------------------------------------- RECOVERY STATISTICS ----------------------------------------------------");
                var numberOfRecoveries = results.Count;
                var years = (endDate - startDate).TotalDays / 365;
                var averageRecoveriesPerYear = numberOfRecoveries / years;
                var averageRecoveriesPerMonth = averageRecoveriesPerYear / 12;

                _.Print($"Number of recoveries  |  {numberOfRecoveries}  |  Per Year: {averageRecoveriesPerYear.ToString("0.0")}  |  Per Month: {averageRecoveriesPerMonth.ToString("0.0")} ");

                var averageRecoveryDays = results.Select(x => x.TotalDays).Average();
                var averageRecoveryHours = averageRecoveryDays * 24;

                var maxDays = results.Select(x => x.TotalDays).Max();
                var maxHours = maxDays * 24;

                _.Print($"Average recovery time  |  {averageRecoveryDays.ToString("0.0")} days OR {averageRecoveryHours.ToString("0.0")} hours");
                _.Print($"Max recovery time  |  {maxDays.ToString("0.0")} days OR {maxHours.ToString("0.0")} hours");
                _.Print("----------------------------------------------------------------------------------------------------------------------------");

                //for int, start from 10 and increment by 10 to 100
                var percentageStart = 0;
                var percentageEnd = 5;
                var recoveries = results.Count(x => x.MaxDrawdownPercentage >= percentageStart && x.MaxDrawdownPercentage < percentageEnd);
                _.Print($"Drawdown between {percentageStart}% and {percentageEnd}%  |  {recoveries}");

                for (int i = 5; i < 100; i += 10)
                {
                    percentageStart = i;
                    percentageEnd = i == 5 ? i + 5 : i + 10;
                    recoveries = results.Count(x => x.MaxDrawdownPercentage >= percentageStart && x.MaxDrawdownPercentage < percentageEnd);
                    _.Print($"Drawdown between {percentageStart}% and {percentageEnd}%  |  {recoveries}");
                }

                _.Print("----------------------------------------------------------------------------------------------------------------------------");
                _.Print("BASKET STATS");
                _.Print("----------------------------------------------------------------------------------------------------------------------------");

                var resultsGroupedBySymbol = results.GroupBy(x => x.Symbol);
                foreach (var group in resultsGroupedBySymbol)
                {
                    var symbol = group.Key;
                    var symbolResults = group.ToList();
                    var count = symbolResults.Count(); ;

                    var averageDrawdown = symbolResults.Select(x => x.MaxDrawdownPercentage).Average();
                    var maxDrawdown = symbolResults.Select(x => x.MaxDrawdownPercentage).Max();

                    _.Print($"Symbol: {symbol}  |  {count} Recoveries  |  Avg Drawdown: {averageDrawdown.ToString("0.0")}%  |  Max Drawdown: {maxDrawdown.ToString("0.0")}%");
                }
                _.Print("----------------------------------------------------------------------------------------------------------------------------");
                _.Print("OPEN BASKETS");
                _.Print("----------------------------------------------------------------------------------------------------------------------------");

                //Open traders
                foreach (var trader in traders.OrderBy(x => x.Value.Positions.Sum(x => x.Position.NetProfit)))
                {
                    var sum = trader.Value.Positions.Sum(x => x.Position.NetProfit).ToString("0.00");
                    var count = trader.Value.Positions.Count;
                    var symbol = trader.Value.Positions.First().Position.Symbol.Name;

                    _.Print($"Symbol: {symbol}  |  {count} Trades  |  Total Profit: {sum}");
                }
            }
            catch (Exception ex)
            {
                _.Print("Error was thrown writing log file");
            }
        }

        public int GetNumberOfRecoveries(string symbolName)
        {
            return results.Where(x => x.Symbol == symbolName).Count();
        }
    }
}