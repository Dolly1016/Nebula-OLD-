using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Database
{
    public class SkeldData : MapData
    {
        public SkeldData() :base(0)
        {
            SabotageMap[SystemTypes.Reactor] = new SabotageData(SystemTypes.Reactor, new Vector3(-21f, -5.4f), true, true);
            SabotageMap[SystemTypes.LifeSupp] = new SabotageData(SystemTypes.LifeSupp, new Vector3(6.5f, -4.7f), true, true);
            SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(-9.3f, -10.9f), true, false);
            SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(4.6f, -16.1f), true, false);

            RoomsRelation[SystemTypes.Cafeteria] = new HashSet<SystemTypes>() {
                SystemTypes.Admin,SystemTypes.Weapons,SystemTypes.MedBay
            };
            RoomsRelation[SystemTypes.Weapons] = new HashSet<SystemTypes>() {
                SystemTypes.LifeSupp,SystemTypes.Cafeteria
            };
            RoomsRelation[SystemTypes.LifeSupp] = new HashSet<SystemTypes>() {
                SystemTypes.Weapons,SystemTypes.Nav
            };
            RoomsRelation[SystemTypes.Nav] = new HashSet<SystemTypes>() {
                SystemTypes.LifeSupp,SystemTypes.Shields
            };
            RoomsRelation[SystemTypes.Shields] = new HashSet<SystemTypes>() {
                SystemTypes.Nav,SystemTypes.Comms
            };
            RoomsRelation[SystemTypes.Comms] = new HashSet<SystemTypes>() {
                SystemTypes.Shields,SystemTypes.Storage
            };
            RoomsRelation[SystemTypes.Storage] = new HashSet<SystemTypes>() {
                SystemTypes.Admin,SystemTypes.Electrical
            };
            RoomsRelation[SystemTypes.Electrical] = new HashSet<SystemTypes>() {
                SystemTypes.Storage,SystemTypes.LowerEngine
            };
            RoomsRelation[SystemTypes.LowerEngine] = new HashSet<SystemTypes>() {
                SystemTypes.Electrical,SystemTypes.Security,SystemTypes.Reactor
            };
            RoomsRelation[SystemTypes.Security] = new HashSet<SystemTypes>() {
                SystemTypes.LowerEngine,SystemTypes.UpperEngine,SystemTypes.Reactor
            };
            RoomsRelation[SystemTypes.Reactor] = new HashSet<SystemTypes>() {
                SystemTypes.LowerEngine,SystemTypes.UpperEngine,SystemTypes.Security
            };
            RoomsRelation[SystemTypes.UpperEngine] = new HashSet<SystemTypes>() {
                SystemTypes.MedBay,SystemTypes.Security,SystemTypes.Reactor
            };
            RoomsRelation[SystemTypes.MedBay] = new HashSet<SystemTypes>() {
                SystemTypes.UpperEngine,SystemTypes.Cafeteria
            };

            DoorRooms.Add(SystemTypes.Cafeteria);
            DoorRooms.Add(SystemTypes.Storage);
            DoorRooms.Add(SystemTypes.Electrical);
            DoorRooms.Add(SystemTypes.LowerEngine);
            DoorRooms.Add(SystemTypes.Security);
            DoorRooms.Add(SystemTypes.UpperEngine);
            DoorRooms.Add(SystemTypes.MedBay);

            DoorHackingCanBlockSabotage = true;

            MapScale = 32f;
        }
    }
}
