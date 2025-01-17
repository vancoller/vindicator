
using cAlgo.API;
using System.Collections.Generic;

namespace Vindicator.Service.Services
{
    public interface IVindicatorService
    {
        public delegate bool RecoveryFilterDelegate(TradeType tradeType, bool isPipsBetweenTradesOK, double nextVolume);

        IEnumerable<int> GetPositionsInRecovery(string symbol);


        /// <summary>
        /// Start recovery process for these trades.
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="botLabel"></param>
        /// <returns></returns>
        bool RecoverTrades(IEnumerable<Position> positions, string botLabel, bool immediatelyAddRecoveryTrade = true);


        /// <summary>
        /// Add trades to recovery basket. Do nothing more.
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="botLabel"></param>
        /// <returns></returns>
        bool AddTradesToRecovery(IEnumerable<Position> positions, string botLabel);

        void Stop();
        double GetFitness(GetFitnessArgs args);

        void UpdatePipsBetweenTrades(int pipsBetweenTrades);

        int GetNumberOfRecoveries(string symbolName);

        void OnPositionClosed(PositionClosedEventArgs args);

        void SetRecoveryCheckFunction(RecoveryFilterDelegate checkFunction);

    }
}
