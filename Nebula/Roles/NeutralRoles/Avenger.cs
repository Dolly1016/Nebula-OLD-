using Nebula.Patches;

namespace Nebula.Roles.NeutralRoles;

public class Avenger : Role
{
    static public Color RoleColor = new Color(141f / 255f, 111f / 255f, 131f / 255f);

    static private CustomButton killButton;

    public int avengerCheckerId;
    public int loversId;
    public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(avengerCheckerId, "Will Win", 0, 1, new string[] { "False", "True" }) }; }


    public Module.CustomOption canKnowExistenceOfAvengerOption;
    public Module.CustomOption murderCanKnowAvengerOption;
    public Module.CustomOption showFlashForMurdererOption;
    private Module.CustomOption avengerKillCoolDownOption;
    private Module.CustomOption avengerNoticeIntervalOption;
    public Module.CustomOption murderNoticeIntervalOption;
    private Module.CustomOption ventCoolDownOption;
    private Module.CustomOption ventDurationOption;
    public Module.CustomOption canTakeOverSabotageWinOption;

    /* 矢印 */
    FixedArrow Arrow;
    private float noticeInterval = 0f;

    public override void LoadOptionData()
    {
        TopOption.AddCustomPrerequisite(() => Roles.Lover.IsSpawnable() && Roles.Lover.loversModeOption.getSelection() == 1);

        avengerKillCoolDownOption = CreateOption(Color.white, "killCoolDown", 20f, 10f, 60f, 2.5f);
        avengerKillCoolDownOption.suffix = "second";

        murderCanKnowAvengerOption = CreateOption(Color.white, "murderCanKnowAvenger", false);

        canKnowExistenceOfAvengerOption = CreateOption(Color.white, "canKnowExistenceOfAvenger", new string[] { "option.switch.off", "role.avenger.canKnowExistenceOfAvenger.everyone", "role.avenger.canKnowExistenceOfAvenger.onlyKiller" });

        showFlashForMurdererOption = CreateOption(Color.white, "showFlashForMurderer", true);

        avengerNoticeIntervalOption = CreateOption(Color.white, "avengerNoticeIntervalOption", 10f, 2.5f, 30f, 2.5f);
        avengerNoticeIntervalOption.suffix = "second";
        murderNoticeIntervalOption = CreateOption(Color.white, "murderNoticeIntervalOption", 10f, 2.5f, 30f, 2.5f).AddPrerequisite(murderCanKnowAvengerOption);
        murderNoticeIntervalOption.suffix = "second";

        ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f);
        ventCoolDownOption.suffix = "second";
        ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f);
        ventDurationOption.suffix = "second";

        canTakeOverSabotageWinOption = CreateOption(Color.white, "canTakeOverSabotageWin", false);
    }


    public SpriteLoader arrowSprite = new SpriteLoader("role.avenger.arrow");

    public override void MyPlayerControlUpdate()
    {
        var myGData = Game.GameData.data.myData.getGlobalData();

        if (myGData.GetRoleData(avengerCheckerId) == 0)
        {
            foreach (var data in Game.GameData.data.AllPlayers.Values)
            {
                if ((int)data.GetExtraRoleData(Roles.AvengerTarget) == myGData.GetRoleData(loversId))
                {
                    if (data.IsAlive)
                    {
                        var target = Helpers.playerById(data.id);
                        if (target)
                        {
                            if (!Arrow)
                            {
                                Arrow = new FixedArrow("AvengerArrow", true, target.transform.position, Color, arrowSprite.GetSprite());
                                noticeInterval = 0f;
                            }
                            noticeInterval -= Time.deltaTime;

                            if (noticeInterval < 0f)
                            {
                                Arrow.Position = target.transform.position;
                                noticeInterval = avengerNoticeIntervalOption.getFloat();
                            }
                        }
                    }
                    break;
                }
            }
        }
        else if (Arrow) Arrow.Destroy();


        Game.MyPlayerData myData = Game.GameData.data.myData;

        myData.currentTarget = Patches.PlayerControlPatch.SetMyTarget();

        Patches.PlayerControlPatch.SetPlayerOutline(myData.currentTarget, Palette.ImpostorRed);
    }

    public override void OnMeetingEnd()
    {
        base.OnMeetingEnd();
        if (Arrow) Arrow.Destroy();
        noticeInterval = Roles.Avenger.avengerNoticeIntervalOption.getFloat();
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        var data = __instance.GetModData();
        data.SetRoleData(avengerCheckerId, 0);
        data.SetRoleData(loversId, (int)data.GetExtraRoleData(Roles.Lover));

    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (killButton != null)
        {
            killButton.Destroy();
        }
        killButton = new CustomButton(
            () =>
            {
                Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget, Game.PlayerData.PlayerStatus.Dead, true);

                killButton.Timer = killButton.MaxTimer;
                Game.GameData.data.myData.currentTarget = null;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove; },
            () => { killButton.Timer = killButton.MaxTimer; },
            __instance.KillButton.graphic.sprite,
            Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent,
            __instance,
            Module.NebulaInputManager.modKillInput.keyCode
        );
        killButton.MaxTimer = avengerKillCoolDownOption.getFloat();
    }

    public override void CleanUp()
    {
        if (killButton != null)
        {
            killButton.Destroy();
            killButton = null;
        }

        if (Arrow) Arrow.Destroy();
    }

    public override bool CheckWin(PlayerControl player, EndCondition winReason)
    {
        if (winReason != EndCondition.AvengerWin) return false;
        if (player.Data.IsDead) return false;
        return player.GetModData().GetRoleData(avengerCheckerId) == 1;
    }

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);

        VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
        VentDurationMaxTimer = ventDurationOption.getFloat();

        Helpers.PlayFlash(Roles.Avenger.Color);
    }

    public Avenger()
        : base("Avenger", "avenger", RoleColor, RoleCategory.Neutral, Side.Avenger, Side.Avenger,
             new HashSet<Side>() { Side.Avenger }, new HashSet<Side>() { Side.Avenger },
             new HashSet<Patches.EndCondition>() { },
             true, VentPermission.CanUseLimittedVent, true, false, false)
    {
        avengerCheckerId = Game.GameData.RegisterRoleDataId("avenger.winChecker");
        loversId = Game.GameData.RegisterRoleDataId("avenger.loversId");

        killButton = null;

        Allocation = AllocationType.None;
        CreateOptionFollowingRelatedRole = true;

        IsGuessableRole = false;

        Arrow = null;
    }
}
