using AmongUs.GameOptions;
using Il2CppSystem.Reflection.Metadata.Ecma335;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Utilities;
using static Il2CppMono.Security.X509.X520;

namespace Nebula.Roles;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class NebulaRoleHoler : Attribute
{

}

public enum RoleCategory
{
    ImpostorRole,
    NeutralRole,
    CrewmateRole
}

public abstract class AbstractRole : IAssignableBase
{
    public abstract RoleCategory RoleCategory { get; }
    //内部用の名称。AllRolesのソートに用いる
    public virtual string InternalName { get => LocalizedName; }
    //翻訳キー用の名称。
    public abstract string LocalizedName { get; }
    public virtual string DisplayName { get => Language.Translate("role." + LocalizedName + ".name"); }
    public virtual string IntroBlurb { get => Language.Translate("role." + LocalizedName + ".blurb"); }
    public abstract Color RoleColor { get; }
    public virtual bool IsDefaultRole { get => false; }
    public abstract RoleInstance CreateInstance(PlayerModInfo player, int[] arguments);
    public int Id { get; set; }
    public abstract int RoleCount { get; }
    public abstract float GetRoleChance(int count);
    public abstract Team Team { get; }

    //追加付与ロールに役職プールの占有性があるか(追加付与ロールが無い場合、無意味)
    public virtual bool HasAdditionalRoleOccupancy { get => true; }
    public virtual AbstractRole[]? AdditionalRole { get => null; }


    //For Config
    public virtual NebulaModifierFilterConfigEntry? ModifierFilter { get => null; }
    public virtual IEnumerable<IAssignableBase> RelatedOnConfig() { yield break; }
    public virtual ConfigurationHolder? RelatedConfig { get => null; }

    public virtual bool CanLoadDefault(IntroAssignableModifier modifier) => true;
    public virtual bool CanLoad(IntroAssignableModifier modifier)=> CanLoadDefault(modifier);

    public virtual bool CanBeGuess { get => true; }

    public abstract void Load();

    public AbstractRole()
    {
        Roles.Register(this);
    }
}

public abstract class ConfigurableRole : AbstractRole {
    public ConfigurationHolder RoleConfig { get; private set; }
    public override ConfigurationHolder? RelatedConfig { get => RoleConfig; }

    private int? myTabMask;
    public ConfigurableRole(int TabMask)
    {
        myTabMask = TabMask;
    }

    public ConfigurableRole() {
    }

    protected virtual void LoadOptions() { }

    public override sealed void Load()
    {
        RoleConfig = new ConfigurationHolder("options.role." + InternalName, new ColorTextComponent(RoleColor, new TranslateTextComponent("role." + LocalizedName + ".name")), myTabMask ?? ConfigurationTab.FromRoleCategory(RoleCategory), CustomGameMode.AllGameModeMask);
        RoleConfig.Priority = IsDefaultRole ? 0 : 1;
        RoleConfig.RelatedAssignable = this;
        LoadOptions();
    }

    public class KillCoolDownConfiguration
    {
        public enum KillCoolDownType
        {
            Immediate = 0,
            Relative = 1,
            Ratio = 2
        }

        private NebulaConfiguration selectionOption;
        private NebulaConfiguration immediateOption, relativeOption, ratioOption;

        public NebulaConfiguration EditorOption => selectionOption;

        private float minCoolDown;

        private NebulaConfiguration GetCurrentOption()
        {
            switch (selectionOption.CurrentValue)
            {
                case 0:
                    return immediateOption;
                case 1:
                    return relativeOption;
                case 2:
                    return ratioOption;
            }
            return null;
        }
        private static string[] AllSelections = new string[] { "options.killCoolDown.type.immediate", "options.killCoolDown.type.relative", "options.killCoolDown.type.ratio" };

        private static Func<object?, string> RelativeDecorator = (mapped) =>
        {
            float val = (float)mapped;
            string str = val.ToString();
            if (val > 0f) str = "+" + str;
            else if (!(val < 0f)) str = "±" + str;
            return str + Language.Translate("options.sec");
        };

        public KillCoolDownConfiguration(ConfigurationHolder holder, string id, KillCoolDownType defaultType, float step, float immediateMin, float immediateMax, float relativeMin, float relativeMax, float ratioStep, float ratioMin, float ratioMax, float defaultImmediate, float defaultRelative, float defaultRatio)
        {
            selectionOption = new NebulaConfiguration(holder, id, null, AllSelections, (int)defaultType, (int)defaultType);
            selectionOption.Editor = () =>
            {
                var currentOption = GetCurrentOption();

                return new CombinedContext(0.55f, IMetaContext.AlignmentOption.Center,
                    new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(2.5f, TMPro.TextAlignmentOptions.Left)) { RawText = selectionOption.Title.Text },
                    NebulaConfiguration.OptionTextColon,
                    new MetaContext.HorizonalMargin(0.04f),
                    NebulaConfiguration.OptionButtonContext(() => selectionOption.ChangeValue(true), selectionOption.ToDisplayString(), 0.9f),
                    new MetaContext.HorizonalMargin(0.2f),
                    NebulaConfiguration.OptionButtonContext(() => currentOption.ChangeValue(false), "<<"),
                    new MetaContext.Text(NebulaConfiguration.OptionValueAttr) { RawText = currentOption.ToDisplayString() },
                    NebulaConfiguration.OptionButtonContext(() => currentOption.ChangeValue(true), ">>")
                );
            };
            selectionOption.Shower = () =>
            {
                var str = selectionOption.Title.Text + " : ";
                switch (selectionOption.CurrentValue)
                {
                    case 0:
                        str += immediateOption!.ToDisplayString();
                        break;
                    case 1:
                        str += relativeOption!.ToDisplayString();
                        break;
                    case 2:
                        str += ratioOption!.ToDisplayString();
                        break;
                }
                str += (" (" + NebulaConfiguration.SecDecorator.Invoke(KillCoolDown) + ")").Color(Color.gray);
                return str;
            };

            immediateOption = new NebulaConfiguration(holder, id + ".immediate", null, immediateMin, immediateMax, step, defaultImmediate, defaultImmediate) { Decorator = NebulaConfiguration.SecDecorator, Editor = NebulaConfiguration.EmptyEditor };
            immediateOption.Shower = null;
            
            relativeOption = new NebulaConfiguration(holder, id + ".relative", null, relativeMin, relativeMax, step, defaultRelative, defaultRelative) { Decorator = RelativeDecorator, Editor = NebulaConfiguration.EmptyEditor };
            relativeOption.Shower = null;
            
            ratioOption = new NebulaConfiguration(holder, id + ".ratio", null, ratioMin, ratioMax, ratioStep, defaultRatio, defaultRatio) { Decorator = NebulaConfiguration.OddsDecorator, Editor = NebulaConfiguration.EmptyEditor };
            ratioOption.Shower = null;

            minCoolDown = immediateMin;
        }

        public float KillCoolDown
        {
            get
            {
                float vanillaCoolDown = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
                switch (selectionOption.CurrentValue)
                {
                    case 0:
                        return immediateOption.GetFloat();
                    case 1:
                        return Mathf.Max(relativeOption.GetFloat() + vanillaCoolDown, minCoolDown);
                    case 2:
                        return ratioOption.GetFloat() * vanillaCoolDown;
                }
                return vanillaCoolDown;
            }
        }
    }

    public class VentConfiguration
    {
        private NebulaConfiguration selectionOption;
        private NebulaConfiguration? coolDownOption, durationOption, usesOption;
        public VentConfiguration(ConfigurationHolder holder, (int min, int max, int defaultValue)? ventUses, (float min, float max, float defaultValue)? ventCoolDown, (float min, float max, float defaultValue)? ventDuration)
        {
            selectionOption = new NebulaConfiguration(holder, "ventOption", new TranslateTextComponent("role.general.ventOption"), false, false);

            coolDownOption = durationOption = usesOption = null;

            List<IMetaParallelPlacable> list = new();
            list.Add(new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(1.4f,TMPro.TextAlignmentOptions.Left)) { MyText = selectionOption.Title });
            list.Add(NebulaConfiguration.OptionTextColon);

            void AddOptionToEditor(NebulaConfiguration config)
            {
                list.Add(new MetaContext.HorizonalMargin(0.1f));
                list.Add(new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(1.1f)) { MyText = config!.Title });
                list.Add(NebulaConfiguration.OptionButtonContext(() => config.ChangeValue(false), "<<"));
                list.Add(new MetaContext.Text(NebulaConfiguration.OptionShortValueAttr) { MyText = new LazyTextComponent(() => config.ToDisplayString()) });
                list.Add(NebulaConfiguration.OptionButtonContext(() => config.ChangeValue(true), ">>"));
            }

            if (ventCoolDown.HasValue)
            {
                coolDownOption = new NebulaConfiguration(holder, "ventCoolDown", new TranslateTextComponent("role.general.ventCoolDown"), ventCoolDown.Value.min, ventCoolDown.Value.max, 2.5f, ventCoolDown.Value.defaultValue, ventCoolDown.Value.defaultValue) { Editor = NebulaConfiguration.EmptyEditor, Decorator = NebulaConfiguration.SecDecorator };
                coolDownOption.Shower = null;
                AddOptionToEditor(coolDownOption);
            }
            if (ventDuration.HasValue)
            {
                durationOption = new NebulaConfiguration(holder, "ventDuration", new TranslateTextComponent("role.general.ventDuration"), ventDuration.Value.min, ventDuration.Value.max, 2.5f, ventDuration.Value.defaultValue, ventDuration.Value.defaultValue) { Editor = NebulaConfiguration.EmptyEditor, Decorator = NebulaConfiguration.SecDecorator };
                durationOption.Shower = null;
                AddOptionToEditor(durationOption);
            }
            if (ventUses.HasValue)
            {
                usesOption = new NebulaConfiguration(holder, "ventUses", new TranslateTextComponent("role.general.ventUses"), ventUses.Value.min, ventUses.Value.max, ventUses.Value.defaultValue, ventUses.Value.defaultValue) { Editor = NebulaConfiguration.EmptyEditor };
                usesOption.Shower = null;
                AddOptionToEditor(usesOption);
            }

            selectionOption.Editor = () => new CombinedContext(0.55f, IMetaContext.AlignmentOption.Center, list.ToArray());
            selectionOption.Shower = () =>
            {
                var str = selectionOption.Title.Text + " :";
                if (coolDownOption != null) str += "\n" + Language.Translate("role.general.ventCoolDown.short") + " : " + coolDownOption.ToDisplayString();
                if (durationOption != null) str += "\n" + Language.Translate("role.general.ventDuration.short") + " : " + durationOption.ToDisplayString();
                if (usesOption != null) str += "\n" + Language.Translate("role.general.ventUses.short") + " : " + usesOption.ToDisplayString();
                return str;
            };
        }

        public int Uses => usesOption?.GetMappedInt() ?? 0;
        public float CoolDown => coolDownOption?.GetFloat() ?? 0f;
        public float Duration => durationOption?.GetFloat() ?? 0f;
    }
}

public abstract class ConfigurableStandardRole : ConfigurableRole
{
    protected NebulaConfiguration RoleCountOption { get; private set; }
    protected NebulaConfiguration RoleChanceOption { get; private set; }
    protected NebulaConfiguration RoleSecondaryChanceOption { get; private set; }
    private NebulaModifierFilterConfigEntry modifierFilter;
    public override NebulaModifierFilterConfigEntry? ModifierFilter { get => modifierFilter; }

    public ConfigurableStandardRole(int TabMask):base(TabMask){ }
    public ConfigurableStandardRole() : base() { }

    public override int RoleCount => RoleCountOption.CurrentValue;
    public override float GetRoleChance(int count)
    {
        if (count > 0 && RoleSecondaryChanceOption.CurrentValue > 0)
            return RoleSecondaryChanceOption.GetFloat();
        return RoleChanceOption.GetFloat();
    }
    public override bool CanLoad(IntroAssignableModifier modifier) => CanLoadDefault(modifier) && !modifierFilter.Contains(modifier);

    protected static TranslateTextComponent CountOptionText = new("options.role.count");
    protected static TranslateTextComponent ChanceOptionText = new("options.role.chance");
    protected static TranslateTextComponent SecondaryChanceOptionText = new("options.role.secondaryChance");
    protected override void LoadOptions() {
        RoleCountOption = new(RoleConfig, "count", CountOptionText, 15, 0, 0);
        RoleConfig.IsActivated = () => RoleCountOption > 0;

        RoleChanceOption = new(RoleConfig, "chance", ChanceOptionText, 10f, 100f, 10f, 0f, 0f) { Decorator = NebulaConfiguration.PercentageDecorator };
        RoleChanceOption.Editor = () =>
        {
            if (RoleCount <= 1)
            {
                return new CombinedContext(0.55f, IMetaContext.AlignmentOption.Center,
                new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(1.8f)) { RawText = ChanceOptionText.Text },
                NebulaConfiguration.OptionTextColon,
                NebulaConfiguration.OptionButtonContext(() => RoleChanceOption.ChangeValue(false), "<<"),
                new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(0.8f)) { RawText = RoleChanceOption.ToDisplayString() },
                NebulaConfiguration.OptionButtonContext(() => RoleChanceOption.ChangeValue(true), ">>")
                );
            }
            else
            {
                return new CombinedContext(0.55f, IMetaContext.AlignmentOption.Center,
               new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(1.8f)) { RawText = ChanceOptionText.Text },
               NebulaConfiguration.OptionTextColon,
               NebulaConfiguration.OptionButtonContext(() => RoleChanceOption.ChangeValue(false), "<<"),
               new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(0.8f)) { RawText = RoleChanceOption.ToDisplayString() },
               NebulaConfiguration.OptionButtonContext(() => RoleChanceOption.ChangeValue(true), ">>"),
               new MetaContext.HorizonalMargin(0.3f),
               new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(1.4f)) { RawText = SecondaryChanceOptionText.Text },
               NebulaConfiguration.OptionTextColon,
               NebulaConfiguration.OptionButtonContext(() => RoleSecondaryChanceOption.ChangeValue(false), "<<"),
               new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(0.8f)) { RawText = RoleSecondaryChanceOption.ToDisplayString() },
               NebulaConfiguration.OptionButtonContext(() => RoleSecondaryChanceOption.ChangeValue(true), ">>")
               );
            }
        };

        RoleSecondaryChanceOption = new(RoleConfig, "secondaryChance", SecondaryChanceOptionText, 0f, 100f, 10f, 0f, 0f) { Decorator = (mapped)=>
        {
            if ((float)mapped > 0f)
                return NebulaConfiguration.PercentageDecorator.Invoke(mapped);
            else
                return Language.Translate("options.followPrimaryChance");
        }
        };
        RoleSecondaryChanceOption.Editor = NebulaConfiguration.EmptyEditor;

        RoleCountOption.Shower = () => {
            var str = Language.Translate("options.role.count.short") + " : " + RoleCountOption.ToDisplayString();
            if (RoleCountOption > 1 && RoleSecondaryChanceOption.CurrentValue > 0)
                str += " (" + RoleChanceOption.ToDisplayString() + "," + RoleSecondaryChanceOption.ToDisplayString() + ")";
            else if(RoleCountOption >= 1)
                str += " (" + RoleChanceOption.ToDisplayString() + ")";
            return str;
        };

        RoleChanceOption.Shower = null;
        RoleSecondaryChanceOption.Shower = null;

        modifierFilter = new NebulaModifierFilterConfigEntry(RoleConfig.Id + ".modifierFilter", Array.Empty<string>());
    }   
}