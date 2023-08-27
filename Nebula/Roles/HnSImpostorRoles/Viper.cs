using Nebula.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

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
    ModAbilityButton poisonButton;
    SpriteLoader spreadButtonSprite = new("Nebula.Resources.ViperButton.png", 115f);

    public override void ButtonInitialize(HudManager __instance)
    {
        /*
           (p)=>!p.GetModData().Attribute.HasAttribute(PlayerAttribute.Poisoned),
            Color.yellow,GameManager.Instance.LogicOptions.GetKillDistance(),
        */
        killButton?.Destroy();
        killButton = HnSImpostorSystem.GenerateKillButton();

        poisonButton?.Destroy();
        poisonButton = new(spreadButtonSprite.GetSprite());
        poisonButton.SetLabelLocalized("button.label.poison");
        poisonButton.MyAttribute = new SimpleAbilityAttribute(
            40f,40f,
            new SimpleButtonEvent((button) => {

                
            }, Module.NebulaInputManager.abilityInput.keyCode)
            );
    }

    public override void CleanUp()
    {
        killButton?.Destroy();
        poisonButton?.Destroy();
    }

    public HnSViper()
            : base("HnSViper", "viperHnS", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.Impostor.impostorSideSet, Impostor.Impostor.impostorSideSet, Impostor.Impostor.impostorEndSet,
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
