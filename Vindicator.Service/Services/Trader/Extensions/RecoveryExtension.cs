using System;
using System.Linq;

namespace Vindicator.Service.Services.Trader.Extensions
{
    public static class RecoveryExtension
    {
        /// <summary>
        /// Default calculculation for the volume based on equity
        /// </summary>
        /// <param name="trader"></param>
        /// <returns></returns>
        public static double CalculateStandardVolume(this RecoveryTrader trader)
        {
            var config = trader.config;

            if (!config.UseVolumePerOneK)
                return config.PerOneKVolume;

            //Using VolumePerOneK to calculate the volume, get the account balance and do the math
            var accountBalance = trader.robot.Account.Equity;
            var volume = accountBalance * config.PerOneKVolume / config.PerOneKEquity;
            volume = Math.Min(volume, config.MaxFirstVolume);
            volume = trader.Symbol.NormalizeVolumeInUnits(volume);

            return volume;
        }

        public static double AdjustGridSize_NumberOfTrades(this RecoveryTrader trader, double gridSize)
        {
            if (trader.config.IncreaseEveryXTrade == 0)
                return gridSize;

            return gridSize * Math.Max(1, (trader.PositionsOpenedHere.Count() / trader.config.IncreaseEveryXTrade + 1));
        }

        public static double AdjustVolume_NumberOfTrades(this RecoveryTrader trader, double volume)
        {
            if (trader.config.IncreaseEveryXTrade == 0)
                return volume;

            return volume * Math.Max(1, (trader.PositionsOpenedHere.Count() / trader.config.IncreaseEveryXTrade + 1));
        }

        public static double AdjustVolume_Elastic(this RecoveryTrader trader, double volume)
        {
            var tradeNumber = trader.PositionsOpenedHere.Count();
            if (tradeNumber >= (trader.config.IncreaseEveryXTrade * 3))
                return volume * 6;

            if (tradeNumber >= (trader.config.IncreaseEveryXTrade * 2))
                return volume * 4;

            if (tradeNumber >= (trader.config.IncreaseEveryXTrade * 1))
                return volume * 2;

            return volume;
        }

        public static double AdjustVolume_DaysInTrade(this RecoveryTrader trader, double volume)
        {
            var daysInTrade = trader.GetDaysInBasket();
            var mutliplier = Math.Max(1, daysInTrade / 60);
            return volume * mutliplier;
        }
    }
}
