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
        [HarmonyPatch(typeof(LightSource), nameof(LightSource.Update))]
        public static class DebugLightPatch
        {
            public static bool Prefix(LightSource __instance)
            {
                __instance.transform.localPosition = new Vector3(0f, -0.2f);

                return true;
            }
        }
    }
    
}
