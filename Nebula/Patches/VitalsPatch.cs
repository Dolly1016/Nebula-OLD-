using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Hazel;

namespace Nebula.Patches
{
    [HarmonyPatch]
    class VitalsPatch
    {
        [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Begin))]
        class VitalsMinigameStartPatch
        {
            static void Postfix(VitalsMinigame __instance)
            {
                Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, role => role.OnVitalsOpen(__instance));
            }
        }

        [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
        class VitalsMinigameUpdatePatch
        {
            static void Postfix(VitalsMinigame __instance)
            {
                Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, role => role.VitalsUpdate(__instance));
            }
        }
    }

}
