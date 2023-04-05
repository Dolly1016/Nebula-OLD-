using BepInEx.Configuration;
using UnityEngine.UI;
using UnityEngine.Events;
using Discord;

namespace Nebula.Patches;

/* [HarmonyPatch(typeof(ServerInfo), nameof(ServerInfo.HttpUrl),MethodType.Getter)]
public static class HttpConvertPatch
{
    public static bool Prefix(ServerInfo __instance,ref string __result)
    {
        if (__instance.Port != 22000) return true;

        __result = string.Format("http://{0}:{1}/", __instance.Ip, __instance.Port);
        return false;
    }
} */

[HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
public static class RegionMenuOpenPatch
{
    private static ConfigEntry<string> SaveIp;
    private static ConfigEntry<ushort> SavePort;

    private static TextBoxTMP? ipField = null;
    private static TextBoxTMP? portField = null;

    public static IRegionInfo[] defaultRegions;
    public static void UpdateRegions()
    {
        ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;
        IRegionInfo[] regions = defaultRegions;
        if (SavePort.Value == 22000 || SavePort.Value == 443)
        {
            var httpIp = (SavePort.Value == 22000 ? "http://" : "https://") + SaveIp.Value;
            var CustomRegion = new StaticHttpRegionInfo("Custom", StringNames.NoTranslation, httpIp,new ServerInfo[]{new ServerInfo("Custom", httpIp, SavePort.Value, false)});
            regions = regions.Concat(new IRegionInfo[] { CustomRegion.Cast<IRegionInfo>() }).ToArray();
        }
        else
        {
            var CustomRegion = new DnsRegionInfo(SaveIp.Value, "Custom", StringNames.NoTranslation, SaveIp.Value, SavePort.Value, false);
            regions = regions.Concat(new IRegionInfo[] { CustomRegion.Cast<IRegionInfo>() }).ToArray();
        }
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

    public static void Postfix(RegionMenu __instance)
    {

        var template = GameObject.Find("NormalMenu/JoinGameButton/JoinGameMenu/GameIdText");
        if (template == null) return;

        if (ipField == null || ipField.gameObject == null)
        {
            ipField = UnityEngine.Object.Instantiate(template.gameObject, __instance.transform).GetComponent<TextBoxTMP>();
            ipField.gameObject.name = "IpTextBox";
            var arrow = ipField.transform.FindChild("arrowEnter");
            if (arrow == null || arrow.gameObject == null) return;
            UnityEngine.Object.DestroyImmediate(arrow.gameObject);

            ipField.transform.localPosition = new Vector3(3f, 1.5f, -100f);
            ipField.characterLimit = 30;
            ipField.AllowSymbols = true;
            ipField.ForceUppercase = false;
            ipField.SetText(SaveIp.Value);
            __instance.StartCoroutine(Effects.Lerp(0.1f, new Action<float>((p) =>
            {
                ipField.outputText.SetText(SaveIp.Value);
                ipField.SetText(SaveIp.Value);
            })));

            ipField.ClearOnFocus = false;
            ipField.OnEnter = ipField.OnChange = new Button.ButtonClickedEvent();
            ipField.OnFocusLost = new Button.ButtonClickedEvent();
            ipField.OnChange.AddListener((UnityAction)onEnterOrIpChange);
            ipField.OnFocusLost.AddListener((UnityAction)onFocusLost);

            void onEnterOrIpChange()
            {
                SaveIp.Value = ipField.text;
            }

            void onFocusLost()
            {
                UpdateRegions();
                __instance.ChooseOption(ServerManager.DefaultRegions[ServerManager.DefaultRegions.Length - 1]);
            }
        }

        if (portField == null || portField.gameObject == null)
        {
            portField = UnityEngine.Object.Instantiate(template.gameObject, __instance.transform).GetComponent<TextBoxTMP>();
            portField.gameObject.name = "PortTextBox";
            var arrow = portField.transform.FindChild("arrowEnter");
            if (arrow == null || arrow.gameObject == null) return;
            UnityEngine.Object.DestroyImmediate(arrow.gameObject);

            portField.transform.localPosition = new Vector3(3f, 0.5f, -100f);
            portField.characterLimit = 5;
            portField.SetText(SavePort.Value.ToString());
            __instance.StartCoroutine(Effects.Lerp(0.1f, new Action<float>((p) =>
            {
                portField.outputText.SetText(SavePort.Value.ToString());
                portField.SetText(SavePort.Value.ToString());
            })));


            portField.ClearOnFocus = false;
            portField.OnEnter = portField.OnChange = new Button.ButtonClickedEvent();
            portField.OnFocusLost = new Button.ButtonClickedEvent();
            portField.OnChange.AddListener((UnityAction)onEnterOrPortFieldChange);
            portField.OnFocusLost.AddListener((UnityAction)onFocusLost);

            void onEnterOrPortFieldChange()
            {
                ushort port = 0;
                if (ushort.TryParse(portField.text, out port))
                {
                    SavePort.Value = port;
                    portField.outputText.color = Color.white;
                }
                else
                {
                    portField.outputText.color = Color.red;
                }
            }

            void onFocusLost()
            {
                UpdateRegions();
                __instance.ChooseOption(ServerManager.DefaultRegions[ServerManager.DefaultRegions.Length - 1]);
            }
        }
    }
}


