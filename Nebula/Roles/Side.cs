using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Nebula.Patches;

namespace Nebula.Roles
{
    public class Side
    {
        public enum IntroDisplayOption
        {
            STANDARD,
            SHOW_ALL,
            SHOW_ONLY_ME
        }

        public static Side Crewmate = new Side("Crewmate", "crewmate", IntroDisplayOption.SHOW_ALL, Palette.CrewmateBlue, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (statistics.GetAlivePlayers(Impostor) == 0 && 
            statistics.GetAlivePlayers(Jackal) == 0 && 
            !Game.GameData.data.AllPlayers.Values.Any((p) => p.IsAlive && p.extraRole.Contains(Roles.SecondarySidekick)))
            {
                return EndCondition.CrewmateWinByVote;
            }
            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks && GameData.Instance.CompletedTasks>0)
            {
                return EndCondition.CrewmateWinByTask;
            }

            return null;
        });

        public static Side Impostor = new Side("Impostor", "impostor", IntroDisplayOption.STANDARD, Palette.ImpostorRed, (PlayerStatistics statistics, ShipStatus status) =>
        {
            //Sabotage
            if (status.Systems != null)
            {
                ISystemType systemType = status.Systems.ContainsKey(SystemTypes.LifeSupp) ? status.Systems.get_Item(SystemTypes.LifeSupp) : null;
                if (systemType != null)
                {
                    LifeSuppSystemType lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>();
                    if (lifeSuppSystemType != null && lifeSuppSystemType.Countdown < 0f)
                    {
                        lifeSuppSystemType.Countdown = 10000f;
                        return EndCondition.ImpostorWinBySabotage;
                    }
                }
                ISystemType systemType2 = status.Systems.ContainsKey(SystemTypes.Reactor) ? status.Systems.get_Item(SystemTypes.Reactor) : null;
                if (systemType2 == null)
                {
                    systemType2 = status.Systems.ContainsKey(SystemTypes.Laboratory) ? status.Systems.get_Item(SystemTypes.Laboratory) : null;
                }
                if (systemType2 != null)
                {
                    ICriticalSabotage criticalSystem = systemType2.TryCast<ICriticalSabotage>();
                    if (criticalSystem != null && criticalSystem.Countdown < 0f)
                    {
                        criticalSystem.ClearSabotage();
                        return EndCondition.ImpostorWinBySabotage;
                    }
                }
            }

            if (statistics.AliveImpostors > 0 &&
            statistics.AliveJackals == 0 &&
            !Game.GameData.data.AllPlayers.Values.Any((p) => p.IsAlive && p.extraRole.Contains(Roles.SecondarySidekick)) &&
            statistics.TotalAlive <= (statistics.AliveImpostors-statistics.AliveInLoveImpostors)*2 &&
            (statistics.AliveImpostorCouple+statistics.AliveImpostorTrilemma==0||
            statistics.AliveImpostorCouple*2 + statistics.AliveImpostorTrilemma*3 >= statistics.AliveCouple*2+statistics.AliveTrilemma*3))
            {
                if (TempData.LastDeathReason == DeathReason.Kill)
                {
                    return EndCondition.ImpostorWinByKill;
                }
                else
                {
                    return EndCondition.ImpostorWinByVote;
                }
            }

            return null;
        });

        public static Side Jackal = new Side("Jackal", "jackal", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Jackal.RoleColor, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if ((statistics.AliveJackals-statistics.AliveInLoveJackals)*2 >= statistics.TotalAlive && statistics.GetAlivePlayers(Impostor)-statistics.AliveImpostorsWithSidekick <= 0&&
            (statistics.AliveJackalCouple + statistics.AliveJackalTrilemma == 0 ||
            statistics.AliveJackalCouple*2 + statistics.AliveJackalTrilemma*3 >= statistics.AliveCouple*2 + statistics.AliveTrilemma*3))
            {
                return EndCondition.JackalWin;
            }
            return null;
        });

        public static Side Jester = new Side("Jester", "jester", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Jester.RoleColor, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (Roles.Jester.WinTrigger)
            {
                return EndCondition.JesterWin;
            }
            return null;
        });

        public static Side Vulture = new Side("Vulture", "vulture", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Vulture.RoleColor, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (Roles.Vulture.WinTrigger)
            {
                return EndCondition.VultureWin;
            }
            return null;
        });

        public static Side Arsonist = new Side("Arsonist", "arsonist", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Arsonist.RoleColor, (PlayerStatistics statistics, ShipStatus side) =>
        {
            if (Roles.Arsonist.WinTrigger)
            {
                return EndCondition.ArsonistWin;
            }
            return null;
        });

        public static Side Empiric = new Side("Empiric", "empiric", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Empiric.RoleColor, (PlayerStatistics statistics, ShipStatus side) =>
        {
            if (Roles.Empiric.WinTrigger)
            {
                return EndCondition.EmpiricWin;
            }
            return null;
        });

        public static Side Opportunist = new Side("Opportunist", "opportunist", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Opportunist.RoleColor, (PlayerStatistics statistics, ShipStatus side) =>
        {
            return null;
        });

        public static Side Avenger = new Side("Avenger", "avenger", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Avenger.RoleColor, (PlayerStatistics statistics, ShipStatus status) =>
        {
            return null;
        },(EndCondition endCondition,PlayerStatistics statistics, ShipStatus status)=> {
            if (endCondition.IsNoBodyWinEnd) return null;
            if (Roles.Avenger.canTakeOverSabotageWinOption.getBool() && endCondition == EndCondition.ImpostorWinBySabotage) return null;

            foreach(var player in Game.GameData.data.AllPlayers.Values)
            {
                if (!player.IsAlive) continue;
                if (player.role != Roles.Avenger) continue;

                if (player.GetRoleData(Roles.Avenger.avengerCheckerId) == 1)
                    return EndCondition.AvengerWin;
            }
            return null;
        });

        public static Side GamePlayer = new Side("GamePlayer", "gamePlayer", IntroDisplayOption.SHOW_ONLY_ME, Palette.CrewmateBlue, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (Game.GameData.data.GameMode == Module.CustomGameMode.Minigame)
            {
                if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks && GameData.Instance.CompletedTasks>0)
                {
                    return EndCondition.MinigameEscapeesWin;
                }
                if (statistics.TotalAlive == 1)
                {
                    return EndCondition.MinigameHunterWin;
                }
            }
            return null;
        });

        public static Side Investigator = new Side("Investigator", "investigator", IntroDisplayOption.SHOW_ALL, Palette.CrewmateBlue, (PlayerStatistics statistics, ShipStatus status) =>
        {
            return null;
        });

        public static Side Extra = new Side("Extra", "extra", IntroDisplayOption.STANDARD, new Color(150, 150, 150), (PlayerStatistics statistics, ShipStatus side) =>
        {
            if (Game.GameData.data.IsCanceled)
            {
                return EndCondition.NoGame;
            }


            if (CustomOptionHolder.limiterOptions.getBool())
            {
                if (Game.GameData.data.Timer < 1f)
                {
                    if (Game.GameData.data.GameMode == Module.CustomGameMode.Minigame)
                    {
                        return EndCondition.MinigameEscapeesWin;
                    }
                    else
                    {
                        switch (PlayerControl.GameOptions.MapId)
                        {
                            case 0:
                            case 3:
                                return EndCondition.NobodySkeldWin;
                            case 1:
                                return EndCondition.NobodyMiraWin;
                            case 2:
                                return EndCondition.NobodyPolusWin;
                            case 4:
                                return EndCondition.NobodyAirshipWin;
                        }
                    }
                }
            }

            //Hostのゴーストがnullの場合
            if ((int)(Game.GameData.data.GameMode & Module.CustomGameMode.Investigators) != 0)
            {
                if (Game.GameData.data.Ghost == null)
                {
                    return EndCondition.HostDisconnected;
                }
            }

            //Lovers単独勝利
            if (statistics.TotalAlive <= 3)
            {
                if (statistics.AliveCouple == 1 && statistics.AliveTrilemma==0) return EndCondition.LoversWin;
            }


            //Trilemma
            if (statistics.TotalAlive <= 5)
            {
                if (statistics.AliveTrilemma == 1 && statistics.AliveCouple==0) return EndCondition.TrilemmaWin;
            }

            if (statistics.TotalAlive == 0)
            {
                return EndCondition.NobodyWin;
            }
            return null;
        });

        public static List<Side> AllSides = new List<Side>()
        {
            Crewmate, Impostor,
            Jackal, Jester, Vulture, Empiric, Arsonist, Avenger,
            Investigator,
            GamePlayer,
            Extra
        };

        public IntroDisplayOption ShowOption { get; }
        public Color color { get; }
        public string side { get; }
        public string localizeSide { get; }

        public EndCriteriaChecker endCriteriaChecker { get; }
        public EndTakeoverChecker endTakeoverChecker { get; }

        private Side(string side, string localizeSide, IntroDisplayOption displayOption, Color color, EndCriteriaChecker endCriteriaChecker,EndTakeoverChecker endTakeoverChecker)
        {
            this.side = side;
            this.localizeSide = localizeSide;
            this.ShowOption = displayOption;
            this.color = color;
            this.endCriteriaChecker = endCriteriaChecker;
            this.endTakeoverChecker = endTakeoverChecker;
        }

        private Side(string side, string localizeSide, IntroDisplayOption displayOption, Color color, EndCriteriaChecker endCriteriaChecker) :
            this(side, localizeSide, displayOption, color, endCriteriaChecker, (a1,a2,a3)=>null)
        {
        }
    }

}
