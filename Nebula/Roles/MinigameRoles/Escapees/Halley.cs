using Nebula.Patches;

namespace Nebula.Roles.MinigameRoles.Escapees
{
    public class Halley : Escapee
    {
        public Halley()
                : base("Halley", "halley", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.GamePlayer, Side.GamePlayer,
                     Player.minigameSideSet, Player.minigameSideSet, new HashSet<EndCondition>() { EndCondition.MinigamePlayersWin },
                     false, VentPermission.CanNotUse, false, false, false)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.Minigame;
            CanCallEmergencyMeeting = false;
            RemoveAllTasksOnDead = true;
        }
    }
}
