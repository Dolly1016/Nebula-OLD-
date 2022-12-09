namespace Nebula.Patches;

//CanUseDoorDespiteSabotageOption
[HarmonyPatch(typeof(InfectedOverlay), nameof(InfectedOverlay.CanUseDoors), MethodType.Getter)]
class CanUseDoorPatch
{
    static void Postfix(InfectedOverlay __instance, ref bool __result)
    {
        if (GameOptionsManager.Instance.CurrentGameOptions.MapId != 4) return;

        __result |= CustomOptionHolder.CanUseDoorDespiteSabotageOption.getBool();
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcRepairSystem))]
class InvokeSabotagePatch
{
    static void Postfix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes systemType)
    {
        if (MapBehaviour.Instance && MapBehaviour.Instance.IsOpen)
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

//サボクールダウン
[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.RepairDamage))]
class SabotageCoolDownPatch
{
    static bool flag = false;

    static void Prefix(SabotageSystemType __instance)
    {
        if (__instance.Timer > 0f) return;
        if (MeetingHud.Instance) return;
        if (!CustomOptionHolder.SabotageOption.getBool()) return;

        flag = true;
    }

    static void Postfix(SabotageSystemType __instance)
    {
        if (flag)
        {
            __instance.Timer = CustomOptionHolder.SabotageCoolDownOption.getFloat();
            flag = false;
        }
    }
}

//サボクールダウンの割合表示
[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.PercentCool), MethodType.Getter)]
class SabotageCoolDownGetterPatch
{
    static bool Prefix(SabotageSystemType __instance, ref float __result)
    {
        if (!CustomOptionHolder.SabotageOption.getBool()) return true;

        __result = __instance.Timer / CustomOptionHolder.SabotageCoolDownOption.getFloat();
        return false;
    }
}