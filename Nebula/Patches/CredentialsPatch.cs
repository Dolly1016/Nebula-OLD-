namespace Nebula.Patches;

[HarmonyPatch]
public static class CredentialsPatch
{
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    private static class VersionShowerPatch
    {
        static void Postfix(VersionShower __instance)
        {
            var amongUsLogo = GameObject.Find("bannerLogo_AmongUs");
            if (amongUsLogo == null) return;

            var credentials = UnityEngine.Object.Instantiate<TMPro.TextMeshPro>(__instance.text);
            credentials.transform.position = new Vector3(0, -0.6f, 0);

            if (Nebula.NebulaPlugin.PluginStage != null)
            {
                credentials.SetText(Nebula.NebulaPlugin.PluginStage + " v" + Nebula.NebulaPlugin.PluginVisualVersion);
            }
            else
            {
                credentials.SetText($"v{Nebula.NebulaPlugin.PluginVisualVersion}");
            }
            credentials.alignment = TMPro.TextAlignmentOptions.Center;
            credentials.fontSize *= 0.75f;

            credentials.transform.SetParent(amongUsLogo.transform);
        }
    }

    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    public static class PingTrackerPatch
    {
        public static GameObject modStamp { get; private set; }

        static void Prefix(PingTracker __instance)
        {
            if (modStamp == null)
            {
                modStamp = new GameObject("ModStamp");
                modStamp.layer = UnityEngine.LayerMask.NameToLayer("UI");
                var rend = modStamp.AddComponent<SpriteRenderer>();
                rend.sprite = NebulaPlugin.GetModStamp();
                rend.color = new Color(1, 1, 1, 0.5f);
                modStamp.transform.SetParent(__instance.transform);
                modStamp.transform.localScale *= 0.6f;
            }
            float offset = (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started) ? 0.75f : 0f;
            modStamp.transform.position = FastDestroyableSingleton<HudManager>.Instance.MapButton.transform.position + Vector3.down * offset;
        }

        static void Postfix(PingTracker __instance)
        {
            __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;
            __instance.text.text = $"<size=130%><color=#9579ce>Nebula on the Ship</color></size> v" + NebulaPlugin.PluginVisualVersion + "\n" + __instance.text.text;
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
            {
                __instance.gameObject.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(1.2f, 0.8f, 0f);
            }
            else if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data.IsDead)
            {
                __instance.gameObject.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(2.0f, 0.1f, 0f);
            }
            else
            {
                __instance.gameObject.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(1.2f, 0.1f, 0f);
            }
            __instance.gameObject.GetComponent<AspectPosition>().AdjustPosition();
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    private static class LogoPatch
    {
        static void Postfix(MainMenuManager __instance)
        {
            var amongUsLogo = GameObject.Find("bannerLogo_AmongUs");
            if (amongUsLogo != null)
            {
                amongUsLogo.transform.localScale *= 0.6f;
                amongUsLogo.transform.position += new Vector3(0f, -0.1f, 0f);
            }

            var nebulaLogo = new GameObject("bannerLogo_Nebula");
            nebulaLogo.transform.position = new Vector3(0f, 0.4f, 0f);
            var renderer = nebulaLogo.AddComponent<SpriteRenderer>();
            renderer.sprite = Helpers.loadSpriteFromResources("Nebula.Resources.Logo.png", 115f);

            GameObject.Find("PlayOnlineButton").transform.position = new Vector3(1.025f, -1.5f, 0f);
            GameObject.Find("PlayLocalButton").transform.position = new Vector3(-1.025f, -1.5f, 0f);
            GameObject.Find("HowToPlayButton").active = false;
            GameObject.Find("FreePlayButton").active = false;
        }
    }
}