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
        if (!(reason is AttributeOperateReason.TimeOver or AttributeOperateReason.PreMeeting)) return;
        RPCEventInvoker.UncheckedMurderPlayer(HnSModificator.Seeker.PlayerId,PlayerControl.LocalPlayer.PlayerId, Game.PlayerData.PlayerStatus.Poisoned.Id,false);
    }
}

public class HnSViper : Role
{
    ModAbilityButton killButton;

    public override void ButtonInitialize(HudManager __instance)
    {
        killButton?.Destroy();
        killButton = new(HudManager.Instance.KillButton.graphic.sprite, Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent);
        killButton.MyAttribute = new InterpersonalAbilityAttribute(
            GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown),
            GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown),
            (p)=>!p.GetModData().Attribute.HasAttribute(PlayerAttribute.Poisoned),
            Color.yellow,GameManager.Instance.LogicOptions.GetKillDistance(),
            new SimpleButtonEvent((button) => {
                RPCEventInvoker.EmitAttributeFactor(Game.GameData.data.myData.currentTarget, new PlayerAttributeFactor(PlayerAttribute.Poisoned, false, 10f, 0, false));
            },Module.NebulaInputManager.modKillInput.keyCode)
            );
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
