using AmongUs.GameOptions;
using HarmonyLib;
using Nebula.Game;
using Nebula.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.Refresh))]
public static class SabotageButtonPatch
{
    static bool Prefix(SabotageButton __instance)
    {
        try
        {
            if (PlayerControl.LocalPlayer.inVent || !GameManager.Instance.SabotagesEnabled() || PlayerControl.LocalPlayer.petting)
                __instance.SetDisabled();
            else
                __instance.SetEnabled();
        }catch(Exception e)
        {
            __instance.SetDisabled();
        }
        return false;
    }
}

[HarmonyPatch(typeof(AdminButton), nameof(AdminButton.Refresh))]
public static class AdminButtonPatch
{
    static bool Prefix(AdminButton __instance)
    {
        try
        {
            if (!(GameManager.Instance == null))
            {
                LogicGameFlowHnS logicGameFlowHnS = GameManager.Instance.LogicFlow.TryCast<LogicGameFlowHnS>();
                if (logicGameFlowHnS != null)
                {
                    __instance.useable = logicGameFlowHnS.SeekerAdminMapEnabled(PlayerControl.LocalPlayer);
                    if (!__instance.useable || (MapBehaviour.Instance && MapBehaviour.Instance.IsOpen))
                        __instance.SetDisabled();
                    else
                        __instance.SetEnabled();
                }
            }
        }catch(Exception e)
        {
            __instance.SetDisabled();
        }
        return false;
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive), typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool))]
public static class HudActivePatch
{
    static bool Prefix(HudManager __instance,[HarmonyArgument(0)]PlayerControl localPlayer, [HarmonyArgument(1)]RoleBehaviour role, [HarmonyArgument(2)]bool isActive)
    {
        __instance.UpdateHudContent();

        __instance.UseButton.transform.parent.gameObject.SetActive(isActive);
        __instance.TaskPanel.gameObject.SetActive(isActive);
        __instance.roomTracker.gameObject.SetActive(isActive);
        __instance.joystick?.ToggleVisuals(isActive);
        __instance.ToggleRightJoystick(isActive);
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Revive))]
public static class RevivePatch
{
    static bool Prefix(PlayerControl __instance)
    {
        __instance.Data.IsDead = false;
        __instance.gameObject.layer = LayerMask.NameToLayer("Players");
        __instance.MyPhysics.ResetMoveState(true);
        __instance.clickKillCollider.enabled = true;
        __instance.cosmetics.SetPetSource(__instance);
        __instance.cosmetics.SetNameMask(true);
        if (__instance.AmOwner)
        {
            DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(true);
            DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
            DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
            DestroyableSingleton<HudManager>.Instance.Chat.SetVisible(false);
        }

        return false;
    }
}


[HarmonyPatch(typeof(VentButton), nameof(VentButton.DoClick))]
public static class VentClickPatch
{
    static bool Prefix(VentButton __instance)
    {
        if ((!PlayerControl.LocalPlayer.inVent) && (PlayerControl.LocalPlayer?.GetModInfo()?.Role?.VentCoolDown?.IsInProcess ?? false))
            return false;

        if (__instance.currentTarget != null)
        {
            var role = PlayerControl.LocalPlayer.GetModInfo()!.Role;
        }

        return true;
    }
}

[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class KillButtonClickPatch
{
    static bool Prefix(KillButton __instance)
    {
        if (__instance.enabled && __instance.currentTarget && !__instance.isCoolingDown && !PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.CanMove)
        {
            PlayerControl.LocalPlayer.ModKill(__instance.currentTarget, true, PlayerState.Dead, EventDetail.Kill);
            __instance.SetTarget(null);
        }
        return false;
    }
}

[HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
public static class KillButtonSetTargetPatch
{
    static bool Prefix(KillButton __instance)
    {
        return __instance.gameObject.active;
    }
}

[HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.InitializeAbilityButton))]
class BlockInitializeAbilityButtonPatch
{
    static bool Prefix(RoleBehaviour __instance)
    {
        HudManager.Instance.AbilityButton.gameObject.SetActive(false);
        return false;
    }
}