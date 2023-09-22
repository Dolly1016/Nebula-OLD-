using Nebula.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

//Vitals
[HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Begin))]
class VitalsMinigameStartPatch
{
    static void Postfix(VitalsMinigame __instance)
    {
        NebulaGameManager.Instance?.ConsoleRestriction?.ShowTimerIfNecessary(ConsoleRestriction.ConsoleType.Vitals, __instance.transform, new Vector3(3.4f, 2f, -50f));
    }
}

[HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
class VitalsMinigameUpdatePatch
{
    static bool Prefix(VitalsMinigame __instance)
    {
        if (ConsoleTimer.IsOpenedByAvailableWay()) return true;

        __instance.SabText.gameObject.SetActive(true);
        __instance.SabText.text = Language.Translate("console.notAvailable");

        foreach (var panel in __instance.vitals) panel.gameObject.SetActive(false);

        return false;
    }
}

//Camera (Skeld)
[HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Begin))]
class SurveillanceMinigameBeginPatch
{
    public static void Prefix(SurveillanceMinigame __instance)
    {
        NebulaGameManager.Instance?.ConsoleRestriction?.ShowTimerIfNecessary(ConsoleRestriction.ConsoleType.Camera, __instance.transform, new Vector3(3.4f, 2f, -50f));
    }
}

[HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
class SurveillanceMinigameUpdatePatch
{
    public static bool Prefix(SurveillanceMinigame __instance)
    {
        if (ConsoleTimer.IsOpenedByAvailableWay()) return true;

        for (int j = 0; j < __instance.ViewPorts.Length; j++)
        {
            __instance.ViewPorts[j].sharedMaterial = __instance.StaticMaterial;
            __instance.SabText[j].gameObject.SetActive(true);
            __instance.SabText[j].text = Language.Translate("console.notAvailable");
        }
        return false;
    }
}

//Camera (Others)
[HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Begin))]
class PlanetSurveillanceMinigameBeginPatch
{
    public static void Prefix(PlanetSurveillanceMinigame __instance)
    {
        NebulaGameManager.Instance?.ConsoleRestriction?.ShowTimerIfNecessary(ConsoleRestriction.ConsoleType.Camera, __instance.transform, new Vector3(0f, -1.9f, -50f));
    }
}

[HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
class PlanetSurveillanceMinigameUpdatePatch
{
    public static bool Prefix(PlanetSurveillanceMinigame __instance)
    {
        if (ConsoleTimer.IsOpenedByAvailableWay()) return true;

        __instance.SabText.gameObject.SetActive(true);
        __instance.SabText.text = Language.Translate("console.notAvailable");
        __instance.isStatic = true;
        __instance.ViewPort.sharedMaterial = __instance.StaticMaterial;

        return false;
    }
}

[HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.NextCamera))]
class PlanetSurveillanceMinigameNextCameraPatch
{
    public static bool Prefix(PlanetSurveillanceMinigame __instance, [HarmonyArgument(0)]int direction)
    {
        if (ConsoleTimer.IsOpenedByAvailableWay()) return true;

        if (direction != 0 && Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(__instance.ChangeSound, false, 1f, null);
        
        __instance.Dots[__instance.currentCamera].sprite = __instance.DotDisabled;
        __instance.currentCamera = (__instance.currentCamera + direction).Wrap(__instance.survCameras.Length);
        __instance.Dots[__instance.currentCamera].sprite = __instance.DotEnabled;
        SurvCamera survCamera = __instance.survCameras[__instance.currentCamera];
        __instance.Camera.transform.position = survCamera.transform.position + __instance.survCameras[__instance.currentCamera].Offset;
        __instance.LocationName.text = ((survCamera.NewName > StringNames.None) ? DestroyableSingleton<TranslationController>.Instance.GetString(survCamera.NewName) : survCamera.CamName);
        
        return false;
    }
}

//Door Log
