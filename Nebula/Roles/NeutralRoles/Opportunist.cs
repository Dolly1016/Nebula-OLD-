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
        static public Color Color = new Color(106f / 255f, 252f / 255f, 45f / 255f);


        private Module.CustomOption cutTasksOption;
        private Module.CustomOption canUseVentsOption;

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);

            canMoveInVents = canUseVents = canUseVentsOption.getBool();
        }

        public override void LoadOptionData()
        {
            base.LoadOptionData();

            canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
        }

        public override bool CheckWin(PlayerControl player, EndCondition condition)
        {
            if (player.Data.IsDead) return false;
            if (condition.TriggerRole!=null) return false;
            if (condition == EndCondition.NobodySkeldWin) return false;
            if (condition == EndCondition.NobodyMiraWin) return false;
            if (condition == EndCondition.NobodyPolusWin) return false;
            if (condition == EndCondition.NobodyAirshipWin) return false;

            if (player.GetModData().Tasks.AllTasks == player.GetModData().Tasks.Completed)
            {
                EndGameManagerSetUpPatch.AddEndText(Language.Language.GetString("role.opportunist.additionalEndText"));
                return true;
            }
            
            return false;
        }

        public Opportunist()
            : base("Opportunist", "opportunist", Color, RoleCategory.Neutral, Side.Opportunist, Side.Opportunist,
                 new HashSet<Side>() { Side.Opportunist }, new HashSet<Side>() { Side.Opportunist },
                 new HashSet<Patches.EndCondition>(),
                 true, true, true, false, false)
        {
            fakeTaskIsExecutable = true;
            ventColor = Color;
        }
    }
}
