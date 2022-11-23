using Nebula.Patches;

namespace Nebula.Roles.ExtraRoles;

public class Lover : ExtraRole
{
    public override RelatedExtraRoleData[] RelatedExtraRoleDataInfo { get => new RelatedExtraRoleData[] { new RelatedExtraRoleData("Lovers Identifer", this, 1, 5) }; }

    private Module.CustomOption maxPairsOption;
    public Module.CustomOption loversModeOption;
    public Module.CustomOption chanceThatOneLoverIsImpostorOption;
    private Module.CustomOption canChangeTrilemmaOption;
    public Module.CustomOption loversAsIndependentSideOption;

    private PlayerControl trilemmaTarget = null;

    static public Color[] iconColor { get; } = new Color[] {
        (Color)new Color32(251, 3, 188, 255) ,
        (Color)new Color32(254, 132, 3, 255) ,
        (Color)new Color32(3, 254, 188, 255) ,
        (Color)new Color32(255, 255, 0, 255) ,
        (Color)new Color32(3, 183, 254, 255) };

    public Game.PlayerData GetLoversData(Game.PlayerData player)
    {
        ulong myLoverId = player.GetExtraRoleData(this);
        PlayerControl target;
        foreach (Game.PlayerData data in Game.GameData.data.AllPlayers.Values)
        {
            if (data == player) continue;
            if (!data.extraRole.Contains(this)) continue;
            if (data.GetExtraRoleData(this) == myLoverId)
            {
                return data;
            }
        }

        return player;
    }

    private bool IsMyLover(PlayerControl player)
    {
        if (player == null) return false;

        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) return false;

        ulong myLoverId = PlayerControl.LocalPlayer.GetModData().GetExtraRoleData(this);
        Game.PlayerData data = player.GetModData();

        if (!data.extraRole.Contains(this)) return false;

        if (data.GetExtraRoleData(this) == myLoverId) return true;

        return false;
    }

    private void ActionForLover(PlayerControl player, System.Action<PlayerControl> action)
    {
        ulong myLoverId = player.GetModData().GetExtraRoleData(this);
        PlayerControl target;
        foreach (Game.PlayerData data in Game.GameData.data.AllPlayers.Values)
        {
            if (!data.extraRole.Contains(this)) continue;
            if (data.GetExtraRoleData(this) == myLoverId)
            {
                target = Helpers.playerById(data.id);
                if (target != null)
                {
                    action.Invoke(target);
                }
            }
        }
    }

    private void ActionForMyLover(System.Action<PlayerControl> action)
    {
        ActionForLover(PlayerControl.LocalPlayer, (player) =>
        {
                //自身であれば特に何もしない
                if (player == PlayerControl.LocalPlayer) return;

            action.Invoke(player);
        });
    }

    public override void OnExiledPre(byte[] voters)
    {
        ActionForMyLover((player) =>
        {
                //自身であれば特に何もしない
                if (player == PlayerControl.LocalPlayer) return;


            if (!player.Data.IsDead) RPCEventInvoker.UncheckedExilePlayer(player.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id);
        }
        );
    }

    public override void OnMurdered(byte murderId)
    {
        ActionForMyLover((player) =>
        {
            if (!player.Data.IsDead)
            {
                if (loversModeOption.getSelection() == 1)
                {
                    if (murderId != PlayerControl.LocalPlayer.PlayerId)
                    {
                        RPCEventInvoker.ImmediatelyChangeRole(player, Roles.Avenger);
                        RPCEventInvoker.SetExtraRole(Helpers.playerById(murderId), Roles.AvengerTarget, Game.GameData.data.myData.getGlobalData().GetExtraRoleData(this));
                        return;
                    }
                }
                RPCEventInvoker.UncheckedMurderPlayer(player.PlayerId, player.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id, false);
            }
        }
        );
    }

    //上記で殺しきれない場合
    public override void OnDied()
    {
        ActionForMyLover((player) =>
        {
            if (player.Data.IsDead) return;

            if (loversModeOption.getSelection() == 1)
            {
                byte murder = Game.GameData.data.deadPlayers[PlayerControl.LocalPlayer.PlayerId].MurderId;
                if (murder != byte.MaxValue && murder != PlayerControl.LocalPlayer.PlayerId)
                {
                    RPCEventInvoker.ImmediatelyChangeRole(player, Roles.Avenger);
                    RPCEventInvoker.AddExtraRole(Helpers.playerById(murder), Roles.AvengerTarget, Game.GameData.data.myData.getGlobalData().GetExtraRoleData(this));
                    return;
                }
            }

            if (Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Guessed ||
                        Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Misguessed)
                RPCEventInvoker.CloseUpKill(player, player, Game.PlayerData.PlayerStatus.Suicide);
            else
                RPCEventInvoker.UncheckedExilePlayer(player.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id);

        }
        );
    }

    public override void Assignment(Patches.AssignMap assignMap)
    {
        int maxPairs = maxPairsOption.getSelection();
        if (maxPairs * 2 > Game.GameData.data.AllPlayers.Count) maxPairs = Game.GameData.data.AllPlayers.Count / 2;

        int pairs = Helpers.CalcProbabilityCount(RoleChanceOption.getSelection() + 1, maxPairs);

        List<PlayerControl> crewmates = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
        List<PlayerControl> impostors = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
        crewmates.RemoveAll(x => x.Data.Role.IsImpostor);
        impostors.RemoveAll(x => !x.Data.Role.IsImpostor);

        crewmates.RemoveAll((player) => { return !player.GetModData().role.CanHaveExtraAssignable(this); });
        impostors.RemoveAll((player) => { return !player.GetModData().role.CanHaveExtraAssignable(this); });

        int[] crewmateIndex = Helpers.GetRandomArray(crewmates.Count);
        int[] impostorIndex = Helpers.GetRandomArray(impostors.Count);
        int crewmateUsed = 0, impostorUsed = 0;

        for (int i = 0; i < pairs; i++)
        {
            //割り当てられるインポスターがいない場合確率に依らずクルー同士をあてがう

            if (impostorUsed < impostorIndex.Length && NebulaPlugin.rnd.NextDouble() * 10 < chanceThatOneLoverIsImpostorOption.getSelection())
            {
                //片方がインポスターの場合

                //割り当てられない場合終了
                if (crewmateUsed >= crewmateIndex.Length) break;

                assignMap.AssignExtraRole(crewmates[crewmateIndex[crewmateUsed]].PlayerId, this.id, (ulong)(i + 1));
                assignMap.AssignExtraRole(impostors[impostorIndex[impostorUsed]].PlayerId, this.id, (ulong)(i + 1));

                crewmateUsed++;
                impostorUsed++;
            }
            else
            {
                //両方ともインポスターでない場合

                //割り当てられない場合終了
                if (crewmateUsed + 1 >= crewmateIndex.Length) break;

                for (int p = 0; p < 2; p++)
                {
                    assignMap.AssignExtraRole(crewmates[crewmateIndex[crewmateUsed]].PlayerId, this.id, (ulong)(i + 1));
                    crewmateUsed++;
                }
            }
        }
    }

    public override void LoadOptionData()
    {
        maxPairsOption = CreateOption(Color.white, "maxPairs", 1f, 0f, 5f, 1f);
        loversModeOption = CreateOption(Color.white, "mode", new string[] { "role.lover.mode.standard", "role.lover.mode.avenger" });
        chanceThatOneLoverIsImpostorOption = CreateOption(Color.white, "chanceThatOneLoverIsImpostor", CustomOptionHolder.rates);
        loversAsIndependentSideOption = CreateOption(Color.white, "loversAsIndependentSide", false);

        canChangeTrilemmaOption = CreateOption(Color.white, "canChangeTrilemma", true);
        canChangeTrilemmaOption.AddCustomPrerequisite(() => loversModeOption.getSelection() == 0);

    }

    public override IEnumerable<Assignable> GetFollowRoles()
    {
        yield return Roles.Avenger;
    }

    public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
    {
        bool showFlag = false;
        if (Game.GameData.data.myData.CanSeeEveryoneInfo) showFlag = true;
        else if (Game.GameData.data.myData.getGlobalData().extraRole.Contains(this))
        {
            ulong pairId = Game.GameData.data.myData.getGlobalData().GetExtraRoleData(this);
            if (Game.GameData.data.playersArray[playerId].GetExtraRoleData(this) == pairId) showFlag = true;
        }

        if (!showFlag && loversModeOption.getSelection() == 1 && Roles.Avenger.canKnowExistenceOfAvengerOption.getBool())
        {
            int selection = Roles.Avenger.canKnowExistenceOfAvengerOption.getSelection();
            var myData = PlayerControl.LocalPlayer.GetModData();
            if (selection == 1 || (selection == 2 && myData.HasExtraRole(Roles.AvengerTarget) && myData.GetExtraRoleData(Roles.AvengerTarget.id) == Game.GameData.data.AllPlayers[playerId].GetExtraRoleData(this)))
            {
                PlayerControl player = Helpers.playerById(playerId);
                if (player == null) return;

                bool avengerFlag = false;
                ActionForLover(player, (p) => { if (p != player && !p.Data.IsDead && player.Data.IsDead) avengerFlag = true; });

                if (avengerFlag) { displayName += Helpers.cs(Roles.Avenger.Color, "♥"); return; }
            }
        }

        if (showFlag) EditDisplayNameForcely(playerId, ref displayName);
    }

    public override void EditDisplayNameForcely(byte playerId, ref string displayName)
    {
        try
        {
            displayName += Helpers.cs(
                    iconColor[Game.GameData.data.AllPlayers[playerId].GetExtraRoleData(this) - 1], "♥");
        }
        catch (Exception e)
        {
            displayName += Helpers.cs(
                    iconColor[0], "♥");
        }
    }

    public override void EditDescriptionString(ref string desctiption)
    {
        string partner = "";
        ActionForMyLover((player) =>
        {
            partner = player.name;
        });
        partner = Helpers.cs(Color, partner);
        desctiption += "\n" + Language.Language.GetString("role.lover.description").Replace("%NAME%", partner);
    }

    public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
    {
        bool winFlag = false;
        ActionForLover(player, (partner) =>
        {
            if (player == partner) return;
            if (partner.Data.IsDead) return;
            if (condition == EndCondition.LoversWin && !player.Data.IsDead)
            {
                winFlag = true;
                return;
            }
            if (partner.GetModData().role.CheckWin(partner, condition))
            {
                winFlag = true;
                return;
            }
            Helpers.RoleAction(partner, (role) =>
            {
                if (role == this) return;
                winFlag |= role.CheckAdditionalWin(partner, condition);
            });
        });
        if (winFlag && condition != EndCondition.LoversWin)
        {
            EndGameManagerSetUpPatch.AddEndText(Language.Language.GetString("role.lover.additionalEndText"));
        }
        return winFlag;
    }

    private CustomButton involveButton;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.InvolveButton.png", 115f);
    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.lover.help.involve",0.3f)
    };

    public override void MyPlayerControlUpdate()
    {
        if (loversModeOption.getSelection() != 0) return;

        trilemmaTarget = Patches.PlayerControlPatch.SetMyTarget(1.5f);

        if (IsMyLover(trilemmaTarget))
        {
            trilemmaTarget = null;
            return;
        }

        Patches.PlayerControlPatch.SetPlayerOutline(trilemmaTarget, iconColor[0]);
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (involveButton != null)
        {
            involveButton.Destroy();
        }

        if (!canChangeTrilemmaOption.getBool() || loversModeOption.getSelection() != 0) return;

        involveButton = new CustomButton(
            () =>
            {
                PlayerControl target = trilemmaTarget;

                    //巻き込まれるのがラバーズであった場合
                    if (target.GetModData().extraRole.Contains(this))
                {
                    ulong removeId = target.GetModData().GetExtraRoleData(id);

                    foreach (Game.PlayerData data in Game.GameData.data.AllPlayers.Values)
                    {
                        if (data.GetExtraRoleData(id) != removeId) continue;

                            //鞍替えする側はなにもしない
                            if (data.id == target.PlayerId) continue;

                            //ロール消去
                            RPCEventInvoker.ImmediatelyUnsetExtraRole(Helpers.playerById(data.id), this);

                        break;
                    }
                }

                    //巻き込まれるのがトリレマであった場合
                    if (target.GetModData().extraRole.Contains(Roles.Trilemma))
                {
                    ulong removeId = target.GetModData().GetExtraRoleData(Roles.Trilemma.id);

                    foreach (Game.PlayerData data in Game.GameData.data.AllPlayers.Values)
                    {
                        if (data.GetExtraRoleData(Roles.Trilemma.id) != removeId) continue;

                            //鞍替えする側はなにもしない
                            if (data.id == target.PlayerId) continue;

                            //ロール消去
                            RPCEventInvoker.ImmediatelyUnsetExtraRole(Helpers.playerById(data.id), Roles.Trilemma);
                    }
                }

                ulong myLoverId = PlayerControl.LocalPlayer.GetModData().GetExtraRoleData(this);

                RPCEventInvoker.ChangeExtraRole(target, this, Roles.Trilemma, myLoverId);
                ActionForMyLover((player) =>
                {
                    RPCEventInvoker.ChangeExtraRole(player, this, Roles.Trilemma, myLoverId);
                });
                RPCEventInvoker.ChangeExtraRole(PlayerControl.LocalPlayer, this, Roles.Trilemma, myLoverId);


                trilemmaTarget = null;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return trilemmaTarget && PlayerControl.LocalPlayer.CanMove; },
            () => { },
            buttonSprite.GetSprite(),
            new Vector3(0f, 1f, 0f),
            __instance,
            Module.NebulaInputManager.modifierAbilityInput.keyCode,
            true,
            "button.label.involve"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        involveButton.MaxTimer = 0;

        trilemmaTarget = null;
    }

    public override void CleanUp()
    {
        if (involveButton != null)
        {
            involveButton.Destroy();
            involveButton = null;
        }
    }


    public virtual bool IsSpawnable()
    {
        if (maxPairsOption.getFloat() == 0f) return false;

        return base.IsSpawnable();
    }

    public override bool HasCrewmateTask(byte playerId)
    {
        return false;
    }

    public override void EditSpawnableRoleShower(ref string suffix, Role role)
    {
        if (IsSpawnable() && role.CanHaveExtraAssignable(this))
        {
            if (role.category != RoleCategory.Impostor || chanceThatOneLoverIsImpostorOption.getSelection() > 0)
            {
                suffix += Helpers.cs(Roles.Lover.Color, "♥");
            }
        }
    }

    public override Module.CustomOption? RegisterAssignableOption(Role role)
    {
        Module.CustomOption option = role.CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeLovers", role.DefaultExtraAssignableFlag(this), true).HiddenOnDisplay(true).SetIdentifier("role." + role.LocalizeName + ".canBeLovers");
        option.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
        option.AddCustomPrerequisite(() => { return Roles.Lover.IsSpawnable(); });
        if (role.category == RoleCategory.Impostor)
            option.AddCustomPrerequisite(() => { return Roles.Lover.chanceThatOneLoverIsImpostorOption.getSelection() > 0; });
        return option;
    }

    public Lover() : base("Lover", "lover", iconColor[0], 48)
    {
        FixedRoleCount = true;
    }
}