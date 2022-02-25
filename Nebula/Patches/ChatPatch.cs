using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{
    [HarmonyPatch]
    class ChatPatch
    {
        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
        public static class SetBubbleName
        {
            public static void Postfix(ChatBubble __instance, [HarmonyArgument(0)] string playerName)
            {
                //チャット欄でImpostor陣営から見たSpyがばれないように
                PlayerControl sourcePlayer = PlayerControl.AllPlayerControls.ToArray().ToList().FirstOrDefault(x => x.Data.PlayerName.Equals(playerName));
                if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    if (sourcePlayer.GetModData().role.DeceiveImpostorInNameDisplay)
                    {
                        __instance.NameText.color = Palette.ImpostorRed;
                    }
                }
            }
        }
    }
}
