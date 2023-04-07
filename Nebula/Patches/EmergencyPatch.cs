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
    static public bool isSpecialEmergency = false;
    public static int meetingsCount = 0, maxMeetingsCount = 15;

    static public float GetPenaltyVotingTime()
    {
        if (GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal)
        {
            float penalty = (Game.GameData.data.deadPlayers.Count * Game.GameData.data.GameRule.deathPenaltyForDiscussionTime);
            int total = GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.VotingTime) - (int)(Game.GameData.data.deadPlayers.Count * Game.GameData.data.GameRule.deathPenaltyForDiscussionTime);

            if (total > 10)
            {
                return penalty;
            }
            return (float)(GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.VotingTime) - 10);
        }
        else
        {
            return 0f;
        }
    }

    static public void Initialize()
    {
        occurredSabotage = false;
        occurredKill = false;
        occurredReport = false;
        isSpecialEmergency = false;
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

    private static bool isInSpecialEmergency = false;
    
    [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Begin))]
    class SpecialEmergencyMinigamePatch
    {
        static EmergencyMinigame? lastMinigame = null;
        static void CallMeeting(EmergencyMinigame __instance)
        {
            __instance.StatusText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EmergencyRequested, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
            if (Constants.ShouldPlaySfx())
            {
                SoundManager.Instance.PlaySound(__instance.ButtonSound, false, 1f, null);
            }
            PlayerControl.LocalPlayer.CmdReportDeadBody(null);
            __instance.ButtonActive = false;
            VibrationManager.Vibrate(1f, 1f, 0.2f, VibrationManager.VibrationFalloff.None, null, false);

            Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (r) => { r.OnCallSpecialMeeting(); });
        }

        static void Postfix(EmergencyMinigame __instance)
        {
            if (lastMinigame != null && lastMinigame.gameObject && lastMinigame.GetInstanceID() == __instance.GetInstanceID()) return;
            lastMinigame = __instance;

            isInSpecialEmergency = isSpecialEmergency;
            isSpecialEmergency = false;

            if (isInSpecialEmergency)
            {
                var onClickEvent = __instance.DefaultButtonSelected.GetComponent<ButtonBehavior>().OnClick;
                onClickEvent.RemoveAllListeners();
                onClickEvent.AddListener((UnityEngine.Events.UnityAction)(()=> { CallMeeting(__instance); }));
            }
        }
    }

    [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
    class EmergencyMinigameUpdatePatch
    {
        static void Postfix(EmergencyMinigame __instance)
        {
            var roleCanCallEmergency = true;
            var statusText = "";

            if (isInSpecialEmergency)
            {
                __instance.StatusText.text = "Special Emergency Button";
                __instance.NumberText.text = string.Empty;
                __instance.ClosedLid.gameObject.SetActive(false);
                __instance.OpenLid.gameObject.SetActive(true);
                __instance.ButtonActive = false;
                return;
            }

            if (!Helpers.RoleActionAll(PlayerControl.LocalPlayer.GetModData(),(r)=>r.CanCallEmergencyMeeting))
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
                __instance.NumberText.text = $"{localRemaining.ToString()} (Total: {teamRemaining.ToString()})";
                __instance.ButtonActive = localRemaining > 0 && teamRemaining > 0;
                __instance.ClosedLid.gameObject.SetActive(!__instance.ButtonActive);
                __instance.OpenLid.gameObject.SetActive(__instance.ButtonActive);
                return;
            }
        }
    }
}