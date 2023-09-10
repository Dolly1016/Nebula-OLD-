using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles;

public interface IAssignableBase
{
    public ConfigurationHolder? RelatedConfig { get; }
    public string InternalName { get; }
    public string LocalizedName { get; }
    public string DisplayName { get; }
    public Color RoleColor { get; }
    public int Id { get; set; }

    public void Load();

    //For Config
    public IEnumerable<IAssignableBase> RelatedOnConfig();
}

public abstract class AssignableInstance : ScriptHolder
{
    public PlayerModInfo MyPlayer { get; private init; }

    public bool AmOwner => MyPlayer.AmOwner;

    public AssignableInstance(PlayerModInfo player)
    {
        this.MyPlayer = player;
    }

    public void Inactivate()
    {
        Release();
        OnInactivated();
    }

    public virtual bool CheckWins(CustomEndCondition endCondition) => false;
    public virtual void OnGameStart() { }
    public virtual void Update() { }
    public virtual void LocalUpdate() { }
    public virtual void OnMeetingStart() { }
    public virtual void OnMeetingEnd() { }
    public virtual void OnEndVoting() { }
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
    public virtual void OnCastVoteLocal(byte target,ref int vote) { }
    public virtual void OnDeadBodyGenerated(DeadBody deadBody) { }
    public virtual void DecoratePlayerName(ref string text, ref Color color) { }
    public virtual void DecorateOtherPlayerName(PlayerModInfo player,ref string text, ref Color color) { }

    public virtual void OnTieVotes(ref List<byte> extraVotes,PlayerVoteArea myVoteArea) { }

    public virtual void OnOpenSabotageMap() { }
    public virtual void OnOpenNormalMap() { }
    public virtual void OnOpenAdminMap() { }
    public virtual void OnMapInstantiated() { }
}