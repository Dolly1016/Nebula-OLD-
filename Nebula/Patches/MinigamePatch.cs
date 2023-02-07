namespace Nebula.Patches;

[HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
public static class ConsoleCanUsePatch
{
    public static bool Prefix(ref float __result, Console __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        canUse = couldUse = false;

        if (Game.GameData.data.myData.getGlobalData().Property.UnderTheFloor) { __result = float.MaxValue; return false; }

        if (!Game.GameData.data.myData.getGlobalData().role.CanFixSabotage)
            if (__instance.TaskTypes.Any(x => x == TaskTypes.FixLights || x == TaskTypes.FixComms)) return false;
        if (!Game.GameData.data.myData.getGlobalData().role.CanFixEmergencySabotage)
            if (__instance.TaskTypes.Any(x => x == TaskTypes.ResetReactor || x == TaskTypes.RestoreOxy || x == TaskTypes.ResetSeismic || x == TaskTypes.StopCharles)) return false;


        if (__instance.AllowImpostor) return true;

        if (GameOptionsManager.Instance.currentGameMode == GameModes.HideNSeek) return true;

        bool hasFakeTask = false, fakeTaskIsExecutable = false;
        Helpers.RoleAction(pc.PlayerId, (role) =>
        {
            hasFakeTask |= !role.HasCrewmateTask(pc.PlayerId);
            fakeTaskIsExecutable |= role.HasExecutableFakeTask(pc.PlayerId);
        });
        if ((pc.GetModData().Tasks?.Quota ?? 0) == 0) hasFakeTask = true;
        if ((!hasFakeTask) || fakeTaskIsExecutable) return true;
        __result = float.MaxValue;

        return false;
    }
}

[HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.CanUse))]
public static class SystemConsoleCanUsePatch
{
    public static bool Prefix(ref float __result, SystemConsole __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        canUse = couldUse = false;

        if (Game.GameData.data.myData.getGlobalData().Property.UnderTheFloor) { __result = float.MaxValue; return false; }

        return true;
    }
}

[HarmonyPatch(typeof(MapConsole), nameof(MapConsole.CanUse))]
public static class MapConsoleCanUsePatch
{
    public static bool Prefix(ref float __result, MapConsole __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        canUse = couldUse = false;

        if (Game.GameData.data.myData.getGlobalData().Property.UnderTheFloor) { __result = float.MaxValue; return false; }

        return true;
    }
}

[HarmonyPatch(typeof(DoorConsole), nameof(DoorConsole.CanUse))]
public static class DoorConsoleCanUsePatch
{
    public static bool Prefix(ref float __result, DoorConsole __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        canUse = couldUse = false;

        if (Game.GameData.data.myData.getGlobalData().Property.UnderTheFloor) { __result = float.MaxValue; return false; }

        return true;
    }
}

[HarmonyPatch(typeof(OpenDoorConsole), nameof(OpenDoorConsole.CanUse))]
public static class OpenDoorConsoleCanUsePatch
{
    public static bool Prefix(ref float __result, OpenDoorConsole __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        canUse = couldUse = false;

        if (Game.GameData.data.myData.getGlobalData().Property.UnderTheFloor) { __result = float.MaxValue; return false; }

        return true;
    }
}

[HarmonyPatch(typeof(MedScanMinigame), nameof(MedScanMinigame.FixedUpdate))]
class MedScanMinigameFixedUpdatePatch
{
    static void Prefix(MedScanMinigame __instance)
    {
        if (CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.allowParallelMedBayScans.getBool())
        {
            __instance.medscan.CurrentUser = PlayerControl.LocalPlayer.PlayerId;
            __instance.medscan.UsersList.Clear();
        }
    }
}

[HarmonyPatch(typeof(ImportantTextTask), nameof(ImportantTextTask.AppendTaskText))]
class ImportantTextTaskPatch
{
    public static bool Prefix(ImportantTextTask __instance)
    {
        return false;
    }
}

[HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
class MinigameBeginPatch
{
    public static bool Prefix(Minigame __instance, [HarmonyArgument(0)] PlayerTask task)
    {
        Minigame.Instance = __instance;
        __instance.MyTask = task;
        try
        {
            __instance.MyNormTask = task.Cast<NormalPlayerTask>();
        }
        catch { __instance.MyNormTask = null; }
        if (PlayerControl.LocalPlayer)
        {
            if (MapBehaviour.Instance && !MapBehaviorPatch.minimapFlag) MapBehaviour.Instance.Close();

            PlayerControl.LocalPlayer.NetTransform.Halt();
        }
        __instance.StartCoroutine(__instance.CoAnimateOpen());

        return false;
    }
}

[HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.NextStep))]
class NextStepPatch
{
    public static void Finalizer(NormalPlayerTask __instance)
    {
        if (MapBehaviour.Instance && MapBehaviour.Instance.IsOpen)
        {
            MapBehaviour.Instance.taskOverlay.Hide();
            MapBehaviour.Instance.taskOverlay.Show();
        }
    }
}