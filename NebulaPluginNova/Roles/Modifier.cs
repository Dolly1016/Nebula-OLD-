using Il2CppSystem.Reflection.Metadata.Ecma335;
using Nebula.Configuration;
using Nebula.Roles.Assignment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Roles.Assignment.IRoleAllocator;

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
    public virtual IEnumerable<IAssignableBase> RelatedOnConfig() { yield break; }
    public virtual ConfigurationHolder? RelatedConfig { get => null; }


    public AbstractModifier()
    {
        Roles.Register(this);
    }
}

public abstract class IntroAssignableModifier : AbstractModifier
{
    public virtual void Assign(IRoleAllocator.RoleTable roleTable) { }
    public abstract string CodeName { get; }
}

public abstract class ConfigurableModifier : IntroAssignableModifier
{
    public ConfigurationHolder RoleConfig { get; private set; }
    public override ConfigurationHolder? RelatedConfig { get => RoleConfig; }

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
        SerializableDocument.RegisterColor("role." + InternalName, RoleColor);
        RoleConfig = new ConfigurationHolder("options.role." + InternalName, new ColorTextComponent(RoleColor, new TranslateTextComponent("role." + LocalizedName + ".name")), myTabMask ?? ConfigurationTab.Modifiers, CustomGameMode.AllGameModeMask);
        RoleConfig.RelatedAssignable = this;
        LoadOptions();
    }
}

public abstract class ConfigurableStandardModifier : ConfigurableModifier
{
    NebulaConfiguration CrewmateRoleCountOption;
    NebulaConfiguration ImpostorRoleCountOption;
    NebulaConfiguration NeutralRoleCountOption;
    NebulaConfiguration RoleChanceOption;

    public ConfigurableStandardModifier(int TabMask) : base(TabMask)
    {
    }

    public ConfigurableStandardModifier()
    {
    }

    protected override void LoadOptions() {
        CrewmateRoleCountOption = new(RoleConfig, "crewmateCount", new TranslateTextComponent("options.role.crewmateCount"), 15, 0, 0);
        ImpostorRoleCountOption = new(RoleConfig, "impostorCount", new TranslateTextComponent("options.role.impostorCount"), 5, 0, 0);
        NeutralRoleCountOption = new(RoleConfig, "neutralCount", new TranslateTextComponent("options.role.neutralCount"), 10, 0, 0);
        RoleChanceOption = new(RoleConfig, "chance", new TranslateTextComponent("options.modifier.chance"), 10f, 100f, 10f, 0f, 0f) { Decorator = NebulaConfiguration.PercentageDecorator };
    }

    private void TryAssign(IRoleAllocator.RoleTable roleTable,RoleCategory category,int num)
    {
        int reallyNum = 0;
        float chance = RoleChanceOption.GetFloat() / 100f;
        for (int i = 0; i < num; i++) if (!(chance < 1f) || (float)System.Random.Shared.NextDouble() < chance) reallyNum++;

        var players = roleTable.GetPlayers(category).Where(tuple=>tuple.role.CanLoad(this)).OrderBy(i => Guid.NewGuid()).ToArray();
        reallyNum = Mathf.Min(players.Length, reallyNum);

        for (int i = 0; i < reallyNum; i++) roleTable.SetModifier(players[i].playerId, this);
    }

    public override void Assign(IRoleAllocator.RoleTable roleTable)
    {
        TryAssign(roleTable, RoleCategory.CrewmateRole, CrewmateRoleCountOption);
        TryAssign(roleTable, RoleCategory.ImpostorRole, ImpostorRoleCountOption);
        TryAssign(roleTable, RoleCategory.NeutralRole, NeutralRoleCountOption);
    }
}
