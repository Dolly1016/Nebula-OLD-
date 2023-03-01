using Nebula.Map;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Nebula.Module;

public class CustomTextureAsset
{
    public Sprite? staticSprite = null;
    public Sprite[]? animation = null;
    public bool isLoop = false;
    public bool terminateEmptyFrame = false;
    public int loopBegin = 0;
    public float frame =0.2f;
    public Vector3 pos = Vector3.zero;
    public int layer = 0;

    public void Discard()
    {
        if (staticSprite != null) staticSprite.hideFlags = HideFlags.None;
        if (animation != null) foreach (var s in animation) s.hideFlags = HideFlags.None;
    }
}

public class CustomTextureHandler : MonoBehaviour
{
    static CustomTextureHandler()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomTextureHandler>();        
    }

    public Il2CppValueField<int> TextureAssetId;
    CustomTextureAsset? textureAsset = null;
    float proceedTime = 0f;
    int animState = 0;
    SpriteRenderer? renderer = null;

    public void Start()
    {
        TexturePack.AllTextureAssets.TryGetValue(TextureAssetId.Get(), out textureAsset);

        renderer = GetComponent<SpriteRenderer>();
        if (!renderer) {
            renderer = null;
            return;
        }

        if (textureAsset == null) return;
        if (textureAsset.staticSprite == null && textureAsset.animation == null) return;

        var animator = gameObject.GetComponent<Animator>();
        if(animator)animator.enabled = false;

        if (textureAsset.animation != null)
        {
            renderer.sprite = textureAsset.animation[0];
        }else if (textureAsset.staticSprite != null)
            renderer.sprite= textureAsset.staticSprite;
    }

    public void Update()
    {
        if(textureAsset==null || renderer==null || textureAsset.animation == null) return;

        if (animState >= textureAsset.animation.Length) return;

        proceedTime += Time.deltaTime;
        if (textureAsset.frame < proceedTime)
        {
            proceedTime = 0;
            animState++;
            if (animState >= textureAsset.animation.Length)
            {
                if (!textureAsset.isLoop)
                {
                    if (textureAsset.staticSprite != null)
                        renderer.sprite = textureAsset.staticSprite;
                    else if(textureAsset.terminateEmptyFrame)
                        renderer.sprite = null;
                    return;
                }
                animState = textureAsset.loopBegin;
            }
            
            renderer.sprite = textureAsset.animation[animState];
        }
    }

    public void SetUpTextureAsset(CustomTextureAsset asset)
    {
        textureAsset = asset;
    }
}

public interface TexturePackLoader
{
    public Stream? GetStream();
    public Texture2D? LoadTexture(string imageId);
}

public class RawTexturePackLoader : TexturePackLoader
{
    string prefix;

    public RawTexturePackLoader()
    {
        prefix = "TexturePack/";
    }

    public Stream? GetStream()
    {
        if (!File.Exists(prefix + "TextureInfo.pack")) return null;

        return File.OpenRead(prefix + "TextureInfo.pack");
    }

    public Texture2D? LoadTexture(string imageId)
    {
        return Helpers.loadTextureFromDisk(prefix + imageId.Replace(".", "/") + ".png");
    }
}

public class ZipTexturePackLoader : TexturePackLoader
{
    string prefix = "";
    ZipArchive zipArchive;

    public ZipTexturePackLoader(ZipArchive zipArchive)
    {
        this.zipArchive = zipArchive;
    }

    public Stream? GetStream()
    {
        ZipArchiveEntry? infoEntry = null;

        //直下にファイルがあるか調べる
        infoEntry = zipArchive.GetEntry("TextureInfo.pack");
        if (infoEntry == null)
        {
            //直下に無い場合
            foreach (var entry in zipArchive.Entries)
            {
                if (entry.Name != "TextureInfo.pack") continue;
                infoEntry = entry;
                prefix = entry.FullName.Substring(0, entry.FullName.Length - entry.Name.Length);
                break;
            }
        }

        return infoEntry?.Open();
    }

    public Texture2D? LoadTexture(string imageId)
    {
        return Helpers.loadTextureFromZip(zipArchive, prefix + imageId.Replace(".", "/") + ".png");
    }
}

public static class TexturePack
{
    public class TexturePackData {
        public bool? BoolVal = null;
        public float? floatVal = null;
        public int? xVal = null, yVal = null;
        public int fpsVal = 30;
        public bool? isLoopVal = null;
        public bool? terminateEmptyFrame = null;
        public int? loopRange = null;
        public Texture2D? texture = null;
        public Vector3? vectorVal = null;
        public bool permanentVal = true;
        public bool shadowyVal = false;
        public int resolutionVal = 100;

        public TexturePackData TerminateEmptyFrame(bool appendEmptyFrame) {
            if (!this.terminateEmptyFrame.HasValue) this.terminateEmptyFrame = appendEmptyFrame;
            return this;
        }

        public TexturePackData IsRepeatable(bool isRepeatable)
        {
            if (!this.isLoopVal.HasValue) this.isLoopVal = isRepeatable;
            return this;
        }
    }


    static private Regex vector3Regex = new Regex("^\\(([^,]*),([^,]*),([^,]*)\\)$");
    //static private Regex floatRegex = new Regex("-?[1-9][0-9]*(\\.[0-9]*)?");

    static private bool Vector3TryParse(string str,out Vector3 vector)
    {
        vector = Vector3.zero;

        var match=vector3Regex.Match(str);
        if (!match.Success) return false;

        if (!float.TryParse(match.Groups[1].Value, out vector.x)) return false;
        if (!float.TryParse(match.Groups[2].Value, out vector.y)) return false;
        if (!float.TryParse(match.Groups[3].Value, out vector.z)) return false;

        return true;
    }

    static public void Deserialize(TexturePackLoader texturePackLoader, Dictionary<string,TexturePackData> dic)
    {
        Stream? stream = texturePackLoader.GetStream();

        if (stream == null)
        {
            NebulaPlugin.Instance.Logger.Print("TexturePack", "TexturePack doesn't have TextureInfo.pack at the correct path.");
            return;
        }


        Regex statementRegex = new Regex("^\"(.+)\":(.+)$");
        Regex imageRegex = new Regex("^\"(.+)\"(\\[([1-9][0-9]*),([1-9][0-9]*)\\])?(\\{(.*)\\})?$");

        try
        {
            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
            {

                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    //スペースを除去
                    line = line.Replace(" ", "").Replace("\t","").Replace("\b", "");

                    Match mt;
        
                    mt = statementRegex.Match(line);
                    if (!mt.Success) continue;

                    var key = mt.Groups[1].Value;
                    var value = mt.Groups[2].Value;

                    //Bool型データ
                    if (value == "false")
                    {
                        dic[key] = new TexturePackData() { BoolVal = false };
                        continue;
                    }
                    if (value == "true")
                    {
                        dic[key] = new TexturePackData() { BoolVal = true };
                        continue;
                    }

                    mt = imageRegex.Match(value);
                    if(mt.Success)
                    {
                        TexturePackData tpd = new();

                        tpd.texture = texturePackLoader.LoadTexture(mt.Groups[1].Value);
                        if (tpd.texture != null) tpd.texture.hideFlags = HideFlags.DontUnloadUnusedAsset;

                        //画像分割
                        if (mt.Groups[2].Success)
                        {
                            tpd.xVal = int.Parse(mt.Groups[3].Value);
                            tpd.yVal = int.Parse(mt.Groups[4].Value);
                        }
                        //プロパティ
                        if (mt.Groups[5].Success)
                        {
                            foreach (string prop in mt.Groups[6].Value.Split(";"))
                            {
                                var propAry = prop.Split("=", 2);
                                if(propAry.Length != 2) continue;

                                if (propAry[0] == "fps" && int.TryParse(propAry[1], out int fps))
                                    tpd.fpsVal = fps;
                                if (propAry[0] == "resolution" && int.TryParse(propAry[1], out int resolution))
                                    tpd.resolutionVal = resolution;
                                if (propAry[0] == "loop" && bool.TryParse(propAry[1], out bool loop))
                                    tpd.isLoopVal = loop;
                                if (propAry[0] == "terminateEmpty" && bool.TryParse(propAry[1], out bool terminateEmptyFrame))
                                    tpd.terminateEmptyFrame = terminateEmptyFrame;
                                if (propAry[0] == "range" && int.TryParse(propAry[1], out int range))
                                    tpd.loopRange = range;
                                if (propAry[0] == "isPermanent" && bool.TryParse(propAry[1], out bool permanent))
                                    tpd.permanentVal = permanent;
                                if (propAry[0] == "isShadowy" && bool.TryParse(propAry[1], out bool shadowy))
                                    tpd.shadowyVal = shadowy;
                                if ((propAry[0] == "pos" || propAry[0] == "position") && Vector3TryParse(propAry[1], out Vector3 pos))
                                    tpd.vectorVal = pos;
                            }
                        }

                        dic[key] = tpd;
                    }
                }
            }
        }
        catch {
            NebulaPlugin.Instance.Logger.Print("Loading texture pack is failed.");
        }

        stream.Close();
    }

    static public bool LoadSprite(Texture2D? texture, int? x,int? y,int resolution,out Sprite[]? sprites)
    {
        sprites = null;

        if (texture == null) return false;

        if (!x.HasValue) x = 1;
        if (!y.HasValue) y = 1;

        sprites = new Sprite[x.Value * y.Value];
        int sizeX = texture.width / x.Value;
        int sizeY = texture.height / y.Value;
        for (int _x = 0;_x < x.Value; _x++)
        {
            for (int _y = 0; _y < y.Value; _y++)
            {
                sprites[_x + _y * x.Value] = Helpers.loadSpriteFromResources(texture, (float)resolution, new Rect(_x * sizeX, _y * sizeY, sizeX, sizeY), new Vector2(0.5f, 0.5f));
                sprites[_x + _y * x.Value].hideFlags= HideFlags.DontUnloadUnusedAsset;
            }
        }

        return true;
    }

    static public bool LoadAsset(string key, Action<TexturePackData>? TexturePackDataEditor,ref CustomTextureAsset? asset, bool canLoadStaticSprite = true, bool canLoadAnimationSprite = true)
    {
        TexturePackData tpd;
        if (!TexturePackDataDic.TryGetValue(key, out tpd)) return false;
        
        if (TexturePackDataEditor != null) TexturePackDataEditor.Invoke(tpd);

        Sprite[]? sprites;
        if (!LoadSprite(tpd.texture, tpd.xVal, tpd.yVal, tpd.resolutionVal,out sprites)) return false;

        if (asset == null) asset = new();

        if (sprites == null) return false;

        
        if (tpd.shadowyVal) asset.layer = LayerExpansion.GetShadowLayer();
        else if (tpd.permanentVal) asset.layer = LayerExpansion.GetObjectsLayer();

        if (tpd.vectorVal.HasValue) asset.pos = tpd.vectorVal.Value;

        if (sprites.Length == 1)
        {
            if (canLoadStaticSprite) asset.staticSprite = sprites[0];
        }
        else
        {
            if (canLoadAnimationSprite)
            {
                asset.isLoop = tpd.isLoopVal.HasValue ? tpd.isLoopVal.Value : false;
                asset.frame = 1f / (float)tpd.fpsVal;
                asset.terminateEmptyFrame = tpd.terminateEmptyFrame.HasValue ? tpd.terminateEmptyFrame.Value : false;
                asset.animation = sprites;
                if(tpd.loopRange.HasValue) asset.loopBegin = Mathf.Max((int)0,(int)(sprites.Length - tpd.loopRange.Value));
            }
        }
        return true;
    }

    static public bool LoadAsset(string key, ref CustomTextureAsset? asset, bool canLoadStaticSprite = true, bool canLoadAnimationSprite = true)
    => LoadAsset(key, null, ref asset, canLoadStaticSprite, canLoadAnimationSprite);

    static public Dictionary<string, TexturePackData> TexturePackDataDic = new();

    static private void Decorate(ref int availableId,ShipStatus shipStatus,CustomTextureAsset asset)
    {
        NebulaPlugin.Instance.Logger.Print("dec-0");
        AllTextureAssets[++availableId] = asset;
        
        GameObject obj = new GameObject("Decoration");

        obj.transform.SetParent(shipStatus.transform);
        obj.transform.localScale = Vector3.one * (Helpers.GetDefaultNormalizeRate() / shipStatus.transform.localScale.x);
        var pos = asset.pos;
        pos.z /= 10f;
        obj.transform.position = pos;
        NebulaPlugin.Instance.Logger.Print("dec-1");
        obj.AddComponent<SpriteRenderer>();
        obj.AddComponent<CustomTextureHandler>().TextureAssetId.Set(availableId);
        NebulaPlugin.Instance.Logger.Print("dec-2");
    }

    static public void Load()
    {
        if (!Directory.Exists("TexturePack")) return;

        DeadBody deadBodyPrefab = GameObject.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<DeadBody>())[0].CastFast<DeadBody>();

        foreach (var path in Directory.GetFiles("TexturePack"))
        {
            NebulaPlugin.Instance.Logger.Print("Check " + path);
            if (!path.EndsWith(".zip")) continue;
            using (var zip = ZipFile.OpenRead(path))
            {
                Deserialize(new ZipTexturePackLoader(zip), TexturePackDataDic);
            }
        }
        Deserialize(new RawTexturePackLoader(), TexturePackDataDic);

        CustomTextureAsset? asset = null;

        int availableId = 0;

        if (LoadAsset("deadBody.spawn", (d) => d.TerminateEmptyFrame(false), ref asset))
        {
            AllTextureAssets[++availableId] = asset;
            deadBodyPrefab.transform.GetChild(1).gameObject.AddComponent<CustomTextureHandler>().TextureAssetId.Set(availableId);
            asset = null;
        }
        if (TexturePackDataDic.TryGetValue("deadBody.showBlood",out var data) && data.BoolVal.HasValue && !data.BoolVal.Value)
        {
            deadBodyPrefab.transform.GetChild(0).gameObject.SetActive(false);
        }else if (LoadAsset("deadBody.blood", (d)=> d.TerminateEmptyFrame(true), ref asset,false))
        {
            AllTextureAssets[++availableId] = asset;
            deadBodyPrefab.transform.GetChild(0).gameObject.AddComponent<CustomTextureHandler>().TextureAssetId.Set(availableId);
            asset = null;
        }

        foreach(var entry in TexturePackDataDic)
        {
            string key = entry.Key;
            if (!key.StartsWith("decoration.")) continue;
            var strings = key.Split('.');

            if (!entry.Value.vectorVal.HasValue) continue;

            if (!LoadAsset(key, null, ref asset)) continue;
            
            switch (strings[1])
            {
                case "skeld":
                    Decorate(ref availableId, MapData.MapDatabase[0].Assets,asset!);
                    break;
                case "mira":
                    Decorate(ref availableId, MapData.MapDatabase[1].Assets, asset!);
                    break;
                case "polus":
                    Decorate(ref availableId, MapData.MapDatabase[2].Assets, asset!);
                    break;
                case "airship":
                    Decorate(ref availableId, MapData.MapDatabase[4].Assets, asset!);
                    break;
            }
        }
    }

    static public Dictionary<int, CustomTextureAsset> AllTextureAssets = new();
}
