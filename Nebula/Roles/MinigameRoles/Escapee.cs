using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;

namespace Nebula.Roles.MinigameRoles
{
    public class Escapee : Role
    {
        public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
        {
            if (condition != EndCondition.MinigameEscapeesWin) return false;
            return !player.Data.IsDead ;
        }

        public Escapee()
                : base("Escapee", "escapee", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.GamePlayer, Side.GamePlayer,
                     Player.minigameSideSet, Player.minigameSideSet, new HashSet<EndCondition>() { EndCondition.MinigamePlayersWin },
                     false, VentPermission.CanNotUse, false, false, false)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.Minigame;
            CanCallEmergencyMeeting = false;
        }
    }
}
