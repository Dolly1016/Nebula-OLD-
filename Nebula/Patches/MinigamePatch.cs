using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
    public static class ConsoleCanUsePatch
    {
        public static bool Prefix(ref float __result, Console __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            canUse = couldUse = false;

            if (Game.GameData.data.myData.getGlobalData().Property.UnderTheFloor) { __result = float.MaxValue; return false; }

            if (!Game.GameData.data.myData.getGlobalData().role.canFixSabotage)
            {
                if (__instance.TaskTypes.Any(x => x == TaskTypes.FixLights || x == TaskTypes.FixComms)) return false;
            }

            if (__instance.AllowImpostor) return true;

            bool hasFakeTask = false, fakeTaskIsExecutable = false;
            Helpers.RoleAction(pc.PlayerId, (role) => { 
                hasFakeTask |= !role.HasCrewmateTask(pc.PlayerId);
                fakeTaskIsExecutable |= role.HasExecutableFakeTask(pc.PlayerId);
            });
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
}
