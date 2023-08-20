using AmongUs.Data.Player;
using HarmonyLib;
using Nebula.Game;
using Nebula.Modules;

namespace Nebula.Patches;


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
public static class PlayerStartPatch
{
    static void Postfix(PlayerControl __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        __result = Effects.Sequence(new Il2CppSystem.Collections.IEnumerator[] {
            __result,
            Effects.Action((Il2CppSystem.Action)(()=>{
                if(PlayerControl.LocalPlayer)DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId = PlayerControl.LocalPlayer.PlayerId }.ReflectMyColor());
            }))
            }.ToArray());
    }
}

[HarmonyPatch(typeof(PlayerCustomizationData), nameof(PlayerCustomizationData.Color), MethodType.Setter)]
public static class PlayerGetColorPatch
{
    static bool Prefix(PlayerCustomizationData __instance)
    {
        __instance.colorID = 15;
        return false;
    }
}

[HarmonyPatch(typeof(PlayerData), nameof(PlayerData.OnLoadComplete))]
public static class PlayerSetColorPatch
{
    static void Postfix(PlayerData __instance)
    {
        __instance.customization.colorID = 15;
    }
}


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckColor))]
public static class PlayerColorPatch
{
    static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] ref byte bodyColor)
    {
        bodyColor = __instance.PlayerId;
    }
}


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class PlayerUpdatePatch
{
    static IEnumerable<SpriteRenderer> AllHighlightable()
    {
        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator()) yield return p.cosmetics.currentBodySprite.BodySprite;
        foreach (var d in Helpers.AllDeadBodies()) foreach (var r in d.bodyRenderers) yield return r;
    }

    static void Prefix(PlayerControl __instance)
    {
        if (__instance.AmOwner)
        {
            foreach(var r in AllHighlightable()) r.material.SetFloat("_Outline", 0f);
        }
    }

    static void Postfix(PlayerControl __instance)
    {
        if (NebulaGameManager.Instance == null) return;
        if (NebulaGameManager.Instance.GameState == NebulaGameStates.NotStarted) return;

        NebulaGameManager.Instance.GetModPlayerInfo(__instance.PlayerId)?.Update();

        if (__instance.AmOwner) NebulaGameManager.Instance.OnFixedUpdate();
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
public static class PlayerCompleteTaskPatch
{
    static void Postfix(PlayerControl __instance)
    {
        if (!__instance.AmOwner) return;

        __instance.GetModInfo().Tasks.OnCompleteTask();
    }
}