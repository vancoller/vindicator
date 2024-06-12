using cAlgo.API;
using System.Linq;
using System.Diagnostics;
using Vindicator.Service.Services;
using Vindicator.Service.Models;
using System;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class Vindicator : Robot
    {
        //BotLabel
        [Parameter("Bot Label", Group = "Settings", DefaultValue = "Vindicator")]
        public string BotLabel { get; set; }

        [Parameter("Max spread allowed to open position", Group = "Settings", DefaultValue = 5.0)]
        public double MaxSpread { get; set; } = 5.0;



        [Parameter("Recovery Pips Grid", DefaultValue = "100", Group = "Recovery")]
        public int PipsBetweenTrades { get; set; }

        [Parameter("Recovery TP Money per 1k volume", DefaultValue = 1, Group = "Recovery")]
        public int TakeProfitMoneyPer1kVolume { get; set; }

        [Parameter("Increase volume every x trade", DefaultValue = 10, Group = "Recovery")]
        public int IncreaseVolumeEveryXTrade { get; set; }


        /// <summary>
        /// VOLUME
        /// </summary>
        [Parameter("X Volume per Equity?", Group = "Money Management", DefaultValue = true)]
        public bool UseVolumePerOneK { get; set; }

        [Parameter("Volume", Group = "Money Management", DefaultValue = 1000, MinValue = 0.01, MaxValue = 1000000)]
        public double PerOneKVolume { get; set; }

        [Parameter("Equity", Group = "Money Management", DefaultValue = 5000, MinValue = 10, MaxValue = 1000000)]
        public double PerOneKEquity { get; set; }

        [Parameter("Max first volume", Group = "Money Management", DefaultValue = 100000, MinValue = 1000, Step = 1000)]
        public double MaxFirstVolume { get; set; }


        [Parameter("Test", Group = "Debug", DefaultValue = false)]
        public bool IsTesting { get; set; }

        [Parameter("Testing Seed", Group = "Debug", DefaultValue = 1)]
        public int TestingSeed { get; set; }

        [Parameter("Debug", Group = "Debug", DefaultValue = false)]
        public bool IsDebug { get; set; }

        private IVindicatorService vindicatorService;
        private Random random;

        protected override void OnStart()
        {
            if (IsDebug)
                Debugger.Launch();

            vindicatorService = new VindicatorService(this, new VindicatorSettings
            {
                MaxSpread = MaxSpread,
                PipsBetweenTrades = PipsBetweenTrades,
                TakeProfitMoney = TakeProfitMoneyPer1kVolume,
                UseVolumePerOneK = UseVolumePerOneK,PerOneKVolume = PerOneKVolume,
                PerOneKEquity = PerOneKEquity,
                MaxFirstVolume = MaxFirstVolume,
                BotLabel = BotLabel,
                IncreaseVolumeEveryXTrade = IncreaseVolumeEveryXTrade
            });

            if (IsTesting)
            {
                random = new Random(TestingSeed);
            }
        }

        protected override void OnBar()
        {
            if (IsTesting)
                TestOnBar();
        }

        private void TestOnBar()
        {
            var recoveryPositionIds = vindicatorService.GetPositionsInRecovery(BotLabel, Symbol.Name);
            var positions = Positions.Where(x => !recoveryPositionIds.Contains(x.Id));

            //Close trades
            foreach (var pos in positions)
            {
                if (pos.NetProfit > 0)
                    pos.Close();
            }

            //Recover trades
            var posRecover = positions.Where(x => x.Pips < -50);
            if (posRecover.Any())
            {
                foreach (var pos in posRecover)
                {
                    vindicatorService.RecoverTrade(pos, BotLabel);
                }
            }

            //Enter new trades
            if (!Positions.Any(x => x.TradeType == TradeType.Buy) && random.Next(1, 4) == 1)
            {
                ExecuteMarketOrder(TradeType.Buy, Symbol.Name, 1000, "Random Bot", null, null);
            }
            else if (!Positions.Any(x => x.TradeType == TradeType.Sell) && random.Next(1, 4) == 1)
            {
                ExecuteMarketOrder(TradeType.Sell, Symbol.Name, 1000, "Sell", null, 10);
            }
        }

        protected override void OnStop()
        {
            if (IsBacktesting)
            {
                foreach(var pos in Positions)
                {
                    pos.Close();
                }
            }

            vindicatorService.Stop();
        }
    }
}
