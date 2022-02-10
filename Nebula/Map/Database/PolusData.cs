using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Database
{
    public class PolusData : MapData
    {
        public PolusData() : base(2)
        {
            CommonTaskIdList = new List<byte>() { 0, 1, 2, 3 };
            ShortTaskIdList = new List<byte>() { 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30, 31, 32 };
            LongTaskIdList = new List<byte>() { 5, 6, 9, 10, 11, 12, 13, 16, 17, 18 };

            SabotageMap[SystemTypes.Laboratory] = new SabotageData(SystemTypes.Reactor, new Vector3(18f, -6f), true, true);
            SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(10f, -11f), true, false);
            SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(14f, -15.5f), true, false);

            DoorRooms.Add(SystemTypes.Laboratory);
            DoorRooms.Add(SystemTypes.Electrical);
            DoorRooms.Add(SystemTypes.Office);
            DoorRooms.Add(SystemTypes.Comms);
            DoorRooms.Add(SystemTypes.Weapons);
            DoorRooms.Add(SystemTypes.LifeSupp);
            DoorRooms.Add(SystemTypes.Storage);

            MapScale = 32f;
        }
    }
}
