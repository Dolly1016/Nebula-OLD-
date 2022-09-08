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
                    }
                }
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
                    DestroyableSingleton<HudManager>.Instance.SetHudActive(false);
                    return false;
                }
            }

        }
    }

    [HarmonyPatch]
    class MapUpdatePatch
    {
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
        class MapBehaviourShowNormalMapPatch
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
