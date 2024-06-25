using cAlgo.API;
using System.Linq;
using System.Diagnostics;
using Vindicator.Service.Models;
using System;
using Vindicator.Service.Services;
using Algolib.Shared;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class Vindicator : Robot
    {
        #region Parameters

        //BotLabel
        [Parameter("Recovery Bot Label", Group = "Settings", DefaultValue = "Vindicator")]
        public string RecoveryBotLabel { get; set; }

        [Parameter("Max spread allowed to open position", Group = "Settings", DefaultValue = 5.0)]
        public double MaxSpread { get; set; } = 5.0;


        [Parameter("Recovery Pips Grid", DefaultValue = "100", Group = "Recovery")]
        public int PipsBetweenTrades { get; set; }

        [Parameter("Recovery TP Money per 1k volume", DefaultValue = 1, Group = "Recovery")]
        public double TakeProfitMoneyPer1kVolume { get; set; }

        [Parameter("Volume Setting", DefaultValue = RecoveryVolumeSetting.Standard, Group = "Recovery")]
        public RecoveryVolumeSetting VolumeSetting { get; set; }

        [Parameter("Increase every x trade", DefaultValue = 10, Group = "Recovery")]
        public int IncreaseEveryXTrade { get; set; }


        [Parameter("Trend EMA Period", DefaultValue = 0, Group = "Recovery")]
        public int TrendEMAPeriod { get; set; }

        [Parameter("Generate Report", DefaultValue = false, Group = "Recovery")]
        public bool GenerateBacktestReport { get; set; }


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


        [Parameter("Using Testing Feature", Group = "Test", DefaultValue = false)]
        public bool IsTesting { get; set; }

        [Parameter("Test Bot Label", Group = "Test", DefaultValue = "Test Bot")]
        public string TestBotLabel { get; set; }

        [Parameter("Long Allowed", Group = "Test", DefaultValue = true)]
        public bool LongAllowed { get; set; }

        [Parameter("Short Allowed", Group = "Test", DefaultValue = true)]
        public bool ShortAllowed { get; set; }

        //[Parameter("Test Recovery Start Pips", DefaultValue = 50, Group = "Test")]
        //public int RecoveryStartPips { get; set; }

        [Parameter("Random Trade every X Hour", Group = "Test", DefaultValue = 0)]
        public int TestHour { get; set; }

        [Parameter("Testing Symbols", Group = "Test", DefaultValue =
            "AUDCAD, AUDNZD,AUDUSD,EURAUD,EURCAD,EURCHF,EURGBP,EURNZD,EURUSD,GBPAUD,GBPCAD,GBPCHF,GBPNZD,GBPUSD,NZDCAD,NZDUSD,USDCAD,USDCHF")]
            //"AUDCAD,AUDCHF,AUDNZD,AUDUSD,CADCHF,EURAUD,EURCAD,EURCHF,EURGBP,EURNZD,EURUSD,GBPAUD,GBPCAD,GBPCHF,GBPNZD,GBPUSD,NZDCAD,NZDCHF,NZDUSD,USDCAD,USDCHF")]
            //"AUDCAD,AUDCHF,AUDJPY,AUDNZD,AUDUSD,CADCHF,CADJPY,CHFJPY,EURAUD,EURCAD,EURCHF,EURGBP,EURJPY,EURNZD,EURUSD,GBPAUD,GBPCAD,GBPCHF,GBPJPY,GBPNZD,GBPUSD,NZDCAD,NZDCHF,NZDJPY,NZDUSD,USDCAD,USDCHF,USDJPY")]
        public string TestingSymbols { get; set; }

        [Parameter("Optimization Symbols", DefaultValue = "", Group = "Test")]
        public SymbolShortCode SymbolToTrade { get; set; }

        [Parameter("Debug", Group = "Debug", DefaultValue = false)]
        public bool IsDebug { get; set; }

        #endregion

        private IVindicatorService vindicatorService;
        private int algoWin;
        private int algoTrade;
        private string allCurrencies = "AUDCAD,AUDCHF,AUDJPY,AUDNZD,AUDUSD,CADCHF,CADJPY,CHFJPY,EURAUD,EURCAD,EURCHF,EURGBP,EURJPY,EURNZD,EURUSD,GBPAUD,GBPCAD,GBPCHF,GBPJPY,GBPNZD,GBPUSD,NZDCAD,NZDCHF,NZDJPY,NZDUSD,USDCAD,USDCHF,USDJPY";

        protected override void OnStart()
        {
            if (IsDebug)
                Debugger.Launch();

            //Symbols to trade
            if (string.IsNullOrEmpty(TestingSymbols))
                allCurrencies = Symbol.Name;
            else
                allCurrencies = TestingSymbols;

            if (SymbolToTrade != SymbolShortCode.None)
            {
                allCurrencies = SymbolToTrade.ToString();
            }

            vindicatorService = new VindicatorService(this, new VindicatorSettings
            {
                MaxSpread = MaxSpread,
                PipsBetweenTrades = PipsBetweenTrades,
                TakeProfitMoney = TakeProfitMoneyPer1kVolume,
                UseVolumePerOneK = UseVolumePerOneK,
                PerOneKVolume = PerOneKVolume,
                PerOneKEquity = PerOneKEquity,
                MaxFirstVolume = MaxFirstVolume,
                BotLabel = RecoveryBotLabel,
                IncreaseEveryXTrade = IncreaseEveryXTrade,
                VolumeSetting = VolumeSetting,
                TrendEMAPeriod = TrendEMAPeriod,
                GenerateBacktestReport = GenerateBacktestReport,
                Symbol = SymbolToTrade != SymbolShortCode.None ? SymbolToTrade.ToString() : String.Empty
            });
            Positions.Closed += OnPositionClosed;
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var recoveryPositionIds = vindicatorService.GetPositionsInRecovery(args.Position.Symbol.Name);
            if (!recoveryPositionIds.Contains(args.Position.Id))
            {
                algoWin++;
            }
        }

        protected override void OnBar()
        {
            if (IsTesting)
                TestOnBar();
        }

        int bar = 0;
        private void TestOnBar()
        {
            bar++;
            foreach (var s in allCurrencies.Split(','))
            {
                var symbol = s.Trim();
                var recoveryPositionIds = vindicatorService.GetPositionsInRecovery(symbol);
                var activePositions = Positions.Where(x => x.Symbol.Name == symbol && !recoveryPositionIds.Contains(x.Id));
                var recoveryPositions = Positions.Where(x => x.Symbol.Name == symbol && recoveryPositionIds.Contains(x.Id));

                //If any active position, start recover
                if (activePositions.Any())
                {
                    vindicatorService.RecoverTrades(activePositions, TestBotLabel);
                }

                if (!recoveryPositions.Any(x => x.TradeType == TradeType.Buy) && LongAllowed)
                {
                    var tradeResult = ExecuteMarketOrder(TradeType.Buy, symbol, CalculateEntryVolume(), TestBotLabel, null, null);
                    vindicatorService.RecoverTrades(new Position[] { tradeResult.Position }, TestBotLabel);
                }

                if (!recoveryPositions.Any(x => x.TradeType == TradeType.Sell) & ShortAllowed)
                {
                    var tradeResult = ExecuteMarketOrder(TradeType.Sell, symbol, CalculateEntryVolume(), TestBotLabel, null, null);
                    vindicatorService.RecoverTrades(new Position[] { tradeResult.Position }, TestBotLabel);
                }

                //Random trade
                if (bar == TestHour)
                {
                    bar = 0;

                    var a = ExecuteMarketOrder(TradeType.Buy, symbol, CalculateEntryVolume(), TestBotLabel, null, null);
                    //vindicatorService.RecoverTrade(a.Position, TestBotLabel);

                    var b = ExecuteMarketOrder(TradeType.Sell, symbol, CalculateEntryVolume(), TestBotLabel, null, null);
                    //vindicatorService.RecoverTrade(b.Position, TestBotLabel);
                }


                ////Close trades
                //foreach (var pos in symbolPositions)
                //{
                //    //Always recover
                //    vindicatorService.RecoverTrade(pos, TestBotLabel);

                //    //if (pos.NetProfit > 0)
                //    //    pos.Close();
                //}

                ////Recover trades
                //var posRecover = symbolPositions.Where(x => x.Pips < -RecoveryStartPips);
                //if (posRecover.Any())
                //{
                //    foreach (var pos in posRecover)
                //    {
                //        vindicatorService.RecoverTrade(pos, TestBotLabel);
                //    }
                //}

                ////Enter new trades
                //if (!symbolPositions.Any(x => x.TradeType == TradeType.Buy) && LongAllowed && bar == TestHour)
                //{
                //    ExecuteMarketOrder(TradeType.Buy, symbol, CalculateEntryVolume(), TestBotLabel, null, null);
                //    algoTrade++;
                //}

                //if (!symbolPositions.Any(x => x.TradeType == TradeType.Sell) && ShortAllowed && bar == TestHour)
                //{
                //    ExecuteMarketOrder(TradeType.Sell, symbol, CalculateEntryVolume(), TestBotLabel, null, 10);
                //    algoTrade++;
                //}
            }
        }

        protected override void OnStop()
        {
            if (IsBacktesting)
            {
                foreach (var pos in Positions)
                {
                    pos.Close();
                }
            }

            Print("-------------------------------------------------- ALGO STATS ----------------------------------------------------------");
            Print($"Win percentage  |  {((double)algoWin / (double)algoTrade).ToString("P")}");

            vindicatorService.Stop();
        }

        protected double CalculateEntryVolume()
        {
            if (!UseVolumePerOneK)
                return PerOneKVolume;

            //Using VolumePerOneK to calculate the volume, get the account balance and do the math
            var accountBalance = Account.Balance;
            var volume = (accountBalance * PerOneKVolume) / PerOneKEquity;
            volume = Math.Min(volume, MaxFirstVolume);
            volume = Symbol.NormalizeVolumeInUnits(volume);

            return volume;
        }

        protected override double GetFitness(GetFitnessArgs args)
        {
            return vindicatorService.GetFitness(args);
        }
    }
}