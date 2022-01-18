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

                    //自身であれば特に何もしない
                    if (target == PlayerControl.LocalPlayer) continue;

                    //指定の方法で自殺する
                    action.Invoke(target);
                }
            }
        }

        private void ActionForMyLover(System.Action<PlayerControl> action)
        {
            ActionForLover(PlayerControl.LocalPlayer,action);
        }

        public override void OnExiledPre(byte[] voters) {
            ActionForMyLover((player) =>
            {
                if(!player.Data.IsDead)RPCEventInvoker.UncheckedExilePlayer(player.PlayerId);
            }
            );
        }

        public override void OnMurdered(byte murderId) {
            ActionForMyLover((player) =>
            {
                if (!player.Data.IsDead) RPCEventInvoker.UncheckedMurderPlayer(player.PlayerId, player.PlayerId, false);
            }
            );
        }

        public override void Assignment(Game.GameData gameData)
        {
            int maxPairs = maxPairsOption.getSelection();
            if (maxPairs * 2 > gameData.players.Count) maxPairs = gameData.players.Count / 2;

            int pairs = Helpers.CalcProbabilityCount(roleChanceOption.getSelection(), maxPairs);

            byte[] playerArray = Helpers.GetRandomArray(gameData.players.Keys);

            for (int i = 0; i < pairs; i++)
            {
                for (int p = 0; p < 2; p++) {
                    RPCEventInvoker.SetExtraRole(Helpers.playerById(playerArray[i * 2 + p]), this, (ulong)(i+1));
                }
            }
        }

        public override void LoadOptionData()
        {
            maxPairsOption = CreateOption(Color.white, "maxPairs", 1f, 0f, 5f, 1f);
            canChangeTrilemmaOption = CreateOption(Color.white, "canChangeTrilemma", true);
        }

        public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
        {
            bool showFlag = false;
            if (PlayerControl.LocalPlayer.Data.IsDead) showFlag = true;
            else if (Game.GameData.data.myData.getGlobalData().extraRole.Contains(this))
            {
                ulong pairId = Game.GameData.data.myData.getGlobalData().GetExtraRoleData(this);
                if (Game.GameData.data.players[playerId].GetExtraRoleData(this) == pairId) showFlag = true;
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
                partner=player.name;
            });
            partner = Helpers.cs(color, partner);
            desctiption += "\n" + Language.Language.GetString("role.lover.description").Replace("%NAME%",partner);
        }

        public override bool CheckWin(PlayerControl player, EndCondition condition)
        {
            if (player.Data.IsDead) return false;

            bool winFlag = false;
            ActionForLover(player, (partner) =>
            {
                winFlag |= partner.GetModData().role.CheckWin(condition);
            });
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
            trilemmaTarget = Patches.PlayerControlPatch.SetMyTarget();
            
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

            if (!canChangeTrilemmaOption.getBool()) return;

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
                new Vector3(0f, -0.06f, 0),
                __instance,
                KeyCode.Z,
                true
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

        public override void ButtonCleanUp()
        {
            if (involveButton != null)
            {
                involveButton.Destroy();
                involveButton = null;
            }
        }

        public Lover() : base("Lover", "lover", iconColor[0],0)
        {
            FixedRoleCount = true;
        }
    }
}
