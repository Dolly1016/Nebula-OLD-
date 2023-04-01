using Nebula.Module;
using UnityEngine;

namespace Nebula.Utilities;

public interface ISpriteLoader
{
    Sprite GetSprite();
}

public interface IOptionalSpriteLoader : ISpriteLoader
{
    bool HasSprite();
}

public class CustumizableSpriteLoader : ISpriteLoader
{
    ISpriteLoader originalLoader;
    IOptionalSpriteLoader optionalLoader;

    public Sprite GetSprite()
    {
        if (optionalLoader.HasSprite()) return optionalLoader.GetSprite();
        return originalLoader.GetSprite();
    }

    public CustumizableSpriteLoader(ISpriteLoader original,IOptionalSpriteLoader optional)
    {
        originalLoader = original;
        optionalLoader = optional;
    }
}

public class AssetSpriteLoader : ISpriteLoader
{
    NebulaAssetBundle assetBundle;
    Sprite sprite = null;
    string address;
    float pixelsPerUnit;

    public AssetSpriteLoader(NebulaAssetBundle assetBundle,string address,float pixelsPerUnit=100f)
    {
        this.assetBundle = assetBundle;
        this.address = address;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public Sprite GetSprite()
    {
        if (!sprite)
        {
            if (address.EndsWith(".png"))
            {
                sprite = Helpers.loadSpriteFromTexture(assetBundle.assetBundle.LoadAsset<Texture2D>(address), pixelsPerUnit);
            }
            else
            {
                sprite = assetBundle.assetBundle.LoadAsset<Sprite>(address);
            }
        }
        return sprite;
    }
}

public class ResourceSpriteLoader : ISpriteLoader
{
    Sprite sprite = null;
    string address;
    float pixelsPerUnit;

    public ResourceSpriteLoader(string address, float pixelsPerUnit)
    {
        this.address = address;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public Sprite GetSprite()
    {
        if (!sprite)
            sprite = Helpers.loadSpriteFromResources(address, pixelsPerUnit);
        return sprite;
    }
}

public class UserSpriteLoader : ISpriteLoader
{
    string? textureId = null;
    Module.CustomTextureAsset? textureAsset = null;
    float pixelsPerUnit;
    Sprite sprite;

    public UserSpriteLoader(string textureId, float pixelsPerUnit = 100f)
    {
        this.textureId = textureId;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public Sprite GetSprite()
    {
        if (!sprite && ((textureId != null && (textureAsset != null || TexturePack.LoadAsset(textureId, null, ref textureAsset)))))
            sprite = textureAsset.staticSprite;
        return sprite;
    }
}


public class SpriteLoader : ISpriteLoader
{
    string? address;
    string? textureId = null;
    Module.CustomTextureAsset? textureAsset = null;
    float pixelsPerUnit;
    Sprite sprite;

    public SpriteLoader(string address, float pixelsPerUnit)
    {
        this.address = address;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public SpriteLoader(string address, float pixelsPerUnit, string textureId)
    {
        this.address = address;
        this.textureId = textureId;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public SpriteLoader(string textureId)
    {
        this.address = null;
        this.textureId = textureId;
        this.pixelsPerUnit = 100f;
    }

    public SpriteLoader(Sprite sprite)
    {
        this.sprite = sprite;
    }

    public Sprite GetSprite()
    {
        if (!sprite)
        {
            if (textureId != null && (textureAsset != null || TexturePack.LoadAsset(textureId, null, ref textureAsset)))
                sprite = textureAsset.staticSprite;
            else if(address!=null)
                sprite = Helpers.loadSpriteFromResources(address, pixelsPerUnit);
        }
        return sprite;
    }
}

public class DividedSpriteLoader : ISpriteLoader
{
    string address;
    float pixelsPerUnit;
    Sprite[] sprites;
    Texture2D texture;
    int x, y;
    int sizeX, sizeY;

    public DividedSpriteLoader(string address, float pixelsPerUnit,int x,int y)
    {
        this.address = address;
        this.pixelsPerUnit = pixelsPerUnit;
        this.x = x;
        this.y = y;
        sprites = new Sprite[x * y];
        texture = null;
        this.sizeX = this.sizeY = 0;
    }

    public Sprite GetSprite(int index)
    {
        if (!texture) {
            texture = Helpers.loadTextureFromResources(address);
            sizeX = texture.width / x;
            sizeY = texture.height / y;
        }
        if (!sprites[index])
        {
            int _x = index % x;
            int _y = index / x;
            sprites[index] = Helpers.loadSpriteFromResources(texture, pixelsPerUnit, new Rect(_x * sizeX, -_y * sizeY, sizeX, sizeY));
        }
        return sprites[index];
    }

    public Sprite GetSprite() => GetSprite(0);
}