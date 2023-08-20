using HarmonyLib;

namespace Nebula.Patches;

[HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.Start))]
public static class OnlineGameModePatch
{
    static void Postfix(CreateOptionsPicker __instance)
    {
        __instance.SetGameMode(AmongUs.GameOptions.GameModes.Normal);
        var buttonTransform = __instance.GameModeText.transform.parent;
        buttonTransform.GetChild(1).gameObject.SetActive(false);
        buttonTransform.GetComponent<Collider2D>().enabled = false;
    }
}


[HarmonyPatch(typeof(HostLocalGameButton), nameof(HostLocalGameButton.Start))]
public static class LocalGameModePatch
{
    static void Postfix(HostLocalGameButton __instance)
    {
        if (__instance.TryGetComponent(Il2CppType.Of<FreeplayPopover>(),out _)) return;
        __instance.transform.FindChild("CreateHnSGameButton")?.gameObject.SetActive(false);
    }
}
