using Nebula.Objects;
using Hazel;

namespace Nebula.Roles.ComplexRoles;

public class FTracker : Template.HasBilateralness
{
    public Module.CustomOption canChangeTrackingTargetOption;
    public Module.CustomOption changeTargetCoolDownOption;
    public Module.CustomOption canUseMeetingActionOption;
    public Module.CustomOption canChangeTrackingTargetInMeegingOption;
    public Module.CustomOption evilTrackerCanKnowImpostorsKillOption;
    public Module.CustomOption evilTrackerCanTrackImpostorsOption;

    static public Color RoleColor = new Color(114f / 255f, 163f / 255f, 207f / 255f);

    public int remainTrapsId { get; private set; }

    public static SpriteLoader trackButtonNiceSprite = new SpriteLoader("Nebula.Resources.TrackNiceButton.png", 115f);
    public static SpriteLoader trackButtonEvilSprite = new SpriteLoader("Nebula.Resources.TrackEvilButton.png", 115f);
    public static SpriteLoader meetingButtonSprite = new SpriteLoader("Nebula.Resources.TrackIcon.png", 150f);

    public override void LoadOptionData()
    {
        base.LoadOptionData();

        TopOption.tab = Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.ImpostorRoles;

        canChangeTrackingTargetOption = CreateOption(Color.white, "canChangeTrackingTarget", false);
        changeTargetCoolDownOption = CreateOption(Color.white, "changeTargetCoolDown", 10f, 5f, 40f, 5f).AddPrerequisite(canChangeTrackingTargetOption);
        changeTargetCoolDownOption.suffix = "second";
        canUseMeetingActionOption = CreateOption(Color.white, "canUseMeetingActionOption", true);
        canChangeTrackingTargetInMeegingOption = CreateOption(Color.white, "canChangeTrackingTargetInMeegingOption", false).AddPrerequisite(canUseMeetingActionOption);
        evilTrackerCanTrackImpostorsOption = CreateOption(Color.white, "evilTrackerCanTrackImpostors", true);
        evilTrackerCanKnowImpostorsKillOption = CreateOption(Color.white, "evilTrackerCanKnowImpostorsKill", true);

        FirstRole = Roles.NiceTracker;
        SecondaryRole = Roles.EvilTracker;
    }

    public FTracker()
            : base("Tracker", "tracker", RoleColor)
    {
    }

    public override List<Role> GetImplicateRoles() { return new List<Role>() { Roles.EvilTracker, Roles.NiceTracker }; }
}

public class Tracker : Template.BilateralnessRole
{
    private CustomButton trackButton;

    private List<Objects.Arrow?> impostorArrows;
    private Game.PlayerObject? trackTarget;
    private Objects.Arrow? arrow;
    private SpriteRenderer? targetIndicator;
    private byte taskTrackTarget;
    private List<Tuple<Vector2, bool>>? tasks;
    SpriteLoader arrowSprite;

    //インポスターはModで操作するFakeTaskは所持していない
    public Tracker(string name, string localizeName, bool isImpostor)
            : base(name, localizeName,
                 isImpostor ? Palette.ImpostorRed : FTracker.RoleColor,
                 isImpostor ? RoleCategory.Impostor : RoleCategory.Crewmate,
                 isImpostor ? Side.Impostor : Side.Crewmate, isImpostor ? Side.Impostor : Side.Crewmate,
                 isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                 isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                 isImpostor ? ImpostorRoles.Impostor.impostorEndSet : CrewmateRoles.Crewmate.crewmateEndSet,
                 false, isImpostor ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse,
                 isImpostor, isImpostor, isImpostor, () => { return Roles.F_Tracker; }, isImpostor)
    {
        IsHideRole = true;
        impostorArrows = new List<Arrow?>();

        arrowSprite = new SpriteLoader("role."+localizeName+".arrow");
    }

    public override Assignable AssignableOnHelp => Roles.F_Tracker;

    public override HelpSprite[] helpSprite => new HelpSprite[] {
            new HelpSprite(category==RoleCategory.Impostor ? FTracker.trackButtonEvilSprite : FTracker.trackButtonNiceSprite,"role.tracker.help.track",0.3f),
            new HelpSprite(FTracker.meetingButtonSprite,"role.tracker.help.meeting",0.7f)
        };

    public override void SetupMeetingButton(MeetingHud __instance)
    {
        if (!Roles.F_Tracker.canUseMeetingActionOption.getBool()) return;

        if (!PlayerControl.LocalPlayer.Data.IsDead && !Game.GameData.data.myData.getGlobalData().HasExtraRole(Roles.SecondaryGuesser))
        {
            List<GameObject> allButton = new List<GameObject>();

            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                targetBox.name = "TrackButton";
                targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
                SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                renderer.sprite = FTracker.meetingButtonSprite.GetSprite();
                PassiveButton button = targetBox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                int copiedIndex = i;
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    if (__instance.CurrentState == MeetingHud.VoteStates.Discussion) return;

                    if (Roles.F_Tracker.canChangeTrackingTargetInMeegingOption.getBool())
                    {
                        trackTarget = new Game.PlayerObject(Helpers.playerById(playerVoteArea.TargetPlayerId));
                        trackButton.UpperText.text = trackTarget.control.GetModData().currentName;
                    }

                    if (category == RoleCategory.Impostor)
                    {
                        taskTrackTarget = playerVoteArea.TargetPlayerId;
                        RPCEventInvoker.RequireCustomData(playerVoteArea.TargetPlayerId, CustomData.CurrentTask);
                    }
                    else
                    {

                    }
                    foreach (var obj in allButton) GameObject.Destroy(obj);
                }));

                allButton.Add(targetBox);
            }
        }
    }


    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f);
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

        if (!Roles.F_Tracker.evilTrackerCanTrackImpostorsOption.getBool()) return;

        RoleSystem.TrackSystem.PlayerTrack_MyControlUpdate(ref arrow, trackTarget, Roles.F_Tracker.Color,arrowSprite);

        int i = 0;
        if (category == RoleCategory.Impostor)
        {
            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if ((p.Data.Role.IsImpostor || p.GetModData().role.DeceiveImpostorInNameDisplay) && !p.Data.IsDead && p.PlayerId != PlayerControl.LocalPlayer.PlayerId && p.PlayerId != (trackTarget?.control?.PlayerId ?? byte.MaxValue))
                {
                    if (impostorArrows.Count >= i) impostorArrows.Add(null);

                    var arrow = impostorArrows[i];
                    RoleSystem.TrackSystem.PlayerTrack_MyControlUpdate(ref arrow, p, Palette.ImpostorRed,arrowSprite);
                    impostorArrows[i] = arrow;

                    i++;
                }
            }
        }
        int removed = impostorArrows.Count - i;
        for (; i < impostorArrows.Count; i++) if (impostorArrows[i] != null) GameObject.Destroy(impostorArrows[i].arrow);
        impostorArrows.RemoveRange(impostorArrows.Count - removed, removed);
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        arrow = null;
        trackTarget = null;
        tasks = null;
        targetIndicator = null;
        taskTrackTarget = byte.MaxValue;

        if (trackButton != null)
        {
            trackButton.Destroy();
        }
        trackButton = new CustomButton(
            () =>
            {
                trackTarget = new Game.PlayerObject(Game.GameData.data.myData.currentTarget);
                trackButton.UpperText.text = trackTarget.control.GetModData().currentName;
                trackButton.Timer = Roles.F_Tracker.canChangeTrackingTargetOption.getBool() ? trackButton.MaxTimer : 0f;
            },
            () =>
            {

                return true && !PlayerControl.LocalPlayer.Data.IsDead;

            },
            () =>
            {
                return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.currentTarget != null && (trackTarget == null || Roles.F_Tracker.canChangeTrackingTargetOption.getBool());
            },
            () => { trackButton.Timer = trackButton.MaxTimer; },
            category == RoleCategory.Impostor ? FTracker.trackButtonEvilSprite.GetSprite() : FTracker.trackButtonNiceSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.track"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());

        trackButton.MaxTimer = Roles.F_Tracker.changeTargetCoolDownOption.getFloat();
    }
    public override void CleanUp()
    {
        if (trackButton != null)
        {
            trackButton.Destroy();
            trackButton = null;
        }

        if (arrow != null)
        {
            GameObject.Destroy(arrow.arrow);
            arrow = null;
        }

        if (targetIndicator != null)
        {
            if (targetIndicator) GameObject.Destroy(targetIndicator.gameObject);
            targetIndicator = null;
        }

        foreach (var a in impostorArrows) if (a != null) GameObject.Destroy(a.arrow);
        impostorArrows.Clear();

        trackTarget = null;
    }

    //フラッシュを表示
    public override void OnAnyoneMurdered(byte murderId, byte targetId)
    {
        if (!Roles.F_Tracker.evilTrackerCanKnowImpostorsKillOption.getBool()) return;
        if (category != RoleCategory.Impostor) return;
        if (targetId == murderId) return;
        if (targetId == PlayerControl.LocalPlayer.PlayerId) return;
        if (murderId == PlayerControl.LocalPlayer.PlayerId) return;
        if (!Helpers.playerById(murderId).Data.Role.IsImpostor) return;

        Helpers.PlayQuickFlash(Palette.ImpostorRed);
    }


    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Psychic);
        RelatedRoles.Add(Roles.Vulture);
    }

    public override bool BlocksShowTaskOverlay { get => MeetingHud.Instance && tasks != null; }

    public override void OnMeetingEnd()
    {
        tasks = null;
        taskTrackTarget = byte.MaxValue;
    }

    public override void OnReceiveCustomData(byte playerId, CustomData data, MessageReader reader)
    {
        if (data != CustomData.CurrentTask) return;
        if (taskTrackTarget != playerId) return;

        if (tasks == null) tasks = new List<Tuple<Vector2, bool>>(); else tasks.Clear();

        int length = reader.ReadInt32();
        for (int i = 0; i < length; i++)
        {
            tasks.Add(new Tuple<Vector2, bool>(new Vector2(reader.ReadSingle(), reader.ReadSingle()), reader.ReadBoolean()));
        }

        taskTrackTarget = Byte.MaxValue;

        if (!MapBehaviour.Instance || !MapBehaviour.Instance.IsOpen) HudManager.Instance.ToggleMapVisible(new MapOptions
        {
            Mode = MapOptions.Modes.Normal,
            ShowLivePlayerPosition = true
        });
    }

    public override void OnShowMapTaskOverlay(MapTaskOverlay mapTaskOverlay, Action<Vector2, bool> iconGenerator)
    {
        if (tasks == null) return;

        foreach (var tuple in tasks)
        {
            iconGenerator(tuple.Item1, tuple.Item2);
        }
    }

    public override void MyMapUpdate(MapBehaviour mapBehaviour)
    {
        if (!mapBehaviour.GetTrackOverlay()) return;
        if (!mapBehaviour.GetTrackOverlay().activeSelf) return;

        Vector2? pos = null;

        if (trackTarget != null && trackTarget.control != null)
        {
            if (!trackTarget.control.Data.IsDead)
            {
                if (MeetingHud.Instance)                
                    pos = trackTarget.control.GetModData().preMeetingPosition;
                else
                    pos = trackTarget.control.transform.position;
            }
            else if (trackTarget.deadBody)
            {
                pos = trackTarget.deadBody.transform.position;
            }
            else
            {
                foreach (var d in Helpers.AllDeadBodies())
                {
                    if (d.ParentId == trackTarget.control.PlayerId)
                    {
                        trackTarget.deadBody = d;
                        pos = d.transform.position;
                        break;
                    }
                }
            }
        }

        if (pos == null)
        {
            if (targetIndicator != null) GameObject.Destroy(targetIndicator.gameObject);
            targetIndicator = null;
            return;
        }

        if (targetIndicator == null || !targetIndicator)
        {
            targetIndicator = GameObject.Instantiate(mapBehaviour.HerePoint, mapBehaviour.GetTrackOverlay().transform);
            PlayerMaterial.SetColors(Palette.DisabledGrey, targetIndicator);
        }

        targetIndicator.transform.localPosition = MapBehaviourExpansion.ConvertMapLocalPosition(pos.Value, 16);

    }
}
