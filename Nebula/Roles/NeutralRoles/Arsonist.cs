namespace Nebula.Roles.NeutralRoles;

public class Arsonist : Template.HasAlignedHologram, Template.HasWinTrigger
{
    static public Color RoleColor = new Color(255f / 255f, 103f / 255f, 1 / 255f);

    static private CustomButton arsonistButton;

    private Module.CustomOption douseDurationOption;
    private Module.CustomOption douseCoolDownOption;
    private Module.CustomOption douseRangeOption;
    private Module.CustomOption canUseVentsOption;

    public bool WinTrigger { get; set; } = false;
    public byte Winner { get; set; } = Byte.MaxValue;

    private float infoUpdateCounter = 0.0f;

    public override HelpSprite[] helpSprite => new HelpSprite[] {
            new HelpSprite(douseSprite,"role.arsonist.help.douse",0.3f),
            new HelpSprite(igniteSprite,"role.arsonist.help.ignite",0.3f)
        };

    public override void LoadOptionData()
    {
        douseDurationOption = CreateOption(Color.white, "douseDuration", 3f, 1f, 10f, 0.5f);
        douseDurationOption.suffix = "second";

        douseCoolDownOption = CreateOption(Color.white, "douseCoolDown", 10f, 0f, 60f, 2.5f);
        douseCoolDownOption.suffix = "second";

        douseRangeOption = CreateOption(Color.white, "douseRange", 1f, 0.5f, 2f, 0.125f);
        douseRangeOption.suffix = "cross";


        canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
    }


    SpriteLoader douseSprite = new SpriteLoader("Nebula.Resources.DouseButton.png", 115f);
    SpriteLoader igniteSprite = new SpriteLoader("Nebula.Resources.IgniteButton.png", 115f);

    static private bool canIgnite = false;

    public override void GlobalIntroInitialize(PlayerControl __instance)
    {
        canMoveInVents = canUseVentsOption.getBool();
        VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
    }

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);

        canIgnite = false;
        WinTrigger = false;
    }

    public override void CleanUp()
    {
        base.CleanUp();

        canIgnite = false;
        WinTrigger = false;

        if (arsonistButton != null)
        {
            arsonistButton.Destroy();
            arsonistButton = null;
        }
    }

    public override void OnMeetingEnd()
    {
        base.OnMeetingEnd();

        CheckIgnite();
    }

    private bool CheckIgnite()
    {
        bool cannotIgnite = false;
        foreach (var entry in PlayerIcons)
        {
            if (!entry.Value.gameObject.active) continue;
            if (activePlayers.Contains(entry.Key)) continue;

            cannotIgnite = true; break;
        }

        if (!cannotIgnite)
        {
            //点火可能
            arsonistButton.Sprite = igniteSprite.GetSprite();
            arsonistButton.SetLabel("button.label.ignite");
            canIgnite = true;
            arsonistButton.Timer = 0f;
        }
        else
        {
            arsonistButton.Timer = arsonistButton.MaxTimer;
        }

        return !cannotIgnite;
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (arsonistButton != null)
        {
            arsonistButton.Destroy();
        }
        arsonistButton = new CustomButton(
            () =>
            {
                if (canIgnite)
                {
                    arsonistButton.isEffectActive = false;
                    arsonistButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                    RPCEventInvoker.WinTrigger(this);
                }
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                if (arsonistButton.isEffectActive && Game.GameData.data.myData.currentTarget == null)
                {
                    arsonistButton.Timer = 0f;
                    arsonistButton.isEffectActive = false;
                }
                return PlayerControl.LocalPlayer.CanMove && (Game.GameData.data.myData.currentTarget != null || canIgnite);
            },
            () =>
            {
                arsonistButton.Timer = arsonistButton.MaxTimer;
                arsonistButton.isEffectActive = false;
                arsonistButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
            },
            douseSprite.GetSprite(),
            new Vector3(-1.8f, 0, 0),
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            douseDurationOption.getFloat(),
            () =>
            {
                if (Game.GameData.data.myData.currentTarget != null)
                {
                    activePlayers.Add(Game.GameData.data.myData.currentTarget.PlayerId);
                    Game.GameData.data.myData.currentTarget = null;
                }

                CheckIgnite();
            },
            false,
            "button.label.douse"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        arsonistButton.MaxTimer = douseCoolDownOption.getFloat();
        arsonistButton.EffectDuration = douseDurationOption.getFloat();
    }


    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1.8f * douseRangeOption.getFloat(), false, false, activePlayers);
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

        infoUpdateCounter += Time.deltaTime;
        if (infoUpdateCounter > 0.5f)
        {
            RPCEventInvoker.UpdatePlayersIconInfo(this, activePlayers, null);
            infoUpdateCounter = 0f;
        }
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Empiric);
        RelatedRoles.Add(Roles.EvilAce);
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        base.GlobalInitialize(__instance);

        new Module.Information.PlayersIconInformation(Helpers.cs(RoleColor, __instance.name), __instance.PlayerId, this);
    }

    public override void OnDied(byte playerId)
    {
        Module.Information.UpperInformationManager.Remove((i) =>
        i is Module.Information.PlayersIconInformation &&
        ((Module.Information.PlayersIconInformation)i).relatedPlayerId == playerId &&
        ((Module.Information.PlayersIconInformation)i).relatedRole == this
        );
    }

    public override void GlobalFinalizeInGame(PlayerControl __instance)
    {
        Module.Information.UpperInformationManager.Remove((i) =>
        i is Module.Information.PlayersIconInformation &&
        ((Module.Information.PlayersIconInformation)i).relatedPlayerId == __instance.PlayerId &&
        ((Module.Information.PlayersIconInformation)i).relatedRole == this
        );
    }

    public Arsonist()
        : base("Arsonist", "arsonist", RoleColor, RoleCategory.Neutral, Side.Arsonist, Side.Arsonist,
             new HashSet<Side>() { Side.Arsonist }, new HashSet<Side>() { Side.Arsonist },
             new HashSet<Patches.EndCondition>() { Patches.EndCondition.ArsonistWin },
             true, VentPermission.CanUseUnlimittedVent, true, false, false)
    {
        arsonistButton = null;

        Patches.EndCondition.ArsonistWin.TriggerRole = this;
    }
}
