namespace Nebula.Patches;

[HarmonyPatch]
class ExileControllerPatch
{
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    class ExileControllerBeginPatch
    {
        public static void Prefix(ExileController __instance, [HarmonyArgument(0)] ref GameData.PlayerInfo exiled, [HarmonyArgument(1)] bool tie)
        {
            OnExiled(exiled);
        }
    }


    [HarmonyPatch(typeof(ExileController), nameof(ExileController.ReEnableGameplay))]
    class ExileControllerReEnableGameplayPatch
    {
        public static void Postfix(ExileController __instance)
        {
            if(CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.additionalEmergencyCoolDown.getFloat() > 0f)
            {
                int deadPlayers = 0;
                foreach(var p in PlayerControl.AllPlayerControls)
                {
                    if (p.Data.IsDead) deadPlayers++;
                }
                if (deadPlayers <= (int)CustomOptionHolder.additionalEmergencyCoolDownCondition.getFloat())
                {
                    ShipStatus.Instance.EmergencyCooldown += CustomOptionHolder.additionalEmergencyCoolDown.getFloat();
                }
            }
        }
    }


    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    class BaseExileControllerPatch
    {
        public static bool Prefix(ExileController __instance)
        {
            WrapUpPrefix(__instance);

            WrapUpPostfix();

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
        public static void Prefix(AirshipExileController __instance)
        {
            WrapUpPrefix(__instance);
        }
        public static void Postfix(AirshipExileController __instance)
        {
            WrapUpPostfix();
        }
    }

    [HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
    class ExilePolusHatFixPatch
    {
        public static void Prefix(PbExileController __instance)
        {
            __instance.Player.cosmetics.hat.transform.localPosition = new Vector3(-0.2f, 0.6f, 1.1f);
        }
    }


    static void OnExiled(GameData.PlayerInfo? exiled)
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

        Events.Schedule.OnPostMeeting();

        if (exiled != null)
        {
            byte[] voters = MeetingHudPatch.GetVoters(exiled.PlayerId);

            if (exiled.GetModData().role.OnExiledPost(voters, exiled.PlayerId))
            {
                Game.GameData.data.playersArray[exiled.PlayerId].Die(Game.PlayerData.PlayerStatus.Exiled);


                PlayerControl @object = exiled.Object;
                if (@object)
                {
                    @object.Exiled();
                }
                exiled.IsDead = true;


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
    }

    static void WrapUpPrefix(ExileController __instance)
    {
        __instance.exiled = null;
    }

    static void WrapUpPostfix()
    {

        Game.GameData.data.ColliderManager.OnMeetingEnd();
        Game.GameData.data.UtilityTimer.OnMeetingEnd();

        Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (r) => r.OnMeetingEnd());

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