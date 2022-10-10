using Nebula.Objects;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Nebula.Module;
using System.Linq;

namespace Nebula.Roles.MetaRoles
{
    public class VOID : Role
    {
        static public Color RoleColor = new Color(173f / 255f, 173f / 255f, 198f / 255f);

        private Module.CustomOption killerCanKnowBaitKillByFlash;

        public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
        {
            initialTasks.Clear();
            actualTasks = null;
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            __instance.Die(DeathReason.Exile);
        }

        public override void Initialize(PlayerControl __instance)
        {
            Game.GameData.data.myData.CanSeeEveryoneInfo = true;
            FastDestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);
        }


        public override void LoadOptionData()
        {

        }

        
        private SpriteLoader voidButtonSprite = new SpriteLoader("Nebula.Resources.VOIDButton.png", 115f);
        private CustomButton voidButton;

       

        public override void MyUpdate()
        {
            if (MetaDialog.dialogOrder.Count == 0 && Input.GetKeyDown(KeyCode.F)) voidButton.onClickEvent();
        }

        private void MetaChangeRole(PlayerControl p)
        {
            Module.MetaDialog.MetaDialogDesigner? dialog = null;
            dialog = Module.MetaDialog.OpenRolesDialog((r) => r.category != RoleCategory.Complex, 0, 60, (r) =>
            {
                RPCEventInvoker.ImmediatelyChangeRole(p, r);
                MetaDialog.EraseDialog(2);
                OpenPlayerDialog(p);
            });
        }

        private void MetaEditModify(PlayerControl p)
        {
            var data = p.GetModData();
            var dialog = Module.MetaDialog.OpenDialog(new Vector2(9f,5f),"Modifies");
            dialog.AddTopic(new MetaDialogString(2f,"Activated",TMPro.TextAlignmentOptions.Center,TMPro.FontStyles.Bold));
            dialog.AddModifyTopic((r) => data.HasExtraRole(r), (r) =>
            {
                RPCEventInvoker.ImmediatelyUnsetExtraRole(p,r);
                MetaDialog.EraseDialog(2);
                OpenPlayerDialog(p);
                MetaEditModify(p);
            });
            dialog.AddTopic(new MetaDialogString(2f, "Unactivated", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));
            dialog.AddModifyTopic((r) => !data.HasExtraRole(r), (r) =>
            {
                RPCEventInvoker.SetExtraRole(p, r, 0);
                MetaDialog.EraseDialog(2);
                OpenPlayerDialog(p);
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

        private void AddModifySpeedTopic(MetaDialog.MetaDialogDesigner designer,PlayerControl? p)
        {
            float speed = 1f;
            float duration = 10f;
            MetaDialogString SpeedText = new MetaDialogString(1.2f, "1x", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
            MetaDialogButton SpeedDownButton = new MetaDialogButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () => { if (speed > -2f) speed -= 0.125f; SpeedText.text.text = speed.ToString() + "x"; });
            MetaDialogButton SpeedUpButton = new MetaDialogButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () => { if (speed < 3f) speed += 0.125f; SpeedText.text.text = speed.ToString() + "x"; });

            MetaDialogButton UnlimitedSpeedButton = new MetaDialogButton(0.8f, 0.5f, "Apply", TMPro.FontStyles.Bold, () => { 
                if(p!=null)RPCEventInvoker.EmitSpeedFactor(p, new Game.SpeedFactor(0, 99999f, speed, true)); else
                {
                    foreach(var player in PlayerControl.AllPlayerControls) RPCEventInvoker.EmitSpeedFactor(player, new Game.SpeedFactor(0, 99999f, speed, true));
                }
            });

            MetaDialogString DurationText = new MetaDialogString(0.8f, "10s", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
            MetaDialogButton DurationDownButton = new MetaDialogButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () => { if (duration > 2.5f) duration -= 2.5f; DurationText.text.text = duration.ToString() + "s"; });
            MetaDialogButton DurationUpButton = new MetaDialogButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () => { if (duration < 60f) duration += 2.5f; DurationText.text.text = duration.ToString() + "s"; });

            MetaDialogButton LimitedSpeedButton = new MetaDialogButton(0.8f, 0.5f, "Apply", TMPro.FontStyles.Bold, () => {
                if (p != null) RPCEventInvoker.EmitSpeedFactor(p, new Game.SpeedFactor(0, duration, speed, true));
                else
                {
                    foreach (var player in PlayerControl.AllPlayerControls) RPCEventInvoker.EmitSpeedFactor(player, new Game.SpeedFactor(0, duration, speed, true));
                }
            });

            designer.AddTopic(
                new MetaDialogString(1.2f, "Speed:", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold), SpeedDownButton, SpeedText, SpeedUpButton, UnlimitedSpeedButton,
                new MetaDialogString(0.6f, "with", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold), DurationDownButton, DurationText, DurationUpButton, LimitedSpeedButton);

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

        private void AddRoleDataTopic(MetaDialog.MetaDialogDesigner designer, PlayerControl player, Game.PlayerData data, int id, string display, int min, int max, string suffix, string[]? replace)
        {
            if(replace==null)
                designer.AddNumericDataTopic(display, data.GetRoleData(id), suffix, min, max, (v) => RPCEventInvoker.UpdateRoleData(player.PlayerId, id, v));
            else
                designer.AddNumericDataTopic(display, data.GetRoleData(id), replace, min, max, (v) => RPCEventInvoker.UpdateRoleData(player.PlayerId, id, v));
        }

        private void AddExtraRoleDataTopic(MetaDialog.MetaDialogDesigner designer, PlayerControl player, Game.PlayerData data, ExtraRole role, string display, int min, int max, string suffix, string[]? replace)
        {
            if (replace == null)
                designer.AddNumericDataTopic(display, (int)data.GetExtraRoleData(role.id), suffix, min, max, (v) => RPCEventInvoker.UpdateExtraRoleData(player.PlayerId, role.id, (ulong)v));
            else
                designer.AddNumericDataTopic(display, (int)data.GetExtraRoleData(role.id), replace, min, max, (v) => RPCEventInvoker.UpdateExtraRoleData(player.PlayerId, role.id, (ulong)v));
        }

        private void OpenPlayerDialog(PlayerControl p)
        {
            var designer = MetaDialog.OpenPlayerDialog(new Vector2(8f, 5f), p);

            MetaDialogButton roleButton = new MetaDialogButton(1f, 0.4f, "Role", TMPro.FontStyles.Bold, () => MetaChangeRole(p));
            MetaDialogButton modifyButton = new MetaDialogButton(1f, 0.4f, "Modify", TMPro.FontStyles.Bold, () => MetaEditModify(p));
            MetaDialogButton killButton = new MetaDialogButton(1f, 0.4f, "Kill", TMPro.FontStyles.Bold, () => MetaKillPlayer(p));
            MetaDialogButton exileButton = new MetaDialogButton(1f, 0.4f, "Exile", TMPro.FontStyles.Bold, () => MetaExilePlayer(p));
            MetaDialogButton reviveButton = new MetaDialogButton(1f, 0.4f, "Revive", TMPro.FontStyles.Bold, () => MetaRevivePlayer(p));

            designer.AddTopic(roleButton, modifyButton,killButton, exileButton, reviveButton);

            AddModifySpeedTopic(designer,p);

            designer.AddTopic(
                new MetaDialogString(1f, "Paint:", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                new MetaDialogButton(1.5f, 0.4f, "Paint", TMPro.FontStyles.Bold, () => {
                    MetaDialog.MetaDialogDesigner? designer = null;
                    designer = MetaDialog.OpenPlayersDialog("Select Source Player", (p, b) => { }, (selected) =>
                    {
                        RPCEventInvoker.Paint(p, selected.GetModData().GetOutfitData(50));
                        designer.dialog.Close();
                    });
                }),
                new MetaDialogButton(2f, 0.4f, "Random Paint", TMPro.FontStyles.Bold, () => {
                    List<PlayerControl> candidiates = new List<PlayerControl>();
                    foreach(var player in PlayerControl.AllPlayerControls)
                    {
                        if (player.PlayerId == p.PlayerId) continue;
                        if (player.Data.IsDead) continue;
                        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                        candidiates.Add(player);
                    }
                    PlayerControl selected = candidiates[NebulaPlugin.rnd.Next(candidiates.Count)];
                    RPCEventInvoker.Paint(p, selected.GetModData().GetOutfitData(50));
                }),
                new MetaDialogButton(1.5f, 0.4f, "Reset", TMPro.FontStyles.Bold, () => {
                    RPCEventInvoker.Paint(p, p.GetModData().GetOutfitData(0));
                })
                );

            var data = p.GetModData();

            foreach (var info in data.role.RelatedRoleDataInfo)
            {
                AddRoleDataTopic(designer, p, data, info.id, info.display, info.min, info.max, info.suffix, info.replaceArray);
                designer.CustomUse(-0.2f);
            }
            Helpers.RoleAction(data, (r) =>
            {
                foreach (var info in r.RelatedExtraRoleDataInfo)
                {
                    AddExtraRoleDataTopic(designer, p, data, info.role, info.display, info.min, info.max, info.suffix, info.replaceArray);
                    designer.CustomUse(-0.2f);
                }
            });
            
        }

        private void OpenPlayersDialog()
        {
            var texts = new List<Tuple<Game.PlayerData, TMPro.TextMeshPro>>();
            var designer = MetaDialog.OpenPlayersDialog("Players", 0.7f, 0f, (p, button) => {
                button.transform.GetChild(0).localPosition += new Vector3(0, 0.16f, 0f);

                TMPro.TextMeshPro text;
                text = MetaDialog.MetaDialogDesigner.AddSubText(button, 2f, 2f, "");
                text.transform.localPosition += new Vector3(-0.32f, -0.15f);
                text.fontStyle = TMPro.FontStyles.Bold;
                texts.Add(new Tuple<Game.PlayerData, TMPro.TextMeshPro>(p.GetModData(), text));

                PassiveButton b;

                b = MetaDialog.MetaDialogDesigner.AddSubButton(button, new Vector2(0.28f, 0.28f), "kill", "K");
                text = b.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
                text.fontStyle = TMPro.FontStyles.Bold;
                text.color = Palette.ImpostorRed;
                b.transform.localPosition += new Vector3(0.4f, -0.14f);
                b.OnClick.AddListener(((UnityEngine.Events.UnityAction)(() => MetaKillPlayer(p))));

                b = MetaDialog.MetaDialogDesigner.AddSubButton(button, new Vector2(0.28f, 0.28f), "exile", "E");
                text = b.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
                text.fontStyle = TMPro.FontStyles.Bold;
                text.color = Palette.White;
                b.transform.localPosition += new Vector3(0.7f, -0.14f);
                b.OnClick.AddListener(((UnityEngine.Events.UnityAction)(() => MetaExilePlayer(p))));

                b = MetaDialog.MetaDialogDesigner.AddSubButton(button, new Vector2(0.28f, 0.28f), "revive", "R");
                text = b.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
                text.fontStyle = TMPro.FontStyles.Bold;
                text.color = Palette.CrewmateBlue;
                b.transform.localPosition += new Vector3(1.0f, -0.14f);
                b.OnClick.AddListener(((UnityEngine.Events.UnityAction)(() => MetaRevivePlayer(p))));



            }, (p) => OpenPlayerDialog(p));
            designer.dialog.updateFunc = (dialog) =>
            {
                foreach (var tuple in texts)
                {
                    string roleNames = Helpers.cs(tuple.Item1.role.Color, Language.Language.GetString("role." + tuple.Item1.role.LocalizeName + ".name"));
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
                new MetaDialogString(1f, "Paint:", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                new MetaDialogButton(2f, 0.4f, "Random Paint", TMPro.FontStyles.Bold, () =>
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
                new MetaDialogButton(1.5f, 0.4f, "Reset", TMPro.FontStyles.Bold, () =>
                {
                    foreach (var p in PlayerControl.AllPlayerControls)RPCEventInvoker.Paint(p, p.GetModData().GetOutfitData(0));
                }));
            dialog.AddTopic(new MetaDialogButton(2.6f, 0.4f, "Emergency Meeting", TMPro.FontStyles.Bold, () => {
                PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                dialog.dialog.Close();
            }));
            dialog.AddTopic(new MetaDialogButton(2f, 0.4f, Helpers.cs(Palette.ImpostorRed, "End Game"), TMPro.FontStyles.Bold, () =>
                 {
                     var designer = MetaDialog.OpenDialog(new Vector2(8.5f,5f),"End Reasons");

                     List<MetaDialogButton> ends = new List<MetaDialogButton>();

                     foreach (var er in Patches.EndCondition.AllEnds)
                     {

                         var end = er;
                         ends.Add(new MetaDialogButton(1.9f, 0.4f,
                             Helpers.cs(end.Color, Language.Language.GetString("game.endText." + end.Identifier)),
                             TMPro.FontStyles.Bold,
                             () =>
                             {
                                 ShipStatus.Instance.enabled = false;
                                 ShipStatus.RpcEndGame(end.Id, false);
                                 dialog.dialog.Close();
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
                    var dialog = MetaDialog.OpenDialog(new Vector2(2f, 1.8f), "VOID");
                    dialog.AddButton(1.6f, "General", "General").OnClick.AddListener((UnityEngine.Events.UnityAction)(()=> OpenGeneralDialog()));
                    dialog.AddButton(1.6f, "Players", "Players").OnClick.AddListener((UnityEngine.Events.UnityAction)(() => OpenPlayersDialog()));
                },
                () => true,
                () => MetaDialog.dialogOrder.Count == 0,
                () => { },
                voidButtonSprite.GetSprite(),
                new Vector3(-1.8f, 0, 0),
                __instance,
                KeyCode.F,
                false,
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

        public VOID()
            : base("VOID", "void", RoleColor, RoleCategory.Neutral, Side.VOID, Side.VOID,
                 new HashSet<Side>(), new HashSet<Side>(), new HashSet<Patches.EndCondition>(),
                 true, VentPermission.CanNotUse, false, false, false)
        {
            DefaultCanBeLovers = false;
            DefaultCanBeDrunk = false;
            DefaultCanBeGuesser = false;
            DefaultCanBeMadmate = false;
            DefaultCanBeSecret = false;

            Allocation = AllocationType.Switch;
            FixedRoleCount = true;

            IsGuessableRole = false;
        }
    }
}
