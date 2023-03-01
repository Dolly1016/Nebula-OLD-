namespace Nebula.Roles.CrewmateRoles;

public class Alien : Role
{
    private CustomButton emiButton;

    private Module.CustomOption emiCoolDownOption;
    private Module.CustomOption emiDurationOption;
    private Module.CustomOption emiRangeOption;
    private Module.CustomOption emiInhibitsCrewmatesOption;
    private Module.CustomOption countOfCallingSabotageOption;

    private int sabotageCount;

    private TMPro.TextMeshPro sabotageUsesString;
    private GameObject sabotageUsesObject;

    public override void LoadOptionData()
    {
        emiCoolDownOption = CreateOption(Color.white, "emiCoolDown", 25f, 10f, 60f, 5f);
        emiCoolDownOption.suffix = "second";

        emiDurationOption = CreateOption(Color.white, "emiDuration", 15f, 5f, 30f, 2.5f);
        emiDurationOption.suffix = "second";

        emiRangeOption = CreateOption(Color.white, "emiRange", 1f, 0.5f, 5f, 0.5f);
        emiRangeOption.suffix = "cross";

        emiInhibitsCrewmatesOption = CreateOption(Color.white, "emiInhibitsCrewmates", true);

        countOfCallingSabotageOption = CreateOption(Color.white, "countOfCallingSabotage", 1f, 0f, 5f, 1f);
    }


    static public Color RoleColor = new Color(187f / 255f, 109f / 255f, 178f / 255f);

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.EMIButton.png", 115f, "ui.button.alien.emi");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.alien.help.emi",0.3f)
    };

    public override void GlobalUpdate(byte playerId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == playerId) return;
        if (!Events.GlobalEvent.IsActive(Events.GlobalEvent.Type.EMI)) return;
        if (Game.GameData.data.myData.getGlobalData().role == Roles.Alien) return;

        //クールダウン上昇
        if (emiRangeOption.getFloat() > PlayerControl.LocalPlayer.transform.position.Distance(Helpers.playerById(playerId).transform.position))
        {
            PlayerControl.LocalPlayer.killTimer += Time.deltaTime;
            foreach (CustomButton button in CustomButton.buttons)
            {
                if (button.isEffectActive) continue;

                if (!emiInhibitsCrewmatesOption.getBool() && PlayerControl.LocalPlayer.GetModData().role.category == RoleCategory.Crewmate)
                {
                    if (button.Timer > 1f)
                        button.Timer -= Time.deltaTime * 0.5f;
                }
                else
                {
                    if (button.MaxTimer > 0f)
                        if (button.Timer > 1f)
                            button.Timer += Time.deltaTime;
                }
            }
        }
    }

    public override bool CanInvokeSabotage => sabotageCount > 0 && !PlayerControl.LocalPlayer.Data.IsDead;
    public override void MyPlayerControlUpdate()
    {
        if (sabotageUsesString) sabotageUsesString.text = sabotageCount.ToString();
    }

    public override void OnInvokeSabotage(SystemTypes systemType)
    {
        sabotageCount--;
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        sabotageCount = (int)countOfCallingSabotageOption.getFloat();
        sabotageUsesObject = HudManager.Instance.SabotageButton.ShowUsesIcon(0,out sabotageUsesString);
        

        if (emiButton != null)
        {
            emiButton.Destroy();
        }
        emiButton = new CustomButton(
            () =>
            {
                RPCEventInvoker.GlobalEvent(Events.GlobalEvent.Type.EMI, emiDurationOption.getFloat());
                new Objects.EffectCircle(PlayerControl.LocalPlayer.gameObject, RoleColor, emiRangeOption.getFloat(), emiDurationOption.getFloat());
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () =>
            {
                emiButton.Timer = emiButton.MaxTimer;
                emiButton.isEffectActive = false;
                emiButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
            },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            emiDurationOption.getFloat(),
            () => { emiButton.Timer = emiButton.MaxTimer; },
            "button.label.emi"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        emiButton.MaxTimer = emiCoolDownOption.getFloat();
        emiButton.EffectDuration = emiDurationOption.getFloat();
    }

    public override void CleanUp()
    {
        if (emiButton != null)
        {
            emiButton.Destroy();
            emiButton = null;
        }
        if (sabotageUsesObject)
        {
            GameObject.Destroy(sabotageUsesObject);
        }
    }

    public Alien()
        : base("Alien", "alien", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, false, false)
    {
        IsGuessableRole = false;
        emiButton = null;
    }
}