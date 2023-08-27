using Nebula.Modules;

namespace Nebula.Roles;

public abstract class RoleInstance : AssignableInstance
{
    public abstract AbstractRole Role { get; }

    public RoleInstance(PlayerModInfo player):base(player)
    {
    }

    public virtual bool CanInvokeSabotage => Role.RoleCategory == RoleCategory.ImpostorRole;
    public virtual bool HasVanillaKillButton => Role.RoleCategory == RoleCategory.ImpostorRole;
    public virtual bool CanReport => true;
    public virtual bool CanUseVent => Role.RoleCategory != RoleCategory.CrewmateRole;
    public virtual bool CanMoveInVent => true;
    public virtual Timer? VentCoolDown => null;
    public virtual Timer? VentDuration => null;
    public virtual string DisplayRoleName => Role.DisplayName.Color(Role.RoleColor);
    public virtual bool CheckWins(CustomEndCondition endCondition) => false;
    public virtual bool HasCrewmateTasks => Role.RoleCategory == RoleCategory.CrewmateRole;
    public virtual bool HasAnyTasks => HasCrewmateTasks;
}
