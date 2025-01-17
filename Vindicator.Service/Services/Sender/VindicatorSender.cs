
using cAlgo.API;
using System.Collections.Generic;
using System.Linq;
using Algolib.Shared;
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

        public bool RecoverTrades(IEnumerable<Position> positions, string botLabel, bool immediatelyAddRecoveryTrade = true)
        {
            //TEMP
            foreach (var position in positions)
            {
                position.ModifyTakeProfitPips(0);
                recoveryTrades.Add(position);

                var nextIndex = _.LocalStorage.GetNextIndex();
                _.LocalStorage.StorePosition(nextIndex, position.Id);
            }

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

        public bool AddTradesToRecovery(IEnumerable<Position> positions, string botLabel)
        {
            //TEMP
            foreach (var position in positions)
            {
                recoveryTrades.Add(position);
            }

            return true;
        }

        public void UpdatePipsBetweenTrades(int pipsBetweenTrades)
        {
        }

        public int GetNumberOfRecoveries(string symbolName)
        {
            return 0;
        }

        public void OnPositionClosed(PositionClosedEventArgs args)
        {
        }

        public void SetRecoveryCheckFunction(IVindicatorService.RecoveryFilterDelegate checkFunction)
        {
        }
    }
}
