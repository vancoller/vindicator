using System;
using System.Collections.Generic;
using System.Linq;
using Vindicator.Service.Models;

namespace Vindicator.Service.Services.Trader
{
    public class RecoveryPositions : List<RecoveryPosition>
    {
        //Get start date of the first position
        public DateTime GetStartingDate()
        {
            return this.Min(x => x.Position.EntryTime);
        }
    }
}
