using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Roles.ExtraRoles
{
    public class AvengerTarget : ExtraRole
    {
        static public Color RoleColor = Palette.ImpostorRed;
        static public Color TargetColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);

        /* 矢印 */
        private Arrow Arrow;
        private float noticeInterval = 0f;
        private Vector2 noticePos = Vector2.zero;

        public override void MyPlayerControlUpdate()
        {
            if (!Roles.Avenger.murderCanKnowAvengerOption.getBool()) return;

            var myGData = Game.GameData.data.myData.getGlobalData();

            bool aliveFlag = false;
            foreach (var data in Game.GameData.data.players.Values)
            {
                if (data.GetExtraRoleData(Roles.Lover) == myGData.GetExtraRoleData(Roles.AvengerTarget))
                {
                    if (data.IsAlive)
                    {
                        aliveFlag = true;

                        if (Helpers.playerById(data.id) != null)
                        {
                            if (Arrow == null)
                            {
                                Arrow = new Arrow(TargetColor);
                                Arrow.arrow.SetActive(true);
                                noticeInterval = 0f;
                            }
                            noticeInterval -= Time.deltaTime;

                            if (noticeInterval < 0f)
                            {
                                noticePos = Helpers.playerById(data.id).transform.position;
                                noticeInterval = Roles.Avenger.murderNoticeIntervalOption.getFloat();
                            }

                            Arrow.Update(noticePos);
                        }
                        break;
                    }
                }

            }
            if (!aliveFlag && Arrow != null)
            {
                UnityEngine.Object.Destroy(Arrow.arrow);
                Arrow = null;
            }
        }

        public override void CleanUp()
        {
            if (Arrow != null)
            {
                UnityEngine.Object.Destroy(Arrow.arrow);
            }
        }

        /*
        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            ulong exData = Game.GameData.data.players[playerId].GetExtraRoleData(this);
            ulong myExData = PlayerControl.LocalPlayer.GetModData().GetExtraRoleData(Roles.Lover);

            if (exData == myExData) displayColor = TargetColor;
        }
        */

        public override void OnExiledPre(byte[] voters)
        {
            OnDied();
        }

        public override void OnDied()
        {
            PlayerControl avenger = null;
            foreach (var player in Helpers.allPlayersById().Values)
            {
                if (player.Data.IsDead) continue;
                if (player.GetModData().role != Roles.Avenger) continue;
                if (player.GetModData().GetExtraRoleData(Roles.Lover) != PlayerControl.LocalPlayer.GetModData().GetExtraRoleData(this)) continue;

                avenger = player;
                break;

            }

            if (avenger == null) return;

            byte murder = byte.MaxValue;
            if (Game.GameData.data.deadPlayers.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
            {
                murder = Game.GameData.data.deadPlayers[PlayerControl.LocalPlayer.PlayerId].MurderId;
            }

            if (murder == avenger.PlayerId)
            {
                //Avengerの目標を達成させる
                RPCEventInvoker.UpdateRoleData(avenger.PlayerId, Roles.Avenger.avengerCheckerId, 1);
            }
            else
            {
                //標的を失ったAvengerを自殺させる
                if (MeetingHud.Instance || ExileController.Instance)
                {
                    if (Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Guessed ||
                           Game.GameData.data.myData.getGlobalData().Status == Game.PlayerData.PlayerStatus.Misguessed)
                        RPCEventInvoker.CloseUpKill(avenger, avenger, Game.PlayerData.PlayerStatus.Suicide);
                    else
                        RPCEventInvoker.UncheckedExilePlayer(avenger.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id);
                }
                else
                {
                    RPCEventInvoker.UncheckedMurderPlayer(avenger.PlayerId, avenger.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id, false);
                }
            }
        }

        public AvengerTarget() : base("AvengerTarget", "avengerTarget", RoleColor, 0)
        {
            IsHideRole = true;

            Arrow = null;
        }
    }
}
