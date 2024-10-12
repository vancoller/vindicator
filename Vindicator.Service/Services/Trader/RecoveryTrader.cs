using Algolib.Enums;
using Algolib.Shared;
using Algolib.Shared.Interfaces;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using Vindicator.Service.Models;
using Vindicator.Service.Services.Trader.Extensions;

namespace Vindicator.Service.Services.Trader
{
    public class RecoveryTrader : IRecoveryTrader
    {
        public RecoveryPositions Positions { get; }
        public IEnumerable<RecoveryPosition> PositionsOpenedHere
        {
            get
            {
                return Positions.Where(x => x.BotLabel == config.BotLabel);
            }
        }

        public List<PendingTrade> PendingTrades { get; set; }
        public Symbol Symbol { get; set; }
        private TradeType tradeType;
        private RecoveryTraderResults results;

        public readonly VindicatorSettings config;
        public readonly IBaseRobot robot;

        public RelativeStrengthIndex rsi;


        public RecoveryTrader(VindicatorSettings _config, IBaseRobot _robot)
        {
            config = _config;
            robot = _robot;
            Positions = new RecoveryPositions();
            PendingTrades = new List<PendingTrade>();
            rsi = robot.Indicators.RelativeStrengthIndex(robot.Bars.ClosePrices, 14);
        }
        
        public void Configure(string _symbol, TradeType _tradeType, int index)
        {
            tradeType = _tradeType;
            Symbol = robot.Symbols.GetSymbol(_symbol);
            results = new RecoveryTraderResults(_symbol, _tradeType);
        }

        public void OnOneMinBarClosed()
        {
            //if (!PendingTrades.Any())
                //ProcessRecovery();
        }

        public RecoveryTraderResults GetResults()
        {
            if (Positions.Any() && robot.BacktestCloseTradesOnEnd)
            {
                foreach (var position in Positions)
                {
                    robot.ClosePosition(position.Position);
                }

                results.EndDate = robot.Time;
            }

            return results;
        }

        public bool AddPositions(IEnumerable<Position> positions, string botLabel, bool addNewRecoveryTrade)
        {
            //First entry
            if (!Positions.Any())
            {
                results.StartDate = robot.Time;
            }

            foreach (var position in positions)
            {
                AddRecoveryPosition(position, botLabel);
            }

            if (addNewRecoveryTrade)
                CreateNewRecoveryTrade();
            
            ProcessRecovery();

            return true;
        }

        public void OnTick()
        {
            if (!Positions.Any())
                return;

            //CheckTakeProfit();

            if (PendingTrades.Any())
                CheckPendingTrades();
            //else
            //ProcessRecovery();
        }

        public void OnBar()
        {
            UpdateStats();
            CheckIfTradesAreClosed();

            if (!PendingTrades.Any())
                ProcessRecovery();

            //CheckMaxDrawdown();
        }

        //private void CheckMaxDrawdown()
        //{
        //    if (results.MaxDrawdownPercentage >= 10)
        //    {
        //        CloseAllPositions();
        //    }
        //}

        private void CheckIfTradesAreClosed()
        {
            var tradesToRemove = new List<RecoveryPosition>();
            foreach (var position in Positions)
            {
                if (robot.GetCTraderPositionsClass().FirstOrDefault(x => x.Id == position.Position.Id) == null)
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

        private void UpdateTakeProfit()
        {
            var takeProfitPrice = CalculateBreakEvenPrice(tradeType);
            foreach (var position in Positions.OrderBy(x => x.Position.EntryPrice))
            {
                robot.ModifyPosition(position.Position, null, takeProfitPrice);
            }
        }

        private double CalculateBreakEvenPrice(TradeType tradeType)
        {
            var totalVolume = 0.0;
            var totalVolumePrice = 0.0;
            var fees = 0.0;
            double result = 0;

            foreach (var position in Positions)
            {
                totalVolume += position.Position.VolumeInUnits;
                totalVolumePrice += position.Position.EntryPrice * position.Position.VolumeInUnits;
                fees += Math.Abs(position.Position.Commissions * 2) + Math.Abs(position.Position.Swap);
            }

            //Add $$ to make profit
            //fees += config.TakeProfitMoney * (totalVolume / 1000);

            var withoutFees = totalVolumePrice / totalVolume;
            var valuePerPip = Symbol.PipValue * totalVolume;
            var pipsRequiredToCoverFees = fees / valuePerPip;

            var addedPips = (config.TakeProfitPips) * Symbol.PipSize;

            if (tradeType == TradeType.Buy)
                result = withoutFees + (pipsRequiredToCoverFees * Symbol.PipSize) + addedPips;
            else
                result = withoutFees - (pipsRequiredToCoverFees * Symbol.PipSize) - addedPips;

            return result;
        }

        private bool CheckFilters()
        {
            if (tradeType == TradeType.Buy)
            {
                if (!this.IsRSILong())
                    return false;
            }

            if (tradeType == TradeType.Sell)
            {
                if (!this.IsRSIShort())
                    return false;
            }

            return true;
        }
        private void ProcessRecovery()
        {
            if (!Positions.Any() || PendingTrades.Any())
                return;

            //Filters -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
            var filterPassed = CheckFilters();
            //if (tradeType == TradeType.Buy)
            //{
            //    if (config.TrendEMAPeriod != 0 && emaTrend.Result.LastValue > Symbol.Bid)
            //        filterPassed = false;

            //    if (!this.IsRSILong())
            //        filterPassed = false;
            //}

            //if (tradeType == TradeType.Sell)
            //{
            //    if (config.TrendEMAPeriod != 0 && emaTrend.Result.LastValue < Symbol.Ask)
            //        filterPassed = false;

            //    if (!this.IsRSIShort())
            //        filterPassed = false;
            //}
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
            var requiredPipsAway = GetRequiredPipsAway();

            var isPipsBetweenTradesOK = pipsBetweenTrades < -requiredPipsAway;
            //var isPipsBetweenTradesDouble = pipsBetweenTrades < -requiredPipsAway * 2;

            if ((filterPassed && isPipsBetweenTradesOK))// || isPipsBetweenTradesDouble)
            {
                CreateNewRecoveryTrade();
            }
        }

        private double GetRequiredPipsAway()
        {
            var gridSizeInPips = (double)config.PipsBetweenTrades;
            gridSizeInPips = this.AdjustGridSize_NumberOfTrades(gridSizeInPips);

            return gridSizeInPips;
        }

        private void CreateNewRecoveryTrade()
        {
            var volume = CalculateRecoveryTradeVolume();
            AddPendingTrade(new PendingTrade(Symbol.Name, volume, tradeType, Algolib.Shared.Enums.OrderType.Market, null, config.BotLabel, null, null, null, null, Constants.TradeRecovery));
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
                    results.Volume += trade.Volume;

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

        protected double CalculateRecoveryTradeVolume()
        {
            var volume = this.CalculateStandardVolume();

            switch(config.VolumeSetting)
            {
                case RecoveryVolumeSetting.Standard:
                    break;
                case RecoveryVolumeSetting.Elastic:
                    volume = this.AdjustVolume_Elastic(volume);
                    break;
                case RecoveryVolumeSetting.IncreasePerTrade:
                    volume = this.AdjustVolume_NumberOfTrades(volume);
                    break;
            }

            return Symbol.NormalizeVolumeInUnits(volume);
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

        public double GetDaysInBasket()
        {
            var startDate = Positions.GetStartingDate();
            var currentDate = robot.Time;
            var daysInTrade = currentDate.Subtract(startDate).TotalDays;

            return daysInTrade;
        }
    }
}