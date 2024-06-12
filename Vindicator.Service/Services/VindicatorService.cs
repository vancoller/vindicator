
using cAlgo.API;
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

        public VindicatorService(Robot _robot, VindicatorSettings _config)
        {
            _ = _robot;
            traders = new Dictionary<(string, TradeType), IRecoveryTrader>();
            config = _config;

            Configure();
            _.Symbol.Tick += OnTick;
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

        private void OnTick(SymbolTickEventArgs args)
        {
            //Remove any traders without work
            var tradesToRemove = traders.Where(x => !x.Value.Positions.Any()).ToList();
            foreach (var trade in tradesToRemove)
                traders.Remove(trade.Key);

            //Tick all traders
            foreach (var trader in traders)
            {
                trader.Value.OnTick();
            }
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