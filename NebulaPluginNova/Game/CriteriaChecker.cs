using Il2CppInterop.Generator;
using Nebula.Configuration;
using Nebula.Roles;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
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
    public Func<PlayerControl?, Tuple<CustomEndCondition, int>?>? OnExiled = null;
    public Func<CustomEndCondition?>? OnTaskUpdated = null;

    public NebulaEndCriteria(int gameModeMask = 0xFFFF)
    {
        this.gameModeMask = gameModeMask;
    }

    static public NebulaEndCriteria SabotageCriteria = new()
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
                        LifeSuppSystemType lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>()!;
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

    static public NebulaEndCriteria CrewmateAliveCriteria = new()
    {
        OnUpdate = () =>
        {
            if (NebulaGameManager.Instance?.AllPlayerInfo().Any(p =>
            {
                if (p.IsDead) return false;
                if (p.Role.Role.Team == Impostor.MyTeam) return true;
                if (p.Role.Role.Team == Jackal.MyTeam || p.AllModifiers.Any(m => m.Role == SidekickModifier.MyRole)) return true;
                return false;
            }) ?? true) return null;

            return NebulaGameEnd.CrewmateWin;
        }
    };

    static public NebulaEndCriteria CrewmateTaskCriteria = new()
    {
        OnTaskUpdated = () =>
        {
            int quota = 0;
            int completed = 0;
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (!p.Tasks.IsCrewmateTask) continue;
                quota += p.Tasks.Quota;
                completed += p.Tasks.TotalCompleted;
            }
            return (quota > 0 && quota <= completed) ? NebulaGameEnd.CrewmateWin : null;
        }
    };

    static public NebulaEndCriteria ImpostorKillCriteria = new()
    {
        OnUpdate = () =>
        {
            int impostors = 0;
            int totalAlive = 0;
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDead) continue;
                totalAlive++;
                if (p.Role.Role.Team == Impostor.MyTeam) impostors++;

                //ジャッカル陣営が生存している間は勝利できない
                if (p.Role.Role.Team == Jackal.MyTeam || p.AllModifiers.Any(m => m.Role == SidekickModifier.MyRole)) return null;
            }

            return impostors * 2 >= totalAlive ? NebulaGameEnd.ImpostorWin : null;
        }
    };

    static public NebulaEndCriteria JackalKillCriteria = new()
    {
        OnUpdate = () =>
        {
            int jackals = 0;
            int totalAlive = 0;
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDead) continue;
                totalAlive++;
                if (p.Role.Role.Team == Jackal.MyTeam || p.AllModifiers.Any(m => m.Role == SidekickModifier.MyRole)) jackals++;

                //インポスターが生存している間は勝利できない
                if (p.Role.Role.Team == Impostor.MyTeam) return null;
            }

            return jackals * 2 >= totalAlive ? NebulaGameEnd.ImpostorWin : null;
        }
    };

    static public NebulaEndCriteria LoversCriteria = new()
    {
        OnUpdate = () =>
        {
            int totalAlive = NebulaGameManager.Instance!.AllPlayerInfo().Count((p) => !p.IsDead);
            if (totalAlive != 3) return null;

            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDead) continue;
                totalAlive++;
                if (p.TryGetModifier<Lover.Instance>(out var lover)){
                    if (lover.MyLover?.IsDead ?? true) continue;

                    return NebulaGameEnd.LoversWin;
                }

            }

            return null;
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
