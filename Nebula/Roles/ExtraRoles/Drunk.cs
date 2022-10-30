using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles
{
    public class Drunk : Template.StandardExtraRole
    {
        static public Color RoleColor = new Color(133f / 255f, 161f / 255f, 190f / 255f);

        protected override bool IsAssignableTo(Role role) => role.CanBeDrunk;

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);
            RPCEvents.EmitSpeedFactor(__instance.PlayerId, new Game.SpeedFactor(0, 99999f, -1f, true));
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    RoleColor, "〻");
        }

        public Drunk() : base("Drunk", "drunk", RoleColor,1)
        {
        }
    }
}
