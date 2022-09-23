using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace Nebula.Patches
{
    [HarmonyPatch]
    class MapBehaviorPatch
    {
        static public void UpdateMapSize(MapBehaviour __instance)
        {
            if (minimapFlag)
            {
                __instance.gameObject.transform.localScale = new Vector3(0.225f, 0.225f, 1f);
                __instance.gameObject.transform.localPosition = new Vector3(4.2f, 0f,-100f);
            }
            else
            {
                __instance.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                __instance.gameObject.transform.localPosition = new Vector3(0f, 0f, -25f);
            }

            
        }

        public static bool minimapFlag = false;

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Awake))]
        class MapBehaviourAwakePatch
        {
            static void Prefix(MapBehaviour __instance)
            {
                minimapFlag = false;
            }

        }

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.GenericShow))]
        class MapBehaviourShowNormalMapPatch
        {
            static void Prefix(MapBehaviour __instance)
            {
                CustomOverlays.Hide();
            }

        }
    }

    [HarmonyPatch]
    class ForcelyShowSabotagePatch
    {
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
        class MapBehaviourShowNormalMapPatch
        {
            static void Postfix(MapBehaviour __instance)
            {
                if (Game.GameData.data.myData.getGlobalData().role.CanInvokeSabotage && !MeetingHud.Instance)
                {
                    if (__instance.IsOpen)
                    {
                        __instance.infectedOverlay.gameObject.SetActive(true);
                        __instance.ColorControl.SetColor(Palette.ImpostorRed);
                        DestroyableSingleton<HudManager>.Instance.SetHudActive(false);
                        ConsoleJoystick.SetMode_Sabotage();
                        return;
                    }
                }


                if(__instance.IsOpen) DestroyableSingleton<HudManager>.Instance.SetHudActive(MapBehaviorPatch.minimapFlag);
                MapBehaviorPatch.UpdateMapSize(__instance);
            }

        }
    }

    [HarmonyPatch]
    class DontShowSabotagePatch
    {
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
        class MapBehaviourShowNormalMapPatch
        {
            static bool Prefix(MapBehaviour __instance)
            {
                if(Game.GameData.data.myData.getGlobalData().role.CanInvokeSabotage)
                    return true;
                else
                {
                    if (__instance.IsOpen)
                    {
                        __instance.Close();
                        return false;
                    }
                    if (!PlayerControl.LocalPlayer.CanMove && !MeetingHud.Instance)
                    {
                        return false;
                    }
                   
                    PlayerControl.LocalPlayer.SetPlayerMaterialColors(__instance.HerePoint);
                    __instance.GenericShow();
                    __instance.taskOverlay.Show();
                    __instance.ColorControl.SetColor(new Color(0.05f, 0.2f, 1f, 1f));
                    DestroyableSingleton<HudManager>.Instance.SetHudActive(MapBehaviorPatch.minimapFlag);
                    return false;
                }
            }

        }
    }

    [HarmonyPatch]
    class MapUpdatePatch
    {
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
        class MapBehaviourUpdatePatch
        {
            static void Postfix(MapBehaviour __instance)
            {
                Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (r) =>
                 {
                     r.MyMapUpdate(__instance);
                 });
            }

        }
    }
}
