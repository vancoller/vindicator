﻿using Algolib.Shared;
using static Vindicator.Service.Services.IVindicatorService;

namespace Vindicator.Service.Models
{
    public class VindicatorSettings
    {
        public double TakeProfitPips { get; set; }
        public int PipsBetweenTrades { get; set; }
        public bool GenerateBacktestReport { get; set; }
        public int StartRecoveryPips { get; set; }
        public string Symbol { get; set; } //This does not always have a value

        public string BotLabel { get; set; }
        public double MaxSpread { get; set; }

        //Recovery Volume
        public RecoveryVolumeSetting VolumeSetting { get; set; }
        public int IncreaseEveryXTrade { get; set; }

        //Money Management
        public bool UseVolumePerOneK { get; set; }
        public double PerOneKVolume { get; set; }
        public double PerOneKEquity { get; set; }
        public double MaxFirstVolume { get; set; }

        //RecoveryFilterDelegate
        public RecoveryFilterDelegate RecoveryFilterDelegate { get; set; }
    }
}