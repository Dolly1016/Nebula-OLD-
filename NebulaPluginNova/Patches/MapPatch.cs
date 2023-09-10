using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnEnable))]
public static class OpenMapCountOverlayPatch
{

    static void Prefix(MapCountOverlay __instance)
    {
        __instance.InitializeModOption();
    }
}


[HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
public static class CountOverlayUpdatePatch
{

    static bool Prefix(MapCountOverlay __instance)
    {
        //0.1秒に1回だけ通る
        __instance.timer += Time.deltaTime;
        if (__instance.timer < 0.1f) return false;
        
        __instance.timer = 0f;
        
        if (!__instance.isSab && MapBehaviourExtension.AffectedByCommSab && PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
        {
            __instance.isSab = true;
            __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
            __instance.SabotageText.gameObject.SetActive(true);
        }
        if (__instance.isSab && (!MapBehaviourExtension.AffectedByCommSab || !PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer)))
        {
            __instance.isSab = false;
            __instance.BackgroundColor.SetColor(MapBehaviourExtension.MapColor ?? Color.green);
            __instance.SabotageText.gameObject.SetActive(false);
        }

        if (__instance.isSab)
        {
            foreach (var counterArea in __instance.CountAreas) counterArea.UpdateCount(0);
            return false;
        }

        HashSet<int> hashSet = new HashSet<int>();
        foreach (var counterArea in __instance.CountAreas)
        {
            if (ShipStatus.Instance.FastRooms.TryGetValue(counterArea.RoomType, out var plainShipRoom) && plainShipRoom.roomArea)
            {
                int num = plainShipRoom.roomArea.OverlapCollider(__instance.filter, __instance.buffer);
                int counter = 0;
                int deadBodies = 0, impostors = 0;
                for (int j = 0; j < num; j++)
                {
                    Collider2D collider2D = __instance.buffer[j];
                    if (collider2D.CompareTag("DeadBody") && __instance.includeDeadBodies)
                    {
                        DeadBody component = collider2D.GetComponent<DeadBody>();
                        if (component != null && hashSet.Add((int)component.ParentId))
                        {
                            counter++;
                            if (MapBehaviourExtension.CanIdentifyDeadBodies) deadBodies++;
                        }
                    }
                    else if (!collider2D.isTrigger)
                    {
                        PlayerControl component2 = collider2D.GetComponent<PlayerControl>();
                        if (component2 && component2.Data != null && !component2.Data.Disconnected && !component2.Data.IsDead && (__instance.showLivePlayerPosition || !component2.AmOwner) && hashSet.Add((int)component2.PlayerId))
                        {
                            counter++;
                            if (component2.Data.Role.IsImpostor && MapBehaviourExtension.CanIdentifyImpostors) impostors++;
                        }
                    }
                }
                counterArea.UpdateCount(counter, impostors, deadBodies);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
public static class OpenNormalMapPatch
{

    static void Postfix(MapCountOverlay __instance)
    {
        PlayerControl.LocalPlayer.GetModInfo()?.RoleAction(r=>r.OnOpenNormalMap());
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
public static class OpenSabotageMapPatch
{

    static void Postfix(MapCountOverlay __instance)
    {
        PlayerControl.LocalPlayer.GetModInfo()?.RoleAction(r => r.OnOpenSabotageMap());
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
public static class OpenAdminMapPatch
{

    static void Postfix(MapCountOverlay __instance)
    {
        PlayerControl.LocalPlayer.GetModInfo()?.RoleAction(r => r.OnOpenAdminMap());
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Awake))]
public static class InitMapPatch
{
    static void Postfix(MapBehaviour __instance)
    {
        PlayerControl.LocalPlayer.GetModInfo()?.RoleAction(r => r.OnMapInstantiated());
    }
}