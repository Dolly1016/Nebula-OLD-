namespace Nebula;

[HarmonyPatch]
public static class MapBehaviourExpansion
{
    static GameObject? TrackOverlay = null;

    static public GameObject? GetTrackOverlay(this MapBehaviour mapBehaviour)
    {
        return TrackOverlay;
    }

    static public void ShowTrackOverlay(this MapBehaviour mapBehaviour)
    {
        if (!TrackOverlay)
        {
            TrackOverlay = new GameObject("TrackOverlay");
            TrackOverlay.transform.SetParent(mapBehaviour.transform);
            TrackOverlay.transform.localScale = new Vector3(1f, 1f, 1f);
            TrackOverlay.transform.localPosition = mapBehaviour.HerePoint.transform.parent.localPosition;
        }
    }

    static public Vector3 ConvertMapLocalPosition(Vector3 position, byte order)
    {
        Vector3 vector = position;
        vector /= ShipStatus.Instance.MapScale;
        vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
        vector.z = -1f - 0.01f * (float)order;
        return vector;
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.GenericShow))]
    static class MapBehaviourGenericShowPatch
    {
        static void Postfix(MapBehaviour __instance)
        {
            __instance.ShowTrackOverlay();
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
    static class MapBehaviourShowCountOverlayPatch
    {
        static void Postfix(MapBehaviour __instance)
        {
            if (TrackOverlay) TrackOverlay.SetActive(false);
        }
    }
}
