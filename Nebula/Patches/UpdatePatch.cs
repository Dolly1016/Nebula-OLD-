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
        static private Color rewriteImpostorColor(GameData.PlayerInfo Info, Color currentColor, Color impostorColor)
        {
            if (Info.Role.IsImpostor)
            {
                return impostorColor;
            }
            else
            {
                Game.PlayerData data = Game.GameData.data.players[Info.PlayerId];
                if (data.IsMyPlayerData())
                {
                    if (data.role.deceiveImpostorInNameDisplay)
                    {
                        return Palette.ImpostorRed;
                    }
                }

                if (data.role.deceiveImpostorInNameDisplay)
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

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!Game.GameData.data.players.ContainsKey(player.PlayerId))
                {
                    continue;
                }

                player.nameText.text = Game.GameData.data.players[player.PlayerId].currentName;
                player.nameText.color = Color.white;
                player.nameText.color = rewriteImpostorColor(player.Data, player.nameText.color, (Color)impostorColor);
            }

            if (MeetingHud.Instance != null)
            {
                foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                {
                    if (!Game.GameData.data.players.ContainsKey(player.TargetPlayerId))
                    {
                        continue;
                    }

                    player.NameText.color = Color.white;
                    player.NameText.color = rewriteImpostorColor(Helpers.allPlayersById()[player.TargetPlayerId].Data, player.NameText.color, (Color)impostorColor);

                }
            }

        }

        private static DeadBody GetDeadBody(byte playerId, DeadBody[] deadBodies)
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
            DeadBody[] deadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            DeadBody deadBody;
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

        public static void Postfix(HudManager __instance)
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;

            CustomButton.HudUpdate();
            
            //
            ResetNameTagsAndColors();

            //引きずられているプレイヤーの処理
            UpdateDraggedPlayer();

            Events.GlobalEvent.Update();


        }
    }
    
}
