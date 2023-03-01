namespace Nebula.Roles.ImpostorRoles;

public class Camouflager : Role
{
    private CustomButton camouflageButton;

    private Module.CustomOption camouflageCoolDownOption;
    private Module.CustomOption camouflageDurationOption;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.CamoButton.png", 115f, "ui.button.camoflager.camo");
    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.camouflager.help.camo",0.3f)
    };

    public override void LoadOptionData()
    {
        camouflageCoolDownOption = CreateOption(Color.white, "camouflageCoolDown", 25f, 10f, 60f, 5f);
        camouflageCoolDownOption.suffix = "second";

        camouflageDurationOption = CreateOption(Color.white, "camouflageDuration", 15f, 5f, 30f, 2.5f);
        camouflageDurationOption.suffix = "second";
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (camouflageButton != null)
        {
            camouflageButton.Destroy();
        }
        camouflageButton = new CustomButton(
            () =>
            {
                RPCEventInvoker.GlobalEvent(Events.GlobalEvent.Type.Camouflage, camouflageDurationOption.getFloat());
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () =>
            {
                camouflageButton.Timer = camouflageButton.MaxTimer;
                camouflageButton.isEffectActive = false;
                camouflageButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
            },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            camouflageDurationOption.getFloat(),
            () => { camouflageButton.Timer = camouflageButton.MaxTimer; },
            "button.label.camouflage"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        camouflageButton.MaxTimer = camouflageCoolDownOption.getFloat();
        camouflageButton.EffectDuration = camouflageDurationOption.getFloat();
    }

    public override void CleanUp()
    {
        if (camouflageButton != null)
        {
            camouflageButton.Destroy();
            camouflageButton = null;
        }
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Opportunist);
        RelatedRoles.Add(Roles.Empiric);
        RelatedRoles.Add(Roles.Alien);
    }

    public Camouflager()
            : base("Camouflager", "camouflager", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        camouflageButton = null;
    }
}