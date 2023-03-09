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

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.Refresh))]
class AbilityButtonRefreshPatch
{
    static bool Prefix(AbilityButton __instance)
    {
        __instance.gameObject.SetActive(false);
        return false;
    }
}

[HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.InitializeAbilityButton))]
class BlockInitializeAbilityButtonPatch
{
    static bool Prefix(RoleBehaviour __instance)
    {
        HudManager.Instance.AbilityButton.gameObject.SetActive(false);
        return false;
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

//コミュを直せない役職からミニゲームをブロックする(MIRA)
[HarmonyPatch(typeof(AuthGame), nameof(AuthGame.Begin))]
class AuthGameBeginPatch
{
    static void Postfix(AuthGame __instance)
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

//リアクターを直せない役職からミニゲームをブロックする
[HarmonyPatch(typeof(ReactorMinigame), nameof(ReactorMinigame.Begin))]
class ReactorMinigameBeginPatch
{
    static void Postfix(ReactorMinigame __instance)
    {
        bool cannotFixSabotage = false;
        Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { cannotFixSabotage |= !role.CanFixEmergencySabotage; });
        if (cannotFixSabotage) __instance.Close();
    }
}

//ヘリサボを直せない役職からミニゲームをブロックする
[HarmonyPatch(typeof(AirshipAuthGame), nameof(AirshipAuthGame.Begin))]
class AirshipAuthGameBeginPatch
{
    static void Postfix(AirshipAuthGame __instance)
    {
        bool cannotFixSabotage = false;
        Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { cannotFixSabotage |= !role.CanFixEmergencySabotage; });
        if (cannotFixSabotage) __instance.Close();
    }
}

//O2を直せない役職からミニゲームをブロックする
[HarmonyPatch(typeof(KeypadGame), nameof(KeypadGame.Begin))]
class KeypadGameBeginPatch
{
    static void Postfix(KeypadGame __instance)
    {
        bool cannotFixSabotage = false;
        Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { cannotFixSabotage |= !role.CanFixEmergencySabotage; });
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


[HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.Start))]
class SystemConsoleStartPatch
{
    static bool Prefix(SystemConsole __instance)
    {
        if (__instance.FreeplayOnly && Game.GameData.data.GameMode!=Module.CustomGameMode.FreePlay)
            UnityEngine.Object.Destroy(__instance.gameObject);
        
        return false;
    }
}