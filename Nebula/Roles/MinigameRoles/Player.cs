using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;
using Nebula.Patches;

namespace Nebula.Roles.MinigameRoles
{
    public class Player : Role
    {
        public static HashSet<Side> minigameSideSet = new HashSet<Side>() { Side.Crewmate };
        public static HashSet<EndCondition> minigameEndSet =
            new HashSet<EndCondition>() { EndCondition.MinigamePlayersWin };

        public override void MyUpdate()
        {
            
        }

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);
        }

        public Player()
                : base("Player", "player", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.GamePlayer, Side.GamePlayer,
                     minigameSideSet, minigameSideSet, minigameEndSet,
                     false, false, false, false, false)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.Minigame;
            CanCallEmergencyMeeting = true;
        }
    }
}
