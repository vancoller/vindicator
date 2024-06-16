
using cAlgo.API;
using System.Collections.Generic;
using System.Linq;
using Vindicator.Service.Enums;
using Vindicator.Service.Models;

namespace Vindicator.Service.Services.Sender
{
    public class VindicatorSender : IVindicatorService
    {
        private readonly Robot _;
        private LocalStorageScope scope;
        private List<Position> recoveryTrades = new List<Position>();

        public VindicatorSender(Robot robot)
        {
            this._ = robot;
        }

        public IEnumerable<int> GetPositionsInRecovery(string symbol)
        {
            return recoveryTrades.Select(x => x.Id);
        }

        public bool RecoverTrade(Position position, string botLabel)
        {
            //TEMP
            position.ModifyTakeProfitPips(0);
            recoveryTrades.Add(position);
            //TEMP


            var nextIndex = _.LocalStorage.GetNextIndex();
            _.LocalStorage.StorePosition(nextIndex, position.Id);

            //Push
            _.LocalStorage.Flush(scope);
            return true;
        }

        public void Stop()
        {
            _.Print("--------------------------------------------------- RECOVERY STATISTICS ----------------------------------------------------");
            _.Print($"Number of recoveries  |  {recoveryTrades.Count}");
        }

        public double GetFitness(GetFitnessArgs args)
        {
            return 0;
        }
    }
}
