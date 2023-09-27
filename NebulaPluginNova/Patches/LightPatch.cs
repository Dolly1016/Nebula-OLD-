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
    public static bool Prefix(ref float __result, ShipStatus __instance, [HarmonyArgument(0)] GameData.PlayerInfo? player)
    {
        if (__instance == null)
        {
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

        bool hasImpostorVision = PlayerControl.LocalPlayer.GetModInfo()?.Role.HasImpostorVision ?? false;
        bool ignoreBlackOut = PlayerControl.LocalPlayer.GetModInfo()?.Role.IgnoreBlackout ?? true;

        if (ignoreBlackOut) t = 1f;

        float radiusRate = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, t);

        __result = radiusRate * GameOptionsManager.Instance.CurrentGameOptions.GetFloat(hasImpostorVision ? FloatOptionNames.ImpostorLightMod : FloatOptionNames.CrewLightMod);

        return false;
    }
}