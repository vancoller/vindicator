using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using Vindicator.Service.Models;

namespace Vindicator.Service.Services
{
    public class RecoveryTrader : IRecoveryTrader
    {
        public List<RecoveryPosition> Positions { get; }
        public IEnumerable<RecoveryPosition> PositionsOpenedHere
        {
            get
            {
                return Positions.Where(x => x.BotLabel == config.BotLabel);
            }
        }

        public List<PendingTrade> PendingTrades { get; set; }
        private Symbol Symbol { get; set; }
        private string symbol;
        private TradeType tradeType;
        private RecoveryTraderResults results;

        private readonly VindicatorSettings config;
        private readonly Robot robot;

        private ExponentialMovingAverage emaTrend;

        public RecoveryTrader(VindicatorSettings _config, Robot _robot)
        {
            config = _config;
            robot = _robot;
            Positions = new List<RecoveryPosition>();
            PendingTrades = new List<PendingTrade>();
            results = new RecoveryTraderResults();
            emaTrend = robot.Indicators.ExponentialMovingAverage(robot.Bars.ClosePrices, 50);
        }

        public void OnOneMinBarClosed()
        {
            if (!PendingTrades.Any())
                ProcessRecovery();
        }

        public void Configure(string _symbol, TradeType _tradeType)
        {
            symbol = _symbol;
            tradeType = _tradeType;
            Symbol = robot.Symbols.GetSymbol(symbol);
        }

        public RecoveryTraderResults GetResults()
        {
            return results;
        }

        public bool AddPosition(Position position, string botLabel)
        {
            //First entry
            if (!Positions.Any())
            {
                results.StartDate = robot.Time;
            }

            AddRecoveryPosition(position, botLabel);
            CreateNewRecoveryTrade();
            return true;
        }

        public void OnTick()
        {
            if (!Positions.Any())
                return;

            UpdateStats();

            //CheckTakeProfit();
            CheckIfTradesAreClosed();

            if (PendingTrades.Any())
                CheckPendingTrades();
            //else
                //ProcessRecovery();
        }

        private void CheckIfTradesAreClosed()
        {
            var tradesToRemove = new List<RecoveryPosition>();
            foreach (var position in Positions)
            {
                if (robot.Positions.FirstOrDefault(x => x.Id == position.Position.Id) == null)
                {
                    tradesToRemove.Add(position);
                }
            }

            foreach (var trade in tradesToRemove)
            {
                Positions.Remove(trade);
            }

            //End
            results.EndDate = robot.Time;
        }

        //private void CheckTakeProfit()
        //{
        //    if (tradeType == TradeType.Buy)
        //    {
        //        if (Symbol.Bid >= takeProfitPrice)
        //        {
        //            CloseAllPositions();
        //        }
        //    }
        //    else
        //    {
        //        if (Symbol.Ask <= takeProfitPrice)
        //        {
        //            CloseAllPositions();
        //        }
        //    }
        //}

        private void UpdateTakeProfit()
        {
            var takeProfitPrice = CalculateBreakEvenPrice(tradeType);

            foreach (var position in Positions.OrderBy(x => x.Position.EntryPrice))
            {
                robot.ModifyPosition(position.Position, null, takeProfitPrice);
            }

            //if (tradeType == TradeType.Buy)
            //    takeProfitPrice += (config.TakeProfitPips * Symbol.PipSize);
            //else
            //    takeProfitPrice -= (config.TakeProfitPips * Symbol.PipSize);
        }

        private double CalculateBreakEvenPrice(TradeType tradeType)
        {
            var totalVolume = 0.0;
            var totalVolumePrice = 0.0;
            var fees = 0.0;

            foreach (var position in Positions)
            {
                totalVolume += position.Position.VolumeInUnits;
                totalVolumePrice += position.Position.EntryPrice * position.Position.VolumeInUnits;
                fees += Math.Abs(position.Position.Commissions * 2) + Math.Abs(position.Position.Swap);
            }

            //Add $$ to make profit
            fees += config.TakeProfitMoney * (totalVolume / 1000);

            var withoutFees = totalVolumePrice / totalVolume;
            var valuePerPip = Symbol.PipValue * totalVolume;
            var pipsRequiredToCoverFees = fees / valuePerPip;

            if (tradeType == TradeType.Buy)
                return withoutFees + (pipsRequiredToCoverFees * Symbol.PipSize);
            else
                return withoutFees - (pipsRequiredToCoverFees * Symbol.PipSize);
        }

        private void ProcessRecovery()
        {
            if (!Positions.Any() || PendingTrades.Any())
                return;

            //Filters -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
            if (emaTrend.Result.LastValue > Symbol.Bid && tradeType == TradeType.Buy)
                return;

            if (emaTrend.Result.LastValue < Symbol.Ask && tradeType == TradeType.Sell)
                return;

            //--------------------------------------------

           double pipsBetweenTrades = 0;
            if (tradeType == TradeType.Buy)
            {
                var lastPrice = Positions.Min(x => x.Position.EntryPrice);
                var currentPrice = Symbol.Bid;
                pipsBetweenTrades = (currentPrice - lastPrice) / Symbol.PipSize;
            }
            else
            {
                var lastPrice = Positions.Max(x => x.Position.EntryPrice);
                var currentPrice = Symbol.Ask;
                pipsBetweenTrades = (lastPrice - currentPrice) / Symbol.PipSize;
            }

            if (pipsBetweenTrades < -config.PipsBetweenTrades)
            {
                CreateNewRecoveryTrade();
            }
        }

        private void CreateNewRecoveryTrade()
        {
            var volume = CalculateEntryVolume();
            volume = AdjustVolume(volume);
            AddPendingTrade(new PendingTrade(Symbol.Name, volume, tradeType, config.BotLabel, null, null, null, null, "calculated_recovery"));
        }

        private double AdjustVolume(double volume)
        {
            return volume * (PositionsOpenedHere.Count() / config.IncreaseVolumeEveryXTrade + 1);
        }

        private void AddPendingTrade(PendingTrade trade)
        {
            PendingTrades.Add(trade);
        }

        private bool CheckPendingTrades()
        {
            double tpPips = 0;
            bool hasNewTrades = false;

            if (!IsSpreadOK())
                return hasNewTrades;

            var tradeToRemove = new List<PendingTrade>();
            foreach (var trade in PendingTrades)
            {
                var result = robot.ExecuteMarketOrder(trade.TradeType, trade.Symbol, trade.Volume, trade.Label, trade.StopLoss, tpPips, trade.Comment);
                if (result.IsSuccessful)
                {
                    tradeToRemove.Add(trade);
                    hasNewTrades = true;

                    AddRecoveryPosition(result.Position, trade.Label);
                }
            }

            //Remove positions
            foreach (var trade in tradeToRemove)
                PendingTrades.Remove(trade);

            return hasNewTrades;
        }

        private void AddRecoveryPosition(Position position, string label)
        {
            //Update position
            robot.ModifyPosition(position, 0, 0);

            //Add to recovery trade
            Positions.Add(new RecoveryPosition
            {
                Position = position,
                BotLabel = label
            });

            UpdateTakeProfit();
        }

        protected bool IsSpreadOK()
        {
            var spreadInPips = Symbol.Spread / Symbol.PipSize;
            if (spreadInPips > config.MaxSpread)
            {
                return false;
            }

            return true;
        }

        protected double CalculateEntryVolume()
        {
            if (!config.UseVolumePerOneK)
                return config.PerOneKVolume;

            //Using VolumePerOneK to calculate the volume, get the account balance and do the math
            var accountBalance = robot.Account.Balance;
            var volume = (accountBalance * config.PerOneKVolume) / config.PerOneKEquity;
            volume = Math.Min(volume, config.MaxFirstVolume);
            volume = Symbol.NormalizeVolumeInUnits(volume);

            return volume;
        }

        private void CloseAllPositions()
        {
            foreach (var position in Positions)
            {
                robot.ClosePosition(position.Position);
            }

            CheckIfTradesAreClosed();
        }

        private void UpdateStats()
        {
            var equityDrawdown = Positions.Sum(x => x.Position.NetProfit);
            if (equityDrawdown < 0)
            {
                var drawdownPercentage = Math.Abs(equityDrawdown / robot.Account.Balance) * 100;
                if (drawdownPercentage > results.MaxDrawdownPercentage)
                {
                    results.MaxDrawdownPercentage = drawdownPercentage;
                    results.MaxDrawdownValue = equityDrawdown;
                }
            }
        }
    }
}