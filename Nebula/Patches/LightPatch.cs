using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using BepInEx.IL2CPP.Utils.Collections;
using UnityEngine.Rendering;

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

                var data = Game.GameData.data.myData.getGlobalData();
                if (data == null) return;

                __result |= data.role.UseImpostorLightRadius;
            }
        }

        [HarmonyPatch(typeof(ShadowCollab), nameof(ShadowCollab.OnEnable))]
        public static class ShadowCameraPatch
        {
            static public IEnumerator GetEnumerator(ShadowCollab __instance)
            {
                Camera cam = Camera.main;
                while (true)
                {
                    __instance.ShadowCamera.orthographicSize = cam.orthographicSize;
                    __instance.ShadowQuad.transform.localScale = new Vector3(cam.orthographicSize * cam.aspect, cam.orthographicSize) * 2f;
                    yield return null;
                }
            }

            public static void Prefix(ShadowCollab __instance)
            {
                __instance.StartCoroutine(GetEnumerator(__instance).WrapToIl2Cpp());
            }
        }

    }
}
