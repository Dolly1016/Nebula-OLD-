using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime;
using Il2CppSystem.Web.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using Cpp2IL.Core.Extensions;

namespace Nebula.Utilities;

public interface ITextureLoader
{
    Texture2D GetTexture();
}

public interface ISpriteLoader
{
    Sprite GetSprite();
}

public interface IDividedSpriteLoader
{
    Sprite GetSprite(int index);
    int Length { get; }
}

public static class GraphicsHelper
{
    public static Texture2D LoadTextureFromResources(string path)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream(path);
        var byteTexture = new byte[stream.Length];
        var read = stream.Read(byteTexture, 0, (int)stream.Length);
        LoadImage(texture, byteTexture, false);
        return texture;
    }

    public static Texture2D LoadTextureFromStream(Stream stream)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
        LoadImage(texture, stream.ReadBytes(), false);
        return texture;
    }

    public static Texture2D LoadTextureFromDisk(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                byte[] byteTexture = File.ReadAllBytes(path);
                LoadImage(texture, byteTexture, false);
                return texture;
            }
        }
        catch
        {
            //System.Console.WriteLine("Error loading texture from disk: " + path);
        }
        return null;
    }

    public static Texture2D LoadTextureFromZip(ZipArchive? zip, string path)
    {
        if (zip == null) return null;
        try
        {
            var entry = zip.GetEntry(path);
            if (entry != null)
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                Stream stream = entry.Open();
                byte[] byteTexture = new byte[entry.Length];
                stream.Read(byteTexture, 0, byteTexture.Length);
                stream.Close();
                LoadImage(texture, byteTexture, false);
                return texture;
            }
        }
        catch
        {
            System.Console.WriteLine("Error loading texture from disk: " + path);
        }
        return null;
    }

    public static Sprite ToSprite(this Texture2D texture, float pixelsPerUnit) => ToSprite(texture, new Rect(0, 0, texture.width, texture.height),pixelsPerUnit);

    public static Sprite ToSprite(this Texture2D texture, Rect rect, float pixelsPerUnit)
    {
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    public static Sprite ToSprite(this Texture2D texture, Rect rect, Vector2 pivot,float pixelsPerUnit)
    {
        return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
    }

    public static Sprite ToExpandableSprite(this Texture2D texture, float pixelsPerUnit)
    {
        return Sprite.CreateSprite(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(texture.width/3f, texture.height / 3f, texture.width/3f, texture.height/3f), false);
    }

    internal delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
    internal static d_LoadImage iCall_LoadImage;
    private static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
    {
        if (iCall_LoadImage == null)
            iCall_LoadImage = IL2CPP.ResolveICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage");
        var il2cppArray = (Il2CppStructArray<byte>)data;
        return iCall_LoadImage.Invoke(tex.Pointer, il2cppArray.Pointer, markNonReadable);
    }
}

public class ResourceTextureLoader : ITextureLoader
{
    string address;
    Texture2D texture = null;

    public ResourceTextureLoader(string address)
    {
        this.address = address;
    }

    public Texture2D GetTexture()
    {
        if (!texture) texture = GraphicsHelper.LoadTextureFromResources(address);
        return texture;
    }
}

public class DiskTextureLoader : ITextureLoader
{
    string address;
    Texture2D texture = null;
    bool isUnloadAsset = false;

    public DiskTextureLoader MarkAsUnloadAsset() { isUnloadAsset = true; return this; }
    public DiskTextureLoader(string address)
    {
        this.address = address;
    }

    public Texture2D GetTexture()
    {
        if (!texture)
        {
            texture = GraphicsHelper.LoadTextureFromDisk(address);
            if (isUnloadAsset) texture.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        }
        return texture;
    }
}

public class ZipTextureLoader : ITextureLoader
{
    string zipAddress;
    string address;
    Texture2D texture = null;

    public ZipTextureLoader(string zipPath,string address)
    {
        this.zipAddress = zipPath;
        this.address = address;
    }

    public Texture2D GetTexture()
    {
        if (!texture)
        {
            texture = GraphicsHelper.LoadTextureFromZip(ZipFile.OpenRead(zipAddress), address);
            if(texture!=null) texture.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        }
        return texture;
    }
}

public class AssetTextureLoader : ITextureLoader
{
    string address;
    Texture2D texture = null;

    public AssetTextureLoader(string address)
    {
        this.address = address;
    }

    public Texture2D GetTexture()
    {
        if (!texture) texture = NebulaAsset.LoadAsset<Texture2D>(address);
        return texture;
    }
}

public class SpriteLoader : ISpriteLoader
{
    Sprite sprite = null;
    float pixelsPerUnit;
    ITextureLoader textureLoader;
    public SpriteLoader(ITextureLoader textureLoader, float pixelsPerUnit)
    {
        this.textureLoader=textureLoader;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public Sprite GetSprite()
    {
        if (!sprite) sprite = textureLoader.GetTexture().ToSprite(pixelsPerUnit);
        sprite.hideFlags = textureLoader.GetTexture().hideFlags;
        return sprite;
    }

    static public SpriteLoader FromResource(string address, float pixelsPerUnit) => new SpriteLoader(new ResourceTextureLoader(address), pixelsPerUnit);
}

public class ResourceExpandableSpriteLoader : ISpriteLoader
{
    Sprite sprite = null;
    string address;
    float pixelsPerUnit;

    public ResourceExpandableSpriteLoader(string address, float pixelsPerUnit)
    {
        this.address = address;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public Sprite GetSprite()
    {
        if (!sprite)
            sprite = GraphicsHelper.LoadTextureFromResources(address).ToExpandableSprite(pixelsPerUnit);
        return sprite;
    }
}


public class DividedSpriteLoader : ISpriteLoader, IDividedSpriteLoader
{
    float pixelsPerUnit;
    Sprite[] sprites;
    ITextureLoader texture;
    Tuple<int, int>? division, size;
    public Vector2 Pivot = new Vector2(0.5f, 0.5f);

    public DividedSpriteLoader(ITextureLoader textureLoader, float pixelsPerUnit, int x, int y, bool isSize = false)
    {
        this.pixelsPerUnit = pixelsPerUnit;
        if (isSize)
        {
            this.size = new(x, y);
            this.division = null;
        }
        else
        {
            this.division = new(x, y);
            this.size = null;
        }
        sprites = null;
        texture = textureLoader;
    }

    public Sprite GetSprite(int index)
    {
        if (size == null || division == null || sprites == null)
        {
            var texture2D = texture.GetTexture();
            if (size == null)
                size = new(texture2D.width / division.Item1, texture2D.height / division.Item2);
            else if (division == null)
                division = new(texture2D.width / size.Item1, texture2D.height / size.Item2);
            sprites = new Sprite[division.Item1 * division.Item2];
        }

        if (!sprites[index])
        {
            int _x = index % division.Item1;
            int _y = index / division.Item1;
            sprites[index] = texture.GetTexture().ToSprite(new Rect(_x * size.Item1, -_y * size.Item2, size.Item1, -size.Item2), Pivot, pixelsPerUnit);
        }
        return sprites[index];
    }

    public Sprite GetSprite() => GetSprite(0);

    public ISpriteLoader WrapLoader(int index) => new WrapSpriteLoader(() => GetSprite(index));

    public int Length {
        get {
            if (division == null) GetSprite(0);
            return division.Item1 * division.Item2;
        }
    }

    static public DividedSpriteLoader FromResource(string address, float pixelsPerUnit, int x, int y, bool isSize = false)
         => new DividedSpriteLoader(new ResourceTextureLoader(address), pixelsPerUnit, x, y, isSize);
    static public DividedSpriteLoader FromDisk(string address, float pixelsPerUnit, int x, int y, bool isSize = false)
         => new DividedSpriteLoader(new DiskTextureLoader(address), pixelsPerUnit, x, y, isSize);
}

public class XOnlyDividedSpriteLoader : ISpriteLoader, IDividedSpriteLoader
{
    float pixelsPerUnit;
    Sprite[] sprites;
    ITextureLoader texture;
    int? division, size;
    public Vector2 Pivot = new Vector2(0.5f, 0.5f);

    public XOnlyDividedSpriteLoader(ITextureLoader textureLoader, float pixelsPerUnit, int x, bool isSize = false)
    {
        this.pixelsPerUnit = pixelsPerUnit;
        if (isSize)
        {
            this.size = x;
            this.division = null;
        }
        else
        {
            this.division = x;
            this.size = null;
        }
        sprites = null;
        texture = textureLoader;
    }

    public Sprite GetSprite(int index)
    {
        if (!size.HasValue || !division.HasValue || sprites == null)
        {
            var texture2D = texture.GetTexture();
            if (size == null)
                size = texture2D.width / division;
            else if (division == null)
                division = texture2D.width / size;
            sprites = new Sprite[division.Value];
        }

        if (!sprites[index])
        {
            var texture2D = texture.GetTexture();
            sprites[index] = texture2D.ToSprite(new Rect(index * size.Value, 0, size.Value, texture2D.height), Pivot, pixelsPerUnit);
        }
        return sprites[index];
    }

    public Sprite GetSprite() => GetSprite(0);

    public int Length
    {
        get
        {
            if (!division.HasValue) GetSprite(0);
            return division.Value;
        }
    }

    public ISpriteLoader WrapLoader(int index) => new WrapSpriteLoader(() => GetSprite(index));

    static public XOnlyDividedSpriteLoader FromResource(string address, float pixelsPerUnit, int x, bool isSize = false)
         => new XOnlyDividedSpriteLoader(new ResourceTextureLoader(address), pixelsPerUnit, x, isSize);
    static public XOnlyDividedSpriteLoader FromDisk(string address, float pixelsPerUnit, int x, bool isSize = false)
         => new XOnlyDividedSpriteLoader(new DiskTextureLoader(address), pixelsPerUnit, x, isSize);
}

public class WrapSpriteLoader : ISpriteLoader
{
    Func<Sprite> supplier;

    public WrapSpriteLoader(Func<Sprite> supplier)
    {
        this.supplier = supplier;
    }

    public Sprite GetSprite() => supplier.Invoke();
}