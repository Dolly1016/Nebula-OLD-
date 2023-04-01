using Nebula.Tasks;
using System.Reflection;
using UnityEngine;

namespace Nebula.Module;

public class NebulaAssetBundle
{
    public AssetBundle assetBundle { get; private set; }

    public NebulaAssetBundle(AssetBundle assetBundle)
    {
        this.assetBundle = assetBundle;
    }

    
}

public static class AssetLoader
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();

    public static NebulaAssetBundle NebulaMainAsset;

    public static AudioClip HadarDive;
    public static AudioClip HadarReappear;
    public static AudioClip HadarFear;

    public static AudioClip PlaceTrap2s;
    public static AudioClip PlaceTrap3s;
    public static AudioClip PlaceKillTrap;

    public static AudioClip SniperShot;
    public static AudioClip SniperEquip;
    public static AudioClip RaiderThrow;

    public static AudioClip Executioner;

    public static AudioClip SpectreFried;
    public static AudioClip SpectreStatueCrush0;
    public static AudioClip SpectreStatueCrush1;
    public static AudioClip SpectreStatueBroken;

    public static AudioClip Paparazzo;

    public static GameObject SkeldDivMap;
    public static GameObject MIRADivMap;
    public static GameObject PolusDivMap;
    public static GameObject AirshipDivMap;

    public static GameObject SpectreFriedMinigamePrefab;
    public static GameObject SpectreRancorMinigamePrefab;
    public static GameObject SpectreStatueMinigamePrefab;

    public static GameObject CameraFinderPrefab;
    
    public static GameObject MetaObjectPrefab;

    static public void Load()
    {
        var resourceStream = assembly.GetManifestResourceStream("Nebula.Resources.Assets.nebula_asset");
        var assetBundleBundle = AssetBundle.LoadFromMemory(resourceStream.ReadFully());
        NebulaMainAsset = new(assetBundleBundle);

        HadarDive = assetBundleBundle.LoadAsset<AudioClip>("HadarDive.wav").DontUnload();
        HadarReappear = assetBundleBundle.LoadAsset<AudioClip>("HadarReappear.wav").DontUnload();
        HadarReappear = assetBundleBundle.LoadAsset<AudioClip>("HadarFear.wav").DontUnload();

        PlaceTrap2s = assetBundleBundle.LoadAsset<AudioClip>("PlaceTrap2s.wav").DontUnload();
        PlaceTrap3s = assetBundleBundle.LoadAsset<AudioClip>("PlaceTrap3s.wav").DontUnload();
        PlaceKillTrap = assetBundleBundle.LoadAsset<AudioClip>("PlaceKillTrap.wav").DontUnload();

        SniperShot = assetBundleBundle.LoadAsset<AudioClip>("SniperShot.wav").DontUnload();
        SniperEquip = assetBundleBundle.LoadAsset<AudioClip>("SniperEquip.wav").DontUnload();
        RaiderThrow = assetBundleBundle.LoadAsset<AudioClip>("RaiderThrow.wav").DontUnload();

        Executioner = assetBundleBundle.LoadAsset<AudioClip>("Executioner.wav").DontUnload();

        SpectreFried = assetBundleBundle.LoadAsset<AudioClip>("SpectreFriedSE.ogg").DontUnload();
        SpectreStatueCrush0 = assetBundleBundle.LoadAsset<AudioClip>("StatueCrush0.ogg").DontUnload();
        SpectreStatueCrush1 = assetBundleBundle.LoadAsset<AudioClip>("StatueCrush1.ogg").DontUnload();
        SpectreStatueBroken = assetBundleBundle.LoadAsset<AudioClip>("StatueBroken.ogg").DontUnload();

        SkeldDivMap = assetBundleBundle.LoadAsset<GameObject>("SkeldDivMap").DontUnload();
        MIRADivMap = assetBundleBundle.LoadAsset<GameObject>("MIRADivMap").DontUnload();
        PolusDivMap = assetBundleBundle.LoadAsset<GameObject>("PolusDivMap").DontUnload();
        AirshipDivMap = assetBundleBundle.LoadAsset<GameObject>("AirshipDivMap").DontUnload();

        SpectreFriedMinigamePrefab = assetBundleBundle.LoadAsset<GameObject>("SpectreFriedMinigame").DontUnload();
        SpectreRancorMinigamePrefab = assetBundleBundle.LoadAsset<GameObject>("SpectreRancorMinigame").DontUnload();
        SpectreStatueMinigamePrefab = assetBundleBundle.LoadAsset<GameObject>("SpectreStatueMinigame").DontUnload();

        CameraFinderPrefab = assetBundleBundle.LoadAsset<GameObject>("CameraFinder").DontUnload();

        MetaObjectPrefab = assetBundleBundle.LoadAsset<GameObject>("MetaObjectPrefab").DontUnload();

        Paparazzo = assetBundleBundle.LoadAsset<AudioClip>("Camera").DontUnload();
    }

    public static Sprite GetMapSprite(byte mapId,Vector2 size,Int32 mask)
    {
        GameObject prefab;
        switch (mapId)
        {
            case 0:
                prefab = SkeldDivMap;
                break;
            case 1:
                prefab = MIRADivMap;
                break;
            case 2:
                prefab = PolusDivMap;
                break;
            case 4:
                prefab = AirshipDivMap;
                break;
            default:
                prefab= null;
                break;
        }
        if (prefab == null) return null;
        var obj = GameObject.Instantiate(prefab);
        Camera cam = obj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size.y/200;
        cam.transform.localScale = Vector3.one;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.cullingMask = 1 << 30;
        cam.enabled = true;

        try
        {
            int children = obj.transform.childCount;
            for (int i = 0; i < children; i++)
            {
                var c = obj.transform.GetChild(i);
                c.GetChild(0).gameObject.SetActive((mask & 1) == 0);
                c.GetChild(1).gameObject.SetActive((mask & 1) == 1);
                c.transform.localPosition += new Vector3(0, 0, 1);
                mask >>= 1;
            }
        }
        catch{
        }


        RenderTexture rt = new RenderTexture((int)size.x, (int)size.y, 16);
        rt.Create();

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = cam.targetTexture;
        Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, false);
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        GameObject.Destroy(rt);
        GameObject.Destroy(obj);


        return Helpers.loadSpriteFromResources(texture2D, 100f, new Rect(0, 0, texture2D.width, texture2D.height));
    }

    public static byte[] ReadFully(this Stream input)
    {
        using (var ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }
    public static T LoadAsset<T>(this AssetBundle assetBundle, string name) where T : UnityEngine.Object
    {
        return assetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>();
    }
    public static T DontUnload<T>(this T obj) where T : UnityEngine.Object
    {
        obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;

        return obj;
    }

    public static AudioClip GetAudioClip(Module.AudioAsset id)
    {
        switch (id)
        {
            case AudioAsset.HadarDive:
                return HadarDive;
            case AudioAsset.HadarReappear:
                return HadarReappear;
            case AudioAsset.HadarFear:
                return HadarFear;

            case AudioAsset.PlaceTrap2s:
                return PlaceTrap2s;
            case AudioAsset.PlaceTrap3s:
                return PlaceTrap3s;
            case AudioAsset.PlaceKillTrap:
                return PlaceKillTrap;

            case AudioAsset.SniperShot:
                return SniperShot;
            case AudioAsset.SniperEquip:
                return SniperEquip;
            case AudioAsset.RaiderThrow:
                return RaiderThrow;

            case AudioAsset.Executioner:
                return Executioner;

            case AudioAsset.SpectreFried:
                return SpectreFried;
            case AudioAsset.SpectreStatueCrush0:
                return SpectreStatueCrush0;
            case AudioAsset.SpectreStatueCrush1:
                return SpectreStatueCrush1;
            case AudioAsset.SpectreStatueBroken:
                return SpectreStatueBroken;

            case AudioAsset.Paparazzo:
                return Paparazzo;
        }

        return null;
    }
}

public enum AudioAsset
{
    HadarDive = 0,
    HadarReappear,
    HadarFear,
    PlaceTrap2s,
    PlaceTrap3s,
    PlaceKillTrap,
    SniperShot,
    SniperEquip,
    RaiderThrow,
    Executioner,
    SpectreFried,
    SpectreStatueCrush0,
    SpectreStatueCrush1,
    SpectreStatueBroken,
    Paparazzo

}