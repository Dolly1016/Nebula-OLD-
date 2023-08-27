using Nebula.Modules;

namespace Nebula.Roles;


public abstract class AssignableInstance : ScriptHolder
{
    public PlayerControl player { get; private init; }

    public bool AmOwner => player.AmOwner;

    public AssignableInstance(PlayerControl player)
    {
        this.player = player;
    }

    public void Inactivate()
    {
        Release();
        OnInactivated();
    }

    public virtual void OnGameStart() { }
    public virtual void Update() { }
    public virtual void LocalUpdate() { }
    public virtual void OnMeetingStart() { }
    public virtual void OnGameReenabled() { }
    public virtual void OnDead() { }
    public virtual void OnExiled() { }
    public virtual void OnMurdered(PlayerControl murder) { }
    public virtual void OnKillPlayer(PlayerControl target) { }
    public virtual void OnPlayerDeadLocal(PlayerControl dead) { }
    public virtual void OnActivated() { }
    public virtual void OnSetTaskLocal(ref List<GameData.TaskInfo> tasks) { }
    public virtual void OnTaskCompleteLocal() { }
    protected virtual void OnInactivated() { }
}

public abstract class RoleInstance : AssignableInstance
{
    public abstract AbstractRole Role { get; }

    public RoleInstance(PlayerControl player):base(player)
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
    public virtual void DecoratePlayerName(PlayerModInfo player,ref string text,ref Color color) { }
}
