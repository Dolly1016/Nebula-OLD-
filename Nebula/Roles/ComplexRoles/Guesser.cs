using Nebula.Module;

namespace Nebula.Roles.ComplexRoles;

public class FGuesser : Template.HasBilateralness
{
    public Module.CustomOption secondoryRoleOption;
    public Module.CustomOption guesserShots;
    public Module.CustomOption canShotSeveralTimesInTheSameMeeting;
    public Module.CustomOption additionalVotingTime;
    public Module.CustomOption spawnableRoleFilter;


    public Module.CustomOption crewmateRoleCountOption;
    public Module.CustomOption impostorRoleCountOption;
    public Module.CustomOption neutralRoleCountOption;

    static public Color RoleColor = new Color(255f / 255f, 255f / 255f, 0f / 255f);

    public override HelpSprite[] helpSprite => new HelpSprite[] {
            new HelpSprite(FGuesser.targetSprite,"role.guesser.help.guess",0.7f)
        };

    public override Patches.AssignRoles.RoleAllocation[] GetComplexAllocations()
    {
        if (!secondoryRoleOption.getBool())
        {
            return base.GetComplexAllocations();
        }
        return null;
    }

    public int remainShotsId { get; private set; }

    public static SpriteLoader targetSprite = new SpriteLoader("Nebula.Resources.TargetIcon.png", 150f);

    public override bool IsSecondaryGenerator { get { return secondoryRoleOption.getBool(); } }

    public override void LoadOptionData()
    {
        base.LoadOptionData();

        TopOption.tab = Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.ImpostorRoles | Module.CustomOptionTab.Modifiers;
        TopOption.SetYellowCondition((tab) =>
        {
            if (!secondoryRoleOption.getBool())
                return tab != Module.CustomOptionTab.Modifiers && RoleChanceOption.selection == RoleChanceOption.selections.Length - 1;
            else
                return tab == Module.CustomOptionTab.Modifiers && (crewmateRoleCountOption.getSelection() > 0 || impostorRoleCountOption.getSelection() > 0 || neutralRoleCountOption.getSelection() > 0);
        });
        var preBuilder = TopOption.preOptionScreenBuilder;
        TopOption.preOptionScreenBuilder = (refresher) =>
        {
            if (secondoryRoleOption.getBool())
            {
                return new Module.MetaScreenContent[][]{
                                new Module.MetaScreenContent[]{
                                    new MSString(3f, RoleChanceOption.getName(), 2f, 0.8f, TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold),
                                    new MSString(0.2f, ":", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                                    new MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () =>
                                    {
                                        RoleChanceOption.addSelection(-1);
                                        refresher();
                                    }),
                                    new MSString(1.5f, RoleChanceOption.getString(), 2f, 0.6f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                                    new MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () =>
                                    {
                                        RoleChanceOption.addSelection(1);
                                        refresher();
                                    }),
                                    new MSMargin(1f)
                                }
                        };
            }
            else return preBuilder(refresher);
        };

        secondoryRoleOption = CreateOption(Color.white, "isSecondaryRole", false);

        spawnableRoleFilter = CreateOption(Color.white, "spawnableRoleFilter", false);
        canShotSeveralTimesInTheSameMeeting = CreateOption(Color.white, "canShotSeveralTimes", false);
        guesserShots = CreateOption(Color.white, "guesserShots", 3f, 1f, 15f, 1f);
        additionalVotingTime = CreateOption(Color.white, "additionalVotingTime", 10f, 0f, 60f, 5f);
        additionalVotingTime.suffix = "second";

        chanceToSpawnAsSecondarySide.AddInvPrerequisite(secondoryRoleOption);
        RoleCountOption.AddInvPrerequisite(secondoryRoleOption);

        crewmateRoleCountOption = CreateOption(Color.white, "crewmateRoleCount", 1f, 0f, 15f, 1f);
        impostorRoleCountOption = CreateOption(Color.white, "impostorRoleCount", 1f, 0f, 5f, 1f);
        neutralRoleCountOption = CreateOption(Color.white, "neutralRoleCount", 1f, 0f, 15f, 1f);

        crewmateRoleCountOption.AddPrerequisite(secondoryRoleOption);
        impostorRoleCountOption.AddPrerequisite(secondoryRoleOption);
        neutralRoleCountOption.AddPrerequisite(secondoryRoleOption);

        FirstRole = Roles.NiceGuesser;
        SecondaryRole = Roles.EvilGuesser;

        foreach (var option in extraAssignableOptions)
        {
            if (option.Value == null) continue;
            option.Value.AddInvPrerequisite(secondoryRoleOption);
        }
    }

    public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
    {
        if (secondoryRoleOption.getBool()) return;
        base.SpawnableTest(ref DefinitiveRoles, ref SpawnableRoles);
    }

    public FGuesser()
            : base("Guesser", "guesser", RoleColor)

    {
    }

    public override List<Role> GetImplicateRoles() { return new List<Role>() { Roles.EvilGuesser, Roles.NiceGuesser }; }
}

static public class GuesserSystem
{
    public static Assignable.RelatedExtraRoleData[] RelatedExtraRoleDataInfo { get => new Assignable.RelatedExtraRoleData[] { new Assignable.RelatedExtraRoleData("Guesser Shot", Roles.SecondaryGuesser, 0, 20) }; }

    public static void GlobalInitialize(PlayerControl __instance)
    {
        __instance.GetModData().SetExtraRoleData(Roles.SecondaryGuesser.id, (ulong)Roles.F_Guesser.guesserShots.getFloat());
    }

    private static GameObject guesserUI;
    static void guesserOnClick(int buttonTarget, MeetingHud __instance)
    {
        if (__instance.CurrentState == MeetingHud.VoteStates.Discussion) return;

        PlayerControl target = Helpers.playerById((byte)__instance.playerStates[buttonTarget].TargetPlayerId);
        if (target == null || target.Data.IsDead) return;

        if (guesserUI != null || !(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted)) return;
        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

        Transform container = UnityEngine.Object.Instantiate(__instance.transform.FindChild("MeetingContents/PhoneUI"), __instance.transform);
        container.transform.localPosition = new Vector3(0, 0, -50f);
        guesserUI = container.gameObject;

        int i = 0;
        var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
        var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
        var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
        var textTemplate = __instance.playerStates[0].NameText;

        Transform exitButtonParent = (new GameObject()).transform;
        exitButtonParent.SetParent(container);
        Transform exitButton = UnityEngine.Object.Instantiate(buttonTemplate.transform, exitButtonParent);
        Transform exitButtonMask = UnityEngine.Object.Instantiate(maskTemplate, exitButtonParent);
        exitButton.gameObject.GetComponent<SpriteRenderer>().sprite = smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
        exitButtonParent.transform.localPosition = new Vector3(2.725f, 2.1f, -5);
        exitButtonParent.transform.localScale = new Vector3(0.25f, 0.9f, 1);
        exitButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
        exitButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
            UnityEngine.Object.Destroy(container.gameObject);
        }));

        List<Transform> buttons = new List<Transform>();
        Transform selectedButton = null;

        foreach (Role role in Roles.AllRoles)
        {
            //撃てないロールを除外する
            if (!role.IsGuessableRole || role.category == RoleCategory.Complex || (int)(role.ValidGamemode & Game.GameData.data.GameMode) == 0) continue;
            if (Roles.F_Guesser.spawnableRoleFilter.getBool() && !role.IsSpawnable()) continue;

            Transform buttonParent = (new GameObject()).transform;
            buttonParent.SetParent(container);
            Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
            Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
            TMPro.TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
            buttons.Add(button);
            int row = i / 6, col = i % 6;
            buttonParent.localPosition = new Vector3(-3.5f + 1.4f * col, 1.5f - 0.37f * row, -5);
            buttonParent.localScale = new Vector3(0.5f, 0.5f, 1f);
            label.text = Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name"));
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
            label.transform.localScale *= 1.7f;
            int copiedIndex = i;

            button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            if (!PlayerControl.LocalPlayer.Data.IsDead) button.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() =>
            {
                if (selectedButton != button)
                {
                    selectedButton = button;
                    buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Color.red : Color.white);
                }
                else
                {
                    if (PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        UnityEngine.Object.Destroy(container.gameObject);
                        return;
                    }

                    PlayerControl focusedTarget = Helpers.playerById((byte)__instance.playerStates[buttonTarget].TargetPlayerId);
                    if (!(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted) || focusedTarget == null) return;
                    if (target.Data.IsDead) return;
                    var focusedTargetData = focusedTarget.GetModData();
                    var actualRole = focusedTargetData.role.GetActualRole(focusedTargetData);
                    PlayerControl dyingTarget =
                    (
                    actualRole == role ||
                    role.GetImplicateRoles().Contains(actualRole) ||
                    role.GetImplicateExtraRoles().Any((r) => focusedTargetData.HasExtraRole(r))
                    )
                    ? focusedTarget : PlayerControl.LocalPlayer;

                    // Reset the GUI
                    __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                    UnityEngine.Object.Destroy(container.gameObject);

                    ulong data = PlayerControl.LocalPlayer.GetModData().GetExtraRoleData(Roles.SecondaryGuesser.id);
                    data--;
                    RPCEventInvoker.UpdateExtraRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.SecondaryGuesser.id, data);

                    if (Roles.F_Guesser.canShotSeveralTimesInTheSameMeeting.getBool() &&
                    Game.GameData.data.myData.getGlobalData().GetExtraRoleData(Roles.SecondaryGuesser) >= 1 && dyingTarget != PlayerControl.LocalPlayer)
                        __instance.playerStates.ToList().ForEach(x => { if (x.TargetPlayerId == dyingTarget.PlayerId && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });
                    else
                        __instance.playerStates.ToList().ForEach(x => { if (x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });

                    // Shoot player and send chat info if activated
                    RPCEventInvoker.Guess(dyingTarget.PlayerId);
                }
            }));

            i++;
        }
        container.transform.localScale *= 0.85f;

        FastDestroyableSingleton<HatManager>.Instance.GetNamePlateById("nameplate_NoPlate").CoLoadViewData((Il2CppSystem.Action<NamePlateViewData>)((n) =>
        {
            foreach (var b in buttons)
                b.GetComponent<SpriteRenderer>().sprite = n.Image;
        }));
    }

    public static void SetupMeetingButton(MeetingHud __instance)
    {
        if (!PlayerControl.LocalPlayer.Data.IsDead && Game.GameData.data.myData.getGlobalData().GetExtraRoleData(Roles.SecondaryGuesser.id) > 0)
        {
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                targetBox.name = "ShootButton";
                targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
                SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                renderer.sprite = ComplexRoles.FGuesser.targetSprite.GetSprite();
                PassiveButton button = targetBox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                int copiedIndex = i;
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => guesserOnClick(copiedIndex, __instance)));
            }
        }
    }

    public static void MeetingUpdate(MeetingHud __instance, TMPro.TextMeshPro meetingInfo)
    {
        ulong left = Game.GameData.data.myData.getGlobalData().GetExtraRoleData(Roles.SecondaryGuesser);
        if (left <= 0) return;
        meetingInfo.text = Language.Language.GetString("role.guesser.guessesLeft") + ": " + left;
        meetingInfo.gameObject.SetActive(true);
    }
}

public class Guesser : Template.BilateralnessRole
{
    public override RelatedExtraRoleData[] RelatedExtraRoleDataInfo => GuesserSystem.RelatedExtraRoleDataInfo;
    //インポスターはModで操作するFakeTaskは所持していない
    public Guesser(string name, string localizeName, bool isImpostor)
            : base(name, localizeName,
                 isImpostor ? Palette.ImpostorRed : FGuesser.RoleColor,
                 isImpostor ? RoleCategory.Impostor : RoleCategory.Crewmate,
                 isImpostor ? Side.Impostor : Side.Crewmate, isImpostor ? Side.Impostor : Side.Crewmate,
                 isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                 isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                 isImpostor ? ImpostorRoles.Impostor.impostorEndSet : CrewmateRoles.Crewmate.crewmateEndSet,
                 false, isImpostor ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse,
                 isImpostor, isImpostor, isImpostor, () => { return Roles.F_Guesser; }, isImpostor)
    {
        IsGuessableRole = false;
        IsHideRole = true;
    }

    public override Assignable AssignableOnHelp => Roles.F_Guesser;
    public override HelpSprite[] helpSprite => Roles.F_Guesser.helpSprite;

    public override void GlobalInitialize(PlayerControl __instance)
    {
        GuesserSystem.GlobalInitialize(__instance);
    }

    public override void SetupMeetingButton(MeetingHud __instance)
    {
        GuesserSystem.SetupMeetingButton(__instance);
    }

    public override void MeetingUpdate(MeetingHud __instance, TMPro.TextMeshPro meetingInfo)
    {
        GuesserSystem.MeetingUpdate(__instance, meetingInfo);
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Agent);
        RelatedRoles.Add(Roles.EvilAce);
    }

    public override bool IsSpawnable()
    {
        if (Roles.F_Guesser.secondoryRoleOption.getBool()) return false;
        return base.IsSpawnable();
    }
}

public class SecondaryGuesser : ExtraRole
{
    public override RelatedExtraRoleData[] RelatedExtraRoleDataInfo => GuesserSystem.RelatedExtraRoleDataInfo;

    public SecondaryGuesser()
            : base("Guesser", "guesser", FGuesser.RoleColor, 0)
    {
        IsHideRole = true;
    }

    public override Assignable AssignableOnHelp => Roles.F_Guesser;
    public override HelpSprite[] helpSprite => Roles.F_Guesser.helpSprite;

    private void _sub_Assignment(Patches.AssignMap assignMap, List<byte> players, int count)
    {
        if (!Roles.F_Guesser.TopOption.getBool()) return;
        if (!Roles.F_Guesser.secondoryRoleOption.getBool()) return;

        int chance = Roles.F_Guesser.RoleChanceOption.getSelection() + 1;

        byte playerId;
        for (int i = 0; i < count; i++)
        {
            //割り当てられない場合終了
            if (players.Count == 0) return;

            if (chance <= NebulaPlugin.rnd.Next(10)) continue;

            playerId = players[NebulaPlugin.rnd.Next(players.Count)];
            assignMap.AssignExtraRole(playerId, id, 0);
            players.Remove(playerId);
        }
    }

    public override void Assignment(Patches.AssignMap assignMap)
    {
        if (!Roles.F_Guesser.secondoryRoleOption.getBool()) return;


        List<byte> impostors = new List<byte>();
        List<byte> crewmates = new List<byte>();
        List<byte> neutrals = new List<byte>();

        foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (!player.GetModData()?.role.CanHaveExtraAssignable(this) ?? true) continue;

            switch (player.GetModData().role.category)
            {
                case RoleCategory.Crewmate:
                    crewmates.Add(player.PlayerId);
                    break;
                case RoleCategory.Impostor:
                    impostors.Add(player.PlayerId);
                    break;
                case RoleCategory.Neutral:
                    neutrals.Add(player.PlayerId);
                    break;
            }
        }

        _sub_Assignment(assignMap, crewmates, (int)Roles.F_Guesser.crewmateRoleCountOption.getFloat());
        _sub_Assignment(assignMap, impostors, (int)Roles.F_Guesser.impostorRoleCountOption.getFloat());
        _sub_Assignment(assignMap, neutrals, (int)Roles.F_Guesser.neutralRoleCountOption.getFloat());
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        GuesserSystem.GlobalInitialize(__instance);
    }

    public override void SetupMeetingButton(MeetingHud __instance)
    {
        GuesserSystem.SetupMeetingButton(__instance);
    }

    public override void MeetingUpdate(MeetingHud __instance, TMPro.TextMeshPro meetingInfo)
    {
        GuesserSystem.MeetingUpdate(__instance, meetingInfo);
    }

    public override void EditDescriptionString(ref string description)
    {
        description += "\n" + Language.Language.GetString("role.secondaryGuesser.description");
    }

    public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
    {
        if (playerId == PlayerControl.LocalPlayer.PlayerId || Game.GameData.data.myData.CanSeeEveryoneInfo)
            EditDisplayNameForcely(playerId, ref displayName);
    }

    public override void EditDisplayNameForcely(byte playerId, ref string displayName)
    {
        displayName += Helpers.cs(Color, "⊕");
    }

    /// <summary>
    /// この役職が発生しうるかどうか調べます
    /// </summary>
    public override bool IsSpawnable()
    {
        if (!Roles.F_Guesser.secondoryRoleOption.getBool()) return false;
        if (!Roles.F_Guesser.TopOption.getBool()) return false;

        return true;
    }

    public override void EditSpawnableRoleShower(ref string suffix, Role role)
    {
        if (IsSpawnable() && role.CanHaveExtraAssignable(this) &&
            (
            (role.side == Side.Crewmate && Roles.F_Guesser.crewmateRoleCountOption.getFloat() > 0) ||
            (role.side == Side.Impostor && Roles.F_Guesser.impostorRoleCountOption.getFloat() > 0) ||
            (role.side != Side.Crewmate && role.side != Side.Impostor && Roles.F_Guesser.neutralRoleCountOption.getFloat() > 0)))
            suffix += Helpers.cs(Roles.SecondaryGuesser.Color, "⊕");
    }

    public override Module.CustomOption? RegisterAssignableOption(Role role)
    {
        Module.CustomOption option = role.CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeGuesser", role.DefaultExtraAssignableFlag(this), true).HiddenOnDisplay(true).SetIdentifier("role." + role.LocalizeName + ".canBeGuesser");
        option.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
        option.AddCustomPrerequisite(() => { return Roles.SecondaryGuesser.IsSpawnable(); });
        option.AddCustomPrerequisite(() =>
        {
            return
                (role.side == Side.Crewmate && Roles.F_Guesser.crewmateRoleCountOption.getFloat() > 0) ||
                (role.side == Side.Impostor && Roles.F_Guesser.impostorRoleCountOption.getFloat() > 0) ||
                (role.side != Side.Crewmate && role.side != Side.Impostor && Roles.F_Guesser.neutralRoleCountOption.getFloat() > 0);
        });
        return option;
    }
}
