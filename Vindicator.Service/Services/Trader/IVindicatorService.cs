
using cAlgo.API;
using System.Collections.Generic;

namespace Vindicator.Service.Services.Trader
{
    public interface IVindicatorService
    {
        IEnumerable<int> GetPositionsInRecovery(string symbol);
        bool RecoverTrade(Position position, string botLabel);
        void Stop();
        double GetFitness(GetFitnessArgs args);
        //IEnumerable<Position> GetPositions();
    }
}
