using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{
    
    [HarmonyPatch]
    class LightPatch
    {
        [HarmonyPatch(typeof(OneWayShadows), nameof(OneWayShadows.IsIgnored))]
        public static class OneWayShadowsPatch
        {
            public static void Postfix(OneWayShadows __instance, ref bool __result)
            {
                if (Game.GameData.data == null) return;
                if (!PlayerControl.LocalPlayer) return;

                var data = PlayerControl.LocalPlayer.GetModData();
                if (data == null) return;

                __result |= data.role.UseImpostorLightRadius;
            }
        }

    }
    
}
