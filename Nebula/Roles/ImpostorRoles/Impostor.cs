using Nebula.Patches;

namespace Nebula.Roles.ImpostorRoles;

public class Impostor : Role
{
    public static HashSet<Side> impostorSideSet = new HashSet<Side>() { Side.Impostor };
    public static HashSet<EndCondition> impostorEndSet =
       new HashSet<EndCondition>() { EndCondition.ImpostorWinByKill, EndCondition.ImpostorWinBySabotage, EndCondition.ImpostorWinByVote, EndCondition.ImpostorWinDisconnect, EndCondition.ImpostorWinHnS };

    public Impostor()
            : base("Impostor", "impostor", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 impostorSideSet, impostorSideSet, impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        ValidGamemode = Module.CustomGameMode.Standard;
    }

    public override bool IsSpawnable() => true;
}
