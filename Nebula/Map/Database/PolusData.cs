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

            //スポーン候補
            SpawnCandidates.Add(new SpawnCandidate("Dropship",new Vector2(16.6f ,-1.5f), "Nebula.Resources.Locations.Dropship.png"));
            SpawnCandidates.Add(new SpawnCandidate("Storage", new Vector2(20.6f, -11.7f), "Nebula.Resources.Locations.Storage.png"));
            SpawnCandidates.Add(new SpawnCandidate("Laboratory", new Vector2(34.8f, -6.0f), "Nebula.Resources.Locations.Laboratory.png"));
            SpawnCandidates.Add(new SpawnCandidate("Specimens", new Vector2(36.5f, -21.2f), "Nebula.Resources.Locations.Specimens.png"));
            SpawnCandidates.Add(new SpawnCandidate("Office", new Vector2(19.5f, -17.6f), "Nebula.Resources.Locations.Office.png"));
            SpawnCandidates.Add(new SpawnCandidate("Weapons", new Vector2(12.2f, -23.3f), "Nebula.Resources.Locations.Weapons.png"));
            SpawnCandidates.Add(new SpawnCandidate("LifeSupport", new Vector2(3.5f, -21.5f), "Nebula.Resources.Locations.LifeSupport.png"));
            SpawnCandidates.Add(new SpawnCandidate("Electrical", new Vector2(7.4f, -9.6f), "Nebula.Resources.Locations.Electrical.png"));
        }
    }
}
