using Nebula.Module;

namespace Nebula.Map;

public class SpawnCandidate
{
    static private Il2CppArrayBase<UnityEngine.Object>? audioClips = null;

    public Vector2 SpawnLocation;
    public Texture2D Texture;
    public Sprite[] Sprites;
    public string TextureAddress;
    public string LocationKey;
    public AudioClip? AudioClip;
    public string? AudioClipName;
    public int spriteWidth = 200;
    public float pixelsPerUnit = 100f;

    public Texture2D GetTexture()
    {
        if (Texture) return Texture;
        Texture = AssetLoader.NebulaMainAsset.assetBundle.LoadAsset<Texture2D>(TextureAddress);
        ReloadSprites();
        return Texture;
    }

    public void ReloadTexture()
    {
        if (!Texture) Texture = AssetLoader.NebulaMainAsset.assetBundle.LoadAsset<Texture2D>(TextureAddress);
        ReloadSprites();
    }

    public void ReloadSprites()
    {
        foreach (Sprite sprite in Sprites)
        {
            if (sprite) UnityEngine.Object.Destroy(sprite);
        }
        Sprites = new Sprite[Texture.width / spriteWidth];

        for (int i = 0; i < Sprites.Length; i++)
        {
            Sprites[i] = Helpers.loadSpriteFromResources(Texture, pixelsPerUnit, new Rect((float)(i * spriteWidth), 0f, spriteWidth, Texture.height), new Vector2(0.5f, 0f));
        }
    }

    public Sprite GetSprite()
    {
        GetTexture();
        if (Sprites.Length > 0) return Sprites[0];
        return null;
    }

    public AudioClip? GetAudioClip()
    {
        if(AudioClip) return AudioClip;

        if (AudioClipName == null) return null;

        if (audioClips == null) audioClips = UnityEngine.Object.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());

        if (AudioClip == null) AudioClip = (audioClips.FirstOrDefault<UnityEngine.Object>((audio) => audio && audio.name == AudioClipName)).TryCast<AudioClip>();
        return AudioClip;
    }

    public Il2CppSystem.Collections.IEnumerator GetEnumerator(SpriteRenderer renderer)
    {
        GetTexture();
        return Effects.Lerp(Sprites.Length * 0.06f, new Action<float>((t) =>
        {
            if (!renderer) return;
            int num = (int)(t * Sprites.Length);
            if (num < Sprites.Length) renderer.sprite = Sprites[num];
        }));
    }

    public SpawnCandidate(string locationKey, Vector2 location, string textureAddress, string? audioClip, float pixelsPerUnit = 100f,int spriteWidth = 200)
    {
        SpawnLocation = location;
        LocationKey = locationKey;
        TextureAddress = textureAddress;
        Sprites = new Sprite[0];

        AudioClip = null;
        AudioClipName = audioClip;

        this.spriteWidth = spriteWidth;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public SpawnCandidate(string locationKey, string textureAddress, int origIndex,int spriteWidth=200)
    {
        LocationKey = locationKey;
        TextureAddress = textureAddress;
        this.spriteWidth = spriteWidth;
        Sprites = new Sprite[0];

        NebulaEvents.OnMapAssetLoaded += () =>
        {
            var loc = MapData.MapDatabase[4].Assets.CastFast<AirshipStatus>().SpawnInGame.Locations[origIndex];
            SpawnLocation = loc.Location;

            AudioClip = loc.RolloverSfx;
        };
    }
}