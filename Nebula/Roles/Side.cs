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
        public static Side Crewmate = new Side("Crewmate", "crewmate", true, Palette.CrewmateBlue, (PlayerStatistics statistics, ShipStatus status) =>
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

        public static Side Impostor = new Side("Impostor", "impostor", false, Palette.ImpostorRed, (PlayerStatistics statistics, ShipStatus status) =>
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

        public static Side Jackal = new Side("Jackal", "jackal", false, NeutralRoles.Jackal.Color, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (statistics.GetAlivePlayers(Jackal)*2 >= statistics.TotalAlive && statistics.GetAlivePlayers(Impostor)==0)
            {
                return EndCondition.JackalWin;
            }
            return null;
        });

        public static Side Jester = new Side("Jester", "jester", false, NeutralRoles.Jester.Color, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (Roles.Jester.WinTrigger)
            {
                return EndCondition.JesterWin;
            }
            return null;
        });

        public static Side Vulture = new Side("Vulture", "vulture", false, NeutralRoles.Vulture.Color, (PlayerStatistics statistics, ShipStatus status) =>
        {
            if (Roles.Vulture.WinTrigger)
            {
                return EndCondition.VultureWin;
            }
            return null;
        });

        public static Side Arsonist = new Side("Arsonist", "arsonist", false, new Color(241, 97, 0), (PlayerStatistics statistics, ShipStatus side) =>
        {
            return null;
        });

        public static Side Extra = new Side("Extra", "extra", false, new Color(150, 150, 150), (PlayerStatistics statistics, ShipStatus side) =>
        {
            if (statistics.TotalAlive == 3)
            {
                foreach(Game.PlayerData player in Game.GameData.data.players.Values)
                {
                    if (!player.IsAlive) continue;
                    if (!player.extraRole.Contains(Roles.Trilemma)) return null;
                }
                return EndCondition.TrilemmaWin;
            }
            return null;
        });

        public static List<Side> AllSides = new List<Side>()
        {
            Crewmate, Impostor,
            Jackal, Jester, Vulture, Arsonist, 
            Extra
        };

        //ロールの設定関わりなく全てのプレイヤーを同陣営として表示するフラグ
        public bool showFullMemberAtIntro { get; }
        public Color color { get; }
        public string side { get; }
        public string localizeSide { get; }

        public EndCriteriaChecker endCriteriaChecker { get; }

        private Side(string side, string localizeSide, bool showFullMemberAtIntro, Color color, EndCriteriaChecker endCriteriaChecker)
        {
            this.side = side;
            this.localizeSide = localizeSide;
            this.showFullMemberAtIntro = showFullMemberAtIntro;
            this.color = color;
            this.endCriteriaChecker = endCriteriaChecker;
        }
    }

}
