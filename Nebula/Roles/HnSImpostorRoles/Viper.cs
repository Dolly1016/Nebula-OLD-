using Nebula.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.HnSImpostorRoles;

public class PoisonedAttribute : PlayerAttribute
{
    public PoisonedAttribute(byte id) : base(id)
    {
    }

    public override void OnLostAttributeLocal(PlayerControl player, AttributeOperateReason reason) { 
        if(reason is AttributeOperateReason.TimeOver or AttributeOperateReason.PreMeeting)
    }
}

public class HnSViper : Role
{
    ModAbilityButton killButton;

    public override void ButtonInitialize(HudManager __instance)
    {
        killButton?.Destroy();
        killButton = new(HudManager.Instance.KillButton.graphic.sprite, Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent);
    }

    public override void CleanUp()
    {
        killButton?.Destroy();
    }

    public HnSViper()
            : base("HnSViper", "viperHnS", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorEndSet,
                 true, VentPermission.CanNotUse, false, true, true)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;
        canInvokeSabotage = false;
        HideKillButtonEvenImpostor = true;
        canReport = false;
    }
}
