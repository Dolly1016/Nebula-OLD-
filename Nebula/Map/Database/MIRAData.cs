using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Database
{
    public class MIRAData : MapData
    {
        public MIRAData() : base(1)
        {
            SabotageMap[SystemTypes.Reactor] = new SabotageData(SystemTypes.Reactor, new Vector3(2.5f, 13f), true, true);
            SabotageMap[SystemTypes.LifeSupp] = new SabotageData(SystemTypes.LifeSupp, new Vector3(3.7f, -1f), false, true);
            SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(15f, 21f), true, false);
            SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(15f, 5f), false, false);

            MapScale = 36f;
        }
    }
}
