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
    public class Jester : Template.Draggable , Template.HasWinTrigger
    {
        static public Color Color = new Color(253f / 255f, 84f / 255f, 167f / 255f);

        public bool WinTrigger { get; set; } = false;
        public byte Winner { get; set; } = Byte.MaxValue;

        private Module.CustomOption canUseVentsOption;
        private Module.CustomOption canInvokeSabotageOption;

        public override bool OnExiledPost(byte[] voters,byte playerId)
        {
            if (playerId == PlayerControl.LocalPlayer.PlayerId)
                RPCEventInvoker.WinTrigger(this);

            return false;    
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            WinTrigger = false;
            canMoveInVents = canUseVents = canUseVentsOption.getBool();
            canInvokeSabotage = canInvokeSabotageOption.getBool();
        }

        public override void LoadOptionData()
        {
            canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
            canInvokeSabotageOption = CreateOption(Color.white, "canInvokeSabotage", true);
        }

        public Jester()
            : base("Jester", "jester", Color, RoleCategory.Neutral, Side.Jester, Side.Jester,
                 new HashSet<Side>() { Side.Jester }, new HashSet<Side>() { Side.Jester },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.JesterWin },
                 true, true, true, false, false)
        {
            Patches.EndCondition.JesterWin.TriggerRole = this;
        }
    }
}