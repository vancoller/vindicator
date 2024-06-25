﻿
using cAlgo.API;
using System.Collections.Generic;

namespace Vindicator.Service.Services
{
    public interface IVindicatorService
    {
        IEnumerable<int> GetPositionsInRecovery(string symbol);
        bool RecoverTrades(IEnumerable<Position> positions, string botLabel);
        void Stop();
        double GetFitness(GetFitnessArgs args);
        //IEnumerable<Position> GetPositions();
    }
}
