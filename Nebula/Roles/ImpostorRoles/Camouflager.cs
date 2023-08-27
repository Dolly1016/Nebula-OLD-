namespace Nebula.Roles.Impostor;

public class Camouflager : Role
{
    private ModAbilityButton camouflageButton;

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
        camouflageButton?.Destroy();
        camouflageButton = new ModAbilityButton(buttonSprite.GetSprite())
            .SetLabelLocalized("button.label.camouflage");

        camouflageButton.SetUpEffectAttribute(Module.NebulaInputManager.abilityInput.keyCode,
            CustomOptionHolder.InitialAbilityCoolDownOption.getFloat(),
            camouflageCoolDownOption.getFloat(), camouflageDurationOption.getFloat(),
            () => RPCEventInvoker.GlobalEvent(Events.GlobalEvent.Type.Camouflage, camouflageDurationOption.getFloat()));
    }

    public override void CleanUp()
    {
        camouflageButton?.Destroy();
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