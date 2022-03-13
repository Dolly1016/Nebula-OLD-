using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nebula.Patches
{
    [Harmony]
    public class AdminPatch
    {
        static float adminTimer = 0f;
        static TMPro.TextMeshPro OutOfTime;
        static TMPro.TextMeshPro TimeRemaining;
        static bool clearedIcons = false;

        public static void ResetData()
        {
            adminTimer = 0f;
            if (TimeRemaining != null)
            {
                UnityEngine.Object.Destroy(TimeRemaining);
                TimeRemaining = null;
            }

            if (OutOfTime != null)
            {
                UnityEngine.Object.Destroy(OutOfTime);
                OutOfTime = null;
            }
        }

        static void UseAdminTime()
        {
            if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.AdminLimitOption.getFloat() > 0f && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                RPCEventInvoker.UpdateAdminRestrictTimer(adminTimer);
            }
            adminTimer = 0f;
        }

        [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.CanUse))]
        public static class MapConsoleCanUsePatch
        {
            public static bool Prefix(ref float __result, MapConsole __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
            {
                // temp fix for the admin bug on airship
                if (PlayerControl.GameOptions.MapId == 4)
                    __instance.useIcon = ImageNames.PolusAdminButton;

                canUse = couldUse = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnEnable))]
        class MapCountOverlayOnEnablePatch
        {
            static void Prefix(MapCountOverlay __instance)
            {
                adminTimer = 0f;
            }
        }


        [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnDisable))]
        class MapCountOverlayOnDisablePatch
        {
            static void Prefix(MapCountOverlay __instance)
            {
                UseAdminTime();
            }
        }

        [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
        class MapCountOverlayUpdatePatch
        {

            static bool Prefix(MapCountOverlay __instance)
            {
                if (CustomOptionHolder.DevicesOption.getBool())
                {
                    adminTimer += Time.deltaTime;
                    if (adminTimer > 0.1f)
                        UseAdminTime();


                    if (OutOfTime == null)
                    {
                        OutOfTime = UnityEngine.Object.Instantiate(__instance.SabotageText, __instance.SabotageText.transform.parent);
                        OutOfTime.text = Language.Language.GetString("game.device.restrictOutOfTime");
                    }

                    if (TimeRemaining == null)
                    {
                        TimeRemaining = UnityEngine.Object.Instantiate(HudManager.Instance.TaskText, __instance.transform);
                        TimeRemaining.alignment = TMPro.TextAlignmentOptions.BottomRight;
                        TimeRemaining.transform.position = Vector3.zero;
                        TimeRemaining.transform.localPosition = new Vector3(3.25f, 5.25f);
                        TimeRemaining.transform.localScale *= 2f;
                        TimeRemaining.color = Palette.White;
                    }

                    if (Game.GameData.data.UtilityTimer.AdminTimer <= 0f)
                    {
                        __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
                        OutOfTime.gameObject.SetActive(true);
                        TimeRemaining.gameObject.SetActive(false);
                        if (clearedIcons == false)
                        {
                            foreach (CounterArea ca in __instance.CountAreas) ca.UpdateCount(0);
                            clearedIcons = true;
                        }
                        return false;
                    }

                    clearedIcons = false;
                    OutOfTime.gameObject.SetActive(false);
                    string timeString = TimeSpan.FromSeconds(Game.GameData.data.UtilityTimer.AdminTimer).ToString(@"mm\:ss\.f");
                    TimeRemaining.text = Language.Language.GetString("game.device.timeRemaining").Replace("%TIMER%",timeString);
                    TimeRemaining.gameObject.SetActive(true);
                }
                
                return true;
            }
        }
    }
}
