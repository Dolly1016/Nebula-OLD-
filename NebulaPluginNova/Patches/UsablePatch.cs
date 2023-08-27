using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nebula.Player;

namespace Nebula.Patches;

[HarmonyPatch(typeof(KeyboardJoystick),nameof(KeyboardJoystick.HandleHud))]
public static class KeyboardInputPatch
{
    public static bool Prefix(KeyboardJoystick __instance)
    {
        if (!DestroyableSingleton<HudManager>.InstanceExists) return false;
        
        if (KeyboardJoystick.player.GetButtonDown(7)) HudManager.Instance.ReportButton.DoClick();
        
        if (KeyboardJoystick.player.GetButtonDown(6)) HudManager.Instance.UseButton.DoClick();
        
        if (KeyboardJoystick.player.GetButtonDown(4) && !HudManager.Instance.Chat.IsOpenOrOpening)
            HudManager.Instance.ToggleMapVisible(GameManager.Instance.GetMapOptions());

        if (KeyboardJoystick.player.GetButtonDown(8) && HudManager.Instance.KillButton.gameObject.active) HudManager.Instance.KillButton.DoClick();
        if (KeyboardJoystick.player.GetButtonDown(50) && HudManager.Instance.ImpostorVentButton.gameObject.active) HudManager.Instance.ImpostorVentButton.DoClick();

        return false;
    }
}


[HarmonyPatch(typeof(Vent),nameof(Vent.CanUse))]
public static class VentCanUsePatch
{
    public static bool Prefix(Vent __instance, ref float __result,[HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        float num = float.MaxValue;
        PlayerControl @object = pc.Object;
        PlayerModInfo modInfo = NebulaGameManager.Instance.GetModPlayerInfo(pc.PlayerId);

        couldUse = true;
        
        if (@object.inVent && Vent.currentVent == __instance)
        {
            //既にベント内にいる場合
        }
        else {
            //ベント外にいる場合
            couldUse &= modInfo.Role.CanUseVent;
            if (modInfo.Role.HasAnyTasks) couldUse &= !@object.MustCleanVent(__instance.Id);
            couldUse &= !pc.IsDead && @object.CanMove;
        }

        ISystemType systemType;
        if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out systemType))
        {
            VentilationSystem ventilationSystem = systemType.Cast<VentilationSystem>();
            if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(__instance.Id)) couldUse = false;
        }

        canUse = couldUse;

        if (canUse)
        {
            Vector3 center = @object.Collider.bounds.center;
            Vector3 position = __instance.transform.position;
            num = Vector2.Distance(center, position);
            canUse &= (num <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(@object.Collider, center, position, Constants.ShipOnlyMask, false));
        }
        __result = num;
        return false;
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
public static class VentSetOutlinePatch
{
    public static bool Prefix(Vent __instance, [HarmonyArgument(0)]bool on, [HarmonyArgument(1)]bool mainTarget) {
        Color color = PlayerControl.LocalPlayer.GetModInfo()!.Role.Role.RoleColor;
        __instance.myRend.material.SetFloat("_Outline", (float)(on ? 1 : 0));
        __instance.myRend.material.SetColor("_OutlineColor", color);
        __instance.myRend.material.SetColor("_AddColor", mainTarget ? color : Color.clear);

        return false;
    }
}

[HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
public static class ConsoleCanUsePatch
{
    public static bool Prefix(ref float __result, Console __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        canUse = couldUse = false;

        if (ShipStatus.Instance.SpecialTasks.Any((task) => __instance.TaskTypes.Contains(task.TaskType))) return true;

        if (__instance.AllowImpostor) return true;

        var info = NebulaGameManager.Instance.GetModPlayerInfo(PlayerControl.LocalPlayer.PlayerId);
        if (info == null) return true;

        if (!info.Role.HasAnyTasks)
        {
            __result = float.MaxValue;
            return false;
        }

        return true;
    }
}
