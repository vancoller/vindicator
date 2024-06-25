using cAlgo.API;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Vindicator.Service.Services.Trader.Extensions
{
    public static class RecoveryExtension
    {
        public static double AdjustGridSize_NumberOfTrades(this RecoveryTrader trader, double gridSize)
        {
            if (trader.config.IncreaseVolumeEveryXTrade == 0)
                return gridSize;

            return gridSize * Math.Max(1, (trader.PositionsOpenedHere.Count() / trader.config.IncreaseVolumeEveryXTrade + 1));
        }

        public static double CalculateStandardVolume(this RecoveryTrader trader)
        {
            var config = trader.config;

            if (!config.UseVolumePerOneK)
                return config.PerOneKVolume;

            //Using VolumePerOneK to calculate the volume, get the account balance and do the math
            var accountBalance = trader.robot.Account.Balance;
            var volume = accountBalance * config.PerOneKVolume / config.PerOneKEquity;
            volume = Math.Min(volume, config.MaxFirstVolume);
            volume = trader.Symbol.NormalizeVolumeInUnits(volume);

            return volume;
        }

        public static double AdjustVolume_NumberOfTrades(this RecoveryTrader trader, double volume)
        {
            if (trader.config.IncreaseVolumeEveryXTrade == 0)
                return volume;

            return volume * Math.Max(1, (trader.PositionsOpenedHere.Count() / trader.config.IncreaseVolumeEveryXTrade + 1));
        }

        public static double AdjustVolume_DaysInTrade(this RecoveryTrader trader, double volume)
        {
            var daysInTrade = trader.GetDaysInBasket();
            var mutliplier = Math.Max(1, daysInTrade / 60);
            return volume * mutliplier;
        }

        public static bool IsRSILong(this RecoveryTrader trader)
        {
            var Bars = trader.robot.Bars;
            var prevBarIsOS = trader.rsi.Result[Bars.Count - 2] <= 35;
            var barBeforePrevIsOs = trader.rsi.Result[Bars.Count - 3] <= 35;

            return !prevBarIsOS && barBeforePrevIsOs;// && IsTrendUp();
        }

        public static bool IsRSIShort(this RecoveryTrader trader)
        {
            var Bars = trader.robot.Bars;
            var prevBarIsOB = trader.rsi.Result[Bars.Count - 2] >= 65;
            var barBeforePrevIsOB = trader.rsi.Result[Bars.Count - 3] >= 65;

            return !prevBarIsOB && barBeforePrevIsOB;// && !IsTrendUp();
        }
    }
}
