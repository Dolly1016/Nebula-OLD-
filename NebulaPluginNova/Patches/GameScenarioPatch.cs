using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Player;
using Nebula.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
public static class InitializeRolePatch
{
    static bool Prefix(RoleManager __instance)
    {
        //ロール割り当て
        var players = PlayerControl.AllPlayerControls.GetFastEnumerator().OrderBy(p => Guid.NewGuid()).ToList();
        
        int adjustedNumImpostors = GameOptionsManager.Instance.CurrentGameOptions.GetAdjustedNumImpostors(PlayerControl.AllPlayerControls.Count);

        List<PlayerControl> impostors = new();
        List<PlayerControl> others = new();

        for (int i = 0; i < players.Count; i++)
            if (i < adjustedNumImpostors)
                impostors.Add(players[i]);
            else
                others.Add(players[i]);

        GeneralConfigurations.CurrentGameMode.RoleAllocator.Assign(impostors, others);

        return false;
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
public static class StartGamePatch
{
    static void Postfix(GameManager __instance)
    {
        __instance.ShouldCheckForGameEnd = false;
    }
}

[HarmonyPatch(typeof(RoleManager),nameof(RoleManager.AssignRoleOnDeath))]
public static class SetGhostRolePatch
{
    static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
static class CheckTaskCompletionPatch
{
    static bool Prefix(GameManager __instance,ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckEndGameViaTasks))]
static class CheckEndGameViaTasksPatch
{
    static bool Prefix(GameManager __instance, ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.IsGameOverDueToDeath))]
public static class BlockGameOverPatch
{
    public static bool Prefix(LogicGameFlowNormal __instance, ref bool __result)
    {
        __result = false;
        return false;
    }
}