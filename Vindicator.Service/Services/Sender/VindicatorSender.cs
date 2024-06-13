
using cAlgo.API;
using System.Collections.Generic;
using Vindicator.Service.Enums;
using Vindicator.Service.Models;
using Vindicator.Service.Services.Trader;

namespace Vindicator.Service.Services.Sender
{
    public class VindicatorSender : IVindicatorService
    {
        private readonly Robot robot;
        private readonly VindicatorSettings settings;
        private LocalStorageScope scope;

        public VindicatorSender(Robot robot, VindicatorSettings settings)
        {
            this.robot = robot;
            this.settings = settings;
        }

        public IEnumerable<int> GetPositionsInRecovery(string symbol)
        {
            throw new System.NotImplementedException();
        }

        public bool RecoverTrade(Position position, string botLabel)
        {
            var nextIndex = robot.LocalStorage.GetNextIndex();
            robot.LocalStorage.StorePosition(nextIndex, position.Id);

            return true;
        }

        public void Stop()
        {
        }

        public double GetFitness(GetFitnessArgs args)
        {
            return 0;
        }
    }
}
