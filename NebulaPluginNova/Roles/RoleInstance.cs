using Nebula.Modules;

namespace Nebula.Roles;


public abstract class AssignableInstance
{
    public PlayerControl player { get; private init; }
    private List<INebulaScriptComponent> myComponent { get; init; }

    public bool AmOwner => player.AmOwner;

    public AssignableInstance(PlayerControl player)
    {
        this.player = player;
        this.myComponent = new();
    }

    protected T Bind<T>(T component) where T : INebulaScriptComponent
    {
        BindComponent(component);
        return component;
    } 

    protected void BindComponent(INebulaScriptComponent component) => myComponent.Add(component);
    
    public void Inactivate()
    {
        foreach (INebulaScriptComponent component in myComponent) NebulaGameManager.Instance?.ReleaseComponent(component);
        myComponent.Clear();

        OnInactivated();
    }

    public virtual void OnGameStart() { }
    public virtual void Update() { }
    public virtual void LocalUpdate() { }
    public virtual void OnMeetingStart() { }
    public virtual void OnDead() { }
    public virtual void OnExiled() { }
    public virtual void OnMurdered(PlayerControl murder) { }
    public virtual void OnPlayerDead(PlayerControl dead) { }
    public virtual void OnActivated() { }
    protected virtual void OnInactivated() { }
}

public abstract class RoleInstance : AssignableInstance
{
    public abstract AbstractRole Role { get; }

    public RoleInstance(PlayerControl player):base(player)
    {
    }

    public virtual bool CanInvokeSabotage => Role.RoleCategory == RoleCategory.ImpostorRole;
    public virtual bool HasVanillaKillButton => Role.RoleCategory != RoleCategory.CrewmateRole;
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
