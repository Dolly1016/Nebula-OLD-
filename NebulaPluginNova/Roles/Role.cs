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

public abstract class AbstractRole
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
    public abstract RoleInstance CreateInstance(PlayerControl player, int[]? arguments);
    public int Id { get; internal set; }
    public abstract int RoleCount { get; }
    public abstract float GetRoleChance(int count);
    public abstract Team Team { get; }

    //追加付与ロールに役職プールの占有性があるか(追加付与ロールが無い場合、無意味)
    public virtual bool HasAdditionalRoleOccupancy { get => true; }
    public virtual AbstractRole[]? AdditionalRole { get => null; }

    public AbstractRole()
    {
        Roles.Register(this);
    }
}

public abstract class ConfigurableRole : AbstractRole {
    public ConfigurationHolder RoleConfig { get; private init; }

    public ConfigurableRole(int TabMask)
    {
        RoleConfig = new ConfigurationHolder("options.role." + LocalizedName, new ColorTextComponent(RoleColor, new TranslateTextComponent("role." + LocalizedName + ".name")), TabMask, CustomGameMode.AllGameModeMask);
        RoleConfig.Priority = IsDefaultRole ? 0 : 1;
        LoadOptions();
    }

    public ConfigurableRole() {
        RoleConfig = new ConfigurationHolder("options.role." + LocalizedName, new ColorTextComponent(RoleColor, new TranslateTextComponent("role." + LocalizedName + ".name")), ConfigurationTab.FromRoleCategory(RoleCategory), CustomGameMode.AllGameModeMask);
        RoleConfig.Priority = IsDefaultRole ? 0 : 1;
        LoadOptions();
    }

    protected virtual void LoadOptions() { }
}

public abstract class ConfigurableStandardRole : ConfigurableRole
{
    protected NebulaConfiguration RoleCountOption { get; private set; }
    protected NebulaConfiguration RoleChanceOption { get; private set; }
    protected NebulaConfiguration RoleSecondaryChanceOption { get; private set; }

    public ConfigurableStandardRole(int TabMask):base(TabMask){ }
    public ConfigurableStandardRole() : base() { }

    public override int RoleCount => RoleCountOption.CurrentValue;
    public override float GetRoleChance(int count)
    {
        if (count > 0 && RoleSecondaryChanceOption.CurrentValue > 0)
            return RoleSecondaryChanceOption.GetFloat() ?? 0f;
        return RoleChanceOption.GetFloat() ?? 0f;
    }

    protected static TranslateTextComponent CountOptionText = new("options.role.count");
    protected static TranslateTextComponent ChanceOptionText = new("options.role.chance");
    protected static TranslateTextComponent SecondaryChanceOptionText = new("options.role.secondaryChance");
    protected override void LoadOptions() {
        RoleCountOption = new(RoleConfig, "count", CountOptionText, 15, 0, 0);
        
        RoleChanceOption = new(RoleConfig, "chance", ChanceOptionText, 10f, 100f, 10f, 0f, 0f) { Decorator = NebulaConfiguration.PercentageDecorator };
        RoleChanceOption.Editor = () =>
        {
            if (RoleCount <= 1)
            {
                return new CombinedContent(0.55f, IMetaContext.AlignmentOption.Center,
                new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(1.8f)) { RawText = ChanceOptionText.Text },
                NebulaConfiguration.OptionButtonContext(() => RoleChanceOption.ChangeValue(false), "<<"),
                new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(0.8f)) { RawText = RoleChanceOption.ToDisplayString() },
                NebulaConfiguration.OptionButtonContext(() => RoleChanceOption.ChangeValue(true), ">>")
                );
            }
            else
            {
                return new CombinedContent(0.55f, IMetaContext.AlignmentOption.Center,
               new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(1.8f)) { RawText = ChanceOptionText.Text },
               NebulaConfiguration.OptionButtonContext(() => RoleChanceOption.ChangeValue(false), "<<"),
               new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(0.8f)) { RawText = RoleChanceOption.ToDisplayString() },
               NebulaConfiguration.OptionButtonContext(() => RoleChanceOption.ChangeValue(true), ">>"),
               new MetaContext.HorizonalMargin(0.3f),
               new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(1.4f)) { RawText = SecondaryChanceOptionText.Text },
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
    }
}