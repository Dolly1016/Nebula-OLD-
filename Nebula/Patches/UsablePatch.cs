using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    class KillButtonDoClickPatch
    {
        public static bool Prefix(KillButton __instance)
        {
            if (__instance.isActiveAndEnabled && __instance.currentTarget && !__instance.isCoolingDown && !PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.CanMove)
            {
                Helpers.MurderAttemptResult res = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, __instance.currentTarget);
                __instance.SetTarget(null);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerTask), nameof(PlayerTask.Initialize))]
    class SabotageTaskRemovePatch
    {
        static void Postfix(PlayerTask __instance)
        {
            if(__instance is SabotageTask)
            EmergencyPatch.SabotageUpdate();
        }
    }
}
