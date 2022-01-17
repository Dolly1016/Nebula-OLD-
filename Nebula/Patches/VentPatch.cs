using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Hazel;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    public static class VentCanUsePatch
    {
        public static bool Prefix(Vent __instance, ref float __result, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            float num = float.MaxValue;
            PlayerControl @object = pc.Object;

            bool roleCouldUse = Game.GameData.data.players[@object.PlayerId].role.canUseVents;

            var usableDistance = __instance.UsableDistance;
            
            if (__instance.GetVentData().Sealed)
            {
                canUse = couldUse = false;
                __result = num;
                return false;
            }

            couldUse = (@object.inVent || roleCouldUse) && !pc.IsDead && (@object.CanMove || @object.inVent);
            canUse = couldUse;
            if (canUse)
            {
                Vector2 truePosition = @object.GetTruePosition();
                Vector3 position = __instance.transform.position;
                num = Vector2.Distance(truePosition, position);

                canUse &= (num <= usableDistance && !PhysicsHelpers.AnythingBetween(truePosition, position, Constants.ShipOnlyMask, false));
            }
            __result = num;
            return false;
        }
    }

    [HarmonyPatch(typeof(VentButton), nameof(VentButton.DoClick))]
    class VentButtonDoClickPatch
    {
        static bool Prefix(VentButton __instance)
        {
            if (__instance.currentTarget != null) Objects.CustomMessage.Create(__instance.currentTarget.gameObject.name, 1f, 0f, 0f, Color.white);

            // Manually modifying the VentButton to use Vent.Use again in order to trigger the Vent.Use prefix patch
            if (__instance.currentTarget != null) __instance.currentTarget.Use();
            return false;
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
    public static class VentUsePatch
    {
        public static bool Prefix(Vent __instance)
        {
            bool canUse;
            bool couldUse;
            bool canMoveInVents;

            __instance.CanUse(PlayerControl.LocalPlayer.Data, out canUse, out couldUse);

            canUse = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role.canUseVents;
            canMoveInVents = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role.canMoveInVents;
            
            if (!canUse) return false; // No need to execute the native method as using is disallowed anyways

            bool isEnter = !PlayerControl.LocalPlayer.inVent;


            if (isEnter)
            {
                PlayerControl.LocalPlayer.MyPhysics.RpcEnterVent(__instance.Id);
            }
            else
            {
                PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(__instance.Id);
            }
            __instance.SetButtons(isEnter && canMoveInVents);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class VentButtonVisibilityPatch
    {
        static void Postfix(PlayerControl? __instance)
        {
            if (__instance == null)
            {
                return;
            }

            if (Game.GameData.data == null)
            {
                return;
            }

            if (!Game.GameData.data.players.ContainsKey(__instance.PlayerId))
            {
                return;
            }

            if (__instance.AmOwner && Game.GameData.data.players[__instance.PlayerId].role.canUseVents && HudManager.Instance.ReportButton.isActiveAndEnabled)
            {
                HudManager.Instance.ImpostorVentButton.Show();
            }
        }
    }
}
