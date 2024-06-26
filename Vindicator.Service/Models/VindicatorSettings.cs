using Algolib.Shared;

namespace Vindicator.Service.Models
{
    public class VindicatorSettings
    {
        public double TakeProfitMoney { get; set; }
        public int PipsBetweenTrades { get; set; }
        public bool GenerateBacktestReport { get; set; }
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
    }
}