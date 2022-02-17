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
            if (statistics.GetAlivePlayers(Impostor) == 0 && statistics.GetAlivePlayers(Jackal) == 0)
            {
                return EndCondition.CrewmateWinByVote;
            }
            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
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
                ISystemType systemType = status.Systems.ContainsKey(SystemTypes.LifeSupp) ? status.Systems[SystemTypes.LifeSupp] : null;
                if (systemType != null)
                {
                    LifeSuppSystemType lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>();
                    if (lifeSuppSystemType != null && lifeSuppSystemType.Countdown < 0f)
                    {
                        lifeSuppSystemType.Countdown = 10000f;
                        return EndCondition.ImpostorWinBySabotage;
                    }
                }
                ISystemType systemType2 = status.Systems.ContainsKey(SystemTypes.Reactor) ? status.Systems[SystemTypes.Reactor] : null;
                if (systemType2 == null)
                {
                    systemType2 = status.Systems.ContainsKey(SystemTypes.Laboratory) ? status.Systems[SystemTypes.Laboratory] : null;
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

            if (statistics.GetAlivePlayers(Impostor) > 0 && statistics.GetAlivePlayers(Jackal) == 0 && statistics.TotalAlive <= 2 * statistics.GetAlivePlayers(Impostor))
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

        public static Side Jackal = new Side("Jackal", "jackal", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Jackal.Color, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (statistics.GetAlivePlayers(Jackal)*2 >= statistics.TotalAlive && statistics.GetAlivePlayers(Impostor)==0)
            {
                return EndCondition.JackalWin;
            }
            return null;
        });

        public static Side Jester = new Side("Jester", "jester", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Jester.Color, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (Roles.Jester.WinTrigger)
            {
                return EndCondition.JesterWin;
            }
            return null;
        });

        public static Side Vulture = new Side("Vulture", "vulture", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Vulture.Color, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (Roles.Vulture.WinTrigger)
            {
                return EndCondition.VultureWin;
            }
            return null;
        });

        public static Side Arsonist = new Side("Arsonist", "arsonist", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Arsonist.Color, (PlayerStatistics statistics, ShipStatus side) =>
        {
            if (Roles.Arsonist.WinTrigger)
            {
                return EndCondition.ArsonistWin;
            }
            return null;
        });

        public static Side Empiric = new Side("Empiric", "empiric", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Empiric.Color, (PlayerStatistics statistics, ShipStatus side) =>
        {
            if (Roles.Empiric.WinTrigger)
            {
                return EndCondition.EmpiricWin;
            }
            return null;
        });

        public static Side Opportunist = new Side("Opportunist", "opportunist", IntroDisplayOption.SHOW_ONLY_ME, NeutralRoles.Opportunist.Color, (PlayerStatistics statistics, ShipStatus side) =>
        {
            return null;
        });

        public static Side Investigator = new Side("Investigator", "investigator", IntroDisplayOption.SHOW_ALL, Palette.CrewmateBlue, (PlayerStatistics statistics, ShipStatus status) =>
        {
            return null;
        });

        public static Side Gambler = new Side("Gambler", "gambler", IntroDisplayOption.SHOW_ONLY_ME, ParlourRoles.Gambler.Color , (PlayerStatistics statistics, ShipStatus status) =>
          {
              return null;
          });

        public static Side Extra = new Side("Extra", "extra", IntroDisplayOption.STANDARD, new Color(150, 150, 150), (PlayerStatistics statistics, ShipStatus side) =>
        {
            if (CustomOptionHolder.limiterOptions.getBool())
            {
                if (Game.GameData.data.Timer < 1f)
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

            //Hostのゴーストがnullの場合
            if ((int)(Game.GameData.data.GameMode & Module.CustomGameMode.Investigators) != 0)
            {
                if (Game.GameData.data.Ghost == null)
                {
                    return EndCondition.HostDisconnected;
                }
            }



            if (statistics.TotalAlive == 3)
            {
                foreach(Game.PlayerData player in Game.GameData.data.players.Values)
                {
                    if (!player.IsAlive) continue;
                    if (!player.extraRole.Contains(Roles.Trilemma)) return null;
                }
                return EndCondition.TrilemmaWin;
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
            Jackal, Jester, Vulture, Empiric, Arsonist, 
            Investigator,
            Gambler,
            Extra
        };

        public IntroDisplayOption ShowOption { get; }
        public Color color { get; }
        public string side { get; }
        public string localizeSide { get; }

        public EndCriteriaChecker endCriteriaChecker { get; }

        private Side(string side, string localizeSide, IntroDisplayOption displayOption, Color color, EndCriteriaChecker endCriteriaChecker)
        {
            this.side = side;
            this.localizeSide = localizeSide;
            this.ShowOption = displayOption;
            this.color = color;
            this.endCriteriaChecker = endCriteriaChecker;
        }
    }

}
