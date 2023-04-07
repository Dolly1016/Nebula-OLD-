using BepInEx.Configuration;
using UnityEngine.UI;
using UnityEngine.Events;
using Discord;

namespace Nebula.Patches;

[HarmonyPatch(typeof(ServerInfo), nameof(ServerInfo.HttpUrl),MethodType.Getter)]
public static class HttpConvertPatch
{
    public static bool Prefix(ServerInfo __instance,ref string __result)
    {
        if (__instance.Port != 22000) return true;
        if (__instance.Ip.IndexOf("://") != -1) return true;

        __result = string.Format("http://{0}:{1}/", __instance.Ip, __instance.Port);
        return false;
    }
}

[HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
public static class RegionMenuOpenPatch
{
    private static ConfigEntry<string> SaveIp;
    private static ConfigEntry<ushort> SavePort;

    private static TextInputField? ipField = null;
    private static TextInputField? portField = null;

    public static IRegionInfo[] defaultRegions;
    public static void UpdateRegions()
    {
        ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;
        IRegionInfo[] regions = defaultRegions;

        //var CustomRegion = new DnsRegionInfo(SaveIp.Value, "Custom", StringNames.NoTranslation, SaveIp.Value, SavePort.Value, false);
        var CustomRegion = new StaticHttpRegionInfo("Custom", StringNames.NoTranslation, SaveIp.Value,
            new ServerInfo[] { new ServerInfo("Custom", SaveIp.Value, SavePort.Value, false) });
        regions = regions.Concat(new IRegionInfo[] { CustomRegion.Cast<IRegionInfo>() }).ToArray();
        ServerManager.DefaultRegions = regions;
        serverManager.AvailableRegions = regions;

    }

    public static void Initialize()
    {
        SaveIp = NebulaPlugin.Instance.Config.Bind("CustomServer", "Ip", "");
        SavePort = NebulaPlugin.Instance.Config.Bind("CustomServer", "Port", (ushort)22000);

        defaultRegions = ServerManager.DefaultRegions;
        UpdateRegions();
    }

    private static void ChooseOption(RegionMenu __instance, IRegionInfo region)
    {

        DestroyableSingleton<ServerManager>.Instance.SetRegion(region);
        __instance.RegionText.text = DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(region.TranslateName, region.Name, new Il2CppReferenceArray<Il2CppSystem.Object>(new Il2CppSystem.Object[0]));
    }

    public static void Postfix(RegionMenu __instance)
    {
        if (!__instance.TryCast<RegionMenu>()) return;
        //var template = GameObject.Find("NormalMenu/JoinGameButton/JoinGameMenu/GameIdText");
        //if (template == null) return;

        if (!ipField)
        {
            ipField = new GameObject("IPField").AddComponent<TextInputField>();
            ipField.transform.SetParent(__instance.transform);
            ipField.SetTextProperty(new Vector2(2.4f,0.3f),2f,TMPro.TextAlignmentOptions.Center,TMPro.FontStyles.Normal);
            ipField.AllowCharacters = (c) =>
            ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || ('0' <= c && c <= '9') || c is '?' or '!' or ',' or '.' or '/' or ':';
            
            ipField.transform.localPosition = new Vector3(0f, -1.75f, -100f);
            ipField.SetText(SaveIp.Value);

            ipField.LoseFocusAction = (text) =>
            {
                SaveIp.Value = text;
                UpdateRegions();
                ChooseOption(__instance, ServerManager.DefaultRegions[ServerManager.DefaultRegions.Length - 1]);
            };
        }

        if (!portField)
        {
            portField = new GameObject("PortField").AddComponent<TextInputField>();
            portField.transform.SetParent(__instance.transform);
            portField.SetTextProperty(new Vector2(2.4f, 0.3f), 2f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Normal);
            portField.AllowCharacters = (c) => '0' <= c && c <= '9';

            portField.transform.localPosition = new Vector3(0f, -2.4f, -100f);
            portField.SetText(SavePort.Value.ToString());

            portField.LoseFocusAction = (text) =>
            {
                SavePort.Value = ushort.TryParse(text, out var port) ? port : (ushort)22000;
                UpdateRegions();
                ChooseOption(__instance,ServerManager.DefaultRegions[ServerManager.DefaultRegions.Length - 1]);
            };
        }
    }
}


