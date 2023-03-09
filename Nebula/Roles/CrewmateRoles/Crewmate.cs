using Nebula.Patches;

namespace Nebula.Roles.CrewmateRoles;

public class Crewmate : Role
{
    public static HashSet<Side> crewmateSideSet = new HashSet<Side>() { Side.Crewmate };
    public static HashSet<EndCondition> crewmateEndSet =
        new HashSet<EndCondition>() { EndCondition.CrewmateWinByTask, EndCondition.CrewmateWinByVote, EndCondition.CrewmateWinDisconnect, EndCondition.CrewmateWinHnS };

    public override bool IsSpawnable() => true;
    public override bool CanHaveExtraAssignable(ExtraAssignable extraRole)
    {
        return Roles.F_Crewmate.CanHaveExtraAssignable(extraRole);
    }

    public override bool IsGuessableRole
    {
        get
        {
            if (Game.GameData.data != null && Game.GameData.data.myData.getGlobalData().role == Roles.Eraser)
            {
                return Roles.Eraser.eraserCanGuessCrewmateOption.getBool();
            }
            else
            {
                return Roles.F_Crewmate.isGuessableOption.getBool();
            }
        }
    }

    public override List<Role> GetImplicateRoles()
    {
        return new List<Role>() { Roles.DamnedCrew, Roles.CrewmateWithoutTasks };
    }

    public Crewmate(bool hasFakeTask = false)
            : base("Crewmate", "crewmate", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 crewmateSideSet, crewmateSideSet, crewmateEndSet,
                 hasFakeTask, VentPermission.CanNotUse, false, false, false)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.Standard;
    }
}