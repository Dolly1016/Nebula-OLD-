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
            CommonTaskIdList = new List<byte>() { 0, 1 };
            ShortTaskIdList = new List<byte>() { 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 };
            LongTaskIdList = new List<byte>() { 2, 3, 4, 8, 9, 10, 11, 12 };

            SabotageMap[SystemTypes.Reactor] = new SabotageData(SystemTypes.Reactor, new Vector3(2.5f, 13f), true, true);
            SabotageMap[SystemTypes.LifeSupp] = new SabotageData(SystemTypes.LifeSupp, new Vector3(3.7f, -1f), false, true);
            SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(15f, 21f), true, false);
            SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(15f, 5f), false, false);

            MapScale = 36f;
        }
    }
}
