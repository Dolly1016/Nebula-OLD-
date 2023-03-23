namespace Nebula.Roles.ImpostorRoles;

public class Cleaner : Role
{
    /* オプション */
    private Module.CustomOption cleanCoolDownOption;
    public override void LoadOptionData()
    {
        cleanCoolDownOption = CreateOption(Color.white, "cleanCoolDown", 30f, 10f, 60f, 5f);
        cleanCoolDownOption.suffix = "second";
    }

    /* ボタン */
    static private CustomButton cleanButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        if (cleanButton != null)
        {
            cleanButton.Destroy();
        }
        cleanButton = new CustomButton(
            () =>
            {
                byte targetId = deadBodyId;

                RPCEventInvoker.CleanDeadBody(targetId);

                //キル・クリーンボタンのクールダウンは同期する
                cleanButton.Timer = cleanButton.MaxTimer;
                PlayerControl.LocalPlayer.killTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
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
        cleanButton.MaxTimer = cleanCoolDownOption.getFloat();
    }

    public override void OnKillPlayer(byte targetId)
    {
        //killボタンと連動する
        cleanButton.Timer = cleanButton.MaxTimer;
    }

    public byte deadBodyId;


    /* 画像 */
    private SpriteLoader cleanButtonSprite = new SpriteLoader("Nebula.Resources.CleanButton.png", 115f, "ui.button.cleaner.clean");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(cleanButtonSprite,"role.cleaner.help.clean",0.3f)
    };

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
    }

    public override void CleanUp()
    {
        if (cleanButton != null)
        {
            cleanButton.Destroy();
            cleanButton = null;
        }
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Vulture);
    }

    public Cleaner()
        : base("Cleaner", "cleaner", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
             Impostor.impostorSideSet, Impostor.impostorSideSet,
             Impostor.impostorEndSet,
             true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        cleanButton = null;
    }
}
