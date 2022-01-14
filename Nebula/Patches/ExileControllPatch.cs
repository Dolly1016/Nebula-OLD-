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
            byte[] voters=MeetingHudPatch.GetVoters(exiled.PlayerId);
            if (exiled.GetModData().role.OnExiled(voters,exiled.PlayerId))
            {
                exiled.GetModData().role.OnDied(exiled.PlayerId);

                if (exiled.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    exiled.GetModData().role.OnExiled(voters);
                    exiled.GetModData().role.OnDied();
                }

                Game.GameData.data.players[exiled.PlayerId].Die(Game.DeadPlayerData.DeathReason.Exiled);
            }
            else
            {
                exiled.IsDead = false;
            }

            Game.GameData.data.myData.getGlobalData().role.OnMeetingEnd();

            //死体はすべて消去される
            foreach(Game.DeadPlayerData deadPlayerData in Game.GameData.data.deadPlayers.Values)
            {
                deadPlayerData.EraseBody();
            }
        }
    }
}
