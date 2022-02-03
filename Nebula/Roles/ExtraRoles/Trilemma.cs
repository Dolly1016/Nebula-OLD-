using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles
{
    public class Trilemma : ExtraRole
    {
        private void ActionForLover(PlayerControl player, System.Action<PlayerControl> action)
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
            ActionForLover(PlayerControl.LocalPlayer, action);
        }

        public override void OnExiledPre(byte[] voters)
        {
            ActionForMyLover((player) =>
            {
                if (!player.Data.IsDead) RPCEventInvoker.UncheckedExilePlayer(player.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id);
            }
            );
        }

        public override void OnMurdered(byte murderId)
        {
            ActionForMyLover((player) =>
            {
                if (!player.Data.IsDead) RPCEventInvoker.UncheckedMurderPlayer(player.PlayerId, player.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id, false);
            }
            );
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

            if (showFlag) EditDisplayNameForcely(playerId, ref displayName);
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    Lover.iconColor[Game.GameData.data.players[playerId].GetExtraRoleData(this) - 1], "♠");
        }

        public override void EditDescriptionString(ref string desctiption)
        {
            string partner = "";
            ActionForMyLover((player) => {
                partner = player.name;
            });
            partner = Helpers.cs(Color, partner);
            desctiption += "\n" + Language.Language.GetString("role.lover.description").Replace("%NAME%", partner);
        }

        public override bool CheckWin(PlayerControl player, EndCondition condition)
        {
            if (player.Data.IsDead) return false;

            return condition == EndCondition.TrilemmaWin;
        }

        public Trilemma() : base("Trilemma", "trilemma", Lover.iconColor[0], 0)
        {
            IsHideRole = true;
        }
    }
}
