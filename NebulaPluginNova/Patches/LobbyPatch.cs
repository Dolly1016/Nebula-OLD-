using HarmonyLib;
using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class MinPlayerPatch
{
    static void Postfix(GameStartManager __instance)
    {
        __instance.MinPlayers = GeneralConfigurations.CurrentGameMode.MinPlayers;
    }
}
