using HarmonyLib;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    class ExileControllerBeginPatch
    {
        public static void Prefix(ExileController __instance, [HarmonyArgument(0)] ref GameData.PlayerInfo exiled, [HarmonyArgument(1)] bool tie)
        {
            if (exiled != null)
            {
                byte[] voters = MeetingHudPatch.GetVoters(exiled.PlayerId);
                exiled.GetModData().role.OnExiledPre(voters, exiled.PlayerId);

                if (exiled.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    Helpers.RoleAction(exiled.PlayerId, (role) => { role.OnExiledPre(voters); });
                }
            }
        }
    }

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

        static void WrapUpPostfix(GameData.PlayerInfo? exiled)
        {
            Events.Schedule.OnPostMeeting();

            if (exiled != null)
            {
                byte[] voters = MeetingHudPatch.GetVoters(exiled.PlayerId);

                if (exiled.GetModData().role.OnExiledPost(voters, exiled.PlayerId))
                {
                    Helpers.RoleAction(exiled.PlayerId, (role) => { role.OnDied(exiled.PlayerId); });

                    if (exiled.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        Helpers.RoleAction(exiled.PlayerId, (role) => { role.OnExiledPost(voters); });
                        Helpers.RoleAction(exiled.PlayerId, (role) => { role.OnDied(); });
                    }

                    Game.GameData.data.players[exiled.PlayerId].Die(Game.DeadPlayerData.DeathReason.Exiled);
                }
                else
                {
                    exiled.IsDead = false;
                }
            }

            Objects.CustomButton.MeetingEndedUpdate();
            Game.GameData.data.myData.getGlobalData().role.OnMeetingEnd();

            //死体はすべて消去される
            foreach(Game.DeadPlayerData deadPlayerData in Game.GameData.data.deadPlayers.Values)
            {
                deadPlayerData.EraseBody();
            }
        }
    }
}
