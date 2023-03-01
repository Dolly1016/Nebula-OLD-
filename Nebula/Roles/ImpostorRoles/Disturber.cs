﻿namespace Nebula.Roles.ImpostorRoles;

public class Disturber : Role
{
    private List<CustomObject> Poles;

    private CustomButton elecButton;

    private SpriteLoader placeButtonSprite = new SpriteLoader("Nebula.Resources.ElecPolePlaceButton.png", 115f, "ui.button.disturber.place");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(placeButtonSprite,"role.disturber.help.disturb",0.3f)
    };

    private Module.CustomOption disturbCoolDownOption;
    public Module.CustomOption disturbDurationOption;
    private Module.CustomOption countOfBoltsOption;

    public override void LoadOptionData()
    {
        disturbCoolDownOption = CreateOption(Color.white, "disturbCoolDown", 20f, 10f, 60f, 2.5f);
        disturbCoolDownOption.suffix = "second";

        disturbDurationOption = CreateOption(Color.white, "disturbDuration", 10f, 5f, 60f, 2.5f);
        disturbDurationOption.suffix = "second";

        countOfBoltsOption = CreateOption(Color.white, "countOfBolts", 2f, 1f, 10f, 1f);
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (elecButton != null)
        {
            elecButton.Destroy();
        }
        elecButton = new CustomButton(
            () =>
            {
                /*
                if (elecButton.HasEffect)
                {
                    foreach (var pole in Poles)
                        RPCEventInvoker.DisturberInvoke(pole[0].Id, pole[1].Id);
                    RPCEventInvoker.GlobalEvent(Events.GlobalEvent.Type.BlackOut, disturbDurationOption.getFloat() + 2f, (ulong)(disturbBlackOutRateOption.getFloat() * 100f));
                }
                else
                {
                    PlaceOnClick();
                }
                */
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return (elecButton.HasEffect ? !Helpers.SabotageIsActive() : Poles.Count < (int)countOfBoltsOption.getFloat()) && PlayerControl.LocalPlayer.CanMove; },
            () =>
            {
                elecButton.Timer = elecButton.MaxTimer;
            },
            placeButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            disturbDurationOption.getFloat(),
            () => { },
            "button.label.place"
        ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
        elecButton.UsesText.text = ((int)countOfBoltsOption.getFloat()).ToString();
        elecButton.SetUsesIcon(1);
        elecButton.MaxTimer = 10f;
        elecButton.HasEffect = false;
    }

    public override void Initialize(PlayerControl __instance)
    {
        Poles = new List<CustomObject>();
    }

    public override void MyUpdate()
    {
        
    }

    public override void OnMeetingEnd()
    {
        if (Poles.Count == (int)countOfBoltsOption.getFloat())
        {
            elecButton.HasEffect = true;
            elecButton.SetLabel("button.label.disturb");
            elecButton.MaxTimer = elecButton.Timer = disturbCoolDownOption.getFloat();
        }
    }

    public override void CleanUp()
    {
        if (elecButton != null)
        {
            elecButton.Destroy();
            elecButton = null;
        }
    }

    public override void OnRoleRelationSetting()
    {

    }

    public Disturber()
            : base("Disturber", "disturber", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        Poles = new List<CustomObject>();
    }
}