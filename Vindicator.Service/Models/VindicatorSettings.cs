
namespace Vindicator.Service.Models
{
    public class VindicatorSettings
    {
        public double TakeProfitMoney { get; set; }
        public int PipsBetweenTrades { get; set; }
        public string BotLabel { get; set; }
        public double MaxSpread { get; set; }

        public bool UseVolumePerOneK { get; set; }
        public double PerOneKVolume { get; set; }
        public double PerOneKEquity { get; set; }
        public double MaxFirstVolume { get; set; }
        public int IncreaseVolumeEveryXTrade { get; set;}

    }
}