using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles
{
    public class Drunk : ExtraRole
    {
        static public Color Color = new Color(133f / 255f, 161f / 255f, 190f / 255f);

        public override void Assignment(Patches.AssignMap assignMap)
        {
            int leftPlayers = RoleCountOption.getSelection();

            byte[] playerArray = Helpers.GetRandomArray(Game.GameData.data.players.Keys);
            float probability = (float)RoleChanceOption.getSelection() / 10;
            for(int i=0;i<playerArray.Length;i++)
            {
                if (i >= leftPlayers) break;

                if (NebulaPlugin.rnd.NextDouble() < probability)
                {
                    assignMap.Assign(playerArray[i], this.id, 0);
                }
                leftPlayers--;
            }
        }

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);

            RPCEventInvoker.EmitSpeedFactor(__instance, new Game.SpeedFactor(0, 99999f, -1f, true));
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    Color, "〻");
        }

        public Drunk() : base("Drunk", "drunk", Color,1)
        {
        }
    }
}
