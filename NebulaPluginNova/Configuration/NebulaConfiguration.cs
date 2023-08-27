using Epic.OnlineServices;
using Il2CppSystem.ComponentModel;
using Il2CppSystem.Runtime.Remoting.Messaging;
using MS.Internal.Xml.XPath;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Roles.Assignment;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Linq.Expressions.Interpreter.NullableMethodCallInstruction;

namespace Nebula.Configuration;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class NebulaOptionHolder : Attribute
{
}

[NebulaPreLoad(typeof(Roles.Roles))]
[NebulaRPCHolder]
public class NebulaConfigEntry
{
    static public DataSaver ConfigData = new DataSaver("Config");
    static public List<NebulaConfigEntry> AllConfig = new();

    static public void Load()
    {
        var types = Assembly.GetAssembly(typeof(RemoteProcessBase))?.GetTypes().Where((type) => type.IsDefined(typeof(NebulaOptionHolder)));
        if (types == null) return;
        foreach (var type in types) System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);

        AllConfig.Sort((c1, c2) => string.Compare(c1.Name, c2.Name));

        for (int i = 0; i < AllConfig.Count; i++) AllConfig[i].Id = i;

        ConfigurationHolder.Load();
    }

    public int CurrentValue { get; private set; }
    private IntegerDataEntry dataEntry;
    public int Id { get; private set; } = -1;
    public string Name { get; private init; }

    public NebulaConfigEntry(string id, int defaultValue)
    {
        dataEntry = new IntegerDataEntry(id, ConfigData, defaultValue);
        LoadFromSaveData();
        AllConfig.Add(this);
        Name = id;
    }

    public void LoadFromSaveData()
    {
        CurrentValue = dataEntry.Value;
    }

    public void UpdateValue(int value, bool save)
    {
        CurrentValue = value;
        if (save) dataEntry.Value = value;
    }

    static private RemoteProcess<Tuple<int, int>> RpcShare = new RemoteProcess<Tuple<int, int>>(
        "ShareOption",
        (writer, message) =>
        {
            writer.Write(message.Item1);
            writer.Write(message.Item2);
        },
       (reader) =>
       {
           return new Tuple<int, int>(reader.ReadInt32(),reader.ReadInt32());
       },
       (message, isCalledByMe) =>
       {
           if (!isCalledByMe) AllConfig[message.Item1].UpdateValue(message.Item2, false);
       }
    );

    //呼び出し時の引数は使用していない
    static private DivisibleRemoteProcess<int, Tuple<int, int>> RpcShareAll = new DivisibleRemoteProcess<int, Tuple<int, int>>(
        "ShareAllOption",
        (message) =>
        {
            //(Item1)番目から(Item2)-1番目まで
            IEnumerator<Tuple<int, int>> GetDivider()
            {
                int done = 0;
                while (done < AllConfig.Count)
                {
                    int max = Mathf.Min(AllConfig.Count, done + 500);
                    yield return new Tuple<int, int>(done, max);
                    done = max;
                }
            }
            return GetDivider();
        },
        (writer, message) =>
        {
            writer.Write(message.Item1);
            writer.Write(message.Item2 - message.Item1);
            for (int i = message.Item1; i < message.Item2; i++) writer.Write(AllConfig[i].CurrentValue);
        },
       (reader) =>
       {
           int index = reader.ReadInt32();
           int num = reader.ReadInt32();
           for(int i = 0; i < num; i++)
           {
               AllConfig[index].UpdateValue(reader.ReadInt32(), false);
               index++;
           }
           return new Tuple<int, int>(0, 0);
       },
       (message, isCalledByMe) =>
       {
           //メッセージを受け取ったときに処理しているのでここでは何もしない
       }
    );

    public void Share()
    {
        RpcShare.Invoke(new Tuple<int, int>(Id,CurrentValue));
    }

    static public void ShareAll()
    {
        RpcShareAll.Invoke(0);
    }

    static public void RestoreAllAndShare()
    {
        foreach (var cfg in AllConfig) cfg.LoadFromSaveData();
        ShareAll();
    }
}


public class ConfigurationHolder
{
    static public List<ConfigurationHolder> AllHolders = new();
    static public void Load()
    {
        AllHolders.Sort((c1, c2) =>
        {
            if (c1.Priority != c2.Priority) return c1.Priority - c2.Priority;
            return string.Compare(c1.Id, c2.Id);
        });
    }

    private NebulaConfigEntry? entry = null;
    private Func<bool>? predicate = null;
    private int tabMask,gamemodeMask;
    public string Id { get; private set; }
    public int Priority { get; set; }
    public ITextComponent Title { get; private init; }
    private List<NebulaConfiguration> myConfigurations = new();
    internal void RegisterOption(NebulaConfiguration config) => myConfigurations.Add(config);

    public ConfigurationHolder(string id, ITextComponent? title,int tabMask,int gamemodeMask)
    {
        Id = id;
        AllHolders.Add(this);
        this.tabMask = tabMask;
        this.gamemodeMask = gamemodeMask;
        this.Title = title ?? new TranslateTextComponent(id);
        this.Priority = 0;
    }

    public ConfigurationHolder SetDefaultShownState(bool shownDefault)
    {
        if (entry == null) entry = new NebulaConfigEntry(Id, shownDefault ? 1 : 0);
        return this;
    }

    public ConfigurationHolder SetPredicate(Func<bool> predicate)
    {
        this.predicate = predicate;
        return this;
    }

    public IMetaContext GetContext()
    {
        MetaContext context = new();
        foreach(var config in myConfigurations)
        {
            if (!config.IsShown) continue;

            var editor = config.GetEditor();
            if (editor != null) context.Append(editor);
        }
        return context;
    }

    public int TabMask => tabMask;
    public int GameModeMask => gamemodeMask;
    public bool IsShown => ((entry?.CurrentValue ?? 1) == 1) && (predicate?.Invoke() ?? true);
    public void Toggle()
    {
        if (entry == null) return;
        entry.UpdateValue(entry.CurrentValue == 1 ? 0 : 1, true);
        entry.Share();
    }
}


public class NebulaConfiguration
{
    private NebulaConfigEntry entry;
    public ConfigurationHolder? MyHolder { get; private set; }
    public Func<object?, string>? Decorator { get; set; } = null;
    public Func<int, object?>? Mapper { get; set; } = null;
    public Func<bool>? Predicate { get; set; } = null;
    public Func<IMetaContext?>? Editor { get; set; } = null;
    public bool LoopAtBothEnds { get; set; } = true;
    public int MaxValue { get; private init; }
    private int InvalidatedValue { get; init; }
    public ITextComponent Title { get; private init; }
    public bool IsShown => (MyHolder?.IsShown ?? true) && (Predicate?.Invoke() ?? true);

    public IMetaContext? GetEditor()
    {
        if (Editor != null)
            return Editor.Invoke();
        return new CombinedContent(0.55f, IMetaContext.AlignmentOption.Center,
            new MetaContext.Text(OptionTitleAttr) { RawText = Title.Text },
            OptionTextColon,
            OptionButtonContext(() => ChangeValue(false), "<<"),
            new MetaContext.Text(OptionValueAttr) { RawText = ToDisplayString() },
            OptionButtonContext(() => ChangeValue(true),">>")
            );
    }

    static protected Func<IMetaContext?> HideConfigurationEditor = () => null;
    static public TextAttribute GetOptionBoldAttr(float width, TMPro.TextAlignmentOptions alignment = TMPro.TextAlignmentOptions.Center) => new(TextAttribute.BoldAttr)
    {
        FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
        Size = new Vector2(width, 0.4f),
        Alignment = alignment
    };
    static protected TextAttribute OptionTitleAttr = GetOptionBoldAttr(4f,TMPro.TextAlignmentOptions.Left);
    static protected TextAttribute OptionValueAttr = GetOptionBoldAttr(1.1f);
    static protected TextAttribute OptionButtonAttr = new(TextAttribute.BoldAttr) {
        FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
        Size = new Vector2(0.32f, 0.22f) 
    };
    static public MetaContext.Button OptionButtonContext(Action clickAction,string rawText) {
        return new MetaContext.Button(() =>
        {
            clickAction();
            if (NebulaSettingMenu.Instance) NebulaSettingMenu.Instance.UpdateSecondaryPage();
        }, OptionButtonAttr)
        {
            RawText = rawText,
            PostBuilder = (button, renderer, text) => { renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; }
        };
    }
    static protected IMetaParallelPlacable OptionTextColon => new MetaContext.Text(new(OptionTitleAttr) { Size = new Vector2(0.2f, 0.4f), Alignment = TMPro.TextAlignmentOptions.Center }) { RawText = ":" };
    

    static public Func<object?, string> PercentageDecorator = (mapped) => mapped + Language.Translate("options.percentage");
    static public Func<object?, string> OddsDecorator = (mapped) => mapped + Language.Translate("options.cross");
    static public Func<object?, string> SecDecorator = (mapped) => mapped + Language.Translate("options.sec");
    static public Func<IMetaContext?> EmptyEditor = () => null;

    public NebulaConfiguration(ConfigurationHolder? holder, string id,ITextComponent? title, int maxValue,int defaultValue,int invalidatedValue)
    {
        MaxValue = maxValue;
        defaultValue = Mathf.Clamp(defaultValue,0,maxValue);
        InvalidatedValue = Mathf.Clamp(invalidatedValue, 0, maxValue);

        MyHolder = holder;
        MyHolder?.RegisterOption(this);

        string entryId = id;
        if (holder != null) entryId = holder.Id + "." + entryId;

        entry = new NebulaConfigEntry(entryId, defaultValue);
        Title = title ?? new TranslateTextComponent(entryId);
    }

    public NebulaConfiguration(ConfigurationHolder holder, string id, ITextComponent? title, int minValue,int maxValue, int defaultValue, int invalidatedValue) :
        this(holder, id, title, maxValue-minValue, defaultValue-minValue, invalidatedValue - minValue)
    {
        Mapper = (i) => i + minValue;
    }

    public NebulaConfiguration(ConfigurationHolder holder, string id, ITextComponent? title, bool defaultValue, bool invalidatedValue) :
        this(holder, id, title, 1, defaultValue ? 1 : 0, invalidatedValue ? 1 : 0)
    {
        Mapper = (i) => i == 1;
        Decorator = (v) => Language.Translate((bool)v! ? "options.switch.on" : "options.switch.off");
    }

    public NebulaConfiguration(ConfigurationHolder holder,string id, ITextComponent? title, string[] selections,string defaultValue,string invalidatedValue):
        this(holder,id, title, selections.Length-1,Array.IndexOf(selections,defaultValue), Array.IndexOf(selections, invalidatedValue))
    {
        Mapper = (i) => selections[i];
        Decorator = (v) => Language.Translate((string?)v);
    }

    public NebulaConfiguration(ConfigurationHolder holder, string id, ITextComponent? title, string[] selections, int defaultIndex,int invalidatedIndex) :
       this(holder, id, title, selections.Length - 1, defaultIndex, invalidatedIndex)
    {
        Mapper = (i) => selections[i];
        Decorator = (v) => Language.Translate((string?)v);
    }

    public NebulaConfiguration(ConfigurationHolder holder, string id, ITextComponent? title, float[] selections, float defaultValue, float invalidatedValue) :
        this(holder, id, title, selections.Length - 1, Array.IndexOf(selections, defaultValue), Array.IndexOf(selections, invalidatedValue))
    {
        Mapper = (i) => selections[i];
    }

    public NebulaConfiguration(ConfigurationHolder holder, string id, ITextComponent? title, float min,float max,float step, float defaultValue, float invalidatedValue) :
        this(holder, id, title, (int)((max - min) / step), (int)((defaultValue-min)/step), (int)((invalidatedValue - min) / step))
    {
        Mapper = (i) => (float)(step * i + min);
    }

    public void ChangeValue(bool increment)
    {
        var current = entry.CurrentValue;
        current += increment ? 1 : -1;
        if (LoopAtBothEnds)
        {
            if (current < 0) current = MaxValue;
            if (current > MaxValue) current = 0;
        }
        else
            current = Mathf.Clamp(current, 0, MaxValue);
        entry.UpdateValue(current, true);
        entry.Share();
    }

    public void ChangeValue(int newValue)
    {
        entry.UpdateValue(Mathf.Clamp(newValue, 0, MaxValue), true);
        entry.Share();
    }

    public int CurrentValue => IsShown ? entry.CurrentValue : InvalidatedValue;
    
    public object? GetMapped()
    {
        return Mapper?.Invoke(CurrentValue) ?? CurrentValue;
    }

    public int? GetMappedInt()
    {
        return (int?)GetMapped();
    }

    public float? GetFloat()
    {
        return (float?)GetMapped();
    }

    public string? GetString()
    {
        return GetMapped()?.ToString();
    }

    public bool? GetBool()
    {
        return (bool?)GetMapped();
    }

    public string ToDisplayString()
    {
        return Decorator?.Invoke(GetMapped()) ?? GetString() ?? "None";
    }
}

public class CustomGameMode
{
    public static List<CustomGameMode> allGameMode = new List<CustomGameMode>();
    public static CustomGameMode Standard = new CustomGameMode(0x01, "gamemode.standard", new StandardRoleAllocator(), 4)
        .AddEndCriteria(NebulaEndCriteria.SabotageCriteria)
        .AddEndCriteria(NebulaEndCriteria.ImpostorKillCriteria)
        .AddEndCriteria(NebulaEndCriteria.CrewmateAliveCriteria)
        .AddEndCriteria(NebulaEndCriteria.CrewmateTaskCriteria);
    public static CustomGameMode FreePlay = new CustomGameMode(0x02, "gamemode.freeplay", new AllCrewmateRoleAllocator(), 0);
    public static int AllGameModeMask = Standard | FreePlay;

    private int bitFlag;
    public string TranslateKey { get; private init; }
    public IRoleAllocator RoleAllocator { get; private init; }
    public List<NebulaEndCriteria> GameModeCriteria { get; private init; } = new();
    public int MinPlayers { get; private init; }
    public CustomGameMode(int bitFlag,string translateKey, IRoleAllocator roleAllocator, int minPlayers)
    {
        this.bitFlag = bitFlag;
        this.RoleAllocator = roleAllocator;
        allGameMode.Add(this);
        this.TranslateKey = translateKey;
        MinPlayers = minPlayers;
    }

    private CustomGameMode AddEndCriteria(NebulaEndCriteria criteria)
    {
        GameModeCriteria.Add(criteria);
        return this;
    }
    public static ReadOnlyCollection<CustomGameMode> AllGameMode { get => allGameMode.AsReadOnly(); }

    public static implicit operator int(CustomGameMode gamemode) => gamemode.bitFlag;
}

public class ConfigurationTab
{
    public static List<ConfigurationTab> allTab = new List<ConfigurationTab>();
    public static ConfigurationTab Settings = new ConfigurationTab(0x01,"options.tab.setting",new Color(0.75f,0.75f,0.75f));
    public static ConfigurationTab CrewmateRoles = new ConfigurationTab(0x02, "options.tab.crewmate", Palette.CrewmateBlue);
    public static ConfigurationTab ImpostorRoles = new ConfigurationTab(0x04, "options.tab.impostor", Palette.ImpostorRed);
    public static ConfigurationTab NeutralRoles = new ConfigurationTab(0x08, "options.tab.neutral", new Color(244f / 255f, 211f / 255f, 53f / 255f));

    private int bitFlag;
    private string translateKey { get; init; }
    public Color Color { get; private init; }
    public ConfigurationTab(int bitFlag, string translateKey,Color color)
    {
        this.bitFlag = bitFlag;
        allTab.Add(this);
        this.translateKey = translateKey;
        Color = color;
    }

    public string DisplayName { get => Language.Translate(translateKey).Replace("[", StringHelper.ColorBegin(Color)).Replace("]", StringHelper.ColorEnd()); }
    public static ReadOnlyCollection<ConfigurationTab> AllTab { get => allTab.AsReadOnly(); }

    public static ConfigurationTab FromRoleCategory(RoleCategory roleCategory)
    {
        switch (roleCategory)
        {
            case RoleCategory.CrewmateRole:
                return CrewmateRoles;
            case RoleCategory.ImpostorRole:
                return ImpostorRoles;
            case RoleCategory.NeutralRole:
                return NeutralRoles;
        }
        return Settings;
    }

    public static implicit operator int(ConfigurationTab tab) => tab.bitFlag;
    


}