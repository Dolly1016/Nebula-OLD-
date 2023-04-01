namespace Nebula.Roles.HnSImpostorRoles;

public class HnSCleaner : Role
{
    public override bool ShowInHelpWindow => false;

    private SpriteLoader cleanButtonSprite = new SpriteLoader("Nebula.Resources.CleanButton.png", 115f, "ui.button.cleaner.clean");

    static private CustomButton cleanButton, killButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        if (killButton != null) killButton.Destroy();
        killButton = HnSImpostorSystem.GenerateKillButton(__instance);

        if (cleanButton != null) cleanButton.Destroy();
        cleanButton = new CustomButton(
            () =>
            {
                byte targetId = deadBodyId;

                RPCEventInvoker.CleanDeadBody(targetId);
                cleanButton.Timer = cleanButton.MaxTimer;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return deadBodyId != Byte.MaxValue && PlayerControl.LocalPlayer.CanMove; },
            () => { cleanButton.Timer = cleanButton.MaxTimer; },
            cleanButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.clean"
        ).SetTimer(CustomOptionHolder.InitialForcefulAbilityCoolDownOption.getFloat());
        cleanButton.MaxTimer = 30f;
    }
    public byte deadBodyId;

    public override void MyPlayerControlUpdate()
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) return;

        /* 消去対象の探索 */

        {
            DeadBody body = Patches.PlayerControlPatch.SetMyDeadTarget();
            if (body)
            {
                deadBodyId = body.ParentId;
            }
            else
            {
                deadBodyId = byte.MaxValue;
            }
            Patches.PlayerControlPatch.SetDeadBodyOutline(body, Color.yellow);
        }

        HnSImpostorSystem.MyPlayerControlUpdate();
    }

    public override void CleanUp()
    {
        if (cleanButton != null)
        {
            cleanButton.Destroy();
            cleanButton = null;
        }

        if (killButton != null)
        {
            killButton.Destroy();
            killButton = null;
        }
    }

    public HnSCleaner()
            : base("HnSCleaner", "cleanerHnS", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorEndSet,
                 true, VentPermission.CanNotUse, false, true, true)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;
        canInvokeSabotage = false;
        canReport = false;
        HideKillButtonEvenImpostor = true;
    }
}

