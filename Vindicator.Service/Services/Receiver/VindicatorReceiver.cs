using cAlgo.API;
using System;
using System.Linq;
using Vindicator.Service.Enums;

namespace Vindicator.Service.Services.Receiver
{
    public class VindicatorReceiver : IVindicatorReceiver
    {
        private LocalStorageScope scope = LocalStorageScope.Device;

        private readonly IVindicatorService vindicatorService;
        private readonly Robot robot;

        public VindicatorReceiver(IVindicatorService _vindicatorService, Robot _robot)
        {
            vindicatorService = _vindicatorService;
            robot = _robot;

            robot.Timer.Start(TimeSpan.FromMinutes(1));
            robot.Timer.TimerTick += Timer_TimerTick;
        }

        private void Timer_TimerTick()
        {
            bool foundTrade = true;

            //Load
            robot.LocalStorage.Reload(scope);
            var index = robot.LocalStorage.GetLastStoredIndex();

            while (foundTrade)
            {
                index++;
                var positionId = robot.LocalStorage.GetStoredPosition(index);
                if (positionId == 0)
                {
                    foundTrade = false;
                    continue;
                }

                //Trade found
                foundTrade = true;
                var position = robot.Positions.FirstOrDefault(x => x.Id == positionId);
                if (position != null)
                {
                    if (vindicatorService.RecoverTrade(position, position.Label))
                    {
                        robot.LocalStorage.SetLastStoredIndex(index);
                        robot.LocalStorage.RemoveStoredPosition(index);
                    }
                }
            }
        }

        
    }
}