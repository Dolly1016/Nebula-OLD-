using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;
using Nebula.Patches;

namespace Nebula.Roles.NeutralRoles
{
    public class Opportunist : Template.ExemptTasks
    {
        static public Color RoleColor = new Color(106f / 255f, 252f / 255f, 45f / 255f);


        private Module.CustomOption canUseVentsOption;
        private Module.CustomOption ventCoolDownOption;
        private Module.CustomOption ventDurationOption;
        private Module.CustomOption canWinWithArsonistOption;
        private Module.CustomOption canWinWithEmpiricOption;
        private Module.CustomOption canWinWithJesterOption;
        private Module.CustomOption canWinWithVultureOption;
        private Module.CustomOption canWinWithAvengerOption;

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);

            canMoveInVents = canUseVentsOption.getBool();
            VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseLimittedVent : VentPermission.CanNotUse;
        }

        public override void LoadOptionData()
        {
            base.LoadOptionData();

            canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
            ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f);
            ventCoolDownOption.suffix = "second";
            ventCoolDownOption.AddPrerequisite(canUseVentsOption);
            ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f);
            ventDurationOption.suffix = "second";
            ventDurationOption.AddPrerequisite(canUseVentsOption);

            canWinWithArsonistOption = CreateOption(Color.white, "canWinWithArsonist", true);
            canWinWithEmpiricOption = CreateOption(Color.white, "canWinWithEmpiric", true);
            canWinWithJesterOption = CreateOption(Color.white, "canWinWithJester", true);
            canWinWithVultureOption = CreateOption(Color.white, "canWinWithVulture", true);
            canWinWithAvengerOption = CreateOption(Color.white, "canWinWithAvenger", true);
        }

        public override void Initialize(PlayerControl __instance)
        {
            VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
            VentDurationMaxTimer = ventDurationOption.getFloat();
        }
        public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
        {
            if (player.Data.IsDead && player.GetModData().FinalData?.status != Game.PlayerData.PlayerStatus.Burned) return false;
            if (condition == EndCondition.ArsonistWin && !canWinWithArsonistOption.getBool()) return false;
            if (condition == EndCondition.EmpiricWin && !canWinWithEmpiricOption.getBool()) return false;
            if (condition == EndCondition.JesterWin && !canWinWithJesterOption.getBool()) return false;
            if (condition == EndCondition.VultureWin && !canWinWithVultureOption.getBool()) return false;
            if (condition == EndCondition.AvengerWin && !canWinWithAvengerOption.getBool()) return false;
            if (condition == EndCondition.NobodySkeldWin) return false;
            if (condition == EndCondition.NobodyMiraWin) return false;
            if (condition == EndCondition.NobodyPolusWin) return false;
            if (condition == EndCondition.NobodyAirshipWin) return false;

            if (player.GetModData().Tasks.AllTasks <= player.GetModData().Tasks.Completed)
            {
                EndGameManagerSetUpPatch.AddEndText(Language.Language.GetString("role.opportunist.additionalEndText"));
                return true;
            }
            
            return false;
        }

        public Opportunist()
            : base("Opportunist", "opportunist", RoleColor, RoleCategory.Neutral, Side.Opportunist, Side.Opportunist,
                 new HashSet<Side>() { Side.Opportunist }, new HashSet<Side>() { Side.Opportunist },
                 new HashSet<Patches.EndCondition>(),
                 true, VentPermission.CanUseLimittedVent, true, false, false)
        {
            FakeTaskIsExecutable = true;
            VentColor = RoleColor;
        }
    }
}
