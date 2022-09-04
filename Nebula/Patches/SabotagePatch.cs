using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Nebula.Utilities;

namespace Nebula.Patches
{

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcRepairSystem))]
    class InvokeSabotagePatch
    {
        static void Postfix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes systemType)
        {
            if(MapBehaviour.Instance && MapBehaviour.Instance.IsOpen)
            Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (role) => role.OnInvokeSabotage(systemType));
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcCloseDoorsOfType))]
    class InvokeDoorSabotagePatch
    {
        static void Postfix(ShipStatus __instance)
        {
            Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (role) => role.OnInvokeSabotage(SystemTypes.Doors));
        }
    }

}
