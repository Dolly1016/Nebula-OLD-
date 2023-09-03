using Il2CppSystem.Reflection.Metadata.Ecma335;
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
    public abstract ModifierInstance CreateInstance(PlayerModInfo player, int[] arguments);
    public int Id { get; set; }

    public virtual void Load() { }

    public AbstractModifier()
    {
        Roles.Register(this);
    }
}

public abstract class ConfigurableModifier : AbstractModifier
{
    public ConfigurationHolder RoleConfig { get; private set; }

    public int? myTabMask;

    public ConfigurableModifier(int TabMask)
    {
        myTabMask = TabMask;
        
    }

    public ConfigurableModifier()
    {
    }

    protected virtual void LoadOptions() { }

    public sealed override void Load()
    {
        RoleConfig = new ConfigurationHolder("options.role." + InternalName, new ColorTextComponent(RoleColor, new TranslateTextComponent("role." + LocalizedName + ".name")), myTabMask ?? ConfigurationTab.Modifiers, CustomGameMode.AllGameModeMask);
        LoadOptions();
    }
}
