using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles;

public abstract class AbstractModifier : IAssignableBase
{
    public virtual string InternalName { get => LocalizedName; }
    public abstract string LocalizedName { get; }
    public virtual string DisplayName { get => Language.Translate("role." + LocalizedName + ".name"); }
    public abstract Color RoleColor { get; }
    public abstract ModifierInstance CreateInstance(PlayerModInfo player, int[]? arguments);
    public int Id { get; set; }


    public AbstractModifier()
    {
        Roles.Register(this);
    }
}

public abstract class ConfigurableModifier : AbstractModifier
{
    public ConfigurationHolder RoleConfig { get; private init; }

    public ConfigurableModifier(int TabMask)
    {
        RoleConfig = new ConfigurationHolder("options.role." + LocalizedName, new ColorTextComponent(RoleColor, new TranslateTextComponent("role." + LocalizedName + ".name")), TabMask, CustomGameMode.AllGameModeMask);
        LoadOptions();
    }

    public ConfigurableModifier()
    {
        RoleConfig = new ConfigurationHolder("options.role." + LocalizedName, new ColorTextComponent(RoleColor, new TranslateTextComponent("role." + LocalizedName + ".name")), ConfigurationTab.Modifiers, CustomGameMode.AllGameModeMask);
        LoadOptions();
    }

    protected virtual void LoadOptions() { }
}
