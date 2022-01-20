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
    public static class EmergencyPatch
    {
        static bool occurredSabotage = false, occurredKill = false, occurredReport = false;
        static int meetingsCount = 0, maxMeetingsCount = 15;

        static public int GetVotingTime(int defaultTime)
        {
            int time = defaultTime-(int)(Game.GameData.data.deadPlayers.Count * Game.GameData.data.GameRule.deathPenaltyForDiscussionTime);
            if (time > 10)
            {
                return time;
            }
            return 10;
        }

        static public void Initialize()
        {
            occurredSabotage = false;
            occurredKill = false;
            occurredReport = false;
            meetingsCount = 0;
            maxMeetingsCount = Game.GameData.data.GameRule.maxMeetingsCount;

            //短縮させた会議時間を元に戻す
            PlayerControl.GameOptions.VotingTime = Game.GameData.data.GameRule.vanillaVotingTime;
        }


        public static void SabotageUpdate()
        {
            occurredSabotage = true;
        }

        public static void KillUpdate()
        {
            occurredKill = true;
        }

       
        public static void MeetingUpdate()
        {
            occurredReport = true;
        }
        

        [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
        class EmergencyMinigameUpdatePatch
        {
            static void Postfix(EmergencyMinigame __instance)
            {
                NebulaPlugin.Instance.Logger.Print("2");
                var roleCanCallEmergency = true;
                var statusText = "";

                // Deactivate emergency button for Swapper
                if (!PlayerControl.LocalPlayer.GetModData().role.CanCallEmergencyMeeting)
                {
                    __instance.StatusText.text = "You can't start an emergency meeting due to your role.";
                    __instance.NumberText.text = string.Empty;
                    __instance.ClosedLid.gameObject.SetActive(true);
                    __instance.OpenLid.gameObject.SetActive(false);
                    __instance.ButtonActive = false;
                    return;
                }

                NebulaPlugin.Instance.Logger.Print("3");

                int score = 0, require = 0;
                if (Game.GameData.data.GameRule.canUseEmergencyWithoutDeath)
                {
                    score += EmergencyPatch.occurredKill ? 1 : 0;
                    require++;
                }
                if (Game.GameData.data.GameRule.canUseEmergencyWithoutReport)
                {
                    score += EmergencyPatch.occurredReport ? 1 : 0;
                    require++;
                }
                if (Game.GameData.data.GameRule.canUseEmergencyWithoutSabotage)
                {
                    score += EmergencyPatch.occurredSabotage ? 1 : 0;
                    require++;
                }
                if (!Game.GameData.data.GameRule.moreStrongEmergencyLock)
                {
                    if (require > 0) require = 1;
                }
                if (score < require)
                {
                    __instance.StatusText.text = "Emergency button has been locked.";
                    __instance.NumberText.text = string.Empty;
                    __instance.ClosedLid.gameObject.SetActive(true);
                    __instance.OpenLid.gameObject.SetActive(false);
                    __instance.ButtonActive = false;
                    return;
                }

                NebulaPlugin.Instance.Logger.Print("4");

                // Handle max number of meetings
                if (__instance.state == 1)
                {
                    int localRemaining = PlayerControl.LocalPlayer.RemainingEmergencies;
                    int teamRemaining = Mathf.Max(0, EmergencyPatch.maxMeetingsCount - EmergencyPatch.meetingsCount);
                    int remaining = teamRemaining;
                    __instance.NumberText.text = $"{localRemaining.ToString()} and the ship has {teamRemaining.ToString()}";
                    __instance.ButtonActive = remaining > 0;
                    __instance.ClosedLid.gameObject.SetActive(!__instance.ButtonActive);
                    __instance.OpenLid.gameObject.SetActive(__instance.ButtonActive);
                    return;
                }
            }
        }
    }
}
