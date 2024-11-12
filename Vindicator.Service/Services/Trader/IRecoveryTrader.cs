using cAlgo.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vindicator.Service.Models;

namespace Vindicator.Service.Services.Trader
{
    public interface IRecoveryTrader
    {
        RecoveryPositions Positions { get; }
        bool AddPositions(IEnumerable<Position> position, string botLabel, bool addNewRecoveryTrade);
        void OnTick();
        void OnOneMinBarClosed();
        void OnBar();
        void Configure(string symbol, TradeType tradeType, int index);
        RecoveryTraderResults GetResults();
        void UpdatePipsBetweenTrades(int pipsBetweenTrades);
        void OnPositionClosed(PositionClosedEventArgs args);
    }
}