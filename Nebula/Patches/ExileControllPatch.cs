using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using HarmonyLib;

namespace Nebula.Patches
{
    [HarmonyPatch]
    class ExileControllerWrapUpPatch
    {

        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                WrapUpPostfix(__instance.exiled);
            }
        }

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
            {
                WrapUpPostfix(__instance.exiled);
            }
        }

        static void WrapUpPostfix(GameData.PlayerInfo exiled)
        {
            if (Game.GameData.data.players[exiled.PlayerId].role.OnExiled())
            {
               
                Game.GameData.data.players[exiled.PlayerId].Die();
            }
            else
            {
                exiled.IsDead = false;
            }

            Game.GameData.data.myData.getGlobalData().role.OnMeetingEnd();
        }
    }
}
