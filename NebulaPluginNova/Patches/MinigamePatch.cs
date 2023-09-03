using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;


[HarmonyPatch(typeof(MedScanMinigame), nameof(MedScanMinigame.FixedUpdate))]
class MedScanMinigameFixedUpdatePatch
{
    static void Prefix(MedScanMinigame __instance)
    {
        __instance.medscan.CurrentUser = PlayerControl.LocalPlayer.PlayerId;
        __instance.medscan.UsersList.Clear();
    }
}