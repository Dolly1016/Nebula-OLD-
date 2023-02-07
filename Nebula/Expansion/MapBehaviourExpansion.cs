using Nebula.Module;

namespace Nebula;

[HarmonyPatch]
public static class MapBehaviourExpansion
{
    static GameObject? TrackOverlay = null;
    static Sprite? defaultSprite;
    static Dictionary<Int32, Sprite> divSprite = new Dictionary<int, Sprite>();

    static public void Initialize()
    {
        defaultSprite = null;
        divSprite.Clear();
    }

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
        else
        {
            TrackOverlay.gameObject.SetActive(true);
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

    static public void EnmaskMap(Int32 mask)
    {
        Sprite? sprite = null;
        Sprite defaultSprite = ShipStatus.Instance.MapPrefab.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite;

        

        if (mask == Int32.MaxValue)
        {
            sprite = defaultSprite;
        }
        else if (!divSprite.TryGetValue(mask, out sprite))
        {
            sprite = AssetLoader.GetMapSprite(GameOptionsManager.Instance.CurrentGameOptions.MapId, defaultSprite.pivot * 2, mask);
            divSprite[mask] = sprite;
        }
        if (sprite != null)
        {
            MapBehaviour.Instance.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.GenericShow))]
    static class MapBehaviourGenericShowPatch
    {
        static void Postfix(MapBehaviour __instance)
        {
            EnmaskMap(Int32.MaxValue);
            __instance.ShowTrackOverlay();
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
    static class ToggleMapPatch
    {
        static void Postfix(MapBehaviour __instance)
        {
            if (TrackOverlay) TrackOverlay.SetActive(false);
        }
    }
}
