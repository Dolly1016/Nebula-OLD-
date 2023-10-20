using BepInEx.Unity.IL2CPP.Utils;
using Cpp2IL.Core.Extensions;
using Il2CppSystem.Linq;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula;

public enum NebulaAudioClip { 
    ThrowAxe,
    SniperShot,
    SniperEquip,
    Trapper2s,
    Trapper3s,
    TrapperKillTrap
}

[NebulaPreLoad]
[NebulaRPCHolder]
public static class NebulaAsset
{
    static AssetBundle AssetBundle = null!;
    public static IEnumerator CoLoad()
    {
        Patches.LoadPatch.LoadingText = "Loading Map Expansions";
        yield return null;

        var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Assets.nebula_asset");
        AssetBundle = AssetBundle.LoadFromMemory(resourceStream!.ReadBytes());

        MultiplyBackShader = AssetBundle.LoadAsset<Shader>("Sprites-MultiplyBackground").MarkDontUnload();
        StoreBackShader = AssetBundle.LoadAsset<Shader>("Sprites-StoreBackground").MarkDontUnload();
        GuageShader = AssetBundle.LoadAsset<Shader>("Sprites-Guage").MarkDontUnload();

        DivMap[0] = AssetBundle.LoadAsset<GameObject>("SkeldDivMap").MarkDontUnload();
        DivMap[1] = AssetBundle.LoadAsset<GameObject>("MIRADivMap").MarkDontUnload();
        DivMap[2] = AssetBundle.LoadAsset<GameObject>("PolusDivMap").MarkDontUnload();
        DivMap[3] = null!;
        DivMap[4] = AssetBundle.LoadAsset<GameObject>("AirshipDivMap").MarkDontUnload();

        audioMap[NebulaAudioClip.ThrowAxe] = AssetBundle.LoadAsset<AudioClip>("RaiderThrow.wav").MarkDontUnload();
        audioMap[NebulaAudioClip.SniperShot] = AssetBundle.LoadAsset<AudioClip>("SniperShot.wav").MarkDontUnload();
        audioMap[NebulaAudioClip.SniperEquip] = AssetBundle.LoadAsset<AudioClip>("SniperEquip.wav").MarkDontUnload();
        audioMap[NebulaAudioClip.Trapper2s] = AssetBundle.LoadAsset<AudioClip>("PlaceTrap2s.wav").MarkDontUnload();
        audioMap[NebulaAudioClip.Trapper3s] = AssetBundle.LoadAsset<AudioClip>("PlaceTrap3s.wav").MarkDontUnload();
        audioMap[NebulaAudioClip.TrapperKillTrap] = AssetBundle.LoadAsset<AudioClip>("PlaceKillTrap.wav").MarkDontUnload();

        PaparazzoShot = AssetBundle.LoadAsset<GameObject>("PhotoObject").MarkDontUnload().AddComponent<Roles.Neutral.PaparazzoShot>();
    }

    private static T LoadAsset<T>(this AssetBundle assetBundle, string name) where T : UnityEngine.Object
    {
        return assetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>()!;
    }

    public static T LoadAsset<T>(string name) where T : UnityEngine.Object
    {
        return AssetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>()!;
    }

    public static Sprite GetMapSprite(byte mapId, Int32 mask, Vector2? size = null)
    {
        GameObject prefab = DivMap[mapId];
        if (prefab == null) return null!;
        if (size == null) size = prefab.transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size * 100f;
        var obj = GameObject.Instantiate(prefab);
        Camera cam = obj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size.Value.y / 200;
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
        catch
        {
        }


        RenderTexture rt = new RenderTexture((int)size.Value.x, (int)size.Value.y, 16);
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

        return texture2D.ToSprite(100f);
    }

    static public Shader MultiplyBackShader { get; private set; } = null!;
    static public Shader StoreBackShader { get; private set; } = null!;
    static public Shader GuageShader { get; private set; } = null!;

    static public ResourceExpandableSpriteLoader SharpWindowBackgroundSprite = new("Nebula.Resources.StatisticsBackground.png", 100f,5,5);
    static public Roles.Neutral.PaparazzoShot PaparazzoShot { get; private set; } = null!;

    static public SpriteRenderer CreateSharpBackground(Vector2 size, Color color, Transform transform)
    {
        var renderer = UnityHelper.CreateObject<SpriteRenderer>("Background", transform, new Vector3(0, 0, 0.25f));
        return CreateSharpBackground(renderer, color, size);
    }

    static public SpriteRenderer CreateSharpBackground(SpriteRenderer renderer, Color color, Vector2 size)
    {
        renderer.sprite = NebulaAsset.SharpWindowBackgroundSprite.GetSprite();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.color = color;
        renderer.size = size;
        return renderer;
    }

    public static GameObject[] DivMap { get; private set; } = new GameObject[5];
    private static Dictionary<NebulaAudioClip, AudioClip> audioMap = new();

    public static void PlaySE(NebulaAudioClip clip)
    {
        SoundManager.Instance.PlaySound(audioMap[clip],false,0.8f);
    }

    public static void PlaySE(NebulaAudioClip clip,Vector2 pos,float minDistance,float maxDistance)
    {
        var audioSource = UnityHelper.CreateObject<AudioSource>("SEPlayer", null, pos);

        float v = (SoundManager.SfxVolume + 80) / 80f;
        v = 1f - v;
        v = v * v;
        v = 1f - v;
        audioSource.volume = v;

        audioSource.transform.position = pos;
        audioSource.priority = 0;
        audioSource.spatialBlend = 1;
        audioSource.clip = audioMap[clip];
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.maxDistance = maxDistance;
        audioSource.minDistance = minDistance;
        audioSource.rolloffMode = UnityEngine.AudioRolloffMode.Linear;
        audioSource.PlayOneShot(audioSource.clip);

        IEnumerator CoPlay()
        {
            yield return new WaitForSeconds(audioSource.clip.length);
            while (audioSource.isPlaying) yield return null;
            GameObject.Destroy(audioSource.gameObject);
            yield break;
        }

        NebulaManager.Instance.StartCoroutine(CoPlay().WrapToIl2Cpp());
    }

    public static readonly RemoteProcess<(NebulaAudioClip clip, Vector2 pos, float minDistance, float maxDistance)> RpcPlaySE = new(
        "PlaySE",
        (message, _) => PlaySE(message.clip, message.pos, message.minDistance, message.maxDistance)
    );
}
