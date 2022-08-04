using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(ShipStatus))]
    public class ShipStatusPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
        public static bool Prefix(ref float __result, ShipStatus __instance, [HarmonyArgument(0)] GameData.PlayerInfo? player)
        {
            if (__instance == null)
            {
                return true;
            }

            if (Game.GameData.data == null)
            {
                return true;
            }

            if (Game.GameData.data.GetPlayerData(PlayerControl.LocalPlayer.PlayerId)==null)
            {
                return true;
            }

            ISystemType systemType = __instance.Systems.ContainsKey(SystemTypes.Electrical) ? __instance.Systems.get_Item(SystemTypes.Electrical) : null;
            if (systemType == null) return true;
            SwitchSystem switchSystem = systemType.TryCast<SwitchSystem>();
            if (switchSystem == null) return true;

            float rate = (float)switchSystem.Value / 255f;

            if (player == null || player.IsDead)
            { // IsDead
                __result = __instance.MaxLightRadius;
                return false;
            }
        
            Roles.Role? role = Game.GameData.data.myData.getGlobalData().role;

            if (role == null)
            {
                return true;
            }

            if (role.IgnoreBlackout)
            {
                rate = __instance.MaxLightRadius * role.LightRadiusMax;
            }
            else
            {
                rate = Mathf.Lerp(__instance.MinLightRadius * role.LightRadiusMin, __instance.MaxLightRadius * role.LightRadiusMax, rate);
                foreach(var e in Events.GlobalEvent.Events)
                {
                    if (e is Events.Variation.BlackOut)
                        rate *= (e as Events.Variation.BlackOut).VisionRate;
                }
            }

            Helpers.RoleAction(PlayerControl.LocalPlayer, role => role.GetLightRadius(ref rate));

            if (role.UseImpostorLightRadius)
            {
                __result = rate * PlayerControl.GameOptions.ImpostorLightMod;
            }
            else
            {
                __result = rate * PlayerControl.GameOptions.CrewLightMod;
            }

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.IsGameOverDueToDeath))]
        public static void Postfix2(ShipStatus __instance, ref bool __result)
        {
            __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
        public static void Postfix(ShipStatus __instance)
        {
            Game.GameData.data.LoadMapData();
        }

        /*
        [HarmonyPatch(typeof(AspectSize), nameof(AspectSize.OnEnable))]
        public static class AspectPatch
        {
            public static bool Prefix()
            {
                return !Map.MapData.GetCurrentMapData().IsModMap;
            }
        }
        */
    }

    //AirshipにてDummyらのスポーン位置を変更する
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.SpawnPlayer))]
    static class AirshipSpawnDummyPatch
    {
        static void Postfix(AirshipStatus __instance, [HarmonyArgument(0)] PlayerControl player)
        {
            if(player.isDummy) player.NetTransform.SnapTo(new Vector2(-0.66f, -0.5f));
        }
    }
}
