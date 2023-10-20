using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Nebula.Behaviour;
using Rewired.UI.ControlMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules;


[NebulaPreLoad]
public class ClientOption
{
    public enum ClientOptionType
    {
        OutputCosmicHash
    }

    static private DataSaver ClientOptionSaver = new("ClientOption");
    static public Dictionary<ClientOptionType,ClientOption> AllOptions = new();
    DataEntry<int> configEntry;
    string id;
    string[] selections;
    ClientOptionType type;

    public ClientOption(ClientOptionType type,string name,string[] selections,int defaultValue)
    {
        id = name;
        configEntry = new IntegerDataEntry(name, ClientOptionSaver, defaultValue);
        this.selections = selections;
        this.type = type;
        AllOptions[type] = this;
    }

    public string DisplayName => Language.Translate("config.client." + id);
    public string DisplayValue => Language.Translate(selections[configEntry.Value]);
    public int Value => configEntry.Value;

    public void Increament()
    {
        configEntry.Value = (configEntry.Value + 1) % selections.Length;
    }
    static public void Load()
    {
        new ClientOption(ClientOptionType.OutputCosmicHash, "outputHash", new string[] { "options.switch.off", "options.switch.on" }, 0);
    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class StartOptionMenuPatch
{
    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        foreach(var button in __instance.GetComponentsInChildren<CustomButton>(true))
        {
            if (button.name != "DoneButton") continue;

            button.onClick.AddListener(() => {
                if (AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                    HudManager.Instance.ShowVanillaKeyGuide();
            });
            Debug.Log("Addlistener: "+button.name);
        }
        var tabs = new List<TabGroup>(__instance.Tabs.ToArray());

        PassiveButton passiveButton;

        //設定項目を追加する

        GameObject nebulaTab = new GameObject("NebulaTab");
        nebulaTab.transform.SetParent(__instance.transform);
        nebulaTab.transform.localScale = new Vector3(1f, 1f, 1f);
        nebulaTab.SetActive(false);

        var nebulaScreen = MetaScreen.GenerateScreen(new(5f, 4.5f), nebulaTab.transform, new(0f, -0.28f, -10f), false, false, false);

        void SetNebulaContext()
        {
            var buttonAttr = new TextAttribute(TextAttribute.BoldAttr) { Size = new Vector2(2.05f, 0.26f) };
            MetaContext nebulaContext = new();
            nebulaContext.Append(ClientOption.AllOptions.Values, (option) => new MetaContext.Button(()=> {
                option.Increament();
                SetNebulaContext();
            }, buttonAttr) { RawText = option.DisplayName + " : " + option.DisplayValue }, 2, -1, 0, 0.4f);
            nebulaContext.Append(new MetaContext.VerticalMargin(0.2f));

            if (!AmongUsClient.Instance || AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
            {
                nebulaContext.Append(new MetaContext.Button(() =>
                {
                    __instance.OpenTabGroup(tabs.Count - 1);
                    SetKeyBindingContext();
                }, buttonAttr)
                { TranslationKey = "config.client.keyBindings", Alignment = IMetaContext.AlignmentOption.Center });
            }

            if(NebulaGameManager.Instance?.VoiceChatManager != null)
            {
                nebulaContext.Append(new MetaContext.Button(() =>
                {
                    NebulaGameManager.Instance?.VoiceChatManager?.OpenSettingScreen();
                }, buttonAttr)
                { TranslationKey = "config.client.vcSettings", Alignment = IMetaContext.AlignmentOption.Center });
            }


            nebulaScreen.SetContext(nebulaContext);
        }

        GameObject keyBindingTab = new GameObject("KeyBindingTab");
        keyBindingTab.transform.SetParent(__instance.transform);
        keyBindingTab.transform.localScale = new Vector3(1f, 1f, 1f);
        keyBindingTab.SetActive(false);

        var keyBindingScreen = MetaScreen.GenerateScreen(new(5f, 4.5f), keyBindingTab.transform, new(0f, -0.28f, -10f), false, false, false);

        IKeyAssignment? currentAssignment = null;

        void SetKeyBindingContext()
        {
            MetaContext keyBindingContext = new();
            TMPro.TextMeshPro? text = null;
            keyBindingContext.Append(IKeyAssignment.AllKeyAssignments, (assignment) =>
            new MetaContext.Button(() =>
            {
                currentAssignment = assignment;
                SetKeyBindingContext();
            }, new(TextAttribute.NormalAttr) { Size = new Vector2(2.2f, 0.26f) })
            { RawText = assignment.DisplayName + " : " + (currentAssignment == assignment ? Language.Translate("input.recording") : ButtonEffect.KeyCodeInfo.GetKeyDisplayName(assignment.KeyInput)), PostBuilder = (_, _, t) => text = t }, 2, -1, 0, 0.55f);
            keyBindingScreen.SetContext(keyBindingContext);
        }

        void CoUpdate()
        {
            if (currentAssignment != null && Input.anyKeyDown)
            {
                foreach (var keyCode in ButtonEffect.KeyCodeInfo.AllKeyInfo.Values)
                {
                    if (Input.GetKeyDown(keyCode.keyCode))
                    {
                        currentAssignment.KeyInput = keyCode.keyCode;
                        currentAssignment = null;
                        SetKeyBindingContext();
                        break;
                    }
                }
            }
        }

        keyBindingScreen.gameObject.AddComponent<ScriptBehaviour>().UpdateHandler += CoUpdate;

        SetNebulaContext();
        SetKeyBindingContext();

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
            SetNebulaContext();
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
