using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.RitualRoles
{
    public class RitualRole : Role
    {
        public RitualRole(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius)
        :base(name,localizeName,color,category,side,introMainDisplaySide,introDisplaySides,introInfluenceSides,winReasons,
             hasFakeTask,canUseVents,canMoveInVents,ignoreBlackout,useImpostorLightRadius){
            CanCallEmergencyMeeting = false;
        }
        public override void IntroInitialize(PlayerControl __instance)
        {
            int i = 0;
            foreach(var d in Game.GameData.data.RitualData.TaskList){
                switch (d.id) {
                    case 0:
                        var wiringTask = PlayerControl.LocalPlayer.myTasks[i].GetComponent<Tasks.RitualWiringTask>();
                        wiringTask.SetRooms(d.rooms, 1);
                        wiringTask.SearchNextLocation();
                        wiringTask.NebulaData[1] = (byte)d.num;
                        break;
                }
                i++;
            }
            
        }
    }
}
