namespace Nebula.Patches;

[HarmonyPatch]
public static class EmergencyPatch
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (__instance.GetModData().role != Roles.Roles.VOID) return true;

            //VOIDが強制的に会議を起こせるようにする
            if (AmongUsClient.Instance.IsGameOver || MeetingHud.Instance) return false;
            MeetingRoomManager.Instance.AssignSelf(__instance, target);
            if (!AmongUsClient.Instance.AmHost) return false;
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(__instance);
            __instance.RpcStartMeeting(target);
            return false;
        }
    }

    static bool occurredSabotage = false, occurredKill = false, occurredReport = false;
    public static int meetingsCount = 0, maxMeetingsCount = 15;

    static public float GetPenaltyVotingTime()
    {
        float penalty = (Game.GameData.data.deadPlayers.Count * Game.GameData.data.GameRule.deathPenaltyForDiscussionTime);
        int total = PlayerControl.GameOptions.VotingTime - (int)(Game.GameData.data.deadPlayers.Count * Game.GameData.data.GameRule.deathPenaltyForDiscussionTime);

        if (total > 10)
        {
            return penalty;
        }
        return (float)(PlayerControl.GameOptions.VotingTime - 10);
    }

    static public void Initialize()
    {
        occurredSabotage = false;
        occurredKill = false;
        occurredReport = false;
        meetingsCount = 0;
        maxMeetingsCount = Game.GameData.data.GameRule.maxMeetingsCount;
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
            if (!Game.GameData.data.GameRule.severeEmergencyLock)
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