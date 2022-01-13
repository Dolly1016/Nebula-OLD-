using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Hazel;

namespace Nebula.Patches
{
    [HarmonyPatch]
    class MeetingHudPatch
    {

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Awake))]
        class MeetingCalculateVotesPatch
        {
            static void Postfix(MeetingHud __instance)
            {
                Events.GlobalEvent.OnMeeting();
            }
        }
    }
}
