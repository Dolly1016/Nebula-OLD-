using Cpp2IL.Core.Extensions;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nebula;

[NebulaPreLoad]
public static class NebulaAsset
{
    static AssetBundle AssetBundle;
    public static void Load()
    {
        var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Assets.nebula_asset");
        AssetBundle = AssetBundle.LoadFromMemory(resourceStream.ReadBytes());

        MultiplyBackShader = AssetBundle.LoadAsset<Shader>("Sprites-MultiplyBackground").MarkDontUnload();
        StoreBackShader = AssetBundle.LoadAsset<Shader>("Sprites-StoreBackground").MarkDontUnload();

        DivMap[0] = AssetBundle.LoadAsset<GameObject>("SkeldDivMap").MarkDontUnload();
        DivMap[1] = AssetBundle.LoadAsset<GameObject>("MIRADivMap").MarkDontUnload();
        DivMap[2] = AssetBundle.LoadAsset<GameObject>("PolusDivMap").MarkDontUnload();
        DivMap[3] = null;
        DivMap[4] = AssetBundle.LoadAsset<GameObject>("AirshipDivMap").MarkDontUnload();
    }

    private static T LoadAsset<T>(this AssetBundle assetBundle, string name) where T : UnityEngine.Object
    {
        return assetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>();
    }

    public static T LoadAsset<T>(string name) where T : UnityEngine.Object
    {
        return AssetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>();
    }

    public static Sprite GetMapSprite(byte mapId, Int32 mask, Vector2? size = null)
    {
        GameObject prefab = DivMap[mapId];
        if (prefab == null) return null;
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

    static public Shader MultiplyBackShader { get; private set; }
    static public Shader StoreBackShader { get; private set; }

    public static GameObject[] DivMap { get; private set; } = new GameObject[5];

}
