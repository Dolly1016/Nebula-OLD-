using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{

    [HarmonyPatch(typeof(AspectPosition), nameof(AspectPosition.OnEnable))]
    class AspectPositionPatch
    {
        //parentCamを 必ずしもMain Cameraではなく、自身を描画するカメラに設定するよう変更
        static public void Prefix(AspectPosition __instance)
        {
            if (__instance.gameObject.layer == LayerExpansion.GetUILayer() && HudManager.Instance)
                __instance.parentCam = HudManager.Instance.UICamera;
            else
                __instance.parentCam = Camera.main;
        }
    }
}
