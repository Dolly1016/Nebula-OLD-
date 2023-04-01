using System.Text;
using System.Reflection;
using Hazel;
using BepInEx.Configuration;
using AmongUs.GameOptions;
using UnityEngine;
using static Nebula.Module.CustomOption;
using JetBrains.Annotations;

namespace Nebula.Module;

[Flags]
public enum CustomGameMode
{
    Standard = 0x0001,
    FreePlay = 0x0002,
    StandardHnS = 0x0010,
    FreePlayHnS = 0x0020,
    All = int.MaxValue,
    AllHnS = 0x0030
        
}

[Flags]
public enum CustomOptionTab
{
    None = 0x00,
    Settings = 0x01,
    CrewmateRoles = 0x02,
    ImpostorRoles = 0x04,
    NeutralRoles = 0x08,
    GhostRoles = 0x10,
    Modifiers = 0x20,
    EscapeRoles = 0x40,
    AdvancedSettings = 0x80,
    MaxValidTabs = 8
}

public static class CustomGameModes
{
    static public List<CustomGameMode> AllGameModes = new List<CustomGameMode>()
        {
            CustomGameMode.Standard,CustomGameMode.FreePlay,CustomGameMode.Standard,CustomGameMode.Standard,
            CustomGameMode.Standard,CustomGameMode.Standard,CustomGameMode.Standard,CustomGameMode.Standard,
            CustomGameMode.StandardHnS,CustomGameMode.FreePlayHnS
        };

    static public CustomGameMode GetGameMode(int GameModeIndex)
    {
        if (AllGameModes.Count > GameModeIndex && GameModeIndex >= 0)
        {
            return AllGameModes[GameModeIndex];
        }
        return AllGameModes[0];
    }
}


public delegate string CustomOptionDecorator(string original, CustomOption option);

[NebulaRPCHolder]
public class CustomOption
{
    public class MSOptionString : MSString
    {
        string optionName;
        public MSOptionString(string optionName, float width, string text, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style):
            base(width,text,alignment,style)
        {
            this.optionName = optionName;
        }

        public MSOptionString(string optionName, float width, string text, float fontSize, float fontSizeMin, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style)
            : base(width, text, fontSize,fontSizeMin,alignment, style)
        {
            this.optionName = optionName;
        }

        public MSOptionString(CustomOption option, float width, string text, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style) :
           this(option.name, width, text, alignment, style){}

        public MSOptionString(CustomOption option, float width, string text, float fontSize, float fontSizeMin, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style)
            : this(option.name,width, text, fontSize, fontSizeMin, alignment, style){}

        public override void Generate(GameObject obj)
        {
            base.Generate(obj);

            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width - 0.1f, 0.36f);

            PassiveButton button = obj.AddComponent<PassiveButton>();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            button.OnMouseOut = new UnityEngine.Events.UnityEvent();
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();

            button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() => {
                if (!UnderInfo) return;
                string str="";
                if (Language.Language.TryGetString(optionName + ".info", ref str))
                    UnderInfo.text = str;
            }));
            button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => {
                if (!UnderInfo) return;
                UnderInfo.text = "";
            }));
        }
    }

    private static void UpdateSelectionProcess(Tuple<int,int> param)
    {
        int optionId = param.Item1;
        int selection = param.Item2;

        if (optionId == int.MaxValue)
        {
            GameOptionsManager.Instance.CurrentGameOptions.SetInt(Int32OptionNames.NumImpostors, selection);
        }
        else
        {
            CustomOption option = CustomOption.AllOptions.FirstOrDefault(opt => opt.id == optionId);
            option.updateSelection(selection);
        }

        GameOptionsDataPatch.dirtyFlag = true;
    }

    public static RemoteProcess<Tuple<int,int>> ShareOption = new("ShareGameOption",
        (writer, message) => {
            writer.WritePacked(message.Item1);
            writer.WritePacked(message.Item2);
        },
        (reader) => {
            return new(reader.ReadPackedInt32(), reader.ReadPackedInt32());
        },
        (message,calledByMe) =>
        {
            if (calledByMe) return;
            UpdateSelectionProcess(message);
        }
        );

    public static DivisibleRemoteProcess<List<CustomOption>, Tuple<int, int>[]> ShareAllOptions = new("ShareAllGameOption",
        (parameter, sender) =>
        {
            int num = parameter.Count;
            int sent = 0;
            while (num > 0)
            {
                int length = num > 8 ? 8 : num;
                var options = new Tuple<int, int>[length];
                for (int i = 0; i < length; i++)
                {
                    options[i] = new(parameter[sent].id, parameter[sent].selection);
                    sent++;
                }
                sender(options);
                num -= length;
            }
        },
        (writer, parameter) =>
        {
            writer.Write(parameter.Length);
            foreach (var option in parameter)
            {
                writer.WritePacked(option.Item1);
                writer.WritePacked(option.Item2);
            }
        },
        (reader) =>
        {
            var result = new Tuple<int, int>[reader.ReadInt32()];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Tuple<int, int>(reader.ReadPackedInt32(), reader.ReadPackedInt32());
            }
            return result;
        },
        (message, calledByMe) =>
        {
            if (calledByMe) return;
            foreach(var option in message) UpdateSelectionProcess(option);
        }
        );
    

    public static DataSaver optionSaver;

    public static List<CustomOption> AllOptions = new List<CustomOption>();
    public static List<CustomOption> TopOptions = new List<CustomOption>();

    static public CustomOptionTab CurrentTab = Module.CustomOptionTab.Settings;

    public int id;
    public UnityEngine.Color color;
    public string identifierName;
    public string name;
    public string format;
    public string prefix, suffix;
    public System.Object[] selections;

    public int defaultSelection;
    public IntegerDataEntry entry;
    public int selection;
    public CustomOption parent;
    public Predicate<CustomOptionTab>? yellowCondition;
    public List<CustomOption> children;
    public bool isHeader;
    public bool isHidden;
    public bool isHiddenOnDisplay;
    public bool isHiddenOnMetaScreen;
    public CustomGameMode GameMode;
    public CustomOptionTab tab;
    public bool showDetailForcely;

    public bool isProtected;

    private static int availableId = 1;

    static public CustomGameMode CurrentGameMode;

    static public TMPro.TextMeshPro UnderInfo = null;

    public List<CustomOption> prerequisiteOptions;
    public List<CustomOption> prerequisiteOptionsInv;
    public List<Func<bool>> prerequisiteOptionsCustom;
    public delegate MetaScreenContent[][] ScreenBuilder(Action refresher);
    public ScreenBuilder? preOptionScreenBuilder;
    public ScreenBuilder? postOptionScreenBuilder;
    public ScreenBuilder? alternativeOptionScreenBuilder;

    public CustomOptionDecorator? NameDecorator { get; set; }
    public CustomOptionDecorator? DisplayValueDecorator { get; set; }
    public CustomOptionDecorator? IntimateValueDecorator { get; set; }

    public virtual bool enabled
    {
        get
        {
            return this.getBool();
        }
    }

    public CustomOption HiddenOnDisplay(bool Hidden)
    {
        isHiddenOnDisplay = Hidden;
        return this;
    }

    public CustomOption HiddenOnMetaScreen(bool Hidden)
    {
        isHiddenOnMetaScreen = Hidden;
        return this;
    }

    public CustomOption SetGameMode(CustomGameMode gameMode)
    {
        GameMode = gameMode;
        return this;
    }

    public CustomOption Protect()
    {
        isProtected = true;
        return this;
    }

    public CustomOption SetIdentifier(string identifier)
    {
        identifierName = identifier;
        if (id > 0) bind();

        return this;
    }

    public bool IsHiddenDisplayInternal(CustomGameMode gameMode)
    {
        return isHidden || (0 == (int)(gameMode & GameMode))
            || prerequisiteOptions.Count > 0 && prerequisiteOptions.Any((option) => { return !option.getBool(); })
            || prerequisiteOptionsInv.Count > 0 && prerequisiteOptionsInv.Any((option) => { return option.getBool(); })
            || prerequisiteOptionsCustom.Count > 0 && prerequisiteOptionsCustom.Any((func) => { return !func.Invoke(); });
    }

    public bool IsHiddenInternal(CustomGameMode gameMode)
    {
        return (tab != CustomOptionTab.None && ((tab & CurrentTab) == 0)) || isHidden || (0 == (int)(gameMode & GameMode))
            || prerequisiteOptions.Count > 0 && prerequisiteOptions.Any((option) => { return !option.getBool(); })
            || prerequisiteOptionsInv.Count > 0 && prerequisiteOptionsInv.Any((option) => { return option.getBool(); })
            || prerequisiteOptionsCustom.Count > 0 && prerequisiteOptionsCustom.Any((func) => { return !func.Invoke(); });
    }

    public bool IsHidden(CustomGameMode gameMode)
    {
        return IsHiddenInternal(gameMode) || (parent != null && (parent.IsHidden(gameMode)));
    }

    public bool IsHiddenOnDisplay(CustomGameMode gameMode)
    {
        return isHiddenOnDisplay || IsHiddenDisplayInternal(gameMode) || (parent != null && parent.IsHiddenOnDisplay(gameMode));
    }

    public static void RegisterTopOption(CustomOption option) { TopOptions.Add(option); }

    // Option creation
    public CustomOption()
    {

    }

    public CustomOption(Color color, string name, System.Object[] selections, int defaultValue, CustomOption parent, bool isHeader, bool isHidden, string format, CustomOptionTab tab)
    {
        this.yellowCondition = null;

        this.id = availableId;
        availableId++;

        this.color = color;
        this.name = name;
        this.identifierName = name;
        this.format = format;
        this.selections = selections;
        int index = defaultValue;
        this.defaultSelection = index >= 0 ? index : 0;
        this.parent = parent;
        this.isHeader = isHeader;
        this.isHidden = isHidden;
        this.tab = tab;
        this.showDetailForcely = false;

        this.prefix = null;
        this.suffix = null;

        this.isHiddenOnDisplay = false;
        this.isHiddenOnMetaScreen = false;

        this.preOptionScreenBuilder = null;
        this.postOptionScreenBuilder = null;
        this.alternativeOptionScreenBuilder = null;

        this.children = new List<CustomOption>();
        if (parent != null)
        {
            parent.children.Add(this);
        }

        selection = 0;

        bind();

        AllOptions.Add(this);

        this.prerequisiteOptions = new List<CustomOption>();
        this.prerequisiteOptionsInv = new List<CustomOption>();
        this.prerequisiteOptionsCustom = new List<Func<bool>>();
        this.GameMode = CustomGameMode.Standard;

        this.NameDecorator = null;
        this.DisplayValueDecorator = null;
        this.IntimateValueDecorator = null;
    }

    private void bind()
    {
        entry = new IntegerDataEntry(identifierName, optionSaver, defaultSelection);
        selection = entry.Value;
        if (selections.Length > 0) selection = Mathf.Clamp(selection, 0, selections.Length - 1);
    }

    public static CustomOption Create(Color color, string name, IEnumerator<object> selections, object defaultValue, CustomOption parent = null, bool isHeader = false, bool isHidden = false, string format = "", CustomOptionTab tab = CustomOptionTab.None)
    {
        List<object> objects = new List<object>();
        while (selections.MoveNext())
        {
            objects.Add(selections.Current);
        }
        var ary = objects.ToArray();
        return new CustomOption(color, name, ary, Array.IndexOf(ary, defaultValue), parent, isHeader, isHidden, format, tab);
    }

    public static CustomOption Create(Color color, string name, string[] selections, string defaultValue, CustomOption parent = null, bool isHeader = false, bool isHidden = false, string format = "", CustomOptionTab tab = CustomOptionTab.None)
    {
        return new CustomOption(color, name, selections, Array.IndexOf(selections, defaultValue), parent, isHeader, isHidden, format, tab);
    }

    public static CustomOption Create(Color color, string name, float defaultValue, float min, float max, float step, CustomOption parent = null, bool isHeader = false, bool isHidden = false, string format = "", CustomOptionTab tab = CustomOptionTab.None)
    {
        List<float> selections = new List<float>();
        for (float s = min; s <= max; s += step)
            selections.Add(s);
        var ary = selections.Cast<object>().ToArray();
        return new CustomOption(color, name, ary, Array.IndexOf(ary, defaultValue), parent, isHeader, isHidden, format, tab);
    }

    public static CustomOption Create(Color color, string name, bool defaultValue, CustomOption parent = null, bool isHeader = false, bool isHidden = false, string format = "", CustomOptionTab tab = CustomOptionTab.None)
    {
        return new CustomOption(color, name, new string[] { "option.switch.off", "option.switch.on" }, defaultValue ? 1 : 0, parent, isHeader, isHidden, format, tab);
    }

    public static CustomOption Create(Color color, string name, int defaultValue, CustomOption parent = null, bool isHeader = false, bool isHidden = false, string format = "", CustomOptionTab tab = CustomOptionTab.None)
    {
        return new CustomOption(color, name, new string[] {}, defaultValue, parent, isHeader, isHidden, format, tab);
    }

    public static void loadOptionWithoutSync(string optionName, int selection)
    {
        foreach (CustomOption option in CustomOption.AllOptions)
        {
            if (option.identifierName != optionName) continue;

            if (option.isProtected) break;

            option.selection = selection;

            break;
        }
    }

    
    public CustomOption AddPrerequisite(CustomOption option)
    {
        prerequisiteOptions.Add(option);
        return this;
    }

    public CustomOption AddInvPrerequisite(CustomOption option)
    {
        prerequisiteOptionsInv.Add(option);
        return this;
    }

    public CustomOption AddCustomPrerequisite(Func<bool> func)
    {
        prerequisiteOptionsCustom.Add(func);
        return this;
    }

    /// <summary>
    /// オプションを黄色くする条件となるオプションを設定します。
    /// </summary>
    /// <param name="yellowCondition"></param>
    public void SetYellowCondition(Predicate<CustomOptionTab>? yellowCondition)
    {
        this.yellowCondition = yellowCondition;
    }

    // Getter

    public virtual int getSelection()
    {
        return selection;
    }

    public virtual bool getBool()
    {
        return selection > 0 || selections.Length == 1;
    }

    public virtual float getFloat()
    {
        return (float)selections[selection];
    }

    protected virtual string getStringSelection()
    {
        string sel = selections[selection].ToString();
        if (format != "")
        {
            return string.Format(Language.Language.GetString(format), sel);
        }
        float temp;
        if (float.TryParse(sel, out temp))
        {
            return sel;
        }
        return Language.Language.GetString(sel);
    }

    public string getRawString()
    {
        return selections[selection].ToString();
    }

    public string getString()
    {
        string text = getStringSelection();

        if (prefix != null)
        {
            text = Language.Language.GetString("option.prefix." + prefix) + text;
        }

        if (suffix != null)
        {
            text += Language.Language.GetString("option.suffix." + suffix);
        }

        if (IntimateValueDecorator != null)
            text = IntimateValueDecorator.Invoke(text,this);

        return text;
    }

    public virtual string getName(bool display = false, Color? color = null)
    {
        string original = Helpers.cs(color != null ? color.Value : this.color, Language.Language.GetString(name));

        if (NameDecorator != null && display)
        {
            return NameDecorator.Invoke(original, this);
        }
        else
        {
            return original;
        }
    }

    // Option changes

    public virtual void addSelection(int addSelection)
    {
        updateSelection(selection + addSelection);
    }

    public virtual void updateSelection(int newSelection)
    {
        if (selections.Length == 0)
            selection = newSelection;
        else
        {
            if (newSelection < 0)
            {
                selection = selections.Length - 1;
            }
            else
            {
                selection = newSelection % selections.Length;
            }
        }


        if (AmongUsClient.Instance?.AmHost == true && PlayerControl.LocalPlayer)
        {
            if (entry != null) entry.Value = selection; // Save selection to config
            ShareOption.Invoke(new(id,selection));
        }

    }

    public void SetParent(CustomOption newParent)
    {
        if (parent != null)
        {
            parent.children.Remove(this);
        }

        parent = newParent;
        if (parent != null)
        {
            parent.children.Add(this);
        }
    }

    public MetaScreenContent[] GetSelecterContents(Action refresher,float width = 1.5f)
    {
        return new MetaScreenContent[]
       {
                new MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () =>
                {
                    this.addSelection(-1);
                    refresher();
                }),
                new MSString(width, this.getString(), 2f, 0.6f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold, true, true),
                new MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () =>
                {
                    this.addSelection(1);
                    refresher();
                }),
       };
    }

    public MetaScreenContent[] GetStandardOptionContents(Action refresher)
    {
        var selecter = GetSelecterContents(refresher);
        return new MetaScreenContent[]
        {
            new MSOptionString(this,3f, this.getName(), 2f, 0.8f, TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold),
                new MSString(0.2f, ":", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                selecter[0],
                selecter[1],
                selecter[2],
                new MSMargin(1f),
        };
    }
}

public class CustomRoleOption : CustomOption
{
    public CustomOption countOption = null;

    public int rate
    {
        get
        {
            return getSelection();
        }
    }

    public int count
    {
        get
        {
            if (countOption != null)
                return Mathf.RoundToInt(countOption.getFloat());

            return 1;
        }
    }

    public (int, int) data
    {
        get
        {
            return (rate, count);
        }
    }

    public CustomRoleOption(string name, Color color, int max = 15) :
        base(color, name, CustomOptionHolder.rates, 0, null, true, false, "", CustomOptionTab.None)
    {
        if (max > 1)
            countOption = CustomOption.Create(Color.white, "option.roleNumAssigned", 1f, 1f, 15f, 1f, this, format: "unitPlayers");
    }
}

public class CustomOptionBlank : CustomOption
{
    public CustomOptionBlank(CustomOption parent)
    {
        this.parent = parent;
        this.id = -1;
        this.name = "";
        this.isHeader = false;
        this.isHidden = true;
        this.children = new List<CustomOption>();
        this.selections = new string[] { "" };
        AllOptions.Add(this);
    }

    public override int getSelection()
    {
        return 0;
    }

    public override bool getBool()
    {
        return true;
    }

    public override float getFloat()
    {
        return 0f;
    }

    protected override string getStringSelection()
    {
        return "";
    }

    public override void updateSelection(int newSelection)
    {
        return;
    }

}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoSpawnPlayer))]
public static class CoSpawnPlayerPatch
{
    public static void Postfix(PlayerPhysics __instance)
    {
        GameOptionsDataPatch.dirtyFlag = true;
    }
}

[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.InitializeOptions))]
public static class GameSettingMenuInitializePatch
{
    public static void Prefix(GameSettingMenu __instance)
    {
        var defaultTransform = __instance.AllItems.FirstOrDefault(x => x.gameObject.activeSelf && x.name.Equals("ResetToDefault", StringComparison.OrdinalIgnoreCase));
        if (defaultTransform != null)
            __instance.HideForOnline = new Transform[] { defaultTransform };
        else
            __instance.HideForOnline = new Transform[] { };
    }
}

delegate void OptionInitializer(GameOptionsMenu menu, StringOption stringTemplate, List<OptionBehaviour> options, GameObject settings);

public class CustomOptionBehaviour : MonoBehaviour
{
    static CustomOptionBehaviour()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomOptionBehaviour>();
    }

}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
class GameOptionsMenuStartPatch
{
    public static GameObject? nebulaSettings = null;
    public static GameObject? presetSettings = null;

    private static bool FixTab(GameObject? currentSettings, GameOptionsMenu __instance, string tabIconPath, string tabName, string settingsName, string settingsDisplayName, OptionInitializer initializer)
    {
        var tabs = GameSettingMenu.Instance.transform.FindChild("Header").FindChild("Tabs");
        tabs.gameObject.SetActive(true);
        
        if (currentSettings)
        {
            var gameGroup = currentSettings.transform.FindChild("GameGroup");
            gameGroup?.FindChild("Text").GetComponent<TMPro.TextMeshPro>().SetText(settingsDisplayName);
            return false;
        }

        var template = UnityEngine.Object.FindObjectOfType<StringOption>(true);
        if (template == null) return false;
        var gameSettings = __instance.transform.parent.parent.transform.gameObject;
        var customSettings = UnityEngine.Object.Instantiate(gameSettings, GameSettingMenu.Instance.transform);
        var customMenu = customSettings.transform.FindChild("GameGroup").FindChild("SliderInner").GetComponent<GameOptionsMenu>();
        UnityEngine.GameObject.SetName(customSettings, settingsName);

        var roleTab = tabs.FindChild("RoleTab");

        var customTab = UnityEngine.Object.Instantiate(roleTab, roleTab.transform.parent);
        customTab.gameObject.name = tabName;
        customTab.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = Helpers.loadSpriteFromResources(tabIconPath, 100f);


        foreach (OptionBehaviour option in customMenu.GetComponentsInChildren<OptionBehaviour>())
            UnityEngine.Object.Destroy(option.gameObject);
        List<OptionBehaviour> customOptions = new List<OptionBehaviour>();

        initializer(customMenu, template, customOptions, customSettings);

        customMenu.Children = customOptions.ToArray();
        customSettings.gameObject.SetActive(false);

        return true;
    }

    public static void OpenConfigSubOptionScreen(GameObject leftTabScreen, CustomOption topOption, int skip)
    {
        var designer = MetaScreen.OpenScreen(leftTabScreen, new Vector2(7.4f, 6f), new Vector2(4.7f, 0f));
        var gamemode = CustomOption.CurrentGameMode;



        designer.AddTopic(new MSButton(0.6f, 0.4f, "<<", TMPro.FontStyles.Bold, () =>
        {
            designer.screen.Close();
            OpenConfigTopOptionScreen(leftTabScreen);
        }), new MSMargin(0.2f),
        new MSString(6f, topOption.getName(), 3f, 3f, TMPro.TextAlignmentOptions.MidlineLeft, TMPro.FontStyles.Bold));
        designer.CustomUse(-0.2f);

        int leftSkip = skip;
        bool canIncrease = false;

        designer.AddTopic(new MSString(0.4f, skip > 0 ? "∧" : "", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));

        void refresher()
        {
            designer.screen.Close();
            OpenConfigSubOptionScreen(leftTabScreen, topOption, skip);
        }

        bool AddTopic(params MetaScreenContent[] contents)
        {
            if (leftSkip > 0)
            {
                leftSkip--;
                return true;
            }
            if (designer.Used > 4f)
            {
                canIncrease = true;
                return false;
            }

            designer.AddTopic(contents);

            return true;
        }

        bool AddOption(CustomOption option)
        {
            if (option.IsHidden(gamemode) || option.isHiddenOnMetaScreen) return true;

            if (option.preOptionScreenBuilder != null)
                foreach (var topic in option.preOptionScreenBuilder(refresher)) if (!AddTopic(topic)) return false;

            if (option.alternativeOptionScreenBuilder != null)
            {
                foreach (var topic in option.alternativeOptionScreenBuilder(refresher)) if (!AddTopic(topic)) return false;
            }
            else
            {
                if (!AddTopic(option.GetStandardOptionContents(refresher))) return false;
            }

            if (option.postOptionScreenBuilder != null)
                foreach (var topic in option.postOptionScreenBuilder(refresher)) if (!AddTopic(topic)) return false;

            if (option.getBool()) foreach (var child in option.children) if (!AddOption(child)) return false;
            return true;
        }

        if (topOption.preOptionScreenBuilder != null)
            foreach (var topic in topOption.preOptionScreenBuilder(refresher))
                if (!AddTopic(topic)) break;

        foreach (var option in topOption.children) if (!AddOption(option)) break;

        if (topOption.postOptionScreenBuilder != null)
            foreach (var topic in topOption.postOptionScreenBuilder(refresher))
                if (!AddTopic(topic)) break;


        designer.AddTopic(new MSString(0.4f, canIncrease ? "∨" : "", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));

        designer.CustomUse(4.55f - designer.Used);
        var underStr = new MSMultiString(8f,1.5f, " \n \n ", TMPro.TextAlignmentOptions.Top, TMPro.FontStyles.Bold);
        designer.AddTopic(underStr);
        CustomOption.UnderInfo = underStr.text;

        skip -= leftSkip;

        IEnumerator GetEnumerator()
        {
            float t = 0;
            while (t < 0.05f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            while (true)
            {
                var d = (int)Input.mouseScrollDelta.y;

                if (d > 0)
                {
                    if (skip > 0)
                    {
                        designer.screen.Close();
                        GameOptionsMenuStartPatch.OpenConfigSubOptionScreen(designer.screen.screen.transform.parent.gameObject, topOption, skip - 1);
                    }
                }
                else if (d < 0)
                {
                    if (canIncrease)
                    {
                        designer.screen.Close();
                        GameOptionsMenuStartPatch.OpenConfigSubOptionScreen(designer.screen.screen.transform.parent.gameObject, topOption, skip + 1);
                    }
                }

                yield return null;
            }
        }

        designer.screen.screen.AddComponent<CustomOptionBehaviour>().StartCoroutine(GetEnumerator().WrapToIl2Cpp());
    }

    private static Color? GetTabUnifiedColor(CustomOptionTab tab)
    {
        if (tab == CustomOptionTab.ImpostorRoles) return Palette.ImpostorRed;
        return null;
    }

    private static void OpenConfigTopOptionScreen(GameObject leftTabScreen)
    {
        var designer = MetaScreen.OpenScreen(leftTabScreen, new Vector2(5.4f, 6f), new Vector2(3.7f, 0f));
        var monitor = MetaScreen.OpenScreen(designer.screen.screen, new Vector2(3.2f, 6f), new Vector2(4.12f, 0f));
        var textArea = new MSTextArea(new Vector2(3.2f, 6f), "", 1.2f, TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Normal);
        monitor.AddTopic(textArea);

        var gamemode = CustomOptionHolder.GetCustomGameMode();
        List<MSButton> buttons = new List<MSButton>();
        List<CustomOption> options = new List<CustomOption>();

        void SetUpButtons()
        {
            int index = 0;

            foreach (var b in buttons)
            {
                var option = options[index];
                index++;

                b.button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    if (option == CustomOptionHolder.roleCountOption)
                    {
                        var builder = new StringBuilder();
                        GameOptionStringGenerator.GenerateRoleCountString(builder);
                        textArea.text.text = builder.ToString();
                    }
                    else
                    {
                        textArea.text.text = GameOptionStringGenerator.optionsToString(option);
                    }
                }));


                if (!option.showDetailForcely && option.selections.Length == 2 && option.getSelection() == 0)
                {
                    b.text.fontSize = b.text.fontSizeMax = 1.4f;
                    b.text.fontSizeMin = 0.7f;
                    continue;
                }


                b.text.fontSize = b.text.fontSizeMax = 1.2f;
                b.text.fontSizeMin = 0.7f;
                if (option.children.Count > 0)
                {
                    b.text.rectTransform.sizeDelta -= new Vector2(0.4f, 0f);
                    b.text.transform.localPosition -= new Vector3(0.2f, 0f, 0f);
                    var subButton = MetaScreen.MSDesigner.AddSubButton(b.button, new Vector2(0.32f, 0.32f), "button", ">");
                    subButton.transform.localPosition += new Vector3(0.55f, 0f);
                    subButton.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshPro>().fontStyle = TMPro.FontStyles.Bold;
                    subButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        designer.screen.Close();
                        OpenConfigSubOptionScreen(leftTabScreen, option, 0);
                    }));
                }

            }

            buttons.Clear();
            options.Clear();
        }

        foreach (var option in CustomOption.TopOptions)
        {
            if (option.IsHidden(gamemode)) continue;

            var myOption = option;
            buttons.Add(new MSButton(1.6f, 0.45f, option.getName(false, GetTabUnifiedColor(CustomOption.CurrentTab)), TMPro.FontStyles.Bold, () =>
            {
                if (myOption.selections.Length > 1)
                {
                    myOption.addSelection(1);
                    designer.screen.Close();
                    OpenConfigTopOptionScreen(leftTabScreen);
                }
            }, (myOption.selections.Length == 2 && myOption.getSelection() == 0) ? Palette.DisabledGrey : ((myOption.yellowCondition != null && myOption.yellowCondition(CustomOption.CurrentTab) ? Color.yellow : Color.white))));
            options.Add(option);

            if (buttons.Count == 3)
            {
                designer.AddTopic(buttons.ToArray());
                designer.CustomUse(-0.05f);
                SetUpButtons();
            }
        }
        if (buttons.Count > 0)
        {
            designer.AddTopic(buttons.ToArray());
            SetUpButtons();
        }
    }

    private static void OpenConfigScreen(GameObject setting)
    {
        var designer = MetaScreen.OpenScreen(setting, new Vector2(1.5f, 6f), new Vector2(-4.15f, -0.8f));

        var gameModeOption = CustomOptionHolder.GetCurrentGameModeOption();
        designer.AddTopic(new MSString(1.5f, gameModeOption.getName(), TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));
        designer.CustomUse(-0.08f);
        designer.AddTopic(new MSButton(1.5f, 0.4f, gameModeOption.getString(), TMPro.FontStyles.Bold, () =>
        {
            gameModeOption.addSelection(1);
            designer.screen.Close();

            //今のタブが存在しないゲームモードに変わる場合
            if (((Game.GameModeProperty.GetProperty(CustomOptionHolder.GetCustomGameMode()).Tabs) & CustomOption.CurrentTab) == 0) CustomOption.CurrentTab = (CustomOptionTab)1;

            OpenConfigScreen(setting);
        }));
        designer.CustomUse(0.2f);

        string[] names =
        {
                "settings","crewmateRoles","impostorRoles","neutralRoles","ghostRoles","modifiers","escapeRoles","advancedSettings"
            };
        Color[] colors =
        {
                Color.white,Palette.CrewmateBlue,Palette.ImpostorRed,new Color(255f/255f,170f/255f,0f),
                new Color(166f/255f,178f/255f,185f/255f),new Color(255f/255f,255f/255f,220f/255f),Color.yellow,
                new Color(128f/255f,194f/255f,255f/255f)
            };

        for (int i = 0; i < (int)CustomOptionTab.MaxValidTabs; i++)
        {

            if ((((int)Game.GameModeProperty.GetProperty(CustomOptionHolder.GetCustomGameMode()).Tabs) & (1 << i)) != 0)
            {
                int index = i;
                MSButton button = new MSButton(2f, 0.37f, Helpers.cs(colors[i], Language.Language.GetString("option.tab." + names[i])), TMPro.FontStyles.Bold, () =>
                {
                    CustomOption.CurrentTab = (Module.CustomOptionTab)(1 << index);
                    OpenConfigScreen(setting);
                    designer.screen.Close();
                }, colors[i].Blend(Color.white, 0.65f));
                designer.AddTopic(button);
                button.text.fontSize = button.text.fontSizeMax = 1.6f;
                button.text.fontSizeMin = 0.8f;
                designer.CustomUse(-0.08f);
            }
        }

        OpenConfigTopOptionScreen(designer.screen.screen);
    }

    private static bool FixNebulaTab(GameOptionsMenu __instance)
    {
        return FixTab(nebulaSettings, __instance, "Nebula.Resources.TabIcon.png", "NebulaTab", "NebulaSettings", "The Nebula Settings", (menu, temp, list, setting) =>
            {
                nebulaSettings = setting;
                nebulaSettings.transform.localPosition = new Vector3(0, 0, 0);
                nebulaSettings.GetComponent<AspectPosition>().enabled = false;
                setting.transform.DestroyChildren();

                OpenConfigScreen(setting);
            });
    }

    private static StringOption SetupStringOption(List<OptionBehaviour> list, StringOption template, GameOptionsMenu menu)
    {
        StringOption stringOption = UnityEngine.Object.Instantiate(template, menu.transform);
        stringOption.enabled = false;
        list.Add(stringOption);
        stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => { });
        stringOption.TitleText.text = "";
        stringOption.TitleText.rectTransform.sizeDelta = new Vector2(stringOption.TitleText.rectTransform.sizeDelta.x + 2.4f, stringOption.TitleText.rectTransform.sizeDelta.y);
        stringOption.TitleText.rectTransform.anchoredPosition = new Vector2(stringOption.TitleText.rectTransform.anchoredPosition.x + 1.2f, 0);

        stringOption.Value = 0;
        stringOption.ValueText.text = "";

        stringOption.gameObject.transform.FindChild("Plus_TMP").gameObject.SetActive(false);
        stringOption.gameObject.transform.FindChild("Minus_TMP").gameObject.SetActive(false);
        stringOption.ValueText.enabled = false;

        BoxCollider2D collider = stringOption.gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(4.5f, 0.45f);

        PassiveButton button = stringOption.gameObject.AddComponent<PassiveButton>();
        button.OnMouseOver = new UnityEngine.Events.UnityEvent();
        button.OnMouseOut = new UnityEngine.Events.UnityEvent();
        button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        foreach (var text in stringOption.gameObject.transform.GetComponentsInChildren<TMPro.TextMeshPro>())
        {
            text.enabled = true;
        }

        return stringOption;
    }

    private static bool FixPresetTab(GameOptionsMenu __instance)
    {
        return FixTab(presetSettings, __instance, "Nebula.Resources.TabIconPreset.png", "PresetTab", "PresetSettings", "Preset Settings", (menu, temp, list, setting) =>
         {
             presetSettings = setting;

             if (!CustomOptionPreset.SaveButton)
             {
                 StringOption stringOption = SetupStringOption(list, temp, menu);
                 stringOption.TitleText.text = Language.Language.GetString("preset.save");

                 PassiveButton button = stringOption.gameObject.GetComponent<PassiveButton>();
                 button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                 {
                     if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(FastDestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 0.8f);
                     CustomOptionPreset.Export().Output();
                     Helpers.ShowDialog("preset.dialog.save");
                 }));

                 CustomOptionPreset.SaveButton = stringOption;
             }
             CustomOptionPreset.SaveButton.gameObject.SetActive(true);

             CustomOptionPreset.LoadPresets();
             foreach (var preset in CustomOptionPreset.Presets)
             {
                 if (!preset.Option)
                 {
                     string name = preset.Name;

                     StringOption stringOption = SetupStringOption(list, temp, menu);
                     stringOption.TitleText.text = preset.Name;

                     PassiveButton button = stringOption.gameObject.GetComponent<PassiveButton>();
                     button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                     {
                         if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(FastDestroyableSingleton<HudManager>.Instance.TaskUpdateSound, false, 0.8f);
                         bool result = CustomOptionPreset.LoadAndInput("Presets/" + name + ".options");

                         Helpers.ShowDialog(result ? "preset.dialog.load" : "preset.dialog.loadFailed");
                     }));

                     preset.Option = stringOption;
                 }

                 preset.Option.gameObject.SetActive(true);
             }
         });
    }

    private static IEnumerator GetEnumrator(GameObject parent, List<GameObject> tabs)
    {
        while (true)
        {
            if (!parent) break;

            parent.SetActive(nebulaSettings.gameObject.active);

            if (nebulaSettings.gameObject.active)
            {
                int index = 0;
                int n = 1;
                foreach (var tab in tabs)
                {
                    if ((((int)Game.GameModeProperty.GetProperty(CustomOptionHolder.GetCustomGameMode()).Tabs) & n) != 0)
                    {
                        tab.SetActive(true);
                        tab.transform.localPosition = new Vector3((float)(index / 6) * -0.8f, -0.7f * (float)(index % 6));
                        index++;
                    }
                    else
                    {
                        tab.SetActive(false);
                    }
                    n <<= 1;
                }
            }
            yield return 0;
        }
        yield break;
    }

    public static void Postfix(GameOptionsMenu __instance)
    {
        bool result = false, f1 = FixNebulaTab(__instance), f2 = FixPresetTab(__instance);
        result = f1 | f2;

        if (!result) return;

        var header = GameSettingMenu.Instance.transform.FindChild("Header").FindChild("Tabs");
        var gameTab = header.FindChild("GameTab").gameObject;
        var nebulaTab = header.FindChild("NebulaTab").gameObject;
        var presetTab = header.FindChild("PresetTab").gameObject;
        header.FindChild("RoleTab").gameObject.SetActive(false);

        gameTab.transform.localPosition = new Vector3(-1f, 0, -5);
        //roleTab.transform.localPosition = new Vector3(-0.5f, 0, -5);
        nebulaTab.transform.localPosition = new Vector3(0f, 0, -5);
        presetTab.transform.localPosition = new Vector3(1f, 0, -5);

        var gameSettings = GameObject.Find("Game Settings");
        var gameSettingMenu = UnityEngine.Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();

        var nebulaTabHighlight = nebulaTab.transform.FindChild("Hat Button").FindChild("Tab Background").GetComponent<SpriteRenderer>();
        var presetTabHighlight = presetTab.transform.FindChild("Hat Button").FindChild("Tab Background").GetComponent<SpriteRenderer>();

        var tabs = new GameObject[] { gameTab, nebulaTab, presetTab };
        for (int i = 0; i < tabs.Length; i++)
        {
            var button = tabs[i].GetComponentInChildren<PassiveButton>();
            if (button == null) continue;
            int copiedIndex = i;
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                gameSettingMenu.RegularGameSettings.SetActive(false);
                gameSettingMenu.HideNSeekSettings.SetActive(false);
                gameSettingMenu.RolesSettings.gameObject.SetActive(false);
                nebulaSettings?.gameObject.SetActive(false);
                presetSettings?.gameObject.SetActive(false);
                gameSettingMenu.GameSettingsHightlight.enabled = false;
                gameSettingMenu.RolesSettingsHightlight.enabled = false;
                nebulaTabHighlight.enabled = false;
                presetTabHighlight.enabled = false;
                if (copiedIndex == 0)
                {
                    if(GameOptionsManager.Instance.currentGameMode==GameModes.Normal)
                        gameSettingMenu.RegularGameSettings.SetActive(true);
                    else
                        gameSettingMenu.HideNSeekSettings.SetActive(true);
                    gameSettingMenu.GameSettingsHightlight.enabled = true;
                }
                else if (copiedIndex == 1)
                {
                    nebulaSettings.gameObject.SetActive(true);
                    nebulaTabHighlight.enabled = true;
                }
                else if (copiedIndex == 2)
                {
                    presetSettings.gameObject.SetActive(true);
                    presetTabHighlight.enabled = true;
                }
            }));
        }


        var killCoolOption = __instance.Children.FirstOrDefault(x => x.name == "KillCooldown")?.TryCast<NumberOption>();
        if (killCoolOption != null) killCoolOption.ValidRange = new FloatRange(2.5f, 60f);

        var commonTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumCommonTasks")?.TryCast<NumberOption>();
        if (commonTasksOption != null) commonTasksOption.ValidRange = new FloatRange(0f, 4f);

        var shortTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumShortTasks")?.TryCast<NumberOption>();
        if (shortTasksOption != null) shortTasksOption.ValidRange = new FloatRange(0f, 23f);

        var longTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumLongTasks")?.TryCast<NumberOption>();
        if (longTasksOption != null) longTasksOption.ValidRange = new FloatRange(0f, 15f);

        var impostorsOption = __instance.Children.FirstOrDefault(x => x.name == "NumImpostors")?.TryCast<NumberOption>();
        if (impostorsOption != null) impostorsOption.ValidRange = new FloatRange(0f, 5f);

    }
}

/*
[HarmonyPatch(typeof(KeyValueOption), nameof(KeyValueOption.OnEnable))]
public class KeyValueOptionEnablePatch
{
    public static void Postfix(KeyValueOption __instance)
    {
        GameOptionsData gameOptions = PlayerControl.GameOptions;
        if (__instance.Title == StringNames.GameMapName)
        {
            __instance.Selected = gameOptions.MapId;
        }
        try
        {
            __instance.ValueText.text = __instance.Values[Mathf.Clamp(__instance.Selected, 0, __instance.Values.Count - 1)].Key;
        }
        catch { }
    }
}
*/

[HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
public class StringOptionEnablePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var setting = __instance.transform.parent.parent.parent;
        if (GameOptionsMenuStartPatch.presetSettings && setting == GameOptionsMenuStartPatch.presetSettings.transform)
        {


            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public class RpcSyncSettingsPatch
{
    public static void Postfix()
    {
        CustomOption.ShareOption.Invoke(new(int.MaxValue, GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumImpostors)));
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoSpawnPlayer))]
public class PlayerJoinedPatch
{
    public static void Postfix()
    {
        if(AmongUsClient.Instance.AmHost)CustomOption.ShareAllOptions.Invoke(CustomOption.AllOptions);
    }
}


[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
class GameOptionsMenuUpdatePatch
{
    private static float timer = 1f;
    public static void Postfix(GameOptionsMenu __instance)
    {
        timer += Time.deltaTime;
        if (timer < 0.1f) return;
        timer = 0f;

        float offset = 2.75f;


        var setting = __instance.transform.parent.parent;
        if (GameOptionsMenuStartPatch.presetSettings && setting == GameOptionsMenuStartPatch.presetSettings.transform)
        {
            offset = 2f;

            var saveButton = CustomOptionPreset.SaveButton;

            saveButton.gameObject.SetActive(true);
            saveButton.transform.localPosition = new Vector3(saveButton.transform.localPosition.x, offset, saveButton.transform.localPosition.z);

            offset -= 0.75f;


            foreach (var preset in CustomOptionPreset.Presets)
            {
                preset.Option.gameObject.SetActive(true);
                preset.Option.transform.localPosition = new Vector3(preset.Option.transform.localPosition.x, offset, preset.Option.transform.localPosition.z);

                offset -= 0.5f;

            }
        }
        else
        {
            return;
        }

        __instance.GetComponentInParent<Scroller>().ContentYBounds.max = (-offset) - 1.5f;
    }
}

/*
[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
class GameSettingMenuStartPatch
{ 
    public static void Postfix(GameSettingMenu __instance)
    {
        // Setup mapNameTransform
        var mapNameTransform = __instance.AllItems.FirstOrDefault(x => x.gameObject.activeSelf && x.name.Equals("MapName", StringComparison.OrdinalIgnoreCase));
        if (mapNameTransform == null) return;

        var options = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<string, int>>();
        for (int i = 0; i < Constants.MapNames.Length; i++)
        {
            var kvp = new Il2CppSystem.Collections.Generic.KeyValuePair<string, int>();
            kvp.key = Constants.MapNames[i];
            kvp.value = i;
            options.Add(kvp);
        }

        mapNameTransform.GetComponent<KeyValueOption>().Values = options;
    }
}
*/


public static class GameOptionStringGenerator
{
    public static string tl(string key)
    {
        return Language.Language.GetString(key);
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(GameOptionsData).GetMethods().Where(x => x.ReturnType == typeof(string) && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(int));
    }

    public static string optionToString(CustomOption option)
    {
        if (option == null) return "";
        string value = option.getString();
        if (option.DisplayValueDecorator != null) value = option.DisplayValueDecorator(value, option);
        return $"{option.getName(true)}: {value}";
    }

    public static string optionsToString(CustomOption option, bool skipFirst = false)
    {
        if (option == null) return "";

        List<string> options = new List<string>();
        if (!option.IsHiddenOnDisplay(CustomOption.CurrentGameMode) && !skipFirst) options.Add(optionToString(option));
        if (option.getBool())
        {
            foreach (CustomOption op in option.children)
            {
                string str = optionsToString(op);
                if (str != "") options.Add(str);
            }
        }
        return string.Join("\n", options);
    }

    public static void GenerateRoleCountString(StringBuilder entry)
    {
        string optionName;
        int min;
        int max;
        string optionValue;

        if ((int)(CustomOptionHolder.crewmateRolesCountMin.GameMode & CustomOption.CurrentGameMode) != 0)
        {
            optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), tl("option.crewmateRoles"));
            min = CustomOptionHolder.crewmateRolesCountMin.getSelection();
            max = CustomOptionHolder.crewmateRolesCountMax.getSelection();
            if (min > max) min = max;
            optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");
        }

        if ((int)(CustomOptionHolder.neutralRolesCountMin.GameMode & CustomOption.CurrentGameMode) != 0)
        {
            optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), tl("option.neutralRoles"));
            min = CustomOptionHolder.neutralRolesCountMin.getSelection();
            max = CustomOptionHolder.neutralRolesCountMax.getSelection();
            if (min > max) min = max;
            optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");
        }

        if ((int)(CustomOptionHolder.impostorRolesCountMin.GameMode & CustomOption.CurrentGameMode) != 0)
        {
            optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), tl("option.impostorRoles"));
            min = CustomOptionHolder.impostorRolesCountMin.getSelection();
            max = CustomOptionHolder.impostorRolesCountMax.getSelection();
            if (min > max) min = max;
            optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");
        }
    }

    public static List<string> GenerateString(int maxLines = 28)
    {
        List<string> pages = new List<string>();
        pages.Add(GameOptionsManager.Instance.CurrentGameOptions.ToHudString(PlayerControl.AllPlayerControls.Count));

        StringBuilder entry = new StringBuilder();
        List<string> entries = new List<string>();

        GenerateRoleCountString(entry);

        entries.Add(entry.ToString().Trim('\r', '\n'));

        void addChildren(CustomOption option, ref StringBuilder builder, bool indent = true, string inheritIndent = "")
        {
            if (!option.enabled || option.IsHiddenOnDisplay(CustomOption.CurrentGameMode)) return;

            foreach (var child in option.children)
            {
                if (!(child.IsHiddenOnDisplay(CustomOption.CurrentGameMode)))
                    builder.AppendLine((indent ? "    " : "") + inheritIndent + optionToString(child));
                addChildren(child, ref builder, indent, inheritIndent + (indent ? "    " : ""));
            }
        }

        foreach (CustomOption option in CustomOption.AllOptions)
        {
            if (option.isHiddenOnDisplay)
            {
                continue;
            }

            if (option.parent == null)
            {
                if (!option.enabled || option.IsHiddenOnDisplay(CustomOption.CurrentGameMode))
                {
                    continue;
                }

                entry = new StringBuilder();
                entry.AppendLine(optionToString(option));
                addChildren(option, ref entry, !option.isHidden);
                entries.Add(entry.ToString().Trim('\r', '\n'));
            }
        }

        int lineCount = 0;
        string page = "";
        foreach (var e in entries)
        {
            int lines = e.Count(c => c == '\n') + 1;

            if (lineCount + lines > maxLines)
            {
                pages.Add(page);
                page = "";
                lineCount = 0;
            }

            page = page + e + "\n\n";
            lineCount += lines + 1;
        }

        page = page.Trim('\r', '\n');
        if (page != "")
        {
            pages.Add(page);
        }

        return pages;
    }
}

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.FixedUpdate))]
public class GameOptionsDataPatch
{
    public static bool dirtyFlag = true;
    static List<String> pages = new List<string>();

    private static void Postfix()
    {
        if (GameOptionsManager.Instance.CurrentGameOptions == null) return;

        CustomOption.CurrentGameMode = CustomOptionHolder.GetCustomGameMode();

        if (dirtyFlag)
        {
            pages = GameOptionStringGenerator.GenerateString();
            dirtyFlag = false;
        }

        int numPages = pages.Count;
        int counter = CustomOptionHolder.optionsPage = CustomOptionHolder.optionsPage % numPages;
        FastDestroyableSingleton<HudManager>.Instance.GameSettings.text = pages[counter].Trim('\r', '\n') + "\n\n" + Language.Language.GetString("option.display.pressTabForMore") + $" ({counter + 1}/{numPages})";
    }
}

[HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Deserialize))]
public static class GameOptionsDeserializePatch
{
    static private int NumImpostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
    public static bool Prefix(GameOptionsData __instance)
    {
        NumImpostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
        return true;
    }

    public static void Postfix(GameOptionsData __instance)
    {
        try
        {
            GameOptionsManager.Instance.CurrentGameOptions.SetInt(Int32OptionNames.NumImpostors, NumImpostors);
        }
        catch{ }
    }
}

[HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Serialize))]
public static class GameOptionsSerializePatch
{
    static private int NumImpostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
    public static bool Prefix(GameOptionsData __instance)
    {
        try
        {
            NumImpostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
            if (NumImpostors == 0)
            {
                GameOptionsManager.Instance.CurrentGameOptions.SetInt(Int32OptionNames.NumImpostors, 1);
            }
            else if (NumImpostors > 3)
            {
                GameOptionsManager.Instance.CurrentGameOptions.SetInt(Int32OptionNames.NumImpostors, 3);
            }
        }
        catch { }
        return true;
    }

    public static void Postfix(GameOptionsData __instance)
    {
        try
        {
            GameOptionsManager.Instance.CurrentGameOptions.SetInt(Int32OptionNames.NumImpostors, NumImpostors);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
public static class GameOptionsNextPagePatch
{
    public static void Postfix(KeyboardJoystick __instance)
    {
        if (Input.GetKeyDown(KeyCode.Tab) && AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
        {
            CustomOptionHolder.optionsPage = CustomOptionHolder.optionsPage + 1;
        }
    }
}


[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public class GameSettingsScalePatch
{
    public static void Prefix(HudManager __instance)
    {
        if (LobbyBehaviour.Instance) if (__instance.GameSettings != null) __instance.GameSettings.fontSize = 1.2f;
    }
}

/*
[HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.Start))]
public class CreateOptionsPickerPatch
{
    public static void Postfix(CreateOptionsPicker __instance)
    {
        int numImpostors = __instance.GetTargetOptions().NumImpostors;
        if (numImpostors > 3)
        {
            numImpostors = 3;
        }else if (numImpostors < 1)
        {
            numImpostors = 1;
        }
        __instance.SetImpostorButtons(numImpostors);
    }
}
*/

[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.Deserialize))]
public static class RpcSyncSettingPatch
{
    public static void Postfix(LogicOptions __instance)
    {
        GameOptionsDataPatch.dirtyFlag = true;
    }
}