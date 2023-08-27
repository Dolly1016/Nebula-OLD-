using Il2CppInterop.Generator;
using Nebula.Configuration;
using Nebula.Roles;
using Nebula.Roles.Impostor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Game;

public class NebulaEndCriteria
{
    int gameModeMask;

    public bool IsValidCriteria => (gameModeMask & GeneralConfigurations.CurrentGameMode) != 0;

    public Func<CustomEndCondition?>? OnUpdate = null;
    public Func<PlayerControl?,Tuple<CustomEndCondition,int>?>? OnExiled = null;
    public Func<CustomEndCondition?>? OnTaskUpdated = null;

    public NebulaEndCriteria(int gameModeMask)
    {
        this.gameModeMask = gameModeMask;
    }

    static public NebulaEndCriteria SabotageCriteria = new(0xFFFF)
    {
        OnUpdate = () =>
        {
            if (ShipStatus.Instance != null)
            {
                var status = ShipStatus.Instance;
                if (status.Systems != null)
                {
                    ISystemType? systemType = status.Systems.ContainsKey(SystemTypes.LifeSupp) ? status.Systems[SystemTypes.LifeSupp] : null;
                    if (systemType != null)
                    {
                        LifeSuppSystemType lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>();
                        if (lifeSuppSystemType != null && lifeSuppSystemType.Countdown < 0f)
                        {
                            lifeSuppSystemType.Countdown = 10000f;
                            return NebulaGameEnd.ImpostorWin;
                        }
                    }
                    ISystemType? systemType2 = status.Systems.ContainsKey(SystemTypes.Reactor) ? status.Systems[SystemTypes.Reactor] : null;
                    if (systemType2 == null)
                    {
                        systemType2 = status.Systems.ContainsKey(SystemTypes.Laboratory) ? status.Systems[SystemTypes.Laboratory] : null;
                    }
                    if (systemType2 != null)
                    {
                        ICriticalSabotage? criticalSystem = systemType2.TryCast<ICriticalSabotage>();
                        if (criticalSystem != null && criticalSystem.Countdown < 0f)
                        {
                            criticalSystem.ClearSabotage();
                            return NebulaGameEnd.ImpostorWin;
                        }
                    }
                }
            }
            return null;
        }
    };

    static public NebulaEndCriteria CrewmateAliveCriteria = new(0xFFFF)
    {
        OnUpdate = () =>
        {
            if (NebulaGameManager.Instance?.AllPlayerInfo().Any(p =>
            {
                if (p.IsDead) return false;
                if (p.Role.Role.Team == Impostor.MyTeam) return true;
                return false;
            }) ?? true) return null;

            return NebulaGameEnd.CrewmateWin;
        }
    };

    static public NebulaEndCriteria CrewmateTaskCriteria = new(0xFFFF)
    {
        OnTaskUpdated = () =>
        {
            int quota = 0;
            int completed = 0;
            foreach(var p in NebulaGameManager.Instance.AllPlayerInfo())
            {
                if (!p.Tasks.IsCrewmateTask) continue;
                quota += p.Tasks.Quota;
                completed += p.Tasks.TotalCompleted;
            }
            return (quota > 0 && quota <= completed) ? NebulaGameEnd.CrewmateWin : null;
        }
    };

    static public NebulaEndCriteria ImpostorKillCriteria = new(0xFFFF)
    {
        OnUpdate = () =>
        {
            int impostors = 0;
            int totalAlive = 0;
            foreach(var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDead) continue;
                totalAlive++;
                if (p.Role.Role.Team == Impostor.MyTeam) impostors++;
            }

            return impostors * 2 >= totalAlive ? NebulaGameEnd.ImpostorWin : null;
        }
    };
}

public class CriteriaManager
{
    HashSet<NebulaEndCriteria> monitorings = new();

    public void AddCriteria(NebulaEndCriteria criteria)
    {
        monitorings.Add(criteria);
    }

    public CustomEndCondition? CheckEnd(Func<NebulaEndCriteria,CustomEndCondition?> checker)
    {
        //ホスト以外はゲーム終了チェックをしない
        if (!AmongUsClient.Instance.AmHost) return null;

        CustomEndCondition? end = null;
        foreach (var c in monitorings)
        {
            var temp = checker.Invoke(c);
            if (temp == null) continue;
            if (end != null && temp.Priority < end.Priority) continue;
            end = temp;
        }
        return end;
    }

    public Tuple<CustomEndCondition, int>? CheckEnd(Func<NebulaEndCriteria, Tuple<CustomEndCondition,int>?> checker)
    {
        //ホスト以外はゲーム終了チェックをしない
        if (!AmongUsClient.Instance.AmHost) return null;

        Tuple<CustomEndCondition, int>? end = null;
        foreach (var c in monitorings)
        {
            var temp = checker.Invoke(c);
            if (temp == null) continue;
            if (end != null && temp.Item1.Priority < end.Item1.Priority) continue;
            end = temp;
        }
        return end;
    }

    public CustomEndCondition? OnUpdate() => CheckEnd(c=>c.OnUpdate?.Invoke());
    public Tuple<CustomEndCondition,int>? OnExiled(PlayerControl? exiled) => CheckEnd(c => c.OnExiled?.Invoke(exiled));
    public CustomEndCondition? OnTaskUpdated() => CheckEnd(c => c.OnTaskUpdated?.Invoke());
}
