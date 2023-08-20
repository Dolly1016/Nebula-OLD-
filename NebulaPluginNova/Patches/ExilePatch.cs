using HarmonyLib;
using Il2CppSystem.Collections;
using Nebula.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

public static class NebulaExileWrapUp
{
    static public System.Collections.IEnumerator WrapUpAndSpawn(ExileController __instance)
    {
        if (__instance.exiled != null)
        {
            PlayerControl @object = __instance.exiled.Object;
            if (@object)
            {
                @object.Exiled();
                NebulaGameManager.Instance.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.Exile, null, 1 << @object.PlayerId));
            }
            else
            {
                //NebulaGameManager.Instance.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.Kill, message.Killer, 1 << message.Target));
            }
            __instance.exiled.IsDead = true;

            @object.GetModInfo()?.RoleAction(role => {
                role.OnExiled();
                role.OnDead();
            });
        }
        NebulaGameManager.Instance?.OnMeetingEnd(__instance.exiled?.Object);

        yield return ShipStatus.Instance.PrespawnStep();
        __instance.ReEnableGameplay();
        GameObject.Destroy(__instance.gameObject);
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
public static class ExileWrapUpPatch
{
    static bool Prefix(ExileController __instance)
    {
        __instance.StartCoroutine(NebulaExileWrapUp.WrapUpAndSpawn(__instance).WrapToIl2Cpp());
        return false;
    }
}

[HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
public static class AirshipExileWrapUpPatch
{
    static bool Prefix(AirshipExileController __instance,IEnumerator __result)
    {
        __result = NebulaExileWrapUp.WrapUpAndSpawn(__instance).WrapToIl2Cpp();
        return false;
    }
}
