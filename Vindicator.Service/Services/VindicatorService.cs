
using cAlgo.API;
using cAlgo.API.Internals;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Vindicator.Service.Models;

namespace Vindicator.Service.Services
{
    public class VindicatorService : IVindicatorService
    {
        private Dictionary<(string, TradeType), IRecoveryTrader> traders;
        private readonly Robot _;
        private ServiceProvider serviceProvider;
        private VindicatorSettings config;
        private List<RecoveryTraderResults> results;

        public VindicatorService(Robot _robot, VindicatorSettings _config)
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
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IRecoveryTrader, RecoveryTrader>();
            services.AddSingleton<VindicatorSettings>(config);
            services.AddSingleton<Robot>(_);
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

        public void Stop()
        {
            FindInactiveTraders();

            _.Print("----------------------------------------------------------------------------------------------------------------------------");
            _.Print("--------------------------------------------------- RECOVERY STATISTICS ----------------------------------------------------");
            _.Print("----------------------------------------------------------------------------------------------------------------------------");
            int numberOfRecoveries = results.Count;
            _.Print($"Number of recoveries  |  {numberOfRecoveries}");

            var averageRecoveryDays = results.Select(x => x.TotalDays).Average();
            var averageRecoveryHours = averageRecoveryDays * 24;

            var maxDays = results.Select(x => x.TotalDays).Max();
            var maxHours = maxDays * 24;

            _.Print($"Average recovery time  |  {averageRecoveryDays.ToString("0.0")} days OR {averageRecoveryHours.ToString("0.0")} hours");
            _.Print($"Max recovery time  |  {maxDays.ToString("0.0")} days OR {maxHours.ToString("0.0")} hours");
            _.Print("----------------------------------------------------------------------------------------------------------------------------");

            //for int, start from 10 and increment by 10 to 100
            for (int i = 0; i < 100; i += 10)
            {
                var percentageStart = i / 100.0;
                var percentageEnd = (i + 10) / 100.0;
                var recoveries = results.Count(x => x.MaxDrawdownPercentage >= percentageStart && x.MaxDrawdownPercentage < percentageEnd);
                _.Print($"Drawdown between {percentageStart.ToString("0.0%")} and {percentageEnd.ToString("0.0%")}  |  {recoveries}");
            }
            _.Print("----------------------------------------------------------------------------------------------------------------------------");

        }

        public bool RecoverTrade(Position position, string botLabel)
        {
            var trader = GetTrader(position.Symbol.Name, position.TradeType);
            return trader.AddPosition(position, botLabel);
        }

        private IRecoveryTrader GetTrader(string symbol, TradeType tradeType)
        {
            if (!traders.ContainsKey((symbol, tradeType)))
            {
                var trader = serviceProvider.GetRequiredService<IRecoveryTrader>();
                trader.Configure(symbol, tradeType);

                traders.Add((symbol, tradeType), trader);
            }

            return traders[(symbol, tradeType)];
        }

        public IEnumerable<int> GetPositionsInRecovery(string botlabel, string symbol)
        {
            return traders.SelectMany(x => x.Value.Positions).Where(x => x.BotLabel == botlabel && x.Position.Symbol.Name == symbol).Select(x => x.Position.Id);
        }
    }
}