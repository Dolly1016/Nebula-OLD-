using System;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Patches
{
    
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class UpdatePatch
    {
        private static SpriteRenderer FS_DeathGuage;

        static private void UpdateFullScreen()
        {
            if (!PlayerControl.LocalPlayer) return;
            if (PlayerControl.LocalPlayer.GetModData()==null) return;

            if (!FS_DeathGuage)
            {
                FS_DeathGuage = GameObject.Instantiate(HudManager.Instance.FullScreen,HudManager.Instance.transform);
                FS_DeathGuage.color = Palette.ImpostorRed.AlphaMultiplied(0f);
                FS_DeathGuage.enabled = true;
                FS_DeathGuage.gameObject.SetActive(true);
            }

            if (PlayerControl.LocalPlayer.Data.IsDead)
                FS_DeathGuage.color = Palette.ClearWhite;
            else if (FS_DeathGuage.color.a != PlayerControl.LocalPlayer.GetModData().DeathGuage*0.25f)
                FS_DeathGuage.color = Palette.ImpostorRed.AlphaMultiplied(PlayerControl.LocalPlayer.GetModData().DeathGuage*0.25f);
        }

        static private bool CannotSeeNameTag(PlayerControl player)
        {
            return
                (player.GetModData().Attribute.HasAttribute(Game.PlayerAttribute.Invisible) && player != PlayerControl.LocalPlayer && !Game.GameData.data.myData.CanSeeEveryoneInfo)
                || (player == PlayerControl.LocalPlayer && EyesightPatch.ObserverMode)
                || (player.GetModData().Property.UnderTheFloor);
        }

        static private bool IsInvisible(PlayerControl player)
        {
            return
                (player == PlayerControl.LocalPlayer && EyesightPatch.ObserverMode)
                || (player.GetModData().Property.UnderTheFloor)
                || (player.inVent)
                || (!PlayerControl.LocalPlayer.Data.IsDead && player.Data.IsDead);
        }

        static private Color rewriteImpostorColor(Game.PlayerData player, Color currentColor, Color impostorColor)
        {
            if (player.role.category==Roles.RoleCategory.Impostor)
            {
                return impostorColor;
            }
            else
            {
                if (player.IsMyPlayerData())
                {
                    if (player.role.DeceiveImpostorInNameDisplay)
                    {
                        return Palette.ImpostorRed;
                    }
                }

                if (player.role.DeceiveImpostorInNameDisplay)
                {
                    return impostorColor;
                }
            }
            return currentColor;
        }

        static void ResetNameTagsAndColors()
        {
            if (PlayerControl.LocalPlayer == null) return;
            if (Game.GameData.data == null) return;

            Color? impostorColor = null;
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                impostorColor = Palette.ImpostorRed;
            }
            else
            {
                impostorColor = Color.white;
            }

            string name;
            Game.PlayerData playerData;
            bool hideFlag;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!Game.GameData.data.players.ContainsKey(player.PlayerId))
                {
                    continue;
                }

                playerData = Game.GameData.data.players[player.PlayerId];

                /* 表示・非表示を設定する */

                player.Visible = !IsInvisible(player);

                if (player.MyPhysics?.GlowAnimator != null)
                {
                    player.MyPhysics.GlowAnimator.gameObject.SetActive(player.Visible && !ShipStatus.Instance);
                }
            


                /* 名前を編集する */
                name = "";
                hideFlag = playerData.currentName.Length == 0;

                Helpers.RoleAction(player, (role) => { role.EditDisplayName(player.PlayerId, ref name, hideFlag); });
                
                player.nameText.text = playerData.currentName+" " +name;
                if (player == PlayerControl.LocalPlayer)
                {
                    //自分自身ならロールの色にする
                    player.nameText.color = playerData.role.Color;
                }
                else
                {
                    player.nameText.color = Color.white;
                }
                player.nameText.color = rewriteImpostorColor(playerData, player.nameText.color, (Color)impostorColor);

                //ロールによる色の変更
                Color color = player.nameText.color;
                Helpers.RoleAction(player.PlayerId, (role) => { role.EditDisplayNameColor(player.PlayerId, ref color); });
                player.nameText.color = color;

                player.nameText.enabled = !CannotSeeNameTag(player);
            }

            if (MeetingHud.Instance != null)
            {
                foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                {
                    if (!Game.GameData.data.players.ContainsKey(player.TargetPlayerId))
                    {
                        continue;
                    }

                    playerData = Game.GameData.data.players[player.TargetPlayerId];

                    /* 名前を編集する */
                    name = "";
                    Helpers.RoleAction(player.TargetPlayerId, (role) => { role.EditDisplayName(player.TargetPlayerId, ref name, false); });
                    if (!name.Equals(""))
                        player.NameText.text = playerData.currentName + " " + name;
                    
                    if (player.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        //自分自身ならロールの色にする
                        player.NameText.color = playerData.role.Color;
                    }
                    else
                    {
                        player.NameText.color = Color.white;
                    }

                    player.NameText.color = rewriteImpostorColor(playerData, player.NameText.color, (Color)impostorColor);

                    //色の変更
                    Color color = player.NameText.color;
                    Helpers.RoleAction(player.TargetPlayerId, (role) => { role.EditDisplayNameColor(player.TargetPlayerId, ref color); });
                    player.NameText.color = color;

                }
            }

        }

        private static DeadBody? GetDeadBody(byte playerId, DeadBody[] deadBodies)
        {
            foreach (DeadBody player in deadBodies)
            {
                if (player.ParentId == playerId)
                {
                    return player;
                }
            }
            return null; 
        }

        public static void UpdateDraggedPlayer()
        {
            Game.PlayerData data;
            DeadBody[] deadBodies = Helpers.AllDeadBodies();
            DeadBody? deadBody;
            float distance;
            Vector3 targetPosition;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!Game.GameData.data.players.ContainsKey(player.PlayerId))
                {
                    continue;
                }
                data = Game.GameData.data.players[player.PlayerId];

                if (data.dragPlayerId==Byte.MaxValue)
                {
                    continue;
                }

                deadBody=GetDeadBody(data.dragPlayerId, deadBodies);

                if ((deadBody == null)||(!data.IsAlive))
                {
                    data.DropPlayer();
                }
                else
                {
                    if (player.inVent) {
                        deadBody.Reported = true;
                        deadBody.bodyRenderer.enabled = false;
                    }
                    else
                    {
                        deadBody.Reported = false;
                        deadBody.bodyRenderer.enabled = true;
                    }
                }

                targetPosition = player.transform.position + new Vector3(-0.1f, -0.1f);
                distance =player.transform.position.Distance(deadBody.transform.position);

                if (distance < 1.8f)
                {
                    deadBody.transform.position+=(targetPosition - deadBody.transform.position)*0.15f;
                }
                else
                {
                    deadBody.transform.position = targetPosition;
                }
            }
        }

        public static void UpdateImpostorKillButton(HudManager __instance)
        {


            if (MeetingHud.Instance != null) return;
            if (Game.GameData.data.myData.getGlobalData().role.side == Roles.Side.Impostor)
            {
                if (Game.GameData.data.myData.getGlobalData().role.HideKillButtonEvenImpostor || !Helpers.ShowButtons)
                {
                    __instance.KillButton.Hide();
                }
                else
                {
                    __instance.KillButton.Show();

                    PlayerControl target = PlayerControlPatch.SetMyTarget(!Roles.Roles.Spy.CanKillImpostor());
                    __instance.KillButton.SetTarget(target);
                    __instance.KillButton.enabled = target != null;
                }
            }
        }

        public static void Postfix(HudManager __instance)
        {
            try
            {
                if (AmongUsClient.Instance == null) return;
                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
                if (!Helpers.HasModData(PlayerControl.LocalPlayer.PlayerId)) return;


                /* スクリーンの更新 */
                UpdateFullScreen();

                /* サボタージュを確認 */
                if (Helpers.SabotageIsActive())
                {
                    EmergencyPatch.SabotageUpdate();
                }

                /* ボタン類の更新 */
                CustomButton.HudUpdate();

                Helpers.RoleAction(PlayerControl.LocalPlayer, (role) => { role.MyUpdate(); });
                if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor && PlayerControl.LocalPlayer.GetModData().role.VentPermission != Roles.VentPermission.CanNotUse)
                {
                    if (Input.GetKeyDown(KeyCode.V))
                        HudManagerStartPatch.Manager.ImpostorVentButton.DoClick();
                }

                //死後経過時間を更新
                foreach (Game.DeadPlayerData deadPlayer in Game.GameData.data.deadPlayers.Values)
                {
                    deadPlayer.Elapsed += Time.deltaTime;
                }

                //名前タグの更新
                ResetNameTagsAndColors();

                //引きずられているプレイヤーの処理
                UpdateDraggedPlayer();

                //マウス角度の調整
                Vector3 mouseDirection = Input.mousePosition - new Vector3(Screen.width / 2, Screen.height / 2);
                Game.GameData.data.myData.getGlobalData().MouseAngle = Mathf.Atan2(mouseDirection.y, mouseDirection.x);


                //インポスターのキルボタンのパッチ
                if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    UpdateImpostorKillButton(__instance);
                }

                if (PlayerControl.LocalPlayer.GetModData().role.VentPermission != Roles.VentPermission.CanNotUse)
                {
                    //ベントの色の設定
                    Color ventColor;
                    foreach (Vent vent in ShipStatus.Instance.AllVents)
                    {
                        ventColor = PlayerControl.LocalPlayer.GetModData().role.VentColor;
                        vent.myRend.material.SetColor("_OutlineColor", ventColor);

                        if (vent.myRend.material.GetColor("_AddColor").a > 0f)
                            vent.myRend.material.SetColor("_AddColor", ventColor);
                    }
                }


                Events.GlobalEvent.Update();
                Events.LocalEvent.Update();

                Game.GameData.data.ColliderManager.Update();

                Objects.Ghost.Update();

                Game.GameData.data.TimerUpdate();

                if (Game.GameData.data.Ghost != null) Game.GameData.data.Ghost.Update();

                PlayerControl.LocalPlayer.myTasks.RemoveAll((Il2CppSystem.Predicate<PlayerTask>)((c) =>
                {
                    return c.GetComponent<ImportantTextTask>() != null;
                }));
            }catch(NullReferenceException excep) { UnityEngine.Debug.Log(excep.StackTrace); }
        }
    }
    
}
