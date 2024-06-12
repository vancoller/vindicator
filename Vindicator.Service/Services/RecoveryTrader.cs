using cAlgo.API;
using cAlgo.API.Internals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Vindicator.Service.Models;

namespace Vindicator.Service.Services
{
    public class RecoveryTrader : IRecoveryTrader
    {
        public List<RecoveryPosition> Positions { get; }
        public List<PendingTrade> PendingTrades { get; set; }

        private string symbol;
        private TradeType tradeType;
        private Symbol Symbol { get; set; }


        private readonly VindicatorSettings config;
        private readonly Robot robot;

        public RecoveryTrader(VindicatorSettings _config, Robot _robot)
        {
            config = _config;
            robot = _robot;
            Positions = new List<RecoveryPosition>();
            PendingTrades = new List<PendingTrade>();
        }

        public void Configure(string _symbol, TradeType _tradeType)
        {
            symbol = _symbol;
            tradeType = _tradeType;
            Symbol = robot.Symbols.GetSymbol(symbol);
        }

        public bool AddPosition(Position position, string botLabel)
        {
            AddRecoveryPosition(position, botLabel);
            return true;
        }

        public void OnTick()
        {
            if (!Positions.Any())
                return;

            if (PendingTrades.Any())
            {
                CheckPendingTrades();
            }
            else
            {
                ProcessRecovery();
            }
        }

        private void UpdateTakeProfit()
        {
            var takeProfitPrice = CalculateBreakEvenPrice(tradeType);

            if (tradeType == TradeType.Buy)
                takeProfitPrice += (config.TakeProfitPips * Symbol.PipSize);
            else
                takeProfitPrice -= (config.TakeProfitPips * Symbol.PipSize);

            foreach (var position in Positions)
            {
                robot.ModifyPosition(position.Position, null, takeProfitPrice);
            }
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

            var withoutFees = totalVolumePrice / totalVolume;
            var valuePerPip = Symbol.PipValue * totalVolume;
            var pipsRequiredToCoverFees = fees / valuePerPip;

            return withoutFees + (pipsRequiredToCoverFees * Symbol.PipSize);
        }

        private void ProcessRecovery()
        {
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
                AddPendingTrade(new PendingTrade(Symbol.Name, CalculateEntryVolume(), tradeType, config.BotLabel, null, null, null, null, "calculated_recovery"));
            }
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

                    //Modify TP if was a price
                    if (trade.TakeProfitPrice.HasValue)
                    {
                        robot.ModifyPosition(result.Position, null, trade.TakeProfitPrice);
                    }

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
    }
}