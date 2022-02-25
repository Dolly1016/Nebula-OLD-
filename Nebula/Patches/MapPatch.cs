using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace Nebula.Patches
{
    [HarmonyPatch]
    class MapBehaviorPatch
    {
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.GenericShow))]
        class MapBehaviourShowNormalMapPatch
        {
            static void Prefix(MapBehaviour __instance)
            {
                CustomOverlays.Hide();
            }

        }
    }
}
