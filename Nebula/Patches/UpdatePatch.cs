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

        static void resetNameTagsAndColors()
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

        public static void Postfix(HudManager __instance)
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;

            CustomButton.HudUpdate();
            
            resetNameTagsAndColors();

            Events.GlobalEvent.Update();

        }
    }
    
}
