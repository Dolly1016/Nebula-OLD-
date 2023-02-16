using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.HnSImpostorRoles;

public class Impostor : Role
{
    public override bool ShowInHelpWindow => false;
    public Impostor(string roleName="Impostor",string localizedName="impostor")
            : base(roleName, localizedName, Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorEndSet,
                 true, VentPermission.CanNotUse, false, true, true)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;
        canInvokeSabotage = false;
    }
}

