namespace Nebula.Roles.CrewmateRoles;
public class CrewmateWithoutTasks : Crewmate
{
    public override bool IsGuessableRole { get => false; }

    public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
    {

    }

    public override bool ShowInHelpWindow => false;

    public override bool CanHaveExtraAssignable(ExtraAssignable extraRole)
    {
        return Roles.F_Crewmate.CanHaveExtraAssignable(extraRole);
    }
    public override bool HasExecutableFakeTask(byte playerId) => false;

    public CrewmateWithoutTasks() : base(true)
    {
        HideInExclusiveAssignmentOption = true;
    }
}