using AmongUs.GameOptions;

namespace Nebula.Patches;

[HarmonyPatch(typeof(ShipStatus))]
public class ShipStatusPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    public static bool Prefix(ref float __result, ShipStatus __instance, [HarmonyArgument(0)] GameData.PlayerInfo? player)
    {
        if (__instance == null)
        {
            return true;
        }

        if (Game.GameData.data == null)
        {
            return true;
        }

        if (Game.GameData.data.GetPlayerData(PlayerControl.LocalPlayer.PlayerId) == null)
        {
            return true;
        }

        ISystemType systemType = __instance.Systems.ContainsKey(SystemTypes.Electrical) ? __instance.Systems[SystemTypes.Electrical] : null;
        if (systemType == null) return true;
        SwitchSystem switchSystem = systemType.TryCast<SwitchSystem>();
        if (switchSystem == null) return true;

        float rate = (float)switchSystem.Value / 255f;

        if (player == null || player.IsDead)
        { // IsDead
            __result = __instance.MaxLightRadius;
            return false;
        }

        Roles.Role? role = Game.GameData.data.myData.getGlobalData().role;

        if (role == null)
        {
            return true;
        }

        if (role.IgnoreBlackout)
        {
            rate = __instance.MaxLightRadius * role.LightRadiusMax;
        }
        else
        {
            float min = __instance.MinLightRadius / __instance.MaxLightRadius;
            if (CustomOptionHolder.SabotageOption.getBool())
            {
                float p = CustomOptionHolder.BlackOutStrengthOption.getFloat();
                if (p < 1f)
                {
                    min = min + (1f - min) * (1f - p);
                }
                else if (p > 1f)
                {
                    min /= CustomOptionHolder.BlackOutStrengthOption.getFloat();
                }
            }

            rate = Mathf.Lerp(__instance.MaxLightRadius * min * role.LightRadiusMin, __instance.MaxLightRadius * role.LightRadiusMax, rate);
            foreach (var e in Events.GlobalEvent.Events)
            {
                if (e is Events.Variation.BlackOut)
                    rate *= (e as Events.Variation.BlackOut).VisionRate;
            }
        }

        Helpers.RoleAction(PlayerControl.LocalPlayer, role => role.GetLightRadius(ref rate));

        if (role.UseImpostorLightRadius)
        {
            __result = rate * GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod);
        }
        else
        {
            __result = rate * GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.CrewLightMod);
        }
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.IsGameOverDueToDeath))]
    public static void Postfix2(LogicGameFlowNormal __instance, ref bool __result)
    {
        __result = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
    public static void Postfix(ShipStatus __instance)
    {
        Game.GameData.data.LoadMapData();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
    public static void Postfix3(ShipStatus __instance)
    {
        Game.GameData.data.ModifyShipStatus();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PolusShipStatus), nameof(PolusShipStatus.OnEnable))]
    public static void Postfix4(PolusShipStatus __instance)
    {
        Game.GameData.data.ModifyShipStatus();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.OnEnable))]
    public static void Postfix5(AirshipStatus __instance)
    {
        Game.GameData.data.ModifyShipStatus();
    }

    /*
    [HarmonyPatch(typeof(AspectSize), nameof(AspectSize.OnEnable))]
    public static class AspectPatch
    {
        public static bool Prefix()
        {
            return !Map.MapData.GetCurrentMapData().IsModMap;
        }
    }
    */
}

//AirshipにてDummyらのスポーン位置を変更する
[HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.SpawnPlayer))]
static class AirshipSpawnDummyPatch
{
    static void Postfix(AirshipStatus __instance, [HarmonyArgument(0)] PlayerControl player)
    {
        if (!player.GetComponent<DummyBehaviour>().enabled) player.NetTransform.SnapTo(new Vector2(-0.66f, -0.5f));
    }
}

//デフォルトのタスク終了を回避する
[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
static class CheckTaskCompletionPatch
{
    static bool Prefix(GameManager __instance)
    {
        return false;
    }
}