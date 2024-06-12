using cAlgo.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vindicator.Service.Models;

namespace Vindicator.Service.Services
{
    public interface IRecoveryTrader
    {
        List<RecoveryPosition> Positions { get; }
        bool AddPosition(Position position, string botLabel);
        void OnTick();
        void OnOneMinBarClosed();
        void Configure(string symbol, TradeType tradeType);
        RecoveryTraderResults GetResults();
    }
}