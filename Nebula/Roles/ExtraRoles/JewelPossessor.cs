using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles
{
    public class JewelPossessor : ExtraRole
    {
        static public Color RoleColor = new Color(255f / 255f, 148f / 255f, 252f / 255f);

        public override void Assignment(Patches.AssignMap assignMap)
        {
            
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    RoleColor, "💎");
        }

        public override void LoadOptionData()
        {
            
        }

        public JewelPossessor() : base("JewelPossessor", "jewelPossessor", RoleColor, 0)
        {
            IsHideRole = true;
        }
    }
}
