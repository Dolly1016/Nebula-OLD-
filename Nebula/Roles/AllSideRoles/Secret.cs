using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.AllSideRoles
{
    public class Secret: AllSideRole, ExtraAssignable
    {
        public override Role GetActualRole()
        {
            Role role;
            ParseActualRole(out role,out bool hasGuesser);
            return role;
        }

        public void ParseActualRole(out Role role,out bool hasGuesser)
        {
            int roleData = Game.GameData.data.myData.getGlobalData().GetRoleData(this.id);
            int roleId = roleData & 0xFF;
            int exRoleId = roleData >> 8;

            role = Role.GetRoleById((byte)roleId);
            hasGuesser = (exRoleId & 1) != 0;
        }

        public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
        {
            if (category == RoleCategory.Crewmate)
            {
                int num = (int)CustomOptionHolder.RequiredTasksForArousal.getFloat();
                actualTasks = Helpers.GetRandomTaskList(num, 0.0);

                for (int i = 0; i < num; i++)
                {
                    if (initialTasks.Count == 0) break;
                    initialTasks.RemoveAt(NebulaPlugin.rnd.Next(initialTasks.Count));
                }

                GetActualRole().OnSetTasks(ref initialTasks,ref actualTasks);
            }
        }

        private void _sub_Assignment(Patches.AssignMap assignMap,PlayerControl player)
        {
            var data = Game.GameData.data.playersArray[player.PlayerId];

            int roleId = data.role.id;

            if (data.HasExtraRole(Roles.SecondaryGuesser))
            {
                roleId |= 1 << 8;
                assignMap.UnassignExtraRole(player.PlayerId, Roles.SecondaryGuesser.id);
            }

            assignMap.AssignRole(player.PlayerId, this.id, roleId);
        }

        public void Assignment(Patches.AssignMap assignMap)
        {
            if (!CustomOptionHolder.SecretRoleOption.getBool()) return;

            List<PlayerControl> players = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            players.RemoveAll(x => x.GetModData().role.category != category || !x.GetModData().role.CanBeSecret);
            int max = 0;
            if (category == RoleCategory.Crewmate) max = CustomOptionHolder.NumOfSecretCrewmateOption.getSelection();
            if (category == RoleCategory.Impostor) max = CustomOptionHolder.NumOfSecretImpostorOption.getSelection();
            for (int i = 0; i < max; i++)
            {
                if (players.Count == 0) break;

                int rnd = NebulaPlugin.rnd.Next(players.Count);
                _sub_Assignment(assignMap, players[rnd]);
                players.RemoveAt(rnd);
            }
        }

        public byte assignmentPriority { get => 128; }

        public override bool HasInfiniteCrewTaskQuota(byte playerId)
        {
            return true;
        }

        public override bool HasExecutableFakeTask(byte playerId)
        {
            return side == Side.Crewmate;
        }

        private void RevealRole()
        {
            //役職を元に戻す
            ParseActualRole(out Role role, out bool hasGuesser);
            List<Tuple<Tuple<ExtraRole, ulong>, bool>> exRoles = new List<Tuple<Tuple<ExtraRole, ulong>, bool>>();
            if (hasGuesser) exRoles.Add(new Tuple<Tuple<ExtraRole, ulong>, bool>(new Tuple<ExtraRole, ulong>(Roles.SecondaryGuesser, 0), true));

            RPCEventInvoker.ImmediatelyChangeRole(PlayerControl.LocalPlayer, role, exRoles.ToArray());
        }

        public override void OnTaskComplete()
        {
            var task = Game.GameData.data.myData.getGlobalData().Tasks;

            if (task.Completed >= task.Quota)
            {
                RevealRole();
            }
        }

        public override void OnKillPlayer(byte targetId)
        {
            if (category == RoleCategory.Impostor)
            {
                int num = (int)CustomOptionHolder.RequiredNumOfKillingForArousal.getFloat();
                var data = Game.GameData.data.myData.getGlobalData();
                if (data.Tasks == null) data.Tasks = new Game.TaskData(num, num, num, false, false);
                data.Tasks.Completed++;

                if (data.Tasks.Completed >= data.Tasks.Quota)
                {
                    RevealRole();
                    data.Tasks = null;
                }
            }
        }

        public override void FinalizeInGame(PlayerControl __instance)
        {
            if (category == RoleCategory.Crewmate)  RPCEventInvoker.ChangeTasks(Game.GameData.data.myData.InitialTasks, true);
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

        public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
        {
            return;
        }

        public Secret(Role templateRole)
            : base(templateRole,"Secret", "secret", templateRole.Color,
                 true, VentPermission.CanNotUse, false, false, false)
        {
            IsHideRole = true;

            Roles.AllExtraAssignable.Add(this);
        }
    }
}
