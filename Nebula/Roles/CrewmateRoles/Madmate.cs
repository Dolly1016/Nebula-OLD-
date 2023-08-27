namespace Nebula.Roles.Crewmate;

public class Madmate : Role
{
    public override RoleCategory oracleCategory { get { return RoleCategory.Impostor; } }


    private Module.CustomOption CanUseVentsOption;
    private Module.CustomOption CanMoveInVentOption;
    public Module.CustomOption CanFixSabotageOption;
    private Module.CustomOption HasImpostorVisionOption;
    private Module.CustomOption IgnoreBlackOutOption;
    private Module.CustomOption CanInvokeSabotageOption;
    private Module.CustomOption InvolveNonImpostorPlayerOnExile;
    private Module.CustomOption CanKnowImpostorsByTasksOption;
    //private Module.CustomOption NumOfMaxImpostorsCanKnowOption;
    private Module.CustomOption[] NumOfTasksRequiredToKnowImpostorsOption;
    public Module.CustomOption SecondoryRoleOption;

    //Local
    private HashSet<byte> knownImpostors = new HashSet<byte>();

    public override List<ExtraRole> GetImplicateExtraRoles() { return new List<ExtraRole>(new ExtraRole[] { Roles.SecondaryMadmate }); }

    public override bool IsSecondaryGenerator { get { return SecondoryRoleOption.getBool(); } }
    public override void LoadOptionData()
    {
        base.LoadOptionData();

        TopOption.tab = Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.Modifiers;
        TopOption.SetYellowCondition((tab) =>
        {
            return (tab == Module.CustomOptionTab.Modifiers) == SecondoryRoleOption.getBool() && RoleChanceOption.selection == RoleChanceOption.selections.Length - 1;
        });

        SecondoryRoleOption = CreateOption(Color.white, "isSecondaryRole", false);

        CanUseVentsOption = CreateOption(Color.white, "canUseVents", true).AddInvPrerequisite(SecondoryRoleOption);
        CanMoveInVentOption = CreateOption(Color.white, "canMoveInVent", true).AddInvPrerequisite(SecondoryRoleOption).AddPrerequisite(CanUseVentsOption);
        CanInvokeSabotageOption = CreateOption(Color.white, "canInvokeSabotage", true).AddInvPrerequisite(SecondoryRoleOption);
        CanFixSabotageOption = CreateOption(Color.white, "canFixLightsAndComms", true);

        HasImpostorVisionOption = CreateOption(Color.white, "hasImpostorVision", false).AddInvPrerequisite(SecondoryRoleOption);
        IgnoreBlackOutOption = CreateOption(Color.white, "ignoreBlackout", false).AddInvPrerequisite(SecondoryRoleOption);

        InvolveNonImpostorPlayerOnExile = CreateOption(Color.white, "involveNonImpostorPlayerOnExile", false).AddInvPrerequisite(SecondoryRoleOption);

        CanKnowImpostorsByTasksOption = CreateOption(Color.white, "canKnowImpostorsByTasks", true).AddInvPrerequisite(SecondoryRoleOption);
        CanKnowImpostorsByTasksOption.postOptionScreenBuilder = (refresher) =>
        {
            Module.MetaScreenContent[] contents;

            if (CanKnowImpostorsByTasksOption.getBool())
            {
                contents = new Module.MetaScreenContent[14];

                contents[0] = new Module.MSMargin(0.5f);
                contents[1] = new Module.MSString(3f, Language.Language.GetString("role.madmate.numOfTasksRequired"), TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold);
                contents[2] = new Module.MSString(0.2f, ":", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
                bool flag = false;
                for (int i = 0; i < 3; i++)
                {
                    int index = i;
                    if (flag)
                    {
                        contents[3 + i * 4] = new Module.MSMargin(0.5f);
                        contents[4 + i * 4] = new Module.MSMargin(0.46f);
                        contents[5 + i * 4] = new Module.MSMargin(0.5f);
                    }
                    else
                    {
                        contents[3 + i * 4] = new Module.MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () =>
                        {
                            NumOfTasksRequiredToKnowImpostorsOption[index].addSelection(-1);
                            refresher();
                        });
                        contents[4 + i * 4] = new Module.MSString(0.4f, NumOfTasksRequiredToKnowImpostorsOption[index].getString(), 2f, 0.6f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
                        contents[5 + i * 4] = new Module.MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () =>
                        {
                            NumOfTasksRequiredToKnowImpostorsOption[index].addSelection(1);
                            refresher();
                        });
                    }
                    if (i != 2) contents[6 + i * 4] = new Module.MSMargin(0.05f);

                    if (NumOfTasksRequiredToKnowImpostorsOption[index].getSelection() == 0) flag = true;
                }
            }
            else
            {
                contents = new Module.MetaScreenContent[0];
            }
            return new Module.MetaScreenContent[][] { contents };
        };
        CanKnowImpostorsByTasksOption.DisplayValueDecorator = (orig, option) =>
        {
            if (!option.getBool()) return orig;
            for (int i = 0; i < 3; i++)
            {
                if (NumOfTasksRequiredToKnowImpostorsOption[i].getSelection() == 0)
                {
                    if (i == 0) return orig;
                    break;
                }
                if (i == 0) orig += " ("; else orig += ", ";
                orig += NumOfTasksRequiredToKnowImpostorsOption[i].getFloat();
            }
            orig += ")";
            return orig;
        };


        NumOfTasksRequiredToKnowImpostorsOption = new Module.CustomOption[3];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            NumOfTasksRequiredToKnowImpostorsOption[i] =
                CreateOption(Color.white, "numOfTasksRequiredToKnowImpostors" + (i + 1), CustomOptionHolder.GetStringMixedSelections("option.display.percentage.andSoForth", 1f, 25f, 1f, 25f, 1f).ToArray(), i == 0 ? (object)1f : (object)"option.display.percentage.andSoForth");
            NumOfTasksRequiredToKnowImpostorsOption[i].isHidden = true;
        }

        CanBeGuesserOption?.AddInvPrerequisite(SecondoryRoleOption);
        CanBeDrunkOption?.AddInvPrerequisite(SecondoryRoleOption);
        CanBeBloodyOption?.AddInvPrerequisite(SecondoryRoleOption);
        CanBeLoversOption?.AddInvPrerequisite(SecondoryRoleOption);
        CanBeMadmateOption?.AddInvPrerequisite(SecondoryRoleOption);
    }

    //適切なタイミングでインポスターを発見する
    public override void OnTaskComplete(PlayerTask? task)
    {
        UpdateKnownImpostors();
    }

    private void UpdateKnownImpostors()
    {
        int completedTasks = Game.GameData.data.myData.getGlobalData().Tasks.Completed;

        //5人全員までは分かっておらず、タスク数が設定されておりかつ条件を満たしている
        while (knownImpostors.Count < 3 && NumOfTasksRequiredToKnowImpostorsOption[knownImpostors.Count].getSelection() != 0 &&
            NumOfTasksRequiredToKnowImpostorsOption[knownImpostors.Count].getFloat() <= completedTasks)
        {
            List<byte> candidates = new List<byte>();
            foreach (var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (player.GetModData().role.category != RoleCategory.Impostor) continue;
                if (knownImpostors.Contains(player.PlayerId)) continue;
                candidates.Add(player.PlayerId);
            }

            if (candidates.Count == 0) return;
            knownImpostors.Add(candidates[NebulaPlugin.rnd.Next(candidates.Count)]);
        }
    }

    public override void EditOthersDisplayNameColor(byte playerId, ref Color displayColor)
    {
        if (knownImpostors.Contains(playerId)) displayColor = Palette.ImpostorRed;
    }

    public override void Initialize(PlayerControl __instance)
    {
        knownImpostors.Clear();

        UpdateKnownImpostors();
    }

    //カットするタスクの数を計上したうえで初期化
    public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo> actualTasks)
    {
        int impostors = 0;
        foreach (var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            if (player.GetModData().role.category == RoleCategory.Impostor) impostors++;
        if (impostors > 3) impostors = 3;

        int requireTasks = 0;
        for (int i = 0; i < impostors; i++)
        {
            //未設定まで到達したらそこで終了
            if (NumOfTasksRequiredToKnowImpostorsOption[i].getSelection() == 0) break;

            if (requireTasks < NumOfTasksRequiredToKnowImpostorsOption[i].getFloat()) requireTasks = (int)NumOfTasksRequiredToKnowImpostorsOption[i].getFloat();
        }

        var gameOptions = GameOptionsManager.Instance.CurrentGameOptions;
        
        while (initialTasks.Count > requireTasks && requireTasks > 0)
        {
            if (initialTasks.Count == 0) break;
            initialTasks.RemoveAt(NebulaPlugin.rnd.Next(initialTasks.Count));
        }
    }

    //黒猫設定
    public override void OnExiledPre(byte[] voters)
    {
        if (!InvolveNonImpostorPlayerOnExile.getBool()) return;

        List<PlayerControl> players = new List<PlayerControl>();
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p.Data.IsDead) continue;
            if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            var data = p.GetModData();
            if (data.role.DeceiveImpostorInNameDisplay || data.role.category == RoleCategory.Impostor) continue;

            players.Add(p);
        }

        //ランダムに相手を選んで追放する
        RPCEventInvoker.UncheckedExilePlayer(players[NebulaPlugin.rnd.Next(players.Count)].PlayerId, Game.PlayerData.PlayerStatus.Embroiled.Id);
    }
    public override bool CanFixSabotage => CanFixSabotageOption.getBool();
    public override void GlobalIntroInitialize(PlayerControl __instance)
    {
        canMoveInVents = CanMoveInVentOption.getBool();
        VentPermission = CanUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
        canInvokeSabotage = CanInvokeSabotageOption.getBool();
        canFixSabotage = CanFixSabotageOption.getBool();
        UseImpostorLightRadius = HasImpostorVisionOption.getBool();
        IgnoreBlackout = IgnoreBlackOutOption.getBool();
    }

    public override bool IsUnsuitable { get { return SecondoryRoleOption.getBool(); } }

    public override bool HasExecutableFakeTask(byte playerId) => CanKnowImpostorsByTasksOption.getBool();

    public Madmate()
            : base("Madmate", "madmate", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Impostor.Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, false, false)
    {
    }
}

public class SecondaryMadmate : ExtraRole
{
    //インポスターはModで操作するFakeTaskは所持していない
    public SecondaryMadmate()
            : base("Madmate", "madmate", Palette.ImpostorRed, 0)
    {
        IsHideRole = true;
    }

    private void _sub_Assignment(Patches.AssignMap assignMap, List<byte> players, int count)
    {
        int chance = Roles.Madmate.RoleChanceOption.getSelection() + 1;

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
        if (!Roles.Madmate.TopOption.getBool() || !Roles.Madmate.SecondoryRoleOption.getBool()) return;

        List<byte> crewmates = new List<byte>();

        foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (!player.GetModData()?.role.CanHaveExtraAssignable(this) ?? true) continue;

            switch (player.GetModData()?.role.category)
            {
                case RoleCategory.Crewmate:
                    crewmates.Add(player.PlayerId);
                    break;
            }
        }

        _sub_Assignment(assignMap, crewmates, (int)Roles.Madmate.RoleCountOption.getFloat());
    }

    public override void EditDisplayRoleName(byte playerId, ref string roleName, bool isIntro)
    => EditDisplayRoleNameForcely(playerId, ref roleName);

    public override void EditDisplayRoleNameForcely(byte playerId, ref string displayName)
    {
        displayName = Helpers.cs(Palette.ImpostorRed, Language.Language.GetString("role.madmate.secondaryPrefix")) + displayName;
    }

    /// <summary>
    /// この役職が発生しうるかどうか調べます
    /// </summary>
    public override bool IsSpawnable()
    {
        if (!Roles.Madmate.SecondoryRoleOption.getBool()) return false;

        return true;
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        canFixSabotage = Roles.Madmate.CanFixSabotageOption.getBool();
    }

    public override bool HasCrewmateTask(byte playerId)
    {
        return false;
    }

    public override bool CanFixSabotage => Roles.Madmate.CanFixSabotage;


    public override bool CheckAdditionalWin(PlayerControl player, Patches.EndCondition condition)
    {
        return Roles.Impostor.winReasons.Contains(condition);
    }

    public override void EditSpawnableRoleShower(ref string suffix, Role role)
    {
        if (IsSpawnable() && role.CanHaveExtraAssignable(this)) suffix += Helpers.cs(Color, "*");
    }

    public override Module.CustomOption? RegisterAssignableOption(Role role)
    {
        if (role.category != RoleCategory.Crewmate) return null;

        Module.CustomOption option = role.CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeMadmate", role.DefaultExtraAssignableFlag(this), true).HiddenOnDisplay(true).SetIdentifier("role." + role.LocalizeName + ".canBeMadmate");
        option.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
        option.AddCustomPrerequisite(() => { return Roles.SecondaryMadmate.IsSpawnable(); });
        return option;
    }
}


public static class MadmateHelper
{
    static public bool IsMadmate(this PlayerControl player)
    {
        return player.GetModData()?.HasExtraRole(Roles.SecondaryMadmate) ?? false;
    }

    static public bool IsMadmate(this Game.PlayerData player)
    {
        return player.HasExtraRole(Roles.SecondaryMadmate);
    }
}