namespace Nebula.Roles.ImpostorRoles;

public class Eraser : Role
{
    private CustomButton eraserButton;

    private Module.CustomOption canEraseSecretRoleOption;
    private Module.CustomOption eraseDurationOption;
    private Module.CustomOption eraseCoolDownOption;
    private Module.CustomOption eraseCoolDownAdditionOption;
    public Module.CustomOption eraserCanGuessCrewmateOption;

    private int eraseCountId;
    public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(eraseCountId, "Erased Roles", 0, 20) }; }


    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.EraseButton.png", 115f, "ui.button.eraser.erase");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.eraser.help.erase",0.3f)
    };

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Arsonist);
    }

    public override void LoadOptionData()
    {
        canEraseSecretRoleOption = CreateOption(Color.white, "canEraseSecretRole", false);

        eraseCoolDownOption = CreateOption(Color.white, "eraseCoolDown", 25f, 10f, 60f, 5f);
        eraseCoolDownOption.suffix = "second";

        eraseCoolDownAdditionOption = CreateOption(Color.white, "eraseCoolDownAddition", 15f, 5f, 30f, 5f);
        eraseCoolDownAdditionOption.suffix = "second";

        eraseDurationOption = CreateOption(Color.white, "eraseDuration", 2f, 0f, 5f, 0.5f);
        eraseDurationOption.suffix = "second";

        eraserCanGuessCrewmateOption = CreateOption(Color.white, "eraserCanGuessCrewmate", false);
    }

    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f, true);
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
    }


    public override void ButtonInitialize(HudManager __instance)
    {
        if (eraserButton != null)
        {
            eraserButton.Destroy();
        }
        eraserButton = new CustomButton(
            () =>
            {

            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                if (eraserButton.isEffectActive && Game.GameData.data.myData.currentTarget == null)
                {
                    eraserButton.Timer = 0f;
                    eraserButton.isEffectActive = false;
                }
                return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.currentTarget != null;
            },
            () =>
            {
                eraserButton.Timer = eraserButton.MaxTimer + eraseCoolDownAdditionOption.getFloat() * PlayerControl.LocalPlayer.GetModData().GetRoleData(eraseCountId);
            },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode, true, eraseDurationOption.getFloat(),
            () =>
            {
                var target = Game.GameData.data.myData.currentTarget;
                var data = target.GetModData();
                if (canEraseSecretRoleOption.getBool() || !(data.role is AllSideRoles.Secret))
                {
                    RPCEventInvoker.ChangeRole(target,
                        !data.role.HasCrewmateTask(target.PlayerId) ?
                        Roles.CrewmateWithoutTasks : Roles.Crewmate);
                }
                Game.GameData.data.myData.currentTarget = null;

                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, eraseCountId, 1);
                eraserButton.Timer = eraserButton.MaxTimer + eraseCoolDownAdditionOption.getFloat() * PlayerControl.LocalPlayer.GetModData().GetRoleData(eraseCountId);
            },
            "button.label.erase"
        ).SetTimer(CustomOptionHolder.InitialForcefulAbilityCoolDownOption.getFloat());
        eraserButton.MaxTimer = eraseCoolDownOption.getFloat();
    }

    public override void CleanUp()
    {
        if (eraserButton != null)
        {
            eraserButton.Destroy();
            eraserButton = null;
        }
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        __instance.GetModData().SetRoleData(eraseCountId, 0);
    }

    public Eraser()
            : base("Eraser", "eraser", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        eraserButton = null;

        eraseCountId = Game.GameData.RegisterRoleDataId("eraser.eraseCount");
    }
}