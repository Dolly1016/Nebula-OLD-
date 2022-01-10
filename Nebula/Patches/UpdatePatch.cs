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
        static void resetNameTagsAndColors()
        {
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                player.nameText.color = Palette.ImpostorRed;
            }
            if (MeetingHud.Instance != null)
            {
                foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                {
                    player.NameText.color = Palette.ImpostorRed;
                }
            }

        }

        public static void Postfix(HudManager __instance)
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;

            CustomButton.HudUpdate();
            //resetNameTagsAndColors();

            Events.GlobalEvent.Update();

        }
    }
    
}
