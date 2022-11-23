using Nebula.Patches;

namespace Nebula.Roles.ImpostorRoles;

public class Damned : Role
{
    public static HashSet<Side> impostorSideSet = new HashSet<Side>() { Side.Impostor };
    public static HashSet<EndCondition> impostorEndSet =
       new HashSet<EndCondition>() { EndCondition.ImpostorWinByKill, EndCondition.ImpostorWinBySabotage, EndCondition.ImpostorWinByVote, EndCondition.ImpostorWinDisconnect };

    public override List<Role> GetImplicateRoles() { return new List<Role>() { Roles.DamnedCrew }; }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Crewmate);
    }

    public override bool IsSpawnable()
    {
        return false;
    }

    public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
    {

    }

    public Damned()
            : base("Damned", "damned", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 impostorSideSet, impostorSideSet, impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        IsHideRole = true;
    }
}