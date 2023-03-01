using Nebula.Module;

namespace Nebula.Roles.MetaRoles;

public class VOID : Role
{
    static public Color RoleColor = new Color(173f / 255f, 173f / 255f, 198f / 255f);

    public override bool CanHaveGhostRole { get => false; }

    public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
    {
        initialTasks.Clear();
        actualTasks = null;
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        __instance.Die(DeathReason.Exile,false);
        __instance.GetModData().Die(Game.PlayerData.PlayerStatus.Exiled);
    }

    public override void Initialize(PlayerControl __instance)
    {
        Game.GameData.data.myData.CanSeeEveryoneInfo = true;
        HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
    }


    public override void LoadOptionData()
    {

    }


    private SpriteLoader voidButtonSprite = new SpriteLoader("Nebula.Resources.VOIDButton.png", 115f);
    private CustomButton voidButton;

    public override HelpSprite[] helpSprite => new HelpSprite[]
     {
            new HelpSprite(voidButtonSprite,"role.void.help.void",0.3f)
     };

    public override void MyUpdate()
    {
        if (MetaDialog.dialogOrder.Count == 0 && Input.GetKeyDown(Module.NebulaInputManager.abilityInput.keyCode)) voidButton.onClickEvent();
    }

    private void MetaChangeRole(PlayerControl p)
    {
        Module.MetaDialog.MSDesigner? dialog = null;
        dialog = Module.MetaDialog.OpenRolesDialog((r) => r.category != RoleCategory.Complex, 0, 60, (r) =>
        {
            RPCEventInvoker.ImmediatelyChangeRole(p, r);
            MetaDialog.EraseDialog(2);
            OpenPlayerDialog(p, 0);
        });
    }

    private void MetaChangeGhostRole(PlayerControl p)
    {
        var data = p.GetModData();
        var dialog = Module.MetaDialog.OpenDialog(new Vector2(9f, 5f), "Ghost Role");
        dialog.AddGhostRoleTopic((r) => true, (r) =>
        {
            RPCEventInvoker.SetGhostRole(p, r);
            MetaDialog.EraseDialog(2);
            OpenPlayerDialog(p, 0);
        });
    }

    private void MetaEditModify(PlayerControl p)
    {
        var data = p.GetModData();
        var dialog = Module.MetaDialog.OpenDialog(new Vector2(9f, 5f), "Modifies");
        dialog.AddTopic(new MSString(2f, "Activated", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));
        dialog.AddModifyTopic((r) => data.HasExtraRole(r), (r) =>
        {
            RPCEventInvoker.ImmediatelyUnsetExtraRole(p, r);
            MetaDialog.EraseDialog(2);
            OpenPlayerDialog(p, 0);
            MetaEditModify(p);
        });
        dialog.AddTopic(new MSString(2f, "Unactivated", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));
        dialog.AddModifyTopic((r) => !data.HasExtraRole(r), (r) =>
        {
            RPCEventInvoker.AddExtraRole(p, r, 0);
            MetaDialog.EraseDialog(2);
            OpenPlayerDialog(p, 0);
            MetaEditModify(p);
        });
    }

    private void MetaKillPlayer(PlayerControl p)
    {
        RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId, p.PlayerId, Game.PlayerData.PlayerStatus.Dead.Id, false);
    }

    private void MetaExilePlayer(PlayerControl p)
    {
        RPCEventInvoker.UncheckedExilePlayer(p.PlayerId, Game.PlayerData.PlayerStatus.Dead.Id);
    }

    private void MetaRevivePlayer(PlayerControl p)
    {
        RPCEventInvoker.RevivePlayer(p, true);
    }

    private void AddModifySpeedTopic(MetaDialog.MSDesigner designer, PlayerControl? p)
    {
        float speed = 1f;
        float duration = 10f;
        MSString SpeedText = new MSString(1.2f, "1x", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
        MSButton SpeedDownButton = new MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () => { if (speed > -2f) speed -= 0.125f; SpeedText.text.text = speed.ToString() + "x"; });
        MSButton SpeedUpButton = new MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () => { if (speed < 3f) speed += 0.125f; SpeedText.text.text = speed.ToString() + "x"; });

        MSButton UnlimitedSpeedButton = new MSButton(0.8f, 0.5f, "Apply", TMPro.FontStyles.Bold, () =>
        {
            if (p != null) RPCEventInvoker.EmitSpeedFactor(p, new Game.SpeedFactor(0, 99999f, speed, true));
            else
            {
                foreach (var player in PlayerControl.AllPlayerControls) RPCEventInvoker.EmitSpeedFactor(player, new Game.SpeedFactor(0, 99999f, speed, true));
            }
        });

        MSString DurationText = new MSString(0.8f, "10s", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
        MSButton DurationDownButton = new MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () => { if (duration > 2.5f) duration -= 2.5f; DurationText.text.text = duration.ToString() + "s"; });
        MSButton DurationUpButton = new MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () => { if (duration < 60f) duration += 2.5f; DurationText.text.text = duration.ToString() + "s"; });

        MSButton LimitedSpeedButton = new MSButton(0.8f, 0.5f, "Apply", TMPro.FontStyles.Bold, () =>
        {
            if (p != null) RPCEventInvoker.EmitSpeedFactor(p, new Game.SpeedFactor(0, duration, speed, true));
            else
            {
                foreach (var player in PlayerControl.AllPlayerControls) RPCEventInvoker.EmitSpeedFactor(player, new Game.SpeedFactor(0, duration, speed, true));
            }
        });

        designer.AddTopic(
            new MSString(1.2f, "Speed:", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold), SpeedDownButton, SpeedText, SpeedUpButton, UnlimitedSpeedButton,
            new MSString(0.6f, "with", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold), DurationDownButton, DurationText, DurationUpButton, LimitedSpeedButton);

        UnlimitedSpeedButton.button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            SpeedText.text.color = Color.yellow;
        }));
        UnlimitedSpeedButton.button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            SpeedText.text.color = Color.white;
        }));

        LimitedSpeedButton.button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            SpeedText.text.color = Color.yellow;
            DurationText.text.color = Color.yellow;
        }));
        LimitedSpeedButton.button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            SpeedText.text.color = Color.white;
            DurationText.text.color = Color.white;
        }));
    }

    private void AddRoleDataTopic(MetaDialog.MSDesigner designer, PlayerControl player, Game.PlayerData data, int id, string display, int min, int max, string suffix, string[]? replace)
    {
        if (replace == null)
            designer.AddNumericDataTopic(display, data.GetRoleData(id), suffix, min, max, (v) => RPCEventInvoker.UpdateRoleData(player.PlayerId, id, v));
        else
            designer.AddNumericDataTopic(display, data.GetRoleData(id), replace, min, max, (v) => RPCEventInvoker.UpdateRoleData(player.PlayerId, id, v));
    }

    private void AddExtraRoleDataTopic(MetaDialog.MSDesigner designer, PlayerControl player, Game.PlayerData data, ExtraRole role, string display, int min, int max, string suffix, string[]? replace)
    {
        if (replace == null)
            designer.AddNumericDataTopic(display, (int)data.GetExtraRoleData(role.id), suffix, min, max, (v) => RPCEventInvoker.UpdateExtraRoleData(player.PlayerId, role.id, (ulong)v));
        else
            designer.AddNumericDataTopic(display, (int)data.GetExtraRoleData(role.id), replace, min, max, (v) => RPCEventInvoker.UpdateExtraRoleData(player.PlayerId, role.id, (ulong)v));
    }

    private void OpenPlayerDialog(PlayerControl p, int page)
    {
        var designer = MetaDialog.OpenPlayerDialog(new Vector2(8f, 5.2f), p);

        MSButton roleButton = new MSButton(1f, 0.4f, "Role", TMPro.FontStyles.Bold, () => MetaChangeRole(p));
        MSButton ghostButton = new MSButton(1f, 0.4f, "Ghost", TMPro.FontStyles.Bold, () => MetaChangeGhostRole(p));
        MSButton modifyButton = new MSButton(1f, 0.4f, "Modify", TMPro.FontStyles.Bold, () => MetaEditModify(p));
        MSButton killButton = new MSButton(1f, 0.4f, "Kill", TMPro.FontStyles.Bold, () => MetaKillPlayer(p));
        MSButton exileButton = new MSButton(1f, 0.4f, "Exile", TMPro.FontStyles.Bold, () => MetaExilePlayer(p));
        MSButton reviveButton = new MSButton(1f, 0.4f, "Revive", TMPro.FontStyles.Bold, () => MetaRevivePlayer(p));

        designer.AddTopic(roleButton, ghostButton, modifyButton, killButton, exileButton, reviveButton);

        AddModifySpeedTopic(designer, p);

        designer.AddTopic(
            new MSString(1f, "Paint:", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
            new MSButton(1.5f, 0.4f, "Paint", TMPro.FontStyles.Bold, () =>
            {
                MetaDialog.MSDesigner? designer = null;
                designer = MetaDialog.OpenPlayersDialog("Select Source Player", (p, b) => { }, (selected) =>
                {
                    RPCEventInvoker.Paint(p, selected.GetModData().GetOutfitData(50));
                    designer.screen.Close();
                });
            }),
            new MSButton(2f, 0.4f, "Random Paint", TMPro.FontStyles.Bold, () =>
            {
                List<PlayerControl> candidiates = new List<PlayerControl>();
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player.PlayerId == p.PlayerId) continue;
                    if (player.Data.IsDead) continue;
                    if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    candidiates.Add(player);
                }
                PlayerControl selected = candidiates[NebulaPlugin.rnd.Next(candidiates.Count)];
                RPCEventInvoker.Paint(p, selected.GetModData().GetOutfitData(50));
            }),
            new MSButton(1.5f, 0.4f, "Reset", TMPro.FontStyles.Bold, () =>
            {
                RPCEventInvoker.Paint(p, p.GetModData().GetOutfitData(0));
            })
            );


        var designers = designer.SplitVertically(new float[] { 0.2f, 0.8f, 0.2f });

        var data = p.GetModData();

        int skip = page * 5;
        int num = 0;
        bool hasNext = false;

        foreach (var info in data.role.RelatedRoleDataInfo)
        {
            if (skip > 0) skip--;
            else if (num < 5)
            {
                AddRoleDataTopic(designer, p, data, info.id, info.display, info.min, info.max, info.suffix, info.replaceArray);
                designer.CustomUse(-0.1f);
                num++;
            }
            else hasNext = true;
        }
        Helpers.RoleAction(data, (r) =>
        {
            foreach (var info in r.RelatedExtraRoleDataInfo)
            {
                if (skip > 0) skip--;
                else if (num < 5)
                {
                    AddExtraRoleDataTopic(designer, p, data, info.role, info.display, info.min, info.max, info.suffix, info.replaceArray);
                    designer.CustomUse(-0.1f);
                    num++;
                }
                else hasNext = true;
            }
        });

        designers[0].CustomUse(0.62f * 2.2f - 0.51f);
        designers[2].CustomUse(0.62f * 2.2f - 0.51f);
        if (page != 0) designers[0].AddButton(new Vector2(0.9f, 0.9f), "Prev", "<<").OnClick.AddListener(
                (UnityEngine.Events.UnityAction)(() =>
                {
                    MetaDialog.EraseDialog(1);
                    OpenPlayerDialog(p, page - 1);
                })
            );
        if (hasNext) designers[2].AddButton(new Vector2(0.9f, 0.9f), "Next", ">>").OnClick.AddListener(
                  (UnityEngine.Events.UnityAction)(() =>
                  {
                      MetaDialog.EraseDialog(1);
                      OpenPlayerDialog(p, page + 1);
                  })
              );
    }

    private void OpenPlayersDialog()
    {
        var texts = new List<Tuple<Game.PlayerData, TMPro.TextMeshPro>>();
        var designer = MetaDialog.OpenPlayersDialog("Players", 0.7f, 0f, (p, button) =>
        {
            button.transform.GetChild(0).localPosition += new Vector3(0, 0.16f, 0f);

            TMPro.TextMeshPro text;
            text = MetaDialog.MSDesigner.AddSubText(button, 1.9f, 2f, "");
            text.transform.localPosition += new Vector3(-0.32f, -0.15f);
            text.fontStyle = TMPro.FontStyles.Bold;
            texts.Add(new Tuple<Game.PlayerData, TMPro.TextMeshPro>(p.GetModData(), text));

            PassiveButton b;

            b = MetaDialog.MSDesigner.AddSubButton(button, new Vector2(0.28f, 0.28f), "kill", "K");
            text = b.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
            text.fontStyle = TMPro.FontStyles.Bold;
            text.color = Palette.ImpostorRed;
            b.transform.localPosition += new Vector3(0.4f, -0.14f);
            b.OnClick.AddListener(((UnityEngine.Events.UnityAction)(() => MetaKillPlayer(p))));

            b = MetaDialog.MSDesigner.AddSubButton(button, new Vector2(0.28f, 0.28f), "exile", "E");
            text = b.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
            text.fontStyle = TMPro.FontStyles.Bold;
            text.color = Palette.White;
            b.transform.localPosition += new Vector3(0.7f, -0.14f);
            b.OnClick.AddListener(((UnityEngine.Events.UnityAction)(() => MetaExilePlayer(p))));

            b = MetaDialog.MSDesigner.AddSubButton(button, new Vector2(0.28f, 0.28f), "revive", "R");
            text = b.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
            text.fontStyle = TMPro.FontStyles.Bold;
            text.color = Palette.CrewmateBlue;
            b.transform.localPosition += new Vector3(1.0f, -0.14f);
            b.OnClick.AddListener(((UnityEngine.Events.UnityAction)(() => MetaRevivePlayer(p))));



        }, (p) => OpenPlayerDialog(p, 0));
        ((MetaDialog)designer.screen).updateFunc = (dialog) =>
        {
            foreach (var tuple in texts)
            {
                string roleNames;

                if (tuple.Item1.ShouldBeGhostRole)
                    roleNames = Helpers.cs(tuple.Item1.ghostRole.Color, Language.Language.GetString("role." + tuple.Item1.ghostRole.LocalizeName + ".name"));
                else
                    roleNames = Helpers.cs(tuple.Item1.role.Color, Language.Language.GetString("role." + tuple.Item1.role.LocalizeName + ".name"));
                Helpers.RoleAction(tuple.Item1, (role) => { role.EditDisplayRoleNameForcely(tuple.Item1.id, ref roleNames); });
                Helpers.RoleAction(tuple.Item1, (role) => { role.EditDisplayNameForcely(tuple.Item1.id, ref roleNames); });
                tuple.Item2.text = roleNames;
            }
        };
    }

    private void OpenGeneralDialog()
    {
        var dialog = MetaDialog.OpenDialog(new Vector2(8f, 5f), "General");

        AddModifySpeedTopic(dialog, null);

        dialog.AddTopic(
            new MSString(1f, "Paint:", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
            new MSButton(2f, 0.4f, "Random Paint", TMPro.FontStyles.Bold, () =>
            {
                List<PlayerControl> players = new List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    if (p.Data.IsDead) continue;
                    players.Add(p);
                }
                List<Game.PlayerData.PlayerOutfitData> allOutfits = new List<Game.PlayerData.PlayerOutfitData>();
                foreach (var p in players) allOutfits.Add(p.GetModData().GetOutfitData(50));
                var randomOutfits = allOutfits.ToArray().OrderBy(i => Guid.NewGuid()).ToArray();

                for (int i = 0; i < players.Count; i++)
                {
                    RPCEventInvoker.Paint(players[i], randomOutfits[i]);
                }
            }),
            new MSButton(1.5f, 0.4f, "Reset", TMPro.FontStyles.Bold, () =>
            {
                foreach (var p in PlayerControl.AllPlayerControls) RPCEventInvoker.Paint(p, p.GetModData().GetOutfitData(0));
            }));
        dialog.AddTopic(new MSButton(2.6f, 0.4f, "Emergency Meeting", TMPro.FontStyles.Bold, () =>
        {
            PlayerControl.LocalPlayer.CmdReportDeadBody(null);
            MetaDialog.EraseDialogAll();
        }));
        dialog.AddTopic(new MSButton(2f, 0.4f, Helpers.cs(Palette.ImpostorRed, "End Game"), TMPro.FontStyles.Bold, () =>
             {
                 var designer = MetaDialog.OpenDialog(new Vector2(8.5f, 5f), "End Reasons");

                 List<MSButton> ends = new List<MSButton>();

                 foreach (var er in Patches.EndCondition.AllEnds)
                 {

                     var end = er;
                     ends.Add(new MSButton(1.9f, 0.4f,
                         Helpers.cs(end.Color, Language.Language.GetString("game.endText." + end.Identifier)),
                         TMPro.FontStyles.Bold,
                         () =>
                         {
                             ShipStatus.Instance.enabled = false;
                             GameManager.Instance.RpcEndGame(end.Id, false);
                             dialog.screen.Close();
                         }));
                     if (ends.Count >= 4)
                     {
                         designer.AddTopic(ends.ToArray());
                         foreach (var e in ends)
                         {
                             e.text.fontSizeMin = 0.5f;
                             e.text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                         }
                         ends.Clear();
                     }
                 }

                 designer.AddTopic(ends.ToArray());
                 foreach (var e in ends)
                 {
                     e.text.fontSizeMin = 0.5f;
                     e.text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                 }
                 ends.Clear();
             }));
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (voidButton != null)
        {
            voidButton.Destroy();
        }
        voidButton = new CustomButton(
            () =>
            {
                float height = 1.8f;
                if (MeetingHud.Instance) height += 0.52f;
                var dialog = MetaDialog.OpenDialog(new Vector2(2f, height), "VOID");
                dialog.AddButton(1.6f, "General", "General").OnClick.AddListener((UnityEngine.Events.UnityAction)(() => OpenGeneralDialog()));
                dialog.AddButton(1.6f, "Players", "Players").OnClick.AddListener((UnityEngine.Events.UnityAction)(() => OpenPlayersDialog()));
                if (MeetingHud.Instance)
                {
                    dialog.AddButton(1.6f, "SkipMeeting", Helpers.cs(Palette.ImpostorRed, "Skip Meeting")).OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        MeetingHud.Instance.RpcVotingComplete(new Il2CppStructArray<MeetingHud.VoterState>(0), null, true);
                        MetaDialog.EraseDialog((MetaDialog)dialog.screen);
                    }));
                }
            },
            () => true,
            () => MetaDialog.dialogOrder.Count == 0,
            () => { },
            voidButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.void"
        ).SetTimer(0f);
    }

    public override void CleanUp()
    {
        if (voidButton != null)
        {
            voidButton.Destroy();
            voidButton = null;
        }

        MetaDialog.Initialize();
    }

    public override void PreloadOptionData()
    {
        foreach (var role in Roles.AllExtraAssignable)
            extraAssignableOptions.Add(role, null);
    }

    public VOID()
        : base("VOID", "void", RoleColor, RoleCategory.Neutral, Side.VOID, Side.VOID,
             new HashSet<Side>(), new HashSet<Side>(), new HashSet<Patches.EndCondition>(),
             true, VentPermission.CanNotUse, false, true, true)
    {

        Allocation = AllocationType.Switch;
        FixedRoleCount = true;

        IsGuessableRole = false;

        ValidGamemode = CustomGameMode.All;
    }
}
