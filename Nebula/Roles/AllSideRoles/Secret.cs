using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.AllSideRoles
{
    public class Secret: AllSideRole, ExtraAssignable
    {
        public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
        {
            if (side == Side.Crewmate) actualTasks = Helpers.GetRandomTaskList(3, 0.0);
        }

        public virtual void Assignment(Patches.AssignMap assignMap)
        {
            foreach (byte p in Game.GameData.data.AllPlayers.Keys)
                if (this.side == Game.GameData.data.playersArray[p].role.side)
                {
                    var roleId = Game.GameData.data.playersArray[p].role.id;
                    assignMap.AssignRole(p, this.id, roleId);
                }
        }

        public byte assignmentPriority { get => 128; }

        public override void OnTaskComplete()
        {
            var task = Game.GameData.data.myData.getGlobalData().Tasks;

            if (task.Completed >= task.Quota)
            {
                //役職を元に戻す
                int roleData = Game.GameData.data.myData.getGlobalData().GetRoleData(this.id);
                int roleId = roleData & 0xFF;
                int exRoleId = roleData >> 8;
                RPCEventInvoker.ImmediatelyChangeRole(PlayerControl.LocalPlayer, Role.GetRoleById((byte)roleId));
            }
        }

        public override void FinalizeInGame(PlayerControl __instance)
        {
            if (side == Side.Crewmate)  RPCEventInvoker.ChangeTasks(Game.GameData.data.myData.InitialTasks, true);
        }

        public override void EditDisplayRoleName(byte playerId, ref string roleName, bool isIntro)
        {
            if(Game.GameData.data.myData.CanSeeEveryoneInfo) EditDisplayRoleNameForcely(playerId, ref roleName);
        }

        public override void EditDisplayRoleNameForcely(byte playerId, ref string roleName)
        {
            int roleData = Game.GameData.data.GetPlayerData(playerId).GetRoleData(this.id);
            int roleId = roleData & 0xFF;
            int exRoleId = roleData >> 8;

            var role = Role.GetRoleById((byte)roleId);
            string shortText = Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".short"));
            roleName += Helpers.cs(new Color(0.6f, 0.6f, 0.6f), $"({shortText})");
        }

        public Secret(Role templateRole)
            : base(templateRole,"Secret", "secret", templateRole.Color,
                 false, VentPermission.CanNotUse, false, false, false)
        {
            DefaultCanBeLovers = false;

            Roles.AllExtraAssignable.Add(this);
            IsHideRole = true;
        }
    }
}
