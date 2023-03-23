using TMPro;
using PowerTools;
using UnityEngine.Playables;

namespace Nebula.Patches;

[HarmonyPatch]
class PrespawnPatch
{
    private static PassiveButton? selected = null;

    [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.Close))]
    public static class PrespawnSpawnClosePatch
    {
        public static bool closeForcelyFlag = false;
        public static bool Prefix(SpawnInMinigame __instance)
        {
            if (closeForcelyFlag)
            {
                closeForcelyFlag = false;
                return true;
            }
            return !CustomOptionHolder.mapOptions.getBool() || !CustomOptionHolder.synchronizedSpawning.getBool();
        }
    }

    [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt))]
    public static class PrespawnSpawnAtPatch
    {
        public static bool SpawnAt(SpawnInMinigame __instance, Vector2 spawnAt)
        {
            if (!CustomOptionHolder.mapOptions.getBool() || !CustomOptionHolder.synchronizedSpawning.getBool())
            {
                if (__instance.amClosing != Minigame.CloseState.None)
                {
                    return true;
                }
                __instance.gotButton = true;
                PlayerControl.LocalPlayer.gameObject.SetActive(true);
                __instance.StopAllCoroutines();
                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(spawnAt);
                DestroyableSingleton<HudManager>.Instance.PlayerCam.SnapToTarget();
                __instance.Close();

                return true;
            }

            RPCEventInvoker.Synchronize(Game.SynchronizeTag.PreSpawnMinigame, PlayerControl.LocalPlayer.PlayerId);
            if (__instance.amClosing != Minigame.CloseState.None)
            {
                return false;
            }
            if (__instance.gotButton) return false;

            __instance.gotButton = true;


            foreach (var button in __instance.LocationButtons)
            {
                button.enabled = false;
            }

            __instance.StartCoroutine(Effects.Lerp(10f, (Il2CppSystem.Action<float>)((p) =>
            {
                float time = p * 10f;


                foreach (var button in __instance.LocationButtons)
                {
                    if (selected == button)
                    {
                        if (time > 0.3f)
                        {
                            float x = button.transform.localPosition.x;
                            if (x < 0f) x += 10f * Time.deltaTime;
                            if (x > 0f) x -= 10f * Time.deltaTime;
                            if (Mathf.Abs(x) < 10f * Time.deltaTime) x = 0f;
                            button.transform.localPosition = new Vector3(x, button.transform.localPosition.y, button.transform.localPosition.z);
                        }
                    }
                    else
                    {
                        var color = button.GetComponent<SpriteRenderer>().color;
                        float a = color.a;
                        if (a > 0f) a -= 2f * Time.deltaTime;
                        if (a < 0f) a = 0f;
                        button.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, a);
                        button.GetComponentInChildren<TextMeshPro>().color = new Color(1f, 1f, 1f, a);
                    }

                    if (__instance.amClosing != Minigame.CloseState.None) return;

                    if (Game.GameData.data.SynchronizeData.Align(Game.SynchronizeTag.PreSpawnMinigame, false,true,false) || p == 1f)
                    {
                        PlayerControl.LocalPlayer.gameObject.SetActive(true);
                        __instance.StopAllCoroutines();
                        PrespawnSpawnClosePatch.closeForcelyFlag = true;
                        __instance.Close();
                        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(spawnAt);
                        HudManager.Instance.PlayerCam.SnapToTarget();
                    }
                }

            })));

            return false;
        }
    }

    [HarmonyPatch(typeof(SpawnInMinigame._RunTimer_d__10), nameof(SpawnInMinigame._RunTimer_d__10.MoveNext))]
    public static class PrespawnTextPatch
    {
        public static void Postfix(SpawnInMinigame._RunTimer_d__10 __instance)
        {
            if (!CustomOptionHolder.mapOptions.getBool() || !CustomOptionHolder.synchronizedSpawning.getBool()) return;

            if (selected != null)
                __instance.__4__this.Text.text = Language.Language.GetString("game.minigame.waitSpawning");
        }
    }

    [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.Begin))]
    public static class PrespawnBeginPatch
    {
        public static bool Prefix(SpawnInMinigame __instance)
        {
            SpawnInMinigame.Instance = __instance;

            if (GameOptionsManager.Instance.CurrentGameOptions.MapId != 4) return true;

            SpawnInMinigame.SpawnLocation[] array = Enumerable.ToArray<SpawnInMinigame.SpawnLocation>(__instance.Locations);
            array = array.OrderBy((i) => Guid.NewGuid()).ToArray();

            array = Enumerable.ToArray<SpawnInMinigame.SpawnLocation>(Enumerable.ThenByDescending<SpawnInMinigame.SpawnLocation, float>(Enumerable.OrderBy<SpawnInMinigame.SpawnLocation, float>(Enumerable.Take<SpawnInMinigame.SpawnLocation>(array, __instance.LocationButtons.Length), (SpawnInMinigame.SpawnLocation s) => s.Location.x), (SpawnInMinigame.SpawnLocation s) => s.Location.y));
            for (int i = 0; i < __instance.LocationButtons.Length; i++)
            {
                PassiveButton passiveButton = __instance.LocationButtons[i];
                SpawnInMinigame.SpawnLocation pt = array[i];
                passiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    PrespawnSpawnAtPatch.SpawnAt(__instance, pt.Location);
                }));
                passiveButton.GetComponent<SpriteAnim>().Stop();
                passiveButton.GetComponent<SpriteRenderer>().sprite = pt.Image;
                passiveButton.GetComponentInChildren<TextMeshPro>().text = DestroyableSingleton<TranslationController>.Instance.GetString(pt.Name, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                ButtonAnimRolloverHandler component = passiveButton.GetComponent<ButtonAnimRolloverHandler>();
                component.StaticOutImage = pt.Image;
                component.RolloverAnim = pt.Rollover;
                component.HoverSound = (pt.RolloverSfx ? pt.RolloverSfx : __instance.DefaultRolloverSound);
            }
            PlayerControl.LocalPlayer.gameObject.SetActive(false);
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(new Vector2(-25f, 40f));
            __instance.StartCoroutine(__instance.RunTimer());
            ControllerManager.Instance.OpenOverlayMenu(__instance.name, null, __instance.DefaultButtonSelected, __instance.ControllerSelectable, false);
            PlayerControl.HideCursorTemporarily();
            ConsoleJoystick.SetMode_Menu();

            return false;
        }

        public static void Postfix(SpawnInMinigame __instance)
        {
            selected = null;

            if (!CustomOptionHolder.mapOptions.getBool() || !CustomOptionHolder.synchronizedSpawning.getBool()) return;

            foreach (var button in __instance.LocationButtons)
            {
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    if (selected == null)
                        selected = button;
                }
                ));
            }
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.PrespawnStep))]
    public static class PrespawnStepPatch
    {
        public static bool RequireCustomSpawnGame()
        {
            if (!CustomOptionHolder.mapOptions.getBool() || CustomOptionHolder.spawnMethod.getSelection() != 1) return false;

            if (Map.MapData.GetCurrentMapData().SpawnOriginalPositionAtFirst && !ExileController.Instance) return false;

            var spawnCandidates = Map.MapData.GetCurrentMapData().ValidSpawnCandidates;
            if (spawnCandidates.Count < 3) return false;

            return true;
        }

        public static bool ShouldSpawnRandomly()
        {
            return CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.spawnMethod.getSelection() == 2 && Map.MapData.GetCurrentMapData().ValidSpawnPoints.Count > 0;
        }

        public static bool ShouldSpawnWithAbnormalWay()
        {
            return CustomOptionHolder.mapOptions.getBool() && ((CustomOptionHolder.spawnMethod.getSelection() == 2 && Map.MapData.GetCurrentMapData().ValidSpawnPoints.Count > 0) || (CustomOptionHolder.spawnMethod.getSelection() == 1 && Map.MapData.GetCurrentMapData().ValidSpawnCandidates.Count >= 3));
        }

        public static IEnumerator GetEmptyEnumerator()
        {
            yield return null;
        }

        public static void Postfix(ShipStatus __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            if (RequireCustomSpawnGame())
            {

                var spawnCandidates = Map.MapData.GetCurrentMapData().ValidSpawnCandidates;
                if (spawnCandidates.Count < 3) return;

                SpawnInMinigame spawnInMinigame = UnityEngine.Object.Instantiate<SpawnInMinigame>(Map.MapData.MapDatabase[4].Assets.gameObject.GetComponent<AirshipStatus>().SpawnInGame);

                spawnInMinigame.transform.SetParent(Camera.main.transform, false);
                spawnInMinigame.transform.localPosition = new Vector3(0f, 0f, -600f);

                /* Begin (Minigame) */
                Minigame.Instance = spawnInMinigame;
                SpawnInMinigame.Instance = spawnInMinigame;
                spawnInMinigame.MyTask = null;
                spawnInMinigame.MyNormTask = null;

                if (PlayerControl.LocalPlayer)
                {
                    if (MapBehaviour.Instance)
                    {
                        MapBehaviour.Instance.Close();
                    }
                    PlayerControl.LocalPlayer.NetTransform.Halt();
                }
                spawnInMinigame.StartCoroutine(spawnInMinigame.CoAnimateOpen());
                /* Begin (Minigame) */

                /* Begin (SpawnInMinigame) */

                var randomArray = Helpers.GetRandomArray(spawnCandidates.Count);
                for (int i = 0; i < spawnInMinigame.LocationButtons.Length; i++)
                {
                    PassiveButton passiveButton = spawnInMinigame.LocationButtons[i];

                    int index = randomArray[i];

                    spawnCandidates[index].ReloadTexture();
                    
                    passiveButton.OnClick.RemoveAllListeners();
                    passiveButton.OnClick.AddListener(new System.Action(() =>
                    {
                        PrespawnSpawnAtPatch.SpawnAt(spawnInMinigame, spawnCandidates[index].SpawnLocation);
                    }));
                    passiveButton.OnMouseOver.AddListener(new System.Action(() => HudManager.Instance.StartCoroutine(spawnCandidates[index].GetEnumerator(passiveButton.GetComponent<SpriteRenderer>()))));
                    
                    passiveButton.GetComponent<SpriteAnim>().Stop();
                    passiveButton.GetComponent<SpriteRenderer>().sprite = spawnCandidates[index].GetSprite();
                    passiveButton.GetComponentInChildren<TextMeshPro>().text = Language.Language.GetString("locations." + spawnCandidates[index].LocationKey);
                    ButtonAnimRolloverHandler component = passiveButton.GetComponent<ButtonAnimRolloverHandler>();
                    component.StaticOutImage = spawnCandidates[index].GetSprite();
                    component.RolloverAnim = new AnimationClip();
                    component.HoverSound = spawnCandidates[index].GetAudioClip() ?? spawnInMinigame.DefaultRolloverSound;
                }
                PlayerControl.LocalPlayer.gameObject.SetActive(false);
                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(new Vector2(20f, 40f));

                PrespawnBeginPatch.Postfix(spawnInMinigame);

                spawnInMinigame.StartCoroutine(spawnInMinigame.RunTimer());
                ControllerManager.Instance.OpenOverlayMenu(spawnInMinigame.name, null, spawnInMinigame.DefaultButtonSelected, spawnInMinigame.ControllerSelectable, false);
                PlayerControl.HideCursorTemporarily();
                ConsoleJoystick.SetMode_Menu();

                /* Begin (SpawnInMinigame) */

                __result = spawnInMinigame.WaitForFinish();
            }
            else if (ShouldSpawnRandomly())
            {
                __result = GetEmptyEnumerator().WrapToIl2Cpp();

                var points = Map.MapData.GetCurrentMapData().ValidSpawnPoints;
                if (points.Count == 0) return;

                Vector2? lastPos = Game.GameData.data.myData.getGlobalData().preMeetingPosition;

                Vector2? spawnAt = null;
                if (lastPos==null || !CustomOptionHolder.respawnNearbyFinalPosition.getBool())
                {
                    spawnAt = points[NebulaPlugin.rnd.Next(points.Count)];
                    
                }
                else
                {
                    float dis = -1f;
                    foreach(var p in points)
                    {
                        float t = ((Vector3)p).Distance(lastPos.Value);
                        if (dis<0f || dis > t)
                        {
                            dis = t;
                            spawnAt = p;
                        }
                    }
                }

                if (spawnAt == null) spawnAt = points[0];

                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(spawnAt.Value);
                DestroyableSingleton<HudManager>.Instance.PlayerCam.SnapToTarget();
            }
        }
    }

    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
    public static class AirshipPrespawnStepPatch
    {
        public static bool Prefix(AirshipStatus __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            //ダミーの位置を修正
            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator()) if (p.isDummy) p.transform.position = Vector3.zero;

            if (CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.shuffledElectricalOption.getBool() && AmongUsClient.Instance.AmHost)
            {
                __instance.Systems[SystemTypes.Decontamination].Cast<ElectricalDoors>().Initialize();
            }

            if (PrespawnStepPatch.ShouldSpawnWithAbnormalWay())
            {
                PrespawnStepPatch.Postfix(__instance, ref __result);
                return false;
            }
            return true;
        }
    }
}