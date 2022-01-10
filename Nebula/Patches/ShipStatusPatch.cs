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

            if (!Game.GameData.data.players.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
            {
                return true;
            }

            ISystemType systemType = __instance.Systems.ContainsKey(SystemTypes.Electrical) ? __instance.Systems[SystemTypes.Electrical] : null;
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

            if (role.ignoreBlackout)
            {
                rate = Mathf.Lerp(__instance.MinLightRadius * role.lightRadiusMin, __instance.MaxLightRadius * role.lightRadiusMax, rate);
            }
            else
            {
                rate = __instance.MaxLightRadius;
            }

            if (role.useImpostorLightRadius)
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

    }
}
