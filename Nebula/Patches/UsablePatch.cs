namespace Nebula.Patches;

[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
class KillButtonDoClickPatch
{
    public static bool Prefix(KillButton __instance)
    {
        if (__instance.isActiveAndEnabled && __instance.currentTarget && !__instance.isCoolingDown && !PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.CanMove)
        {
            Helpers.MurderAttemptResult res = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, __instance.currentTarget, Game.PlayerData.PlayerStatus.Dead);
            if (res != Helpers.MurderAttemptResult.BlankKill)
            {
                PlayerControl.LocalPlayer.killTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
            }
            __instance.SetTarget(null);
        }
        return false;
    }
}

[HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.NextStep))]
class TaskCompletePatch
{
    static void Prefix(NormalPlayerTask __instance)
    {
        if (__instance.MaxStep - 1 == __instance.TaskStep)
            if (__instance.Owner.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                RPCEventInvoker.CompleteTask(__instance.Owner.PlayerId);
    }
}

[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.Refresh))]
class SabotageButtonRefreshPatch
{
    static void Postfix(SabotageButton __instance)
    {
        if (!HudManager.InstanceExists) return;
        if (Game.GameData.data == null) return;
        if (Game.GameData.data.myData.getGlobalData() == null) return;

        if (!Game.GameData.data.myData.getGlobalData().role.CanInvokeSabotage)
        {
            __instance.SetDisabled();
        }
    }
}

[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
public static class SabotageButtonDoClickPatch
{
    public static bool Prefix(SabotageButton __instance)
    {
        //インポスターなら特段何もしない
        if (PlayerControl.LocalPlayer.Data.Role.TeamType == RoleTeamTypes.Impostor) return true;

        HudManager.Instance.ToggleMapVisible(new MapOptions()
        {
            Mode = MapOptions.Modes.Sabotage,
            AllowMovementWhileMapOpen = true
        });
        return false;
    }
}

//コミュを直せない役職からミニゲームをブロックする
[HarmonyPatch(typeof(TuneRadioMinigame), nameof(TuneRadioMinigame.Begin))]
class CommsMinigameBeginPatch
{
    static void Postfix(TuneRadioMinigame __instance)
    {
        bool cannotFixSabotage = false;
        Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { cannotFixSabotage |= !role.CanFixSabotage; });
        if (cannotFixSabotage) __instance.Close();
    }
}

//停電を直せない役職からミニゲームをブロックする
[HarmonyPatch(typeof(SwitchMinigame), nameof(SwitchMinigame.Begin))]
class LightsMinigameBeginPatch
{
    static void Postfix(SwitchMinigame __instance)
    {
        bool cannotFixSabotage = false;
        Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { cannotFixSabotage |= !role.CanFixSabotage; });
        if (cannotFixSabotage) __instance.Close();
    }
}

//ぬ～ん使用不可能
[HarmonyPatch(typeof(MovingPlatformBehaviour), nameof(MovingPlatformBehaviour.MeetingCalled))]
class MovingPlatformBehaviourMeetingCalledPatch
{
    static bool Prefix(MovingPlatformBehaviour __instance)
    {
        return !(CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.oneWayMeetingRoomOption.getBool());
    }
}

[HarmonyPatch(typeof(MovingPlatformBehaviour), nameof(MovingPlatformBehaviour.InUse),MethodType.Getter)]
class CanUseMovingPlayformPatch
{
    static bool Prefix(MovingPlatformBehaviour __instance,bool __result)
    {
        if (CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.oneWayMeetingRoomOption.getBool())
        {
            __result = true;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(MovingPlatformBehaviour), nameof(MovingPlatformBehaviour.SetSide))]
class MovingPlatformBehaviourSetSidePatch
{
    static bool Prefix(MovingPlatformBehaviour __instance)
    {
        if (GameOptionsManager.Instance.currentGameMode == GameModes.HideNSeek) return true;

        __instance.IsDirty = true;
        return !(CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.oneWayMeetingRoomOption.getBool());
    }
}