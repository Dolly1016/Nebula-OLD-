using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles
{
    public class Lover : ExtraRole
    {
        private Module.CustomOption maxPairsOption;
        static private Color[] iconColor = new Color[] {
        (Color)new Color32(251, 3, 188, 255) ,
        (Color)new Color32(254, 132, 3, 255) ,
        (Color)new Color32(3, 254, 188, 255) ,
        (Color)new Color32(255, 255, 0, 255) ,
        (Color)new Color32(3, 183, 254, 255) };

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

        public Lover() : base("Lover", "lover", iconColor[0],0)
        {
            FixedRoleCount = true;
        }
    }
}
