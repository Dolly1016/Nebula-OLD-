namespace Nebula.Patches;

[Harmony]
public class CameraPatch
{
    static float cameraTimer = 0f;

    public static void ResetData()
    {
        cameraTimer = 0f;
        SurveillanceMinigamePatch.ResetData();
        PlanetSurveillanceMinigamePatch.ResetData();
    }

    static void UseCameraTime()
    {
        if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.CameraAndDoorLogLimitOption.getBool() && !PlayerControl.LocalPlayer.Data.IsDead)
        {
            RPCEventInvoker.UpdateCameraAndDoorLogRestrictTimer(cameraTimer);
        }
        cameraTimer = 0f;
    }

    [HarmonyPatch]
    class SurveillanceMinigamePatch
    {
        private static int page = 0;
        private static float timer = 0f;
        static TMPro.TextMeshPro TimeRemaining;
        static List<TMPro.TextMeshPro> OutOfTime = new List<TMPro.TextMeshPro>();

        public static void ResetData()
        {
            if (TimeRemaining != null)
            {
                if (TimeRemaining) UnityEngine.Object.Destroy(TimeRemaining);
                TimeRemaining = null;
            }
            foreach (var Text in OutOfTime)
            {
                if (Text) UnityEngine.Object.Destroy(Text);
            }
            OutOfTime.Clear();
        }

        [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Begin))]
        class SurveillanceMinigameBeginPatch
        {
            public static void Prefix(SurveillanceMinigame __instance)
            {
                cameraTimer = 0f;
                ResetData();
            }
        }

        [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
        class SurveillanceMinigameUpdatePatch
        {
            public static bool Prefix(SurveillanceMinigame __instance)
            {
                cameraTimer += Time.deltaTime;
                if (cameraTimer > 0.1f)
                    UseCameraTime();

                if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.CameraAndDoorLogLimitOption.getBool())
                {
                    if (TimeRemaining == null)
                    {
                        TimeRemaining = UnityEngine.Object.Instantiate(HudManager.Instance.TaskPanel.taskText, __instance.transform);
                        TimeRemaining.text = "";
                        TimeRemaining.alignment = TMPro.TextAlignmentOptions.Center;
                        TimeRemaining.transform.position = Vector3.zero;
                        TimeRemaining.transform.localPosition = new Vector3(0.0f, -1.7f);
                        TimeRemaining.transform.localScale *= 1.8f;
                        TimeRemaining.color = Palette.White;
                    }

                    if (OutOfTime.Count == 0)
                    {
                        foreach (var sabText in __instance.SabText)
                        {
                            if (sabText)
                            {
                                var text = UnityEngine.Object.Instantiate(sabText, sabText.transform.parent);
                                text.text = Language.Language.GetString("game.device.restrictOutOfTime");
                                OutOfTime.Add(text);
                            }
                        }
                    }

                    if (Game.GameData.data.UtilityTimer.CameraTimer <= 0f)
                    {
                        TimeRemaining.gameObject.SetActive(false);
                        __instance.isStatic = true;
                        for (int i = 0; i < __instance.ViewPorts.Length; i++)
                        {
                            if (CustomOptionHolder.UnlimitedCameraSkeldOption.getSelection() - 1 == i)
                            {
                                if (!PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
                                {
                                    __instance.ViewPorts[i].sharedMaterial = __instance.DefaultMaterial;
                                    __instance.ViewPorts[i].material.SetTexture("_MainTex", __instance.textures[i]);
                                    OutOfTime[i].gameObject.SetActive(false);
                                }
                                else
                                {
                                    __instance.ViewPorts[i].sharedMaterial = __instance.StaticMaterial;
                                    __instance.SabText[i].gameObject.SetActive(true);
                                }
                            }
                            else
                            {
                                __instance.ViewPorts[i].sharedMaterial = __instance.StaticMaterial;
                                OutOfTime[i].gameObject.SetActive(true);
                                __instance.SabText[i].gameObject.SetActive(false);
                            }
                        }

                        return false;
                    }

                    foreach (var text in OutOfTime)
                        text.gameObject.SetActive(false);
                    string timeString = TimeSpan.FromSeconds(Game.GameData.data.UtilityTimer.CameraTimer).ToString(@"mm\:ss\.f");
                    TimeRemaining.text = Language.Language.GetString("game.device.timeRemaining").Replace("%TIMER%", timeString);
                    TimeRemaining.gameObject.SetActive(true);

                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Close))]
        class SurveillanceMinigameClosePatch
        {
            static void Prefix(SurveillanceMinigame __instance)
            {
                UseCameraTime();
            }
        }
    }

    [HarmonyPatch]
    class PlanetSurveillanceMinigamePatch
    {
        static TMPro.TextMeshPro TimeRemaining;
        static TMPro.TextMeshPro OutOfTime;

        public static void ResetData()
        {
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

        [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Begin))]
        class PlanetSurveillanceMinigameBeginPatch
        {
            public static void Prefix(PlanetSurveillanceMinigame __instance)
            {
                cameraTimer = 0f;
            }
        }

        [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
        class PlanetSurveillanceMinigameUpdatePatch
        {
            public static bool Prefix(PlanetSurveillanceMinigame __instance)
            {
                cameraTimer += Time.deltaTime;
                if (cameraTimer > 0.1f)
                    UseCameraTime();

                if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.CameraAndDoorLogLimitOption.getBool())
                {
                    if (OutOfTime == null)
                    {
                        OutOfTime = UnityEngine.Object.Instantiate(__instance.SabText, __instance.SabText.transform.parent);
                        OutOfTime.text = Language.Language.GetString("game.device.restrictOutOfTime");
                    }

                    if (TimeRemaining == null)
                    {
                        TimeRemaining = UnityEngine.Object.Instantiate(HudManager.Instance.TaskPanel.taskText, __instance.transform);
                        TimeRemaining.alignment = TMPro.TextAlignmentOptions.BottomRight;
                        TimeRemaining.transform.position = Vector3.zero;
                        TimeRemaining.transform.localPosition = new Vector3(0.95f, 4.45f);
                        TimeRemaining.transform.localScale *= 1.8f;
                        TimeRemaining.color = Palette.White;
                    }

                    if (Game.GameData.data.UtilityTimer.CameraTimer <= 0f)
                    {
                        OutOfTime.gameObject.SetActive(true);
                        TimeRemaining.gameObject.SetActive(false);

                        if ((CustomOptionHolder.GetUnlimitedCameraOption()?.getSelection() ?? 1) - 1 == __instance.currentCamera)
                        {
                            OutOfTime.gameObject.SetActive(false);

                            if (PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
                            {
                                __instance.SabText.gameObject.SetActive(true);

                                __instance.isStatic = true;
                                __instance.ViewPort.sharedMaterial = __instance.StaticMaterial;
                            }
                            else
                            {
                                __instance.SabText.gameObject.SetActive(false);
                                __instance.isStatic = false;
                                __instance.ViewPort.sharedMaterial = __instance.DefaultMaterial;
                                __instance.ViewPort.material.SetTexture("_MainTex", __instance.texture);
                            }
                        }
                        else
                        {
                            __instance.SabText.gameObject.SetActive(false);

                            __instance.isStatic = true;
                            __instance.ViewPort.sharedMaterial = __instance.StaticMaterial;
                        }

                        return false;
                    }

                    OutOfTime.gameObject.SetActive(false);
                    string timeString = TimeSpan.FromSeconds(Game.GameData.data.UtilityTimer.CameraTimer).ToString(@"mm\:ss\.f");
                    TimeRemaining.text = Language.Language.GetString("game.device.timeRemaining").Replace("%TIMER%", timeString);
                    TimeRemaining.gameObject.SetActive(true);
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Close))]
        class PlanetSurveillanceMinigameClosePatch
        {
            public static void Prefix(PlanetSurveillanceMinigame __instance)
            {
                UseCameraTime();
            }
        }

        [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.NextCamera))]
        class PlanetSurveillanceMinigamePulseStaticPatch
        {
            public static bool Prefix(PlanetSurveillanceMinigame __instance, [HarmonyArgument(0)] int direction)
            {
                if (!CustomOptionHolder.DevicesOption.getBool()) return true;
                if (Game.GameData.data.UtilityTimer.CameraTimer > 0f) return true;

                if (direction != 0 && Constants.ShouldPlaySfx())
                {
                    SoundManager.Instance.PlaySound(__instance.ChangeSound, false, 1f);
                }
                __instance.Dots[__instance.currentCamera].sprite = __instance.DotDisabled;

                if ((__instance.currentCamera + direction) >= 0)
                {
                    __instance.currentCamera = (__instance.currentCamera + direction) % __instance.survCameras.Length;
                }
                else
                {
                    int self = __instance.currentCamera + direction;
                    int max = __instance.survCameras.Length;
                    __instance.currentCamera = (self + -(self / max) * max + max) % max;
                }

                __instance.Dots[__instance.currentCamera].sprite = __instance.DotEnabled;
                SurvCamera survCamera = __instance.survCameras[__instance.currentCamera];
                __instance.Camera.transform.position = survCamera.transform.position + __instance.survCameras[__instance.currentCamera].Offset;
                __instance.LocationName.text = ((survCamera.NewName > StringNames.ExitButton) ? DestroyableSingleton<TranslationController>.Instance.GetString(survCamera.NewName, new Il2CppReferenceArray<Il2CppSystem.Object>(0)) : survCamera.CamName);
                if (!PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
                {
                    __instance.ViewPort.sharedMaterial = __instance.StaticMaterial;
                    __instance.ViewPort.material.SetTexture("_MainTex", null);
                }
                return false;
            }
        }
    }

    [HarmonyPatch]
    class DoorLogPatch
    {
        static TMPro.TextMeshPro TimeRemaining;
        static TMPro.TextMeshPro OutOfTime;

        public static void ResetData()
        {
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

        [HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
        class SecurityLogGameBeginPatch
        {
            public static void Prefix(Minigame __instance)
            {
                if (__instance is SecurityLogGame)
                    cameraTimer = 0f;
            }
        }

        [HarmonyPatch(typeof(SecurityLogGame), nameof(SecurityLogGame.Update))]
        class SecurityLogGameUpdatePatch
        {
            public static bool Prefix(SecurityLogGame __instance)
            {
                cameraTimer += Time.deltaTime;
                if (cameraTimer > 0.1f)
                    UseCameraTime();

                if (CustomOptionHolder.DevicesOption.getBool() && CustomOptionHolder.CameraAndDoorLogLimitOption.getBool())
                {
                    if (OutOfTime == null)
                    {
                        OutOfTime = UnityEngine.Object.Instantiate(__instance.SabText, __instance.SabText.transform.parent);
                        OutOfTime.text = Language.Language.GetString("game.device.restrictOutOfTime");
                    }

                    if (TimeRemaining == null)
                    {
                        TimeRemaining = UnityEngine.Object.Instantiate(HudManager.Instance.TaskPanel.taskText, __instance.transform);
                        TimeRemaining.alignment = TMPro.TextAlignmentOptions.BottomRight;
                        TimeRemaining.transform.position = Vector3.zero;
                        TimeRemaining.transform.localPosition = new Vector3(1.0f, 4.25f);
                        TimeRemaining.transform.localScale *= 1.6f;
                        TimeRemaining.color = Palette.White;
                    }

                    if (Game.GameData.data.UtilityTimer.CameraTimer <= 0f)
                    {
                        OutOfTime.gameObject.SetActive(true);
                        TimeRemaining.gameObject.SetActive(false);
                        __instance.EntryPool.ReclaimAll();

                        return false;
                    }

                    OutOfTime.gameObject.SetActive(false);
                    string timeString = TimeSpan.FromSeconds(Game.GameData.data.UtilityTimer.CameraTimer).ToString(@"mm\:ss\.f");
                    TimeRemaining.text = Language.Language.GetString("game.device.timeRemaining").Replace("%TIMER%", timeString);
                    TimeRemaining.gameObject.SetActive(true);
                }

                return true;
            }
        }


        [HarmonyPatch(typeof(Minigame), nameof(Minigame.Close), new Type[0])]
        class SecurityLogGameClosePatch
        {

            static void Prefix(Minigame __instance)
            {
                if (__instance is SecurityLogGame)
                    UseCameraTime();
            }
        }
    }
}