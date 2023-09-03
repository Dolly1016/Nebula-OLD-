using HarmonyLib;
using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules;

public class EastAsianFontChanger
{
    private static TMPro.TMP_FontAsset? FontJP = null, FontSC = null, FontKR = null;

    public static void LoadFont()
    {
        var fonts = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<TMPro.TMP_FontAsset>());
        foreach (var font in fonts)
        {
            if (font.name == "NotoSansJP-Regular SDF")
                FontJP = font.CastFast<TMPro.TMP_FontAsset>();
            if (font.name == "NotoSansSC-Regular SDF")
                FontSC = font.CastFast<TMPro.TMP_FontAsset>();
            if (font.name == "NotoSansKR-Regular SDF")
                FontKR = font.CastFast<TMPro.TMP_FontAsset>();
        }
    }
    public static void SetUpFont(string language)
    {
        TMPro.TMP_FontAsset? localFont = null;
        
        if (language == "Korean")
            localFont = FontKR;
        else if (language == "SChinese" || language == "TChinese")
            localFont = FontSC;
        else
            localFont = FontJP;

        if (localFont == null) return;

        var fonts = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<TMPro.TMP_FontAsset>());
        foreach (var font in fonts)
        {
            var asset = font.CastFast<TMPro.TMP_FontAsset>();
            asset.fallbackFontAssetTable.Clear();

            if (font.name == localFont.name) continue;

            asset.fallbackFontAssetTable.Add(localFont);
            if (localFont != FontJP) asset.fallbackFontAssetTable.Add(FontJP);
            if (localFont != FontSC) asset.fallbackFontAssetTable.Add(FontSC);
            if (localFont != FontKR) asset.fallbackFontAssetTable.Add(FontKR);
        }
    }
}

[NebulaPreLoad]
public class Language
{
    static private Language? CurrentLanguage = null;
    static private Language? DefaultLanguage = null;

    public Dictionary<string, string> translationMap = new();

    public static string GetCurrentLanguage() => GetLanguage((uint)AmongUs.Data.DataManager.Settings.Language.CurrentLanguage);
    
    public static string GetLanguage(uint language)
    {
        switch (language)
        {
            case 0:
                return "English";
            case 1:
                return "Latam";
            case 2:
                return "Brazilian";
            case 3:
                return "Portuguese";
            case 4:
                return "Korean";
            case 5:
                return "Russian";
            case 6:
                return "Dutch";
            case 7:
                return "Filipino";
            case 8:
                return "French";
            case 9:
                return "German";
            case 10:
                return "Italian";
            case 11:
                return "Japanese";
            case 12:
                return "Spanish";
            case 13:
                return "SChinese";
            case 14:
                return "TChinese";
            case 15:
                return "Irish";
        }
        return "English";
    }

    public static IEnumerator CoLoad()
    {
        LoadPatch.LoadingText = "Loading Language Data";
        yield return null;

        DefaultLanguage = new Language();
        DefaultLanguage.Deserialize(StreamHelper.OpenFromResource("Nebula.Resources.Color.dat"));
        DefaultLanguage.Deserialize(StreamHelper.OpenFromResource("Nebula.Resources.Lang.dat"));

        EastAsianFontChanger.LoadFont();
    }

    public static void OnChangeLanguage(uint language)
    {
        string lang = GetLanguage(language);
        EastAsianFontChanger.SetUpFont(lang);

        CurrentLanguage = new Language();
        CurrentLanguage.Deserialize(StreamHelper.OpenFromResource("Nebula.Resources.Languages." + lang + ".dat"));
        CurrentLanguage.Deserialize(StreamHelper.OpenFromResource("Nebula.Resources.Languages." + lang + "_Help.dat"));

        foreach(var addon in NebulaAddon.AllAddons)
        {
            using var stream = addon.OpenStream("Language/" + lang + ".dat");
            if (stream != null) CurrentLanguage.Deserialize(stream);
        }
    }

    private void Deserialize(Stream? stream)
    {
        if (stream == null) return;
        using (var reader = new StreamReader(stream, Encoding.GetEncoding("utf-8"))) {
            string? line;
            string[] strings;
            string? key = null;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length < 3) continue;
                if (line[0] == '#') continue;

                strings = line.Split(':', 2);
                for(int i = 0; i < 2; i++)
                {
                    int first = strings[i].IndexOf('"') + 1;
                    int last = strings[i].LastIndexOf('"');

                    try
                    {
                        strings[i] = strings[i].Substring(first, last - first);
                    }
                    catch
                    {
                        NebulaPlugin.Log.Print(NebulaLog.LogCategory.Language,"Cannot read the line \"" + line + "\"");
                        continue;
                    }
                }

                if (strings.Length != 2)
                {
                    NebulaPlugin.Log.Print(NebulaLog.LogCategory.Language, "Failed to read the line \"" + line + "\"");
                    continue;
                }


                key = strings[0];
                translationMap[key] = strings[1];
            }
        }
    }

    public static string Translate(string? translationKey)
    {
        if (translationKey == null) return "Invalid Key";
        string? result;
        if (CurrentLanguage?.translationMap.TryGetValue(translationKey, out result) ?? false)
            return result!;
        if (DefaultLanguage?.translationMap.TryGetValue(translationKey,out result) ?? false)
            return result!;
        return "*" + translationKey;
    }
}

[HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.SetLanguage))]
public static class ChangeLanguagePatch
{
    public static void Postfix(LanguageSetter __instance, [HarmonyArgument(0)] LanguageButton selected)
    {
        Language.OnChangeLanguage((uint)AmongUs.Data.DataManager.Settings.Language.CurrentLanguage);
    }
}
