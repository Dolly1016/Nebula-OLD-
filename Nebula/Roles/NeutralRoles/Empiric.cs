namespace Nebula.Roles.NeutralRoles;

public class Empiric : Template.HasAlignedHologram, Template.HasWinTrigger
{
    static public Color RoleColor = new Color(183f / 255f, 233f / 255f, 0f / 255f);

    static private CustomButton infectButton;

    private Module.CustomOption maxInfectMyselfOption;
    private Module.CustomOption infectRangeOption;
    private Module.CustomOption infectDurationOption;
    private Module.CustomOption canInfectMyKillerOption;
    private Module.CustomOption coastingPhaseOption;
    private Module.CustomOption canUseVentsOption;
    private Module.CustomOption ventCoolDownOption;
    private Module.CustomOption ventDurationOption;

    private int leftInfect;
    private Dictionary<byte, float> infectProgress;
    private float coasting;
    private float infoUpdateCounter = 0f;

    public bool WinTrigger { get; set; } = false;
    public byte Winner { get; set; } = Byte.MaxValue;

    public override void LoadOptionData()
    {
        maxInfectMyselfOption = CreateOption(Color.white, "maxInfectMyself", 1f, 1f, 5f, 1f);

        infectRangeOption = CreateOption(Color.white, "infectRange", 1f, 0.25f, 3f, 0.25f);
        infectRangeOption.suffix = "cross";

        infectDurationOption = CreateOption(Color.white, "infectDuration", 20f, 5f, 60f, 1f);
        infectDurationOption.suffix = "second";

        canInfectMyKillerOption = CreateOption(Color.white, "canInfectMyKiller", true);

        coastingPhaseOption = CreateOption(Color.white, "coastingPhase", 10f, 0f, 30f, 1f);
        coastingPhaseOption.suffix = "second";

        canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
        ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f).AddPrerequisite(canUseVentsOption);
        ventCoolDownOption.suffix = "second";
        ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f).AddPrerequisite(canUseVentsOption);
        ventDurationOption.suffix = "second";
    }

    SpriteLoader infectSprite = new SpriteLoader("Nebula.Resources.InfectButton.png", 115f, "ui.button.empiric.infect");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(infectSprite,"role.empiric.help.infect",0.3f)
    };

    public override void GlobalIntroInitialize(PlayerControl __instance)
    {
        canMoveInVents = canUseVentsOption.getBool();
        VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
    }

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);

        infectProgress.Clear();
        leftInfect = (int)maxInfectMyselfOption.getFloat();
        WinTrigger = false;

        VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
        VentDurationMaxTimer = ventDurationOption.getFloat();
    }

    public override void CleanUp()
    {
        base.CleanUp();

        leftInfect = 0;
        WinTrigger = false;

        if (infectButton != null)
        {
            infectButton.Destroy();
            infectButton = null;
        }
    }

    public override void InitializePlayerIcon(PoolablePlayer player, byte PlayerId, int index)
    {
        base.InitializePlayerIcon(player, PlayerId, index);

        player.cosmetics.nameText.transform.localScale *= 2f;
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (infectButton != null)
        {
            infectButton.Destroy();
        }
        infectButton = new CustomButton(
            () =>
            {
                if (!activePlayers.Contains(Game.GameData.data.myData.currentTarget.PlayerId))
                {
                    activePlayers.Add(Game.GameData.data.myData.currentTarget.PlayerId);
                    leftInfect--;
                    infectButton.UsesText.text = (leftInfect).ToString();
                    Game.GameData.data.myData.currentTarget = null;
                }
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead && leftInfect > 0; },
            () =>
            {
                return Game.GameData.data.myData.currentTarget != null && PlayerControl.LocalPlayer.CanMove;
            },
            () => { },
            infectSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.infect"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        infectButton.UsesText.text = (leftInfect).ToString();
        infectButton.SetUsesIcon(2);
        infectButton.MaxTimer = CustomOptionHolder.InitialAbilityCoolDownOption.getFloat();
    }


    public override void OnMeetingStart()
    {
        base.OnMeetingStart();

        //停滞期
        coasting = coastingPhaseOption.getFloat();
    }

    public override void OnMurdered(byte murderId)
    {
        base.OnMurdered(murderId);

        if (canInfectMyKillerOption.getBool())
            activePlayers.Add(murderId);
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f, false, false, activePlayers);
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

        //感染停滞期を進める
        if (MeetingHud.Instance == null && SpawnInMinigame.Instance == null && ExileController.Instance == null)
        {
            coasting -= Time.deltaTime;
        }

        //感染しない間はなにもしない
        if (coasting > 0f || MeetingHud.Instance != null)
        {
            return;
        }

        bool allPlayerInfected = true;

        float infectDistance = 1f * infectRangeOption.getFloat();
        float infectProgressPerTime = Time.deltaTime / infectDurationOption.getFloat();
        bool infectProceedFlag = false;
        PlayerControl infected;
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            if (player.Data.IsDead) continue;
            if (activePlayers.Contains(player.PlayerId)) continue;
            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            if (!player.gameObject.active) continue;

            allPlayerInfected = false;

            infectProceedFlag = false;

            foreach (byte playerId in activePlayers)
            {
                infected = Helpers.playerById(playerId);
                if (infected.Data.IsDead) continue;
                if (infected.transform.position.Distance(player.transform.position) < infectDistance)
                {
                    infectProceedFlag = true;
                    break;
                }
            }

            if (infectProceedFlag)
            {
                if (!infectProgress.ContainsKey(player.PlayerId))
                {
                    infectProgress.Add(player.PlayerId, 0);
                }
                infectProgress[player.PlayerId] += infectProgressPerTime;

                if (infectProgress[player.PlayerId] > 1)
                {
                    activePlayers.Add(player.PlayerId);
                }
            }
        }

        if (allPlayerInfected) RPCEventInvoker.WinTrigger(this);

        foreach (KeyValuePair<byte, PoolablePlayer> player in PlayerIcons)
        {
            if (!player.Value.gameObject.active)
            {
                player.Value.cosmetics.nameText.text = "";
                continue;
            }

            if (activePlayers.Contains(player.Key))
            {
                player.Value.cosmetics.nameText.text = "";
            }
            else
            {
                if (infectProgress.ContainsKey(player.Key))
                {
                    player.Value.cosmetics.nameText.text = String.Format("{0:f1}%", infectProgress[player.Key] * 100f);
                }
                else
                {
                    player.Value.cosmetics.nameText.text = "0.0%";
                }
                player.Value.cosmetics.nameText.color = Color.white;
            }
        }

        infoUpdateCounter += Time.deltaTime;
        if (infoUpdateCounter > 0.5f)
        {
            RPCEventInvoker.UpdatePlayersIconInfo(this, activePlayers, infectProgress);
            infoUpdateCounter = 0f;
        }
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        base.GlobalInitialize(__instance);

        new Module.Information.PlayersIconInformation(Helpers.cs(RoleColor, __instance.name), __instance.PlayerId, this);
    }

    public override void GlobalFinalizeInGame(PlayerControl __instance)
    {
        Module.Information.UpperInformationManager.Remove((i) =>
        i is Module.Information.PlayersIconInformation &&
        ((Module.Information.PlayersIconInformation)i).relatedPlayerId == __instance.PlayerId &&
        ((Module.Information.PlayersIconInformation)i).relatedRole == this
        );
    }

    public Empiric()
        : base("Empiric", "empiric", RoleColor, RoleCategory.Neutral, Side.Empiric, Side.Empiric,
             new HashSet<Side>() { Side.Empiric }, new HashSet<Side>() { Side.Empiric },
             new HashSet<Patches.EndCondition>() { Patches.EndCondition.EmpiricWin },
             true, VentPermission.CanUseLimittedVent, true, false, false)
    {
        infectButton = null;
        infectProgress = new Dictionary<byte, float>();
        coasting = 0f;

        Patches.EndCondition.EmpiricWin.TriggerRole = this;
    }
}
