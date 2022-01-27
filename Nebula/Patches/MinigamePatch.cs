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

            if (__instance.AllowImpostor) return true;
            if (!Game.GameData.data.players[pc.PlayerId].role.hasFakeTask) return true;
            if (Game.GameData.data.players[pc.PlayerId].role.hasFakeTask && Game.GameData.data.players[pc.PlayerId].role.fakeTaskIsExecutable) return true;
            __result = float.MaxValue;
            
            return false;
        }
    }

}
