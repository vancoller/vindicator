
using cAlgo.API;
using System.Collections.Generic;

namespace Vindicator.Service.Services
{
    public interface IVindicatorService
    {
        IEnumerable<int> GetPositionsInRecovery(string botlabel, string symbol);

        bool RecoverTrade(Position position, string botLabel);
        void Stop();
        //IEnumerable<Position> GetPositions();
    }
}
