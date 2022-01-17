using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.NeutralRoles
{
    public class Jester : Template.Draggable
    {
        static public Color Color = new Color(253f / 255f, 84f / 255f, 167f / 255f);

        public bool WinTrigger=false;

        private Module.CustomOption canUseVentsOption;

        public override bool OnExiledPost(byte[] voters,byte playerId)
        {
            WinTrigger = true;
            return false;    
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            WinTrigger = false;
            canMoveInVents = canUseVents = canUseVentsOption.getBool();
        }

        public override void LoadOptionData()
        {
            canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
        }

        public Jester()
            : base("Jester", "jester", Color, RoleCategory.Neutral, Side.Jester, Side.Jester,
                 new HashSet<Side>() { Side.Jester }, new HashSet<Side>() { Side.Jester },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.JesterWin },
                 true, true, true, false, false)
        { 
        }
    }
}