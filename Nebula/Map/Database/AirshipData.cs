using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Database
{
    public class AirshipData : MapData 
    {
        public AirshipData() : base(4)
        {
            SabotageMap[SystemTypes.GapRoom] = new SabotageData(SystemTypes.GapRoom, new Vector3(8f, 8.3f), true, true);
            SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(0f, 0f), false, false);
            SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(-13.6f, 2f), true, false);

            DoorRooms.Add(SystemTypes.Brig);
            DoorRooms.Add(SystemTypes.Records);
            DoorRooms.Add(SystemTypes.Medical);
            DoorRooms.Add(SystemTypes.Comms);
            DoorRooms.Add(SystemTypes.MainHall);
            DoorRooms.Add(SystemTypes.Kitchen);


            MapScale = 30f;
            HasDefaultPrespawnMinigame = true;
        }
    }
}
