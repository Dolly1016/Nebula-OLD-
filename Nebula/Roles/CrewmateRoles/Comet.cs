namespace Nebula.Roles.CrewmateRoles;

public class Comet : Role
{
    static public Color RoleColor = new Color(121f / 255f, 175f / 255f, 206f / 255f);

    private CustomButton boostButton;

    private Module.CustomOption boostCooldownOption;
    private Module.CustomOption boostDurationOption;
    private Module.CustomOption boostSpeedOption;
    private Module.CustomOption boostLightOption;
    private Module.CustomOption boostViewOption;

    private float lightLevel = 1f;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.BoostButton.png", 115f, "ui.button.comet.blaze");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.comet.help.blaze",0.3f)
    };

    public override void MyUpdate()
    {
        if (boostButton == null) return;

        if (boostButton.isEffectActive)
            lightLevel += 0.5f * Time.deltaTime;
        else
            lightLevel -= 0.5f * Time.deltaTime;
        lightLevel = Mathf.Lerp(0f, 1f, lightLevel);
    }

    public override void GetLightRadius(ref float radius)
    {
        radius *= Mathf.Lerp(1f, boostLightOption.getFloat(), lightLevel);
    }

    public override void Initialize(PlayerControl __instance)
    {
        lightLevel = 0f;
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (boostButton != null)
        {
            boostButton.Destroy();
        }
        boostButton = new CustomButton(
            () =>
            {
                RPCEventInvoker.UpdatePlayerVisibility(PlayerControl.LocalPlayer.PlayerId, false);
                RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, boostDurationOption.getFloat(), boostSpeedOption.getFloat(), false));
                RPCEventInvoker.EmitAttributeFactor(PlayerControl.LocalPlayer, new Game.PlayerAttributeFactor(Game.PlayerAttribute.Invisible, boostDurationOption.getFloat(), 0, false));
                Game.GameData.data.myData.Vision.Register(new Game.VisionFactor(boostDurationOption.getFloat(), 1.5f));
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () =>
            {
                boostButton.Timer = boostButton.MaxTimer;
                boostButton.isEffectActive = false;
                boostButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                RPCEventInvoker.UpdatePlayerVisibility(PlayerControl.LocalPlayer.PlayerId, true);
            },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
           boostDurationOption.getFloat(),
           () =>
           {
               boostButton.Timer = boostButton.MaxTimer;
               RPCEventInvoker.UpdatePlayerVisibility(PlayerControl.LocalPlayer.PlayerId, true);
           },
            "button.label.blaze"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        boostButton.MaxTimer = boostCooldownOption.getFloat();
    }

    public override void CleanUp()
    {
        if (boostButton != null)
        {
            boostButton.Destroy();
            boostButton = null;
        }
    }

    public override void LoadOptionData()
    {
        boostCooldownOption = CreateOption(Color.white, "boostCoolDown", 20f, 10f, 60f, 5f);
        boostCooldownOption.suffix = "second";

        boostDurationOption = CreateOption(Color.white, "boostDuration", 10f, 5f, 30f, 5f);
        boostDurationOption.suffix = "second";

        boostSpeedOption = CreateOption(Color.white, "boostSpeed", 2f, 1.25f, 3f, 0.25f);
        boostSpeedOption.suffix = "cross";

        boostLightOption = CreateOption(Color.white, "boostVisionRate", 1.5f, 1f, 4f, 0.5f);
        boostLightOption.suffix = "cross";

        boostViewOption = CreateOption(Color.white, "boostViewRate", 1.5f, 1f, 4f, 0.5f);
        boostViewOption.suffix = "cross";
    }

    public Comet()
        : base("Comet", "comet", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, true, false)
    {
        boostButton = null;
    }
}