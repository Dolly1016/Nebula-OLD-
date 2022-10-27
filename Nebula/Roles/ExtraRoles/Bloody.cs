using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles
{
    public class Bloody : ExtraRole
    {
        static public Color RoleColor = new Color(180f / 255f, 0f / 255f, 0f / 255f);

        public override void Assignment(Patches.AssignMap assignMap)
        {

        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);
        }

        public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
        {
            bool showFlag = false;
            if (playerId == PlayerControl.LocalPlayer.PlayerId || Game.GameData.data.myData.CanSeeEveryoneInfo) showFlag = true;

            if (showFlag) EditDisplayNameForcely(playerId, ref displayName);
        }


        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    RoleColor, "†");
        }

        public override void LoadOptionData()
        {

        }

        public Bloody() : base("Bloody", "bloody", RoleColor, 0)
        {
        }
    }
}
