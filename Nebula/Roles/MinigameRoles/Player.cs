using Nebula.Patches;

namespace Nebula.Roles.MinigameRoles;

public class Player : Role
{
    public static HashSet<Side> minigameSideSet = new HashSet<Side>() { Side.Crewmate };
    

    public override void MyUpdate()
    {

    }

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);
    }

    public Player()
            : base("Player", "player", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.GamePlayer, Side.GamePlayer,
                 minigameSideSet, minigameSideSet, new HashSet<EndCondition>(),
                 false, VentPermission.CanNotUse, false, false, false)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.FreePlay;
        CanCallEmergencyMeeting = false;
    }
}
