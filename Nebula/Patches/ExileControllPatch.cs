using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.ReEnableGameplay))]
        class ExileControllerReEnableGameplayPatch
        {
            public static void Postfix(ExileController __instance)
            {
                CustomOverlays.OnMeetingEnd();
            }
        }


        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static bool Prefix(ExileController __instance)
            {
                if (__instance.exiled != null)
                {
                    PlayerControl @object = __instance.exiled.Object;
                    if (@object)
                    {
                        @object.Exiled();
                    }
                    __instance.exiled.IsDead = true;
                }

                WrapUpPostfix(__instance.exiled);

                List<Il2CppSystem.Collections.IEnumerator> sequence = new List<Il2CppSystem.Collections.IEnumerator>();

                if (DestroyableSingleton<TutorialManager>.InstanceExists || !ShipStatus.Instance.IsGameOverDueToDeath())
                {
                    sequence.Add(ShipStatus.Instance.PrespawnStep());
                    sequence.Add(Effects.Action(new System.Action(() => { __instance.ReEnableGameplay(); })));
                }
                sequence.Add(Effects.Action(new System.Action(() =>
                {
                    UnityEngine.Object.Destroy(__instance.gameObject);
                })));

                var refArray = new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Collections.IEnumerator>(sequence.ToArray());
                HudManager.Instance.StartCoroutine(Effects.Sequence(refArray));


                return false;
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
                    Game.GameData.data.players[exiled.PlayerId].Die(Game.PlayerData.PlayerStatus.Exiled);

                    Helpers.RoleAction(exiled.PlayerId, (role) => { role.OnDied(exiled.PlayerId); });
                    Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { role.OnAnyoneDied(exiled.PlayerId); });

                    if (exiled.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        Helpers.RoleAction(exiled.PlayerId, (role) => { role.OnExiledPost(voters); });
                        Helpers.RoleAction(exiled.PlayerId, (role) => { role.OnDied(); });

                        Game.GameData.data.myData.CanSeeEveryoneInfo = true;
                    }
                }
                else
                {
                    exiled.IsDead = false;
                }
            }

            Objects.CustomButton.OnMeetingEnd();
            Objects.CustomObject.OnMeetingEnd();
            Game.GameData.data.ColliderManager.OnMeetingEnd();
            Game.GameData.data.UtilityTimer.OnMeetingEnd();
            Game.GameData.data.myData.getGlobalData().role.OnMeetingEnd();

            if (Game.GameData.data.GameMode == Module.CustomGameMode.Investigators)
            {
                Ghost.InvestigatorMeetingUI.EndMeeting();
            }

            //死体はすべて消去される
            foreach (Game.DeadPlayerData deadPlayerData in Game.GameData.data.deadPlayers.Values)
            {
                deadPlayerData.EraseBody();
            }
        }
    }
}
