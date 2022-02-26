using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{
    /*
    [HarmonyPatch]
    class LightPatch
    {
        [HarmonyPatch(typeof(LightSource), nameof(LightSource.Update))]
        public static class DebugLightPatch
        {
            private static bool inPrefix = false;
            public static bool Prefix(LightSource __instance)
            {
                if (inPrefix) return true;

                inPrefix = true;


                __instance.transform.localPosition = new Vector3(2f,0.5f); ;
                __instance.Update();

                __instance.transform.localPosition = new Vector3(-2f, 0.5f); ;
                //__instance.Update();

                return false;
            }
        }
    }
    */
}
