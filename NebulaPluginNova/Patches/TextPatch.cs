using HarmonyLib;
using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(TextTranslatorTMP),nameof(TextTranslatorTMP.ResetText))]
public static class TextPatch
{
    static public bool Prefix(TextTranslatorTMP __instance)
    {
        if ((short)__instance.TargetText != short.MaxValue) return true;
        __instance.GetComponent<TMPro.TextMeshPro>().text = Language.Translate(__instance.defaultStr);
        return false;
    }
}
