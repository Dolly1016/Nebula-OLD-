namespace Nebula.Patches;

[HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.SetLanguage))]
public static class ChangeLanguagePatch
{
    public static void Postfix(LanguageSetter __instance, [HarmonyArgument(0)] LanguageButton selected)
    {
        Language.Language.Load();
    }
}