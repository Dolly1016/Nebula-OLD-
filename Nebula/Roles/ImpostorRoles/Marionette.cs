namespace Nebula.Roles.ImpostorRoles;

public class Marionette : Role
{
    public class DecoyEvent : Events.LocalEvent
    {
        CustomObject decoy;
        public DecoyEvent(CustomObject decoy, float duration) : base(duration)
        {
            this.decoy = decoy;
        }

        public override void OnTerminal()
        {
            if (decoy) { RPCEventInvoker.ObjectDestroy(decoy); }
        }

        public override void LocalUpdate()
        {
            if (Roles.Marionette.marionetteButton != null)
                Roles.Marionette.marionetteButton.UpperText.text = ((int)duration).ToString() + "s";
        }
    }



    private CustomButton? placeButton;
    private CustomButton? marionetteButton;
    private CustomButton? cameraButton;

    private Module.CustomOption swapCoolDownOption;
    private Module.CustomOption decoyDurationOption;

    private CustomObject? decoy;
    private SpriteRenderer? decoyIndicator;
    private int marionetteMode;

    private void ChangeMarionetteMode()
    {
        SetMarionetteMode((marionetteMode + 1) % 2);
    }

    private void SetMarionetteMode(int mode)
    {
        marionetteMode = mode;
        if (marionetteMode == 0)
        {
            marionetteButton!.Sprite = swapButtonSprite.GetSprite();
            marionetteButton!.SetLabel("button.label.swap");
        }
        else
        {
            marionetteButton!.Sprite = destroyButtonSprite.GetSprite();
            marionetteButton!.SetLabel("button.label.destroy");
        }
    }

    private SpriteLoader decoyButtonSprite = new SpriteLoader("Nebula.Resources.DecoyButton.png", 115f, "ui.button.marionette.decoy");
    private SpriteLoader swapButtonSprite = new SpriteLoader("Nebula.Resources.DecoySwapButton.png", 115f, "ui.button.marionette.swap");
    private SpriteLoader destroyButtonSprite = new SpriteLoader("Nebula.Resources.DecoyDestroyButton.png", 115f, "ui.button.marionette.destroy");
    private SpriteLoader monitorButtonSprite = new SpriteLoader("Nebula.Resources.DecoyMonitorButton.png", 115f, "ui.button.marionette.monitor");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(decoyButtonSprite,"role.marionette.help.decoy",0.3f),
            new HelpSprite(swapButtonSprite,"role.marionette.help.swap",0.3f),
            new HelpSprite(monitorButtonSprite,"role.marionette.help.monitor",0.3f),
            new HelpSprite(destroyButtonSprite,"role.marionette.help.destroy",0.3f)
    };
    public override void LoadOptionData()
    {
        decoyDurationOption = CreateOption(Color.white, "decoyDuration", 45f, 15f, 300f, 5f);
        decoyDurationOption.suffix = "second";

        swapCoolDownOption = CreateOption(Color.white, "swapCoolDown", 5f, 0f, 40f, 0.5f);
        swapCoolDownOption.suffix = "second";
    }

    public override void ButtonInitialize(HudManager __instance)
    {

        decoy = null;
        decoyIndicator = null;

        if (placeButton != null)
        {
            placeButton.Destroy();
        }
        placeButton = new CustomButton(
            () =>
            {
                decoy = RPCEventInvoker.ObjectInstantiate(Objects.CustomObject.Type.Decoy, PlayerControl.LocalPlayer.transform.position);

                placeButton.Timer = placeButton.MaxTimer;
                marionetteButton.Timer = marionetteButton.MaxTimer;
                marionetteMode = 0;

                Events.LocalEvent.Activate(new DecoyEvent(decoy, decoyDurationOption.getFloat()));

                SetMarionetteMode(0);
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead && decoy == null; },
            () => { return PlayerControl.LocalPlayer.CanMove || HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer; },
            () =>
            {
                placeButton.Timer = 10f;
            },
            decoyButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.BottomContent,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.decoy"
        ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
        placeButton.MaxTimer = 10f;

        marionetteMode = 0;
        if (marionetteButton != null)
        {
            marionetteButton.Destroy();
        }
        marionetteButton = new CustomButton(
            () =>
            {
                if (marionetteMode == 0)
                {
                    RPCEventInvoker.DecoySwap(decoy);
                    if (HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer) HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
                }
                else
                {
                    RPCEventInvoker.ObjectDestroy(decoy);
                    decoy = null;
                    placeButton.Timer = placeButton.MaxTimer;
                }
                marionetteButton.Timer = marionetteButton.MaxTimer;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead && decoy != null && decoy.GameObject != null; },
            () => { return PlayerControl.LocalPlayer.CanMove || HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer; },
            () =>
            {
                marionetteButton.Timer = 10f;
                SetMarionetteMode(0);
            },
            decoyButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.BottomContent,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.decoy"
        );
        marionetteButton.MaxTimer = swapCoolDownOption.getFloat();
        marionetteButton.SetAidAction(Module.NebulaInputManager.changeAbilityInput.keyCode, true, ChangeMarionetteMode);

        if (cameraButton != null)
        {
            cameraButton.Destroy();
        }
        cameraButton = new CustomButton(
            () =>
            {
                if (HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer) HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
                else HudManager.Instance.PlayerCam.SetTargetWithLight(decoy.Behaviour);
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead && decoy != null; },
            () => { return PlayerControl.LocalPlayer.CanMove || HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer; },
            () =>
            {
                marionetteButton.Timer = marionetteButton.MaxTimer;
            },
            monitorButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.secondaryAbilityInput.keyCode,
            "button.label.monitor"
        );

        SetMarionetteMode(0);
    }

    public override void MyUpdate()
    {
        if (decoy != null && decoy.GameObject == null)
        {
            decoy = null;
            placeButton.Timer = placeButton.MaxTimer;
        }

    }

    public override void CleanUp()
    {
        if (placeButton != null)
        {
            placeButton.Destroy();
            placeButton = null;
        }
        if (marionetteButton != null)
        {
            marionetteButton.Destroy();
            marionetteButton = null;
        }
        if (cameraButton != null)
        {
            cameraButton.Destroy();
            cameraButton = null;
        }
        decoy = null;
        decoyIndicator = null;
    }

    public override void FinalizeInGame(PlayerControl __instance)
    {
        HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
        if (decoyIndicator != null && decoyIndicator.gameObject) GameObject.Destroy(decoyIndicator.gameObject);
        decoyIndicator = null;
    }

    public override void OnDied()
    {
        HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
    }

    public override void OnMeetingStart()
    {
        HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
        if (decoy != null)
        {
            RPCEventInvoker.ObjectDestroy(decoy);
            decoy = null;
        }
    }


    //デコイをマップに表示
    public override void MyMapUpdate(MapBehaviour mapBehaviour)
    {
        if (!mapBehaviour.GetTrackOverlay()) return;
        if (!mapBehaviour.GetTrackOverlay().activeSelf) return;

        if (decoy == null)
        {
            if (decoyIndicator != null) GameObject.Destroy(decoyIndicator.gameObject);
            return;
        }

        if (decoyIndicator == null)
        {
            decoyIndicator = GameObject.Instantiate(mapBehaviour.HerePoint, mapBehaviour.GetTrackOverlay().transform);
            PlayerMaterial.SetColors(Palette.DisabledGrey, decoyIndicator);
        }

        decoyIndicator.transform.localPosition = MapBehaviourExpansion.ConvertMapLocalPosition(decoy.GameObject.transform.position, 16);

    }


    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Reaper);
    }

    public Marionette()
            : base("Marionette", "marionette", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        marionetteButton = null;
    }
}
