using Rewired;

namespace Nebula.Patches;

[Harmony]
public class HardTaskPatch
{
    //数字タスクの一部迷彩化
    [HarmonyPatch(typeof(UnlockManifoldsMinigame), nameof(UnlockManifoldsMinigame.Begin))]
    public static class UnlockManifoldsPatch
    {
        static private Sprite emptySprite = null;
        static private Sprite GetEmptySprite()
        {
            if (emptySprite) return emptySprite;
            emptySprite = Helpers.loadSpriteFromResources("Nebula.Resources.EmptyManifolds.png", 100f);
            return emptySprite;

        }

        public static void Postfix(UnlockManifoldsMinigame __instance)
        {
            if (!CustomOptionHolder.TasksOption.getBool() || !CustomOptionHolder.MeistersManifoldsOption.getBool()) return;

            var randomArray = Helpers.GetRandomArray(10);
            int hideButtons = NebulaPlugin.rnd.Next(3, 5);
            for (int i = 0; i < hideButtons; i++)
            {
                __instance.Buttons[randomArray[i]].sprite = GetEmptySprite();
            }
        }
    }

    //葉っぱタスクの葉っぱ数増加
    [HarmonyPatch(typeof(LeafMinigame), nameof(LeafMinigame.Begin))]
    public static class LeavesPatch
    {
        public static bool Prefix(LeafMinigame __instance, [HarmonyArgument(0)] PlayerTask task)
        {
            if (!CustomOptionHolder.TasksOption.getBool() || !CustomOptionHolder.MeistersFilterOption.getBool()) return true;

            task.gameObject.GetComponent<NormalPlayerTask>().MaxStep = 20;
            return true;
        }
    }

    //タスクのステップ数改変
    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.Initialize))]
    public static class VeryLongTaskPatch
    {
        public static bool Prefix(NormalPlayerTask __instance)
        {
            if (!CustomOptionHolder.TasksOption.getBool()) return true;

            if (__instance.TaskType == TaskTypes.FixWiring)
            {
                __instance.MaxStep = (int)CustomOptionHolder.StepsOfWiringOption.getFloat();
            }

            return true;
        }

        public static void Postfix(NormalPlayerTask __instance)
        {
            //給油タスク
            if (__instance.TaskType == TaskTypes.FuelEngines && __instance.StartAt == SystemTypes.CargoBay)
            {
                //タスクの見た目のバグ修正
                var stages = __instance.MinigamePrefab.gameObject.GetComponent<MultistageMinigame>().Stages;
                stages[2] = stages[1];
            }

            if (!CustomOptionHolder.TasksOption.getBool()) return;

            if (CustomOptionHolder.MeistersFuelEnginesOption.getBool())
            {
                if (__instance.TaskType == TaskTypes.FuelEngines && __instance.MaxStep != 1)
                {
                    __instance.MaxStep *= 2;
                }
            }
        }
    }

    //給油タスクの仕様変更
    [HarmonyPatch(typeof(RefuelStage), nameof(RefuelStage.FixedUpdate))]
    public static class RefuelStagePatch
    {
        private static byte airshipEngineHistory = 1;
        private static bool filledHalf = false;
        private static bool IsHardMode() { return (CustomOptionHolder.TasksOption.getBool() && CustomOptionHolder.MeistersFuelEnginesOption.getBool()); }

        public static bool Prefix(RefuelStage __instance)
        {
            if (!IsHardMode()) return true;

            if (ReInput.players.GetPlayer(0).GetButton(21))
            {
                if (!__instance.isDown)
                {
                    __instance.usingController = true;
                    __instance.Refuel();
                }
            }
            else if (__instance.isDown && __instance.usingController)
            {
                __instance.usingController = false;
                __instance.Refuel();
            }
            if (__instance.complete)
            {
                return false;
            }
            if (__instance.isDown && __instance.timer < 1f)
            {

                __instance.timer += (Time.fixedDeltaTime / __instance.RefuelDuration) * ((IsHardMode() && __instance.srcGauge) ? 0.5f : 1f);

                __instance.MyNormTask.Data[0] = (byte)Mathf.Min(255f, __instance.timer * 255f);
                if (__instance.timer >= ((IsHardMode() && __instance.srcGauge) ? 0.5f : 1f))
                {
                    __instance.complete = true;
                    if (__instance.greenLight)
                        __instance.greenLight.color = __instance.green;

                    if (__instance.redLight)
                        __instance.redLight.color = __instance.darkRed;

                    if (__instance.MyNormTask.MaxStep == 1)
                        __instance.MyNormTask.NextStep();

                    else if (__instance.MyNormTask.StartAt == SystemTypes.CargoBay || __instance.MyNormTask.StartAt == SystemTypes.Engine)
                    {
                        __instance.MyNormTask.Data[0] = 0;
                        switch (__instance.MyNormTask.TaskStep)
                        {
                            case 0:
                                airshipEngineHistory = __instance.MyNormTask.Data[1] = (byte)(NebulaPlugin.rnd.Next(1, 3));
                                filledHalf = false;
                                break;
                            case 1:
                                __instance.MyNormTask.Data[1] = 0;
                                break;
                            case 2:
                                __instance.MyNormTask.Data[1] = airshipEngineHistory;
                                filledHalf = true;
                                break;
                        }

                        __instance.MyNormTask.NextStep();
                    }
                    else
                    {
                        __instance.MyNormTask.Data[0] = 0;

                        if ((__instance.MyNormTask.TaskStep % 2) == 0 && __instance.MyNormTask.Data[1] % 2 == 1)
                            __instance.MyNormTask.Data[1]--;
                        else
                            __instance.MyNormTask.Data[1]++;

                        if (__instance.MyNormTask.Data[1] % 2 == 0)
                            __instance.MyNormTask.NextStep();
                        else
                            filledHalf = __instance.MyNormTask.TaskStep % 2 == 1;


                        __instance.MyNormTask.UpdateArrow();
                    }
                }
            }

            if (IsHardMode() && __instance.srcGauge && filledHalf)
                __instance.destGauge.value = __instance.timer + 0.5f;
            else
                __instance.destGauge.value = __instance.timer;

            if (__instance.srcGauge)
                __instance.srcGauge.value = 1f - (IsHardMode() ? 2f : 1f) * __instance.timer;


            return false;
        }
    }

    //配線タスクの順番ランダム化
    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.PickRandomConsoles))]
    public static class RandomTaskPatch
    {
        static public void Postfix(NormalPlayerTask __instance, [HarmonyArgument(0)] TaskTypes taskType, [HarmonyArgument(1)] ref Il2CppStructArray<byte> consoleIds)
        {
            if (!CustomOptionHolder.TasksOption.getBool() || !CustomOptionHolder.RandomizedWiringOption.getBool()) return;

            var newArray = consoleIds.OrderBy(i => Guid.NewGuid()).ToArray();
            for (int i = 0; i < newArray.Length; i++)
                consoleIds[i] = newArray[i];

        }

        static public bool Prefix(NormalPlayerTask __instance, ref Il2CppSystem.Collections.Generic.List<Console> __result, [HarmonyArgument(0)] TaskTypes taskType, [HarmonyArgument(1)] ref Il2CppStructArray<byte> consoleIds)
        {
            if (!CustomOptionHolder.TasksOption.getBool()) return true;

            List<Console> orgList = ShipStatus.Instance.AllConsoles.Where((t) => { return t.TaskTypes.Contains(taskType); }).ToList<Console>();
            List<Console> list = new List<Console>(orgList);
            List<Console> result = new List<Console>();

            __result = new Il2CppSystem.Collections.Generic.List<Console>();
            foreach (var console in orgList)
                __result.Add(console);

            for (int i = 0; i < consoleIds.Length; i++)
            {
                if (list.Count == 0)
                    list = new List<Console>(orgList);
                int index = NebulaPlugin.rnd.Next(list.Count);
                result.Add(list[index]);
                list.RemoveAt(index);
            }

            if (!CustomOptionHolder.RandomizedWiringOption.getBool())
                result.Sort((console1, console2) => { return console2.ConsoleId - console1.ConsoleId; });

            for (int i = 0; i < consoleIds.Length; i++)
                consoleIds[i] = (byte)result[i].ConsoleId;

            return false;
        }
    }

    [HarmonyPatch]
    class SafeMinigamePatch
    {
        static int[] numbers = new int[5];
        static int[] received = new int[5] { 0, 0, 0, 0, 0 };
        static int progress = 0;
        static bool canPushButton;
        private static Sprite backgroundSprite;
        private static Texture2D buttonTexture;
        private static HashSet<SpriteRenderer> allRenderers = new HashSet<SpriteRenderer>();
        private static AudioClip denySound;
        private static AudioClip buttonSound;

        static Sprite GetBackgroundSprite()
        {
            if (!backgroundSprite) backgroundSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SafeMinigameBackground.png", 190f);
            return backgroundSprite;
        }

        static Texture2D GetButtonsTexture()
        {
            if (!buttonTexture) buttonTexture = Helpers.loadTextureFromResources("Nebula.Resources.SafeMinigameButtons.png");
            return buttonTexture;
        }

        [HarmonyPatch(typeof(SafeMinigame), nameof(SafeMinigame.Begin))]
        class SafeMinigameBeginPatch
        {
            static IEnumerator GetEnumerator()
            {
                canPushButton = false;
                for (int i = 0; i < 6; i++)
                {
                    foreach (var r in allRenderers) r.color = (i % 2 == 0) ? new Color(0.7f, 0.2f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
                    float t = 0f;
                    while (true)
                    {
                        t += Time.deltaTime;
                        if (t > 0.3f) break;
                        yield return null;
                    }
                }
                foreach (var r in allRenderers) r.color = Color.white;
                canPushButton = true;
            }

            static void Postfix(SafeMinigame __instance)
            {
                if (CustomOptionHolder.mapOptions.getBool() && !CustomOptionHolder.UseVanillaSafeTaskOption.getBool())
                {
                    if (!denySound) denySound = Helpers.FindSound("panel_admin_carddeny");
                    if (!buttonSound) buttonSound = Helpers.FindSound("panel_divertpower_switch");

                    __instance.latched[0] = false;
                    canPushButton = true;


                    __instance.ComboText.transform.localPosition = new Vector3(0.1648f, 0f);
                    __instance.Tumbler.gameObject.SetActive(false);

                    __instance.ComboText.text = "";

                    progress = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        numbers[i] = NebulaPlugin.rnd.Next(10);
                        if (i != 0) __instance.ComboText.text += " ";
                        __instance.ComboText.text += numbers[i].ToString();
                    }

                    foreach (var arrow in __instance.Arrows) arrow.gameObject.SetActive(false);

                    if (__instance.transform.childCount != 6) return;

                    allRenderers.Clear();

                    GameObject background = new GameObject("Input");
                    background.layer = LayerExpansion.GetUILayer();
                    background.transform.SetParent(__instance.transform);
                    background.transform.localPosition = __instance.ComboText.transform.parent.localPosition + new Vector3(0f, -2.2f, 0f);
                    background.transform.localScale = new Vector3(1f, 1f, 1f);
                    var backRenderer = background.AddComponent<SpriteRenderer>();
                    backRenderer.sprite = GetBackgroundSprite();
                    allRenderers.Add(backRenderer);

                    GameObject button;
                    for (int i = 0; i < 12; i++)
                    {
                        button = new GameObject("Button");
                        button.layer = LayerExpansion.GetUILayer();
                        button.transform.SetParent(background.transform);
                        button.transform.localPosition = new Vector3((float)(-1f + (float)(i % 3)) * 0.6f, (float)(1.5f - (float)(i / 3)) * 0.6f, -1f);
                        button.transform.localScale = new Vector3(1f, 1f, 1f);

                        int buttonType = 0;
                        if (i == 9) buttonType = 2;
                        if (i == 11) buttonType = 1;
                        var renderer = button.AddComponent<SpriteRenderer>();
                        renderer.sprite = Helpers.loadSpriteFromResources(GetButtonsTexture(), 160f, new Rect(buttonType * 88f, 0, 88f, -88f));
                        allRenderers.Add(renderer);

                        var collider = button.AddComponent<CircleCollider2D>();
                        collider.radius = 0.22f;

                        var passiveButton = button.AddComponent<PassiveButton>();
                        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                        passiveButton.OnMouseOut = new UnityEngine.Events.UnityEvent();
                        passiveButton.OnMouseOver = new UnityEngine.Events.UnityEvent();

                        var text = GameObject.Instantiate(__instance.ComboText, button.transform);
                        text.transform.localPosition = new Vector3();
                        text.transform.localScale = new Vector3(0.6f, 0.6f, -1f);
                        text.alignment = TMPro.TextAlignmentOptions.Center;


                        if (i == 9)
                        {
                            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                            {
                                if (!__instance.latched[0] && canPushButton)
                                {
                                    progress = 0;
                                    SoundManager.Instance.StopSound(buttonSound);
                                    SoundManager.Instance.PlaySound(buttonSound, false, 1f, null).pitch = 1f + ((float)NebulaPlugin.rnd.NextDouble() - 0.5f) * 0.1f;
                                }
                            }));

                            text.text = "×";
                        }
                        else if (i == 11)
                        {
                            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                            {
                                if (__instance.latched[0] || !canPushButton) return;

                                bool matched = true;
                                for (int i = 0; i < 5; i++)
                                {
                                    if (numbers[i] != received[i])
                                    {
                                        matched = false;
                                        break;
                                    }
                                }
                                if (matched)
                                {
                                    SoundManager.Instance.StopSound(buttonSound);
                                    SoundManager.Instance.PlaySound(buttonSound, false, 1f, null).pitch = 1f + ((float)NebulaPlugin.rnd.NextDouble() - 0.5f) * 0.1f;
                                    for (int i = 0; i < __instance.latched.Count; i++) __instance.latched[i] = true;
                                    foreach (var renderer in allRenderers) renderer.color = Palette.DisabledGrey;
                                    __instance.Spinner.GetComponent<SpriteRenderer>().color = Color.white;
                                    canPushButton = false;
                                }
                                else
                                {
                                    progress = 0;
                                    SoundManager.Instance.StopSound(denySound);
                                    SoundManager.Instance.PlaySound(denySound, false, 1f, null);
                                    __instance.StartCoroutine(GetEnumerator().WrapToIl2Cpp());
                                }
                            }));

                            text.text = "⇒";
                        }
                        else
                        {
                            int num = i + 1;
                            if (i == 10) num = 0;

                            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                            {
                                if (__instance.latched[0] || !canPushButton) return;

                                if (progress < received.Length)
                                {
                                    received[progress] = num;
                                    progress++;
                                }

                                SoundManager.Instance.PlaySound(buttonSound, false, 1f, null).pitch = 1f + ((float)NebulaPlugin.rnd.NextDouble() - 0.5f) * 0.1f;
                            }));
                            text.text = num.ToString();
                        }

                        passiveButton.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() =>
                        {
                            if (!__instance.latched[0] && canPushButton) renderer.color = new Color(0.6f, 0.6f, 0.6f);
                        }));
                        passiveButton.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() =>
                        {
                            if (!__instance.latched[0] && canPushButton) renderer.color = Color.white;
                        }));
                    }
                }
            }
        }
    }
}