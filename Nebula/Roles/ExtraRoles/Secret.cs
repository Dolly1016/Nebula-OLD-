using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles
{
    public class Secret : ExtraRole
    {
        static public Color RoleColor = new Color(133f / 255f, 161f / 255f, 190f / 255f);

        public override void Assignment(Patches.AssignMap assignMap)
        {
            List<byte> playerArray = new List<byte>(Helpers.GetRandomArray(Game.GameData.data.AllPlayers.Keys));
            playerArray.RemoveAll((id) => { return !Game.GameData.data.playersArray[id].role.CanBeDrunk; });

            int leftPlayers = RoleCountOption.getSelection();

            float probability = (float)RoleChanceOption.getSelection() / 10;

            while (leftPlayers > 0)
            {
                if (playerArray.Count == 0) break;

                if (NebulaPlugin.rnd.NextDouble() < probability)
                {
                    //割り当て
                }
                leftPlayers--;
            }
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);
            RPCEvents.EmitSpeedFactor(__instance.PlayerId, new Game.SpeedFactor(0, 99999f, -1f, true));
        }

        public override void EditDisplayRoleName(byte playerId, ref string roleName)
        {
            if (Game.GameData.data.myData.CanSeeEveryoneInfo) EditDisplayRoleNameForcely(playerId, ref roleName);
        }

        public override void EditDisplayRoleNameForcely(byte playerId,ref string roleName)
        {
            var role = Game.GameData.data.GetPlayerData(playerId).role;
            string shortText = Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".short"));
            roleName = Helpers.cs(role.side.color, Language.Language.GetString("side." + role.side.localizeSide + ".name")) +" " + Helpers.cs(new Color(0.6f, 0.6f, 0.6f), $"({shortText})");
        }

        public override void LoadOptionData()
        {
            
        }

        public Secret() : base("Secret", "secret", RoleColor, 1)
        {
        }
    }
}
