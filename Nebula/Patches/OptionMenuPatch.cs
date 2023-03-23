using BepInEx.Configuration;
using System.Diagnostics;

namespace Nebula.Patches;

public enum NebulaPictureDest
{
    AmongUsDirectory,
    MyPictures,
    MyDocuments,
    Desktop
}
public enum ProcessorAffinity
{
    DontCare,
    DualCoreHT,
    DualCore,
    SingleCore
}


public class NebulaOption
{
    static public Module.DataSaver configSaver = new Module.DataSaver("config.dat");
    static public Module.IntegerDataEntry configPictureDest;
    static public Module.IntegerDataEntry configProcessorAffinity;
    static public Module.BooleanDataEntry configPrioritizeAmongUs;
    static public Module.IntegerDataEntry configTimeoutExtension;
    static public Module.BooleanDataEntry configDontCareMismatchedNoS;
    static public Module.BooleanDataEntry configSnapshot;
    static public Module.BooleanDataEntry configOutputHash;
    static public Module.BooleanDataEntry configGameControl;
    static public Module.IntegerDataEntry configGameControlArgument;
    static public Module.BooleanDataEntry configPreventSpoiler;

    static public bool GetGameControlArgument(int index)
    {
        return (NebulaOption.configGameControlArgument.Value & (1 << index)) != 0;
    }

    static public string GetPicturePath(NebulaPictureDest dest)
    {
        switch (dest)
        {
            case NebulaPictureDest.AmongUsDirectory:
                return "Screenshots";
            case NebulaPictureDest.MyPictures:
                return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\NebulaOnTheShip";
            case NebulaPictureDest.MyDocuments:
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\NebulaOnTheShip";
            case NebulaPictureDest.Desktop:
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\NebulaOnTheShip";

        }
        return "";
    }

    static public string GetPictureDisplayPath(NebulaPictureDest dest)
    {
        switch (dest)
        {
            case NebulaPictureDest.AmongUsDirectory:
                return "AmongUsDirectory\\Screenshots";
            case NebulaPictureDest.MyPictures:
                return "MyPictures\\NebulaOnTheShip";
            case NebulaPictureDest.MyDocuments:
                return "MyDocuments\\NebulaOnTheShip";
            case NebulaPictureDest.Desktop:
                return "Desktop\\NebulaOnTheShip";

        }
        return "";
    }

    static private string GetCurrentTimeString()
    {
        return DateTime.Now.ToString("yyyyMMddHHmmss");
    }

    static public string CreateDirAndGetPictureFilePath(out string displayPath)
    {
        string dir = GetPicturePath((NebulaPictureDest)configPictureDest.Value);
        displayPath = GetPictureDisplayPath((NebulaPictureDest)configPictureDest.Value);
        string currentTime = GetCurrentTimeString();
        displayPath += "\\" + currentTime + ".png";
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        return dir + "\\" + currentTime + ".png";
    }

    static public string GetPictureDestMode()
    {
        switch ((NebulaPictureDest)configPictureDest.Value)
        {
            case NebulaPictureDest.AmongUsDirectory:
                return Language.Language.GetString("config.option.pictureDest.amongUsDirectory");
            case NebulaPictureDest.MyPictures:
                return Language.Language.GetString("config.option.pictureDest.myPictures");
            case NebulaPictureDest.MyDocuments:
                return Language.Language.GetString("config.option.pictureDest.myDocuments");
            case NebulaPictureDest.Desktop:
                return Language.Language.GetString("config.option.pictureDest.desktop");
        }
        return "";
    }

    static public string GetProcessorAffinityMode()
    {
        switch ((ProcessorAffinity)configProcessorAffinity.Value)
        {
            case ProcessorAffinity.DontCare:
                return Language.Language.GetString("config.option.processorRestriction.dontCare");
            case ProcessorAffinity.DualCoreHT:
                return Language.Language.GetString("config.option.processorRestriction.dualCoreHT");
            case ProcessorAffinity.DualCore:
                return Language.Language.GetString("config.option.processorRestriction.dualCore");
            case ProcessorAffinity.SingleCore:
                return Language.Language.GetString("config.option.processorRestriction.singleCore");
        }
        return "";
    }

    static public string GetTimeoutExtension()
    {
        string value = "";
        string postfix = Language.Language.GetString("option.suffix.cross");
        switch ((int)configTimeoutExtension.Value)
        {
            case 0:
                value = "1";
                break;
            case 1:
                value = "1.5";
                break;
            case 2:
                value = "2";
                break;
            case 3:
                value = "3";
                break;

        }
        return value + postfix;
    }

    static public void ReflectProcessorPriority()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            string id = process.Id.ToString();

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "CPUAffinityEditor.exe";
            processStartInfo.Arguments = id + " " + (configPrioritizeAmongUs.Value ? "Enable" : "Disable");
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            Process.Start(processStartInfo);
        }
        catch
        {
        }
    }

    static public void ReflectProcessorAffinity()
    {
        try
        {
            string? mode = null;
            switch ((ProcessorAffinity)configProcessorAffinity.Value)
            {
                case ProcessorAffinity.DontCare:
                    mode = "0";
                    break;
                case ProcessorAffinity.DualCoreHT:
                    mode = "2HT";
                    break;
                case ProcessorAffinity.DualCore:
                    mode = "2";
                    break;
                case ProcessorAffinity.SingleCore:
                    mode = "1";
                    break;
            }

            if (mode == null) return;

            var process = System.Diagnostics.Process.GetCurrentProcess();
            string id = process.Id.ToString();

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "CPUAffinityEditor.exe";
            processStartInfo.Arguments = id + " " + mode;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            Process.Start(processStartInfo);
        }
        catch
        {
            NebulaPlugin.Instance.Logger.Print("Error");
        }
    }
}

[HarmonyPatch(typeof(ToggleButtonBehaviour), nameof(ToggleButtonBehaviour.ResetText))]
public static class ResetTextPatch
{
    public static bool Prefix(ToggleButtonBehaviour __instance)
    {
        return __instance.BaseText != 0;
    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class StartOptionMenuPatch
{

    static public void LoadOption()
    {
        NebulaOption.configPictureDest = new Module.IntegerDataEntry("picutureDest",NebulaOption.configSaver, 0);
        NebulaOption.configProcessorAffinity = new Module.IntegerDataEntry("processorAffinity", NebulaOption.configSaver, 0);
        NebulaOption.configPrioritizeAmongUs = new Module.BooleanDataEntry("prioritizeAmongUs", NebulaOption.configSaver, false);
        NebulaOption.configTimeoutExtension = new Module.IntegerDataEntry("timeoutExtension", NebulaOption.configSaver, 0);
        NebulaOption.configDontCareMismatchedNoS = new Module.BooleanDataEntry("dontCareMismatchedNoS", NebulaOption.configSaver, false);
        NebulaOption.configSnapshot = new Module.BooleanDataEntry("snapshot", NebulaOption.configSaver, false);
        NebulaOption.configOutputHash = new Module.BooleanDataEntry("outputHash", NebulaOption.configSaver, false);
        NebulaOption.configGameControl = new Module.BooleanDataEntry("gameControl", NebulaOption.configSaver, false);
        NebulaOption.configPreventSpoiler = new Module.BooleanDataEntry("preventSpoiler", NebulaOption.configSaver, false);
        NebulaOption.configGameControlArgument = new Module.IntegerDataEntry("gameControlArgument", NebulaOption.configSaver, 0x0);
        NebulaOption.ReflectProcessorAffinity();
        NebulaOption.ReflectProcessorPriority();
    }

    static public void UpdateCustomText(this ToggleButtonBehaviour button, Color color, string? text)
    {
        button.onState = false;
        button.Background.color = color;
        if (text != null) button.Text.text = text;
        if (button.Rollover)
        {
            button.Rollover.ChangeOutColor(color);
        }
    }

    static public void UpdateToggleText(this ToggleButtonBehaviour button, bool on, string text)
    {
        button.onState = on;
        Color color = on ? new Color(0f, 1f, 0.16470589f, 1f) : Color.white;
        button.Background.color = color;
        button.Text.text = text + ": " + DestroyableSingleton<TranslationController>.Instance.GetString(button.onState ? StringNames.SettingsOn : StringNames.SettingsOff, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        if (button.Rollover)
        {
            button.Rollover.ChangeOutColor(color);
        }
    }

    static public void UpdateButtonText(this ToggleButtonBehaviour button, string text, string state)
    {
        button.onState = false;
        Color color = Color.white;
        button.Background.color = color;
        button.Text.text = text + ": " + state;
        if (button.Rollover)
        {
            button.Rollover.ChangeOutColor(color);
        }
    }

    static ToggleButtonBehaviour debugSnapshot;
    static ToggleButtonBehaviour debugOutputHash;

    static ToggleButtonBehaviour processorAffinity;
    static ToggleButtonBehaviour prioritizeAmongUs;
    static ToggleButtonBehaviour pictureDest;
    static ToggleButtonBehaviour timeoutExtension;
    static ToggleButtonBehaviour dontCareMismatchedNoS;
    static ToggleButtonBehaviour preventSpoiler;

    private static GameObject ShowConfirmDialogue(Transform parent, GameObject buttonTemplate, string text, System.Action yesAction)
    {
        GameObject result;
        TMPro.TMP_Text tmpText;
        SpriteRenderer background;

        if (HudManager.InstanceExists)
        {
            var dialogue = GameObject.Instantiate(HudManager.Instance.Dialogue, parent);
            dialogue.transform.localScale = new Vector3(1, 1, 1);
            GameObject.Destroy(dialogue.BackButton.gameObject);
            background = dialogue.gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();

            tmpText = dialogue.target;
            result = dialogue.gameObject;
        }
        else
        {
            var dialogue = GameObject.Instantiate(DestroyableSingleton<Twitch.TwitchManager>.Instance.TwitchPopup, parent);
            dialogue.transform.localScale = new Vector3(1, 1, 1);
            dialogue.transform.localPosition = new Vector3(0f, 0f, -10f);
            dialogue.destroyOnClose = true;
            GameObject.Destroy(dialogue.gameObject.transform.GetChild(2).gameObject);
            background = dialogue.gameObject.transform.GetChild(3).GetComponent<SpriteRenderer>();

            tmpText = dialogue.TextAreaTMP;
            result = dialogue.gameObject;
        }

        background.size = new Vector2(5f, 2f);

        tmpText.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
        tmpText.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        tmpText.gameObject.transform.localPosition = new Vector3(0f, 0.48f, -1f);
        tmpText.alignment = TMPro.TextAlignmentOptions.Center;
        tmpText.fontSize = 2;
        tmpText.fontSizeMin = 2;
        tmpText.fontSizeMax = 2;
        tmpText.enableAutoSizing = false;
        tmpText.enableWordWrapping = false;
        tmpText.text = text;

        PassiveButton passiveButton;
        GameObject textObj;

        //いいえボタン
        var noButton = GameObject.Instantiate(buttonTemplate, null);
        noButton.transform.SetParent(result.transform);
        noButton.transform.localScale = new Vector3(1f, 1f, 1f);
        noButton.transform.localPosition = new Vector3(-0.9f, -0.6f, -1f);
        noButton.name = "NoButton";
        textObj = noButton.transform.GetChild(1).gameObject;
        textObj.GetComponent<TMPro.TMP_Text>().text = Language.Language.GetString("config.option.no");
        textObj.GetComponent<TextTranslatorTMP>().enabled = false;
        passiveButton = noButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            GameObject.Destroy(result);
        }
        ));

        //はいボタン
        var yesButton = GameObject.Instantiate(buttonTemplate, null);
        yesButton.transform.SetParent(result.transform);
        yesButton.transform.localScale = new Vector3(1f, 1f, 1f);
        yesButton.transform.localPosition = new Vector3(0.9f, -0.6f, -1f);
        yesButton.name = "YesButton";
        textObj = yesButton.transform.GetChild(1).gameObject;
        textObj.GetComponent<TMPro.TMP_Text>().text = Language.Language.GetString("config.option.yes");
        textObj.GetComponent<TextTranslatorTMP>().enabled = false;
        passiveButton = yesButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            yesAction();
            GameObject.Destroy(result);
        }
        ));

        result.SetActive(true);
        return result;
    }

    private static ToggleButtonBehaviour AddButton(Vector2 pos,string name, Action onClicked, GameObject nebulaTab,GameObject toggleButtonTemplate)
    {
        var button = GameObject.Instantiate(toggleButtonTemplate, null);
        button.transform.SetParent(nebulaTab.transform);
        button.transform.localScale = new Vector3(1f, 1f, 1f);
        button.transform.localPosition = new Vector3(1.3f * (pos.x * 2f - 1f), 1.6f - 0.5f * pos.y, 0f);
        button.name = name;
        var result = button.GetComponent<ToggleButtonBehaviour>();
        PassiveButton passiveButton = button.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)onClicked);
        return result;
    }

    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        var tabs = new List<TabGroup>(__instance.Tabs.ToArray());

        PassiveButton passiveButton;
        ToggleButtonBehaviour toggleButton;

        //設定項目を追加する

        GameObject nebulaTab = new GameObject("NebulaTab");
        nebulaTab.transform.SetParent(__instance.transform);
        nebulaTab.transform.localScale = new Vector3(1f, 1f, 1f);
        nebulaTab.SetActive(false);

        GameObject keyBindingTab = new GameObject("KeyBindingTab");
        keyBindingTab.transform.SetParent(__instance.transform);
        keyBindingTab.transform.localScale = new Vector3(1f, 1f, 1f);
        keyBindingTab.SetActive(false);

        GameObject applyButtonTemplate = tabs[1].Content.transform.GetChild(0).FindChild("ApplyButton").gameObject;
        GameObject toggleButtonTemplate = tabs[0].Content.transform.FindChild("MiscGroup").FindChild("StreamerModeButton").gameObject;

        //Snapshot
        debugSnapshot = AddButton(new Vector2(0,0),"SnapshotButton", () =>
        {
            debugSnapshot.UpdateToggleText(!debugSnapshot.onState, Language.Language.GetString("config.debug.snapshot"));
            NebulaOption.configSnapshot.Value = debugSnapshot.onState;
        },nebulaTab,toggleButtonTemplate);

        //OutputHash
        debugOutputHash = AddButton(new Vector2(1, 0), "OutputHashButton", () =>
        {
            debugOutputHash.UpdateToggleText(!debugOutputHash.onState, Language.Language.GetString("config.debug.outputHash"));
            NebulaOption.configOutputHash.Value = debugOutputHash.onState;
        }, nebulaTab, toggleButtonTemplate);

        //ProcessorAffinity
        processorAffinity = AddButton(new Vector2(0, 1), "ProcessorAffinity", () =>
        {
            NebulaOption.configProcessorAffinity.Value++;
            NebulaOption.configProcessorAffinity.Value %= 4;
            processorAffinity.UpdateButtonText(Language.Language.GetString("config.option.processorRestriction"), NebulaOption.GetProcessorAffinityMode());
            NebulaOption.ReflectProcessorAffinity();
        }, nebulaTab, toggleButtonTemplate);

        //PrioritizeAmongUs
        prioritizeAmongUs = AddButton(new Vector2(1, 1), "PrioritizeAmongUs", () =>
        {
            if (!NebulaOption.configPrioritizeAmongUs.Value)
            {
                ShowConfirmDialogue(nebulaTab.transform, applyButtonTemplate, Language.Language.GetString("config.option.prioritizeAmongUs.confirm"), () =>
                {
                    NebulaOption.configPrioritizeAmongUs.Value = true;
                    prioritizeAmongUs.UpdateToggleText(true, Language.Language.GetString("config.option.prioritizeAmongUs"));
                    NebulaOption.ReflectProcessorPriority();
                });
            }
            else
            {
                NebulaOption.configPrioritizeAmongUs.Value = false;
                prioritizeAmongUs.UpdateToggleText(false, Language.Language.GetString("config.option.prioritizeAmongUs"));
                NebulaOption.ReflectProcessorPriority();
            }
        }, nebulaTab, toggleButtonTemplate);

        //PictureDest
        pictureDest = AddButton(new Vector2(0, 2), "PictureDest", () =>
        {
            NebulaOption.configPictureDest.Value++;
            NebulaOption.configPictureDest.Value %= 4;
            pictureDest.UpdateButtonText(Language.Language.GetString("config.option.pictureDest"), NebulaOption.GetPictureDestMode());
        }, nebulaTab, toggleButtonTemplate);

        //TimeoutExtension
        timeoutExtension = AddButton(new Vector2(1, 2), "TimeoutExtension", () =>
        {
            NebulaOption.configTimeoutExtension.Value++;
            NebulaOption.configTimeoutExtension.Value %= 4;
            timeoutExtension.UpdateButtonText(Language.Language.GetString("config.option.timeoutExtension"), NebulaOption.GetTimeoutExtension());
        }, nebulaTab, toggleButtonTemplate);

        dontCareMismatchedNoS = AddButton(new Vector2(0, 3), "dontCareMismatchedNos", () =>
        {
            NebulaOption.configDontCareMismatchedNoS.Value = !NebulaOption.configDontCareMismatchedNoS.Value;
            dontCareMismatchedNoS.UpdateToggleText(NebulaOption.configDontCareMismatchedNoS.Value, Language.Language.GetString("config.option.dontCareMismatchedNoS"));
        }, nebulaTab, toggleButtonTemplate);

        preventSpoiler = AddButton(new Vector2(1, 3), "preventSpoiler", () =>
        {
            NebulaOption.configPreventSpoiler.Value = !NebulaOption.configPreventSpoiler.Value;
            preventSpoiler.UpdateToggleText(NebulaOption.configPreventSpoiler.Value, Language.Language.GetString("config.option.preventSpoiler"));
        }, nebulaTab, toggleButtonTemplate);


        //キー割り当てボタン
        GameObject TextObject;

        List<ToggleButtonBehaviour> allKeyBindingButtons = new List<ToggleButtonBehaviour>();
        int selectedKeyBinding = -1;

        var defaultButton = GameObject.Instantiate(applyButtonTemplate, null);
        defaultButton.transform.SetParent(keyBindingTab.transform);
        defaultButton.transform.localScale = new Vector3(1f, 1f, 1f);
        defaultButton.transform.localPosition = new Vector3(0f, -2.5f, 0f);
        defaultButton.name = "RestoreDefaultsButton";
        defaultButton.transform.GetChild(0).GetComponent<SpriteRenderer>().size = new Vector2(2.25f, 0.4f);
        TextObject = defaultButton.transform.FindChild("Text_TMP").gameObject;
        TextObject.GetComponent<TMPro.TextMeshPro>().text = Language.Language.GetString("config.option.keyBinding.restoreDefaults");
        TextObject.GetComponent<TMPro.TextMeshPro>().rectTransform.sizeDelta *= 2;
        TextObject.GetComponent<TextTranslatorTMP>().enabled = false;
        passiveButton = defaultButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            selectedKeyBinding = -1;

            SoundManager.Instance.PlaySound(Module.MetaScreen.getSelectClip(), false, 0.8f);

            for (int i = 0; i < Module.NebulaInputManager.allInputs.Count; i++)
            {
                var input = Module.NebulaInputManager.allInputs[i];
                input.resetToDefault();
                allKeyBindingButtons[i].UpdateCustomText(Color.white, Language.Language.GetString("config.option.keyBinding." + input.identifier) + ": " + Module.NebulaInputManager.allKeyCodes[input.keyCode].displayKey);
            }
        }
        ));

        foreach (var input in Module.NebulaInputManager.allInputs)
        {
            int index = allKeyBindingButtons.Count;

            var inputButton = GameObject.Instantiate(toggleButtonTemplate, null);
            inputButton.transform.SetParent(keyBindingTab.transform);
            inputButton.transform.localScale = new Vector3(1f, 1f, 1f);
            inputButton.transform.localPosition = new Vector3(1.3f * (float)((index % 2) * 2 - 1), 1.5f - 0.5f * (float)(index / 2), 0f);
            inputButton.name = input.identifier;
            var inputToggleButton = inputButton.GetComponent<ToggleButtonBehaviour>();
            inputToggleButton.BaseText = 0;
            inputToggleButton.Text.text = Language.Language.GetString("config.option.keyBinding." + input.identifier) + ": " + Module.NebulaInputManager.allKeyCodes[input.keyCode].displayKey;
            passiveButton = inputButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                if (selectedKeyBinding == index)
                {
                    selectedKeyBinding = -1;
                    inputToggleButton.UpdateCustomText(Color.white, null);
                }
                else
                {
                    selectedKeyBinding = index;
                    inputToggleButton.UpdateCustomText(Color.yellow, null);
                }
            }));

            allKeyBindingButtons.Add(inputToggleButton);
        }

        var keyBindingButton = GameObject.Instantiate(applyButtonTemplate, null);
        keyBindingButton.transform.SetParent(nebulaTab.transform);
        keyBindingButton.transform.localScale = new Vector3(1f, 1f, 1f);
        keyBindingButton.transform.localPosition = new Vector3(0f, -1.5f, 0f);
        keyBindingButton.name = "KeyBindingButton";
        keyBindingButton.transform.GetChild(0).GetComponent<SpriteRenderer>().size = new Vector2(2.25f, 0.4f);
        TextObject = keyBindingButton.transform.FindChild("Text_TMP").gameObject;
        TextObject.GetComponent<TMPro.TextMeshPro>().text = Language.Language.GetString("config.option.keyBinding");
        TextObject.GetComponent<TMPro.TextMeshPro>().rectTransform.sizeDelta *= 2;
        TextObject.GetComponent<TextTranslatorTMP>().enabled = false;
        passiveButton = keyBindingButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            SoundManager.Instance.PlaySound(Module.MetaScreen.getSelectClip(), false, 0.8f);
            __instance.OpenTabGroup(tabs.Count - 1);
        }
        ));

        IEnumerator getEnumerator()
        {
            while (true)
            {
                if (HudManager.InstanceExists && !GameStartManager.InstanceExists)
                    keyBindingButton.gameObject.SetActive(false);

                if (keyBindingTab.gameObject.active && Input.anyKeyDown && selectedKeyBinding != -1)
                {
                    foreach (var entry in Module.NebulaInputManager.allKeyCodes)
                    {
                        if (!Input.GetKeyDown(entry.Key)) continue;

                        var input = Module.NebulaInputManager.allInputs[selectedKeyBinding];
                        input.changeKeyCode(entry.Key);
                        allKeyBindingButtons[selectedKeyBinding].UpdateCustomText(Color.white, Language.Language.GetString("config.option.keyBinding." + input.identifier) + ": " + Module.NebulaInputManager.allKeyCodes[input.keyCode].displayKey);
                        selectedKeyBinding = -1;
                        break;
                    }
                }
                else if (!keyBindingTab.gameObject.active && selectedKeyBinding != -1)
                {
                    allKeyBindingButtons[selectedKeyBinding].UpdateCustomText(Color.white, null);
                    selectedKeyBinding = -1;
                }
                yield return null;
            }
        }

        if (HudManager.InstanceExists)
            HudManager.Instance.StartCoroutine(getEnumerator().WrapToIl2Cpp());
        else
            __instance.StartCoroutine(getEnumerator().WrapToIl2Cpp());


        //タブを追加する

        tabs[tabs.Count - 1] = (GameObject.Instantiate(tabs[1], null));
        var nebulaButton = tabs[tabs.Count - 1];
        nebulaButton.gameObject.name = "NebulaButton";
        nebulaButton.transform.SetParent(tabs[0].transform.parent);
        nebulaButton.transform.localScale = new Vector3(1f, 1f, 1f);
        nebulaButton.Content = nebulaTab;
        var textObj = nebulaButton.transform.FindChild("Text_TMP").gameObject;
        textObj.GetComponent<TextTranslatorTMP>().enabled = false;
        textObj.GetComponent<TMPro.TMP_Text>().text = "NoS";

        tabs.Add((GameObject.Instantiate(tabs[1], null)));
        var keyBindingTabButton = tabs[tabs.Count - 1];
        keyBindingTabButton.gameObject.name = "KeyBindingButton";
        keyBindingTabButton.transform.SetParent(tabs[0].transform.parent);
        keyBindingTabButton.transform.localScale = new Vector3(1f, 1f, 1f);
        keyBindingTabButton.Content = keyBindingTab;
        keyBindingTabButton.gameObject.SetActive(false);

        passiveButton = nebulaButton.gameObject.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            __instance.OpenTabGroup(tabs.Count - 2);

            debugSnapshot.UpdateToggleText(NebulaOption.configSnapshot.Value, Language.Language.GetString("config.debug.snapshot"));
            debugOutputHash.UpdateToggleText(NebulaOption.configOutputHash.Value, Language.Language.GetString("config.debug.outputHash"));
            pictureDest.UpdateButtonText(Language.Language.GetString("config.option.pictureDest"), NebulaOption.GetPictureDestMode());
            processorAffinity.UpdateButtonText(Language.Language.GetString("config.option.processorRestriction"), NebulaOption.GetProcessorAffinityMode());
            prioritizeAmongUs.UpdateToggleText(NebulaOption.configPrioritizeAmongUs.Value, Language.Language.GetString("config.option.prioritizeAmongUs"));
            timeoutExtension.UpdateButtonText(Language.Language.GetString("config.option.timeoutExtension"), NebulaOption.GetTimeoutExtension());
            dontCareMismatchedNoS.UpdateToggleText(NebulaOption.configDontCareMismatchedNoS.Value, Language.Language.GetString("config.option.dontCareMismatchedNoS"));
            preventSpoiler.UpdateToggleText(NebulaOption.configPreventSpoiler.Value, Language.Language.GetString("config.option.preventSpoiler"));

            passiveButton.OnMouseOver.Invoke();
        }
        ));

        float y = tabs[0].transform.localPosition.y, z = tabs[0].transform.localPosition.z;
        if (tabs.Count == 4)
            for (int i = 0; i < 3; i++) tabs[i].transform.localPosition = new Vector3(1.7f * (float)(i - 1), y, z);
        else if (tabs.Count == 5)
            for (int i = 0; i < 4; i++) tabs[i].transform.localPosition = new Vector3(1.62f * ((float)i - 1.5f), y, z);

        __instance.Tabs = new Il2CppReferenceArray<TabGroup>(tabs.ToArray());


    }
}