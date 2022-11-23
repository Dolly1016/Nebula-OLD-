namespace Nebula.Patches
{
    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    public static class VentEnterPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
        {
            if (pc != PlayerControl.LocalPlayer) return;
            Game.GameData.data.myData.VentDurationTimer = pc.GetModData().role.VentDurationMaxTimer;
            Helpers.RoleAction(pc, (role) => role.OnEnterVent(__instance));
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
    public static class VentExitPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
        {
            if (pc != PlayerControl.LocalPlayer) return;
            Game.GameData.data.myData.VentCoolDownTimer = pc.GetModData().role.VentCoolDownMaxTimer;
            Helpers.RoleAction(pc, (role) => role.OnExitVent(__instance));
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    public static class VentCanUsePatch
    {
        public static bool Prefix(Vent __instance, ref float __result, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            float num = float.MaxValue;
            PlayerControl @object = pc.Object;

            bool roleCouldUse = false;
            if (Game.GameData.data.playersArray[PlayerControl.LocalPlayer.PlayerId].role.VentPermission != Roles.VentPermission.CanNotUse)
                roleCouldUse = !HudManager.Instance.ImpostorVentButton.isCoolingDown;

            var usableDistance = __instance.UsableDistance;
            
            if (__instance.GetVentData()!=null && __instance.GetVentData().Sealed)
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

                if (@object.MyPhysics.Animator.Clip == @object.MyPhysics.CurrentAnimationGroup.EnterVentAnim && @object.MyPhysics.Animator.Playing) canUse = false;
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
            bool canUse=false;
            bool couldUse;
            bool canMoveInVents;

            __instance.CanUse(PlayerControl.LocalPlayer.Data, out canUse, out couldUse);

            if (Game.GameData.data.playersArray[PlayerControl.LocalPlayer.PlayerId].role.VentPermission != Roles.VentPermission.CanNotUse)
                canUse &= !HudManager.Instance.ImpostorVentButton.isCoolingDown;
            
            canMoveInVents = Game.GameData.data.playersArray[PlayerControl.LocalPlayer.PlayerId].role.CanMoveInVents;
            
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

            if (!__instance.AmOwner) return;

            var data = Game.GameData.data.GetPlayerData(__instance.PlayerId);
            if (data == null) return;

            HudManager hudManager = HudManager.Instance;

            var role = data.role;
            var showFlag = hudManager.ReportButton.isActiveAndEnabled;


            if (__instance.CanMove) Game.GameData.data.myData.VentCoolDownTimer -= Time.deltaTime;
            Game.GameData.data.myData.VentDurationTimer -= Time.deltaTime;

            if (Game.GameData.data.myData.VentCoolDownTimer < 0f) Game.GameData.data.myData.VentCoolDownTimer = 0f;
            if (Game.GameData.data.myData.VentDurationTimer < 0f) Game.GameData.data.myData.VentDurationTimer = 0f;

            if (role.VentPermission != Roles.VentPermission.CanNotUse && showFlag)
            {
                hudManager.ImpostorVentButton.Show();

                if (!hudManager.ImpostorVentButton.cooldownTimerText)
                {
                    hudManager.ImpostorVentButton.cooldownTimerText =
                    UnityEngine.Object.Instantiate(hudManager.AbilityButton.cooldownTimerText, hudManager.ImpostorVentButton.transform);
                }

                if (role.VentPermission == Roles.VentPermission.CanUseLimittedVent)
                {
                    if (__instance.inVent)
                    {
                        hudManager.ImpostorVentButton.cooldownTimerText.text = Mathf.CeilToInt(Game.GameData.data.myData.VentDurationTimer).ToString();
                        hudManager.ImpostorVentButton.cooldownTimerText.gameObject.SetActive(true);
                    }
                    else
                    {
                        hudManager.ImpostorVentButton.SetCoolDown(Game.GameData.data.myData.VentCoolDownTimer, role.VentCoolDownMaxTimer);
                    }
                }
                else
                {
                    hudManager.ImpostorVentButton.SetCoolDown(0f,10f);
                }
            }
            else
                hudManager.ImpostorVentButton.Hide();
            
            if (role.CanInvokeSabotage && showFlag)
                hudManager.SabotageButton.Show();
            else
                hudManager.SabotageButton.Hide();

            if(__instance.inVent && role.VentPermission == Roles.VentPermission.CanUseLimittedVent &&
                !(Game.GameData.data.myData.VentDurationTimer > 0f))
            {
                Vent vent = hudManager.ImpostorVentButton.currentTarget;
                if (!vent.GetVentData().Sealed)
                {
                    __instance.MyPhysics.RpcExitVent(vent.Id);
                    vent.SetButtons(false);
                }
            }
        }
    }
    

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        static void Postfix(PlayerPhysics __instance,ref Il2CppSystem.Collections.IEnumerator __result)
        {
            List<Il2CppSystem.Collections.IEnumerator> sequence = new List<Il2CppSystem.Collections.IEnumerator>();
            sequence.Add(Effects.Action((Il2CppSystem.Action)(() =>
            {
                __instance.myPlayer.Collider.enabled = false;
            })));
            sequence.Add(__result);
            sequence.Add(Effects.Action((Il2CppSystem.Action)(() =>
            {
                __instance.myPlayer.Collider.enabled = true;
            })));
            __result = Effects.Sequence(new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Collections.IEnumerator>(sequence.ToArray()));
        }
    }
}
