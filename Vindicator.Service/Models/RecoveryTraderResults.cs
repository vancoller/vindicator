using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vindicator.Service.Models
{
    public class RecoveryTraderResults
    {
        public string Symbol { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double MaxDrawdownPercentage { get; set; }
        public double MaxDrawdownValue { get; set; }

        public double TotalDays
        {
            get
            {
                return (EndDate - StartDate).TotalDays;
            }
        }

        public RecoveryTraderResults(string symbol)
        {
            Symbol = symbol;
        }
    }
}
