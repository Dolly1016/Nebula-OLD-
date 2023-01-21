namespace Nebula.Patches;

[HarmonyPatch]
class VitalsPatch
{
    static float vitalsTimer = 0f;
    static TMPro.TextMeshPro TimeRemaining;
    static TMPro.TextMeshPro OutOfTime;


    public static void ResetData()
    {
        vitalsTimer = 0f;
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

    static void UseVitalsTime()
    {
        if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.VitalsLimitOption.getBool() && !PlayerControl.LocalPlayer.Data.IsDead)
        {
            RPCEventInvoker.UpdateVitalsRestrictTimer(vitalsTimer);
        }
        vitalsTimer = 0f;
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Begin))]
    class VitalsMinigameStartPatch
    {
        static void Postfix(VitalsMinigame __instance)
        {
            vitalsTimer = 0f;

            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, role => role.OnVitalsOpen(__instance));
        }
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    class VitalsMinigameUpdatePatch
    {
        static bool Prefix(VitalsMinigame __instance)
        {
            if (!__instance.BatteryText.IsActive())
            {
                //Doctorのバイタルは除外する

                vitalsTimer += Time.deltaTime;
                if (vitalsTimer > 0.1f)
                    UseVitalsTime();

                if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.VitalsLimitOption.getBool())
                {
                    if (TimeRemaining == null)
                    {
                        TimeRemaining = UnityEngine.Object.Instantiate(HudManager.Instance.TaskPanel.taskText, __instance.transform);
                        TimeRemaining.alignment = TMPro.TextAlignmentOptions.BottomRight;
                        TimeRemaining.transform.position = Vector3.zero;
                        TimeRemaining.transform.localPosition = new Vector3(1.7f, 4.45f);
                        TimeRemaining.transform.localScale *= 1.8f;
                        TimeRemaining.color = Palette.White;
                    }

                    if (OutOfTime == null)
                    {
                        OutOfTime = UnityEngine.Object.Instantiate(__instance.SabText, __instance.SabText.transform.parent);
                        OutOfTime.text = Language.Language.GetString("game.device.restrictOutOfTime");
                    }

                    if (Game.GameData.data.UtilityTimer.VitalsTimer <= 0f)
                    {
                        OutOfTime.gameObject.SetActive(true);
                        TimeRemaining.gameObject.SetActive(false);
                        for (int i = 0; i < __instance.vitals.Length; i++)
                        {
                            __instance.vitals[i].gameObject.SetActive(false);
                        }
                        return false;
                    }

                    OutOfTime.gameObject.SetActive(false);
                    string timeString = TimeSpan.FromSeconds(Game.GameData.data.UtilityTimer.VitalsTimer).ToString(@"mm\:ss\.f");
                    TimeRemaining.text = Language.Language.GetString("game.device.timeRemaining").Replace("%TIMER%", timeString);
                    TimeRemaining.gameObject.SetActive(true);
                }
            }

            return true;
        }

        static void Postfix(VitalsMinigame __instance)
        {
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, role => role.VitalsUpdate(__instance));
        }
    }
}
