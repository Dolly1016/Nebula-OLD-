using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Roles.ExtraRoles
{
    public class Lover : ExtraRole
    {
        private Module.CustomOption maxPairsOption;
        public Module.CustomOption loversModeOption;
        public Module.CustomOption chanceThatOneLoverIsImpostorOption;
        private Module.CustomOption canChangeTrilemmaOption;

        private PlayerControl trilemmaTarget=null;

        static public Color[] iconColor { get; } = new Color[] {
        (Color)new Color32(251, 3, 188, 255) ,
        (Color)new Color32(254, 132, 3, 255) ,
        (Color)new Color32(3, 254, 188, 255) ,
        (Color)new Color32(255, 255, 0, 255) ,
        (Color)new Color32(3, 183, 254, 255) };

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

        private void ActionForLover(PlayerControl player,System.Action<PlayerControl> action)
        {
            ulong myLoverId = player.GetModData().GetExtraRoleData(this);
            PlayerControl target;
            foreach (Game.PlayerData data in Game.GameData.data.players.Values)
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

        public override void OnExiledPre(byte[] voters) {
            ActionForMyLover((player) =>
            {
                //自身であれば特に何もしない
                if (player == PlayerControl.LocalPlayer) return;


                if (!player.Data.IsDead) RPCEventInvoker.UncheckedExilePlayer(player.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id);
            }
            );
        }

        public override void OnMurdered(byte murderId) {
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
            if (maxPairs * 2 > Game.GameData.data.players.Count) maxPairs = Game.GameData.data.players.Count / 2;

            int pairs = Helpers.CalcProbabilityCount(RoleChanceOption.getSelection(), maxPairs);

            List<PlayerControl> crewmates = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            List<PlayerControl> impostors = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            crewmates.RemoveAll(x => x.Data.Role.IsImpostor);
            impostors.RemoveAll(x => !x.Data.Role.IsImpostor);

            crewmates.RemoveAll((player) => { return !player.GetModData().role.CanBeLovers; });
            impostors.RemoveAll((player) => { return !player.GetModData().role.CanBeLovers; });

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

                    assignMap.Assign(crewmates[crewmateIndex[crewmateUsed]].PlayerId, this.id, (ulong)(i + 1));
                    assignMap.Assign(impostors[impostorIndex[impostorUsed]].PlayerId, this.id, (ulong)(i + 1));

                    crewmateUsed++;
                    impostorUsed++;
                }
                else
                {
                    //両方ともインポスターでない場合

                    //割り当てられない場合終了
                    if (crewmateUsed+1 >= crewmateIndex.Length) break;

                    for (int p = 0; p < 2; p++)
                    {
                        assignMap.Assign(crewmates[crewmateIndex[crewmateUsed]].PlayerId, this.id, (ulong)(i + 1));
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
                if (Game.GameData.data.players[playerId].GetExtraRoleData(this) == pairId) showFlag = true;
            }

            if (!showFlag && loversModeOption.getSelection()==1 && Roles.Avenger.canKnowExistenceOfAvengerOption.getBool())
            {
                int selection=Roles.Avenger.canKnowExistenceOfAvengerOption.getSelection();
                var myData= PlayerControl.LocalPlayer.GetModData();
                if (selection == 1 || (selection == 2 && myData.HasExtraRole(Roles.AvengerTarget) && myData.GetExtraRoleData(Roles.AvengerTarget.id) == Game.GameData.data.players[playerId].GetExtraRoleData(this)))
                {
                    PlayerControl player = Helpers.playerById(playerId);
                    if (player == null) return;

                    bool avengerFlag = false;
                    ActionForLover(player, (p) => { if (p != player && !p.Data.IsDead && player.Data.IsDead) avengerFlag = true; });

                    if (avengerFlag) { displayName += Helpers.cs(Roles.Avenger.Color, "♥"); return; }
                }
            }

            if (showFlag)EditDisplayNameForcely(playerId,ref displayName);
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    iconColor[Game.GameData.data.players[playerId].GetExtraRoleData(this) - 1], "♥");
        }

        public override void EditDescriptionString(ref string desctiption)
        {
            string partner="";
            ActionForMyLover((player)=> {
                partner =player.name;
            });
            partner = Helpers.cs(Color, partner);
            desctiption += "\n" + Language.Language.GetString("role.lover.description").Replace("%NAME%",partner);
        }

        public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
        {
            bool winFlag = false;
            ActionForLover(player, (partner) =>
            {
                if (player == partner) return;
                if (partner.Data.IsDead) return;
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
            if (winFlag)
            {
                EndGameManagerSetUpPatch.AddEndText(Language.Language.GetString("role.lover.additionalEndText"));
            }
            return winFlag;
        }

        private CustomButton involveButton;

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.InvolveButton.png", 115f);
            return buttonSprite;
        }

        public override void MyPlayerControlUpdate()
        {
            if (loversModeOption.getSelection() != 0) return;

            trilemmaTarget = Patches.PlayerControlPatch.SetMyTarget(1.5f);
            
            if (IsMyLover(trilemmaTarget)) {
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

            if (!canChangeTrilemmaOption.getBool() || loversModeOption.getSelection()!=0) return;

            involveButton = new CustomButton(
                () =>
                {
                    PlayerControl target = trilemmaTarget;

                    //巻き込まれるのがラバーズであった場合
                    if (target.GetModData().extraRole.Contains(this))
                    {
                        ulong removeId = target.GetModData().GetExtraRoleData(id);

                        foreach(Game.PlayerData data in Game.GameData.data.players.Values)
                        {
                            if (data.GetExtraRoleData(id) != removeId) continue;

                            //鞍替えする側はなにもしない
                            if (data.id == target.PlayerId) continue;

                            //ロール消去
                            RPCEventInvoker.UnsetExtraRole(Helpers.playerById(data.id),this);

                            break;
                        }
                    }

                    //巻き込まれるのがトリレマであった場合
                    if (target.GetModData().extraRole.Contains(Roles.Trilemma))
                    {
                        ulong removeId = target.GetModData().GetExtraRoleData(Roles.Trilemma.id);

                        foreach (Game.PlayerData data in Game.GameData.data.players.Values)
                        {
                            if (data.GetExtraRoleData(Roles.Trilemma.id) != removeId) continue;

                            //鞍替えする側はなにもしない
                            if (data.id == target.PlayerId) continue;

                            //ロール消去
                            RPCEventInvoker.UnsetExtraRole(Helpers.playerById(data.id), Roles.Trilemma);
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
                getButtonSprite(),
                new Vector3(0f, 1f, 0f),
                __instance,
                KeyCode.Z,
                true,
                "button.label.involve"
            );
            involveButton.MaxTimer = 0;

            trilemmaTarget = null;
        }

        public override void ButtonActivate()
        {
            if(involveButton!=null)
            involveButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            if(involveButton!=null)
            involveButton.setActive(false);
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

        public Lover() : base("Lover", "lover", iconColor[0],0)
        {
            FixedRoleCount = true;
        }
    }
}
