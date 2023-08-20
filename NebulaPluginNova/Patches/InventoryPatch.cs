using HarmonyLib;
using Nebula.Game;
using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
public static class ColorTabEnablePatch
{
    static bool Prefix(PlayerTab __instance)
    {
        //InventoryTab の OnEnable
        __instance.PlayerPreview.gameObject.SetActive(true);
        if (__instance.HasLocalPlayer())    
            __instance.PlayerPreview.UpdateFromLocalPlayer(PlayerMaterial.MaskType.None);
        else
            __instance.PlayerPreview.UpdateFromDataManager(PlayerMaterial.MaskType.None);

        if (!__instance.gameObject.TryGetComponent<NebulaPlayerTab>(out var tab))
        {
            __instance.gameObject.ForEachChild((Il2CppSystem.Action<UnityEngine.GameObject>)((obj) => { 
                if(obj.name.StartsWith("ColorChip"))GameObject.Destroy(obj);
            }));
            
            __instance.gameObject.AddComponent<NebulaPlayerTab>().playerTab = __instance;
        }
        
        return false;
    }
}

[HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.GetDefaultSelectable))]
public static class ColorTabDefaultSelectablePatch
{
    static bool Prefix(PlayerTab __instance,ref ColorChip __result)
    {
        __result = null;
        return false;
    }
}

[HarmonyPatch(typeof(PlayerCustomizationMenu), nameof(PlayerCustomizationMenu.HandleSelection))]
public static class CustomizationHandleSelectionPatch
{
    static bool Prefix(PlayerCustomizationMenu __instance,[HarmonyArgument(1)]ColorChip selectedChip)
    {
        return selectedChip != null;
    }
}

[HarmonyPatch(typeof(PlayerCustomizationMenu), nameof(PlayerCustomizationMenu.Update))]
class PlayerCustomizationMenuUpdatePatch
{
    public static void Postfix(PlayerCustomizationMenu __instance)
    {
        if (__instance.selectedTab == 0)
        {
            __instance.equippedText.SetActive(false);
        }
    }
}