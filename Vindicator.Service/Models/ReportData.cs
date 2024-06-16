using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vindicator.Service.Models
{
    public class ReportData
    {
        public string TimeFrame { get; set; }
        public double Balance { get; set; }
        public double Equity { get; set; }
        //public double RecoveryFactor { get; set; }
        //public double Drawdown { get; set; }
        //public double DrawdownPercent { get; set; }
        //public double MaxDrawdown { get; set; }
        //public double MaxDrawdownPercent { get; set; }
        //public double Balance { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        //public int TotalTrades { get; set; }
        //public int WinningTrades { get; set; }
        //public int LosingTrades { get; set; }

        public RecoveryTraderResults[] Recoveries { get; set; }
        public VindicatorSettings Config { get; set; }
    }

}
