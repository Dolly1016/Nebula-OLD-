using AmongUs.GameOptions;
using HarmonyLib;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
public class LightPatch
{
    public static float lastRange = 1f;

    public static bool Prefix(ref float __result, ShipStatus __instance, [HarmonyArgument(0)] GameData.PlayerInfo? player)
    {
        if (__instance == null)
        {
            lastRange = 1f;
            return true;
        }

        if (player == null || player.IsDead)
        {
            __result = __instance.MaxLightRadius;
            return false;
        }

        if ((NebulaGameManager.Instance?.GameState ?? NebulaGameStates.NotStarted) == NebulaGameStates.NotStarted) return true;

        ISystemType? systemType = __instance.Systems.ContainsKey(SystemTypes.Electrical) ? __instance.Systems[SystemTypes.Electrical] : null;
        SwitchSystem? switchSystem = systemType?.TryCast<SwitchSystem>();

        float t = (float)(switchSystem?.Value ?? 255f) / 255f;

        var info = PlayerControl.LocalPlayer.GetModInfo();
        bool hasImpostorVision = info?.Role.HasImpostorVision ?? false;
        bool ignoreBlackOut = info?.Role.IgnoreBlackout ?? true;

        if (ignoreBlackOut) t = 1f;

        float radiusRate = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, t);
        float range = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(hasImpostorVision ? FloatOptionNames.ImpostorLightMod : FloatOptionNames.CrewLightMod);
        float rate = 1f;
        info?.RoleAction(r=>r.EditLightRange(ref rate));
        rate = Mathf.Lerp(lastRange, rate, 0.7f * Time.deltaTime);
        lastRange = rate;
        __result = radiusRate * range * rate;

        return false;
    }
}