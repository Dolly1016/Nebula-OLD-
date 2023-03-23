using LibCpp2IL;
using Nebula.Patches;
using Nebula.Utilities;
using Newtonsoft.Json.Linq;
using Sentry;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using static Rewired.Controller;

namespace Nebula.Module;
public abstract class CustomVariable
{
    public string Id { get; set; }

    public void Load(JToken token)
    {
        LoadValue(token[Id]);
    }
    protected virtual void LoadValue(JToken? token)
    {

    }

    public virtual bool DoesResourceRequireDownload(string directoryPath, MD5 md5) { return false; }

    public virtual async Task Download(string repoPath, string directoryPath, HttpClient http) { }
    public virtual void LoadImage(string parent, bool fromDisk = false) { }
    public abstract void ToJson(out string label,out JsonContent? content);

    public CustomVariable(string Id)
    {
        this.Id = Id;
    }
}

public class CustomVHatImage : CustomVImage
{
    public override Sprite CreateSprite(Texture2D texture, Rect rect)
    {
        return Sprite.Create(texture, rect, new Vector2(0.53f, 0.575f), 112.875f);
    }

    public CustomVHatImage(string Id) : base(Id)
    {
    }
}

public class CustomVImage : CustomVariable
{
    private bool Loaded;

    public static implicit operator bool(CustomVImage image) { return image.Address != null; }
    public string? Hash { get; set; }
    public string? Address { get; set; }
    public Sprite?[] Images { get; set; }
    public Texture2D Texture { get; set; }
    public int Length { get; set; }

    public Sprite? GetMainImage()
    {
        if (Images == null || Images.Length == 0) return null;
        return Images[0];
    }

    private string? SanitizeResourcePath(string res)
    {
        if (res == null)
            return null;

        return res;
    }

    protected override void LoadValue(JToken? token)
    {
        Address = SanitizeResourcePath(token?["Address"]?.ToString());
        Hash = SanitizeResourcePath(token?["Hash"]?.ToString());
        try
        {
            Length = int.Parse(token?["Length"]?.ToString());
        }
        catch { Length = 1; }
    }

    public virtual Sprite CreateSprite(Texture2D texture, Rect rect)
    {
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
    }

    private void LoadImage(Texture2D texture)
    {
        if (Texture != null && texture != Texture) Texture.hideFlags = HideFlags.None;
        if (Images != null) foreach (var i in Images) i.hideFlags = HideFlags.None;

        Texture = texture;
        Texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        
        if (Images == null || Images.Length != Length) Images = new Sprite?[Length];
        int width = texture.width / Length;
        for (int i = 0; i < Length; i++)
        {
            Sprite sprite = CreateSprite(texture, new Rect(width * i, 0, width, texture.height));
            if (sprite == null)
                return;
            Images[i] = sprite;
        }

        Loaded = true;
    }

    public void ResaveImage(CustomCosmicItem item)
    {
        if (Address == null) return;
        if (Path.IsPathRooted(Address))
        {
            //絶対パスの場合は外から画像を持ってきている
            System.Text.StringBuilder pathBuilder = new();
            string prepath = "";
            if (item is CustomHat)
                prepath = "MoreCosmic/hats/";
            else if (item is CustomVisor)
                prepath = "MoreCosmic/visors/";
            else if (item is CustomNamePlate)
                prepath = "MoreCosmic/nameplates/";
            else
                return;

            foreach (var c in item.Author.Value) pathBuilder.Append(((int)c).ToString("X4"));
            pathBuilder.Append("/");
            foreach (var c in item.Name.Value) pathBuilder.Append(((int)c).ToString("X4"));

            string path = pathBuilder.ToString();
            Directory.CreateDirectory(prepath + path);

            path += "/" + Id + ".png";
            File.WriteAllBytes(prepath + path, Texture.EncodeToPNG());

            Address = path;
            return;
        }
        return;
    }
    public void LoadImage(DesignersImageParameter parameter)
    {
        Address = parameter.Path;
        Length = parameter.Split;
        LoadImage(parameter.Texture);
    }

    public override void LoadImage(string parent, bool fromDisk = false)
    {
        if (Loaded) return;

        if (Address == null) return;
        Texture = fromDisk ? Helpers.loadTextureFromDisk(Path.GetDirectoryName(Application.dataPath) + "/" + parent + "/" + Address) : Helpers.loadTextureFromResources(parent + "." + Address);
        if (Texture == null)
            return;

        Texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;

        LoadImage(Texture);

        for (int i = 0; i < Length; i++) if (Images[i]) Images[i].hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
    }

    public override bool DoesResourceRequireDownload(string directoryPath, MD5 md5)
    {
        if (Hash == null)
        {
            //ローカルコスミックのハッシュを出力
            if (File.Exists(directoryPath + Address) && Patches.NebulaOption.configOutputHash.Value)
            {
                using (var stream = File.OpenRead(directoryPath + Address))
                {
                    var hash = System.BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    NebulaPlugin.Instance.Logger.Print("HASH: " + hash + " (" + directoryPath + Address + ")");
                }
            }
            return false;
        }

        if (!this) return false;
        if (!File.Exists(directoryPath + Address)) return true;


        using (var stream = File.OpenRead(directoryPath + Address))
        {
            var hash = System.BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            var result = !Hash.Equals(hash);

            return result;
        }
    }

    public override async Task Download(string repoPath, string directoryPath, HttpClient http)
    {
        var hatFileResponse = await http.GetAsync($"{repoPath}{Address}", HttpCompletionOption.ResponseContentRead);
        if (hatFileResponse.StatusCode != HttpStatusCode.OK) return;
        using (var responseStream = await hatFileResponse.Content.ReadAsStreamAsync())
        {
            //サブディレクトリまでを作っておく
            Directory.CreateDirectory(Path.GetDirectoryName(directoryPath + Address));

            using (var fileStream = File.Create($"{directoryPath}{Address}"))
            {
                responseStream.CopyTo(fileStream);
            }

            if (Patches.NebulaOption.configOutputHash.Value)
            {
                MD5 md5 = MD5.Create();

                using (var stream = File.OpenRead($"{directoryPath}{Address}"))
                {
                    var hash = System.BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    var result = !Hash.Equals(hash);

                    NebulaPlugin.Instance.Logger.Print("Downloaded Image's Hash: " + hash + " (" + directoryPath + Address + ")");
                }
            }
        }
    }

    public override void ToJson(out string label, out JsonContent? content)
    {
        label = Id;
        if (Images==null || Images.Length == 0)
        {
            content = null;
            return;
        }

        var myContent = new JsonObjectContent();
        myContent.AddContent("Address",new JsonStringContent(Address));
        myContent.AddContent("Hash", new JsonStringContent(Hash));
        if (Length > 1) myContent.AddContent("Length", new JsonStringContent(Length.ToString()));
        content = myContent;
    }

    public CustomVImage(string Id) : base(Id)
    {
        Hash = Address = null;
        Images = new Sprite[] { };
        Length = 1;
        Loaded = false;
    }
}

public class CustomVSecPerFrame : CustomVariable
{
    public float SecPerFrame;

    protected override void LoadValue(JToken? token)
    {
        try
        {
            SecPerFrame = 1f / ((float)int.Parse(token?.ToString()));
        }
        catch { }
    }

    public override void ToJson(out string label, out JsonContent content)
    {
        label = Id;
        int val = (int)(1f / SecPerFrame);
        if (val == 1)
        {
            content = null;
            return;
        }
        content = new JsonStringContent((val).ToString());
    }

    public CustomVSecPerFrame(string Id) : base(Id)
    {
        SecPerFrame = 1f;
    }
}

public class CustomVBool : CustomVariable
{
    public bool Value { get; set; }

    protected override void LoadValue(JToken? token)
    {
        string? str = token?.ToString();
        if (str != null) Value = bool.Parse(str);
    }

    public override void ToJson(out string label, out JsonContent content)
    {
        label = Id;
        content = new JsonStringContent(Value.ToString());
    }

    public CustomVBool(string Id) : base(Id)
    {
        Value = false;
    }
}

public class CustomVString : CustomVariable
{
    public string Value { get; set; }
    public bool UseUnicodeEscape { get; set; } = false;

    protected override void LoadValue(JToken? token)
    {
        Value = token?.ToString() ?? Value;
    }

    public override void ToJson(out string label, out JsonContent content)
    {
        label = Id;
        if (UseUnicodeEscape)
            content = new JsonStringContent(Helpers.ToUnicodeEscapeSequence(Value.ToString()));
        else
            content = new JsonStringContent(Value.ToString());
    }

    public CustomVString(string Id,bool useUnicodeEscape=false) : base(Id)
    {
        Value = "";
        UseUnicodeEscape = useUnicodeEscape;
    }

    public CustomVString(string Id, string initialValue, bool useUnicodeEscape = false) : base(Id)
    {
        Value = initialValue;
        UseUnicodeEscape = useUnicodeEscape;
    }
}

public interface CustomItem
{
    IEnumerable<CustomVariable> Contents();
}

public class CustomCosmicItem : CustomItem
{
    public CustomVString Author { get; set; }
    public CustomVString Package { get; set; }
    public CustomVString Name { get; set; }
    public bool IsLocal { get; set; }

    public IEnumerable<CustomVariable> Contents()
    {
        yield return Author;
        yield return Package;
        yield return Name;
        foreach (var content in ExtendedContents()) yield return content;
        yield break;
    }

    protected virtual IEnumerable<CustomVariable> ExtendedContents()
    {
        yield break;
    }

    public virtual bool HasAnimation() { return false; }

    public CustomCosmicItem()
    {
        Author = new CustomVString("Author", "",true);
        Package = new CustomVString("Package", "local");
        Name = new CustomVString("Name", "Untitled", true);
        IsLocal = true;
    }
    
    public void UpdateCommonInfo(string name,string author,string package)
    {
        Name.Value = name;
        Author.Value = author;
        Package.Value = package;
        ReflectNameToVanillaData();
    }

    public void ResaveAllImages()
    {
        foreach (var content in Contents()) if (content is CustomVImage image) image.ResaveImage(this);
    }

    public virtual void ReflectNameToVanillaData() { }

    public virtual void Remove(){
        foreach(var content in Contents())
        {
            if(content is CustomVImage image)
            {
                if (image.Texture != null) image.Texture.hideFlags = HideFlags.None;
                if (image.Images != null) foreach (var sprite in image.Images) sprite.hideFlags = HideFlags.None;
            }
        }
    }
}


public class CustomHat : CustomCosmicItem
{
    public CustomVHatImage I_Main { get; set; }
    public CustomVHatImage I_Flip { get; set; }
    public CustomVHatImage I_Back { get; set; }
    public CustomVHatImage I_BackFlip { get; set; }
    public CustomVHatImage I_Move { get; set; }
    public CustomVHatImage I_MoveFlip { get; set; }
    public CustomVHatImage I_MoveBack { get; set; }
    public CustomVHatImage I_MoveBackFlip { get; set; }
    public CustomVHatImage I_Climb { get; set; }
    public CustomVHatImage I_ClimbFlip { get; set; }
    public CustomVBool Bounce { get; set; }
    public CustomVBool Adaptive { get; set; }
    public CustomVBool Behind { get; set; }
    public CustomVBool HideHands { get; set; }
    public CustomVBool IsSkinny { get; set; }
    public CustomVSecPerFrame SecPerFrame { get; set; }
    public HatData HatData { get; set; }

    public override bool HasAnimation()
    {
        return
            I_Main.Length > 1 || I_Flip.Length > 1 || I_Back.Length > 1 || I_BackFlip.Length > 1 ||
            I_Move.Length > 1 || I_MoveFlip.Length > 1 || I_MoveBack.Length > 1 || I_MoveBackFlip.Length > 1 ||
            I_Climb.Length > 1 || I_ClimbFlip.Length > 1;
    }

    protected override IEnumerable<CustomVariable> ExtendedContents()
    {
        yield return I_Main;
        yield return I_Flip;
        yield return I_Back;
        yield return I_BackFlip;
        yield return I_Move;
        yield return I_MoveFlip;
        yield return I_MoveBack;
        yield return I_MoveBackFlip;
        yield return I_Climb;
        yield return I_ClimbFlip;
        yield return Bounce;
        yield return Adaptive;
        yield return Behind;
        yield return HideHands;
        yield return IsSkinny;
        yield return SecPerFrame;
        yield break;
    }

    public CustomHat() : base()
    {
        I_Main = new CustomVHatImage("Main");
        I_Flip = new CustomVHatImage("Flip");
        I_Back = new CustomVHatImage("Back");
        I_BackFlip = new CustomVHatImage("BackFlip");
        I_Move = new CustomVHatImage("Move");
        I_MoveFlip = new CustomVHatImage("MoveFlip");
        I_MoveBack = new CustomVHatImage("MoveBack");
        I_MoveBackFlip = new CustomVHatImage("MoveBackFlip");
        I_Climb = new CustomVHatImage("Climb");
        I_ClimbFlip = new CustomVHatImage("ClimbFlip");
        Bounce = new CustomVBool("Bounce");
        Adaptive = new CustomVBool("Adaptive");
        Behind = new CustomVBool("Behind");
        HideHands = new CustomVBool("HideHands");
        IsSkinny = new CustomVBool("IsSkinny");
        SecPerFrame = new CustomVSecPerFrame("FPS");
    }

    public override void ReflectNameToVanillaData()
    {
        if (HatData == null) return;
        HatData.StoreName = HatData.name = Name.Value + (Author.Value.Length == 0 ? "\nfrom Local" : ("\nby " + Author.Value));
    }

    public override void Remove() {
        base.Remove();
        if (HatData != null)
        {
            var list = HatManager.Instance.allHats.ToList();
            list.Remove(HatData);
            HatManager.Instance.allHats = new(list.ToArray());
            CustomParts.CustomHatRegistry.Remove(HatData.GetInstanceID());
        }
    }
}

public class CustomNamePlate : CustomCosmicItem
{
    public CustomVImage I_Plate { get; set; }
    public NamePlateData NamePlateData { get; set; }

    protected override IEnumerable<CustomVariable> ExtendedContents()
    {
        yield return I_Plate;
        yield break;
    }

    public CustomNamePlate() : base()
    {
        I_Plate = new CustomVImage("Plate");
    }

    public override void ReflectNameToVanillaData()
    {
        if (NamePlateData == null) return;
        NamePlateData.name = Name.Value + (Author.Value.Length == 0 ? "\nfrom Local" : ("\nby " + Author.Value));
    }

    public override void Remove()
    {
        base.Remove();
        if (NamePlateData != null)
        {
            var list = HatManager.Instance.allNamePlates.ToList();
            list.Remove(NamePlateData);
            HatManager.Instance.allNamePlates = new(list.ToArray());
            CustomParts.CustomNamePlateRegistry.Remove(NamePlateData.GetInstanceID());
        }
    }
}

public class CustomVisor : CustomCosmicItem
{
    public CustomVHatImage I_Main { get; set; }
    public CustomVHatImage I_Flip { get; set; }
    public CustomVBool Adaptive { get; set; }
    public CustomVBool BehindHat { get; set; }
    public CustomVSecPerFrame SecPerFrame { get; set; }
    public VisorData VisorData { get; set; }

    public override bool HasAnimation() { return I_Main.Length > 1 || I_Flip.Length > 1; }

    protected override IEnumerable<CustomVariable> ExtendedContents()
    {
        yield return I_Main;
        yield return I_Flip;
        yield return Adaptive;
        yield return BehindHat;
        yield return SecPerFrame;
        yield break;
    }

    public CustomVisor() : base()
    {
        I_Main = new CustomVHatImage("Main");
        I_Flip = new CustomVHatImage("Flip");
        Adaptive = new CustomVBool("Adaptive");
        BehindHat = new CustomVBool("BehindHat");
        SecPerFrame = new CustomVSecPerFrame("FPS");
    }

    public override void ReflectNameToVanillaData()
    {
        if (VisorData == null) return;
        VisorData.name = Name.Value + (Author.Value.Length == 0 ? "\nfrom Local" : ("\nby " + Author.Value));
    }

    public override void Remove()
    {
        base.Remove();
        if (VisorData != null)
        {
            var list = HatManager.Instance.allVisors.ToList();
            list.Remove(VisorData);
            HatManager.Instance.allVisors = new(list.ToArray());
            CustomParts.CustomVisorRegistry.Remove(VisorData.GetInstanceID());
        }
    }
}

public class CustomPackage : CustomItem
{
    public static Dictionary<string, int> orderDic = new Dictionary<string, int>();
    public static List<CustomPackage> AllPackage = new();

    public CustomVString Key { get; set; }
    public CustomVString Format { get; set; }
    public CustomVString Priority { get; set; }
    public bool IsLocal = false;

    static public void ReloadPackages()
    {
        orderDic.Clear();
        foreach(var package in AllPackage)
        {
            package.RegisterPackage();
        }
    }

    public void RegisterPackage()
    {
        if (int.TryParse(Priority.Value, out int priority))
        {
            orderDic[Key.Value] = 500 + priority;
            Language.Language.AddDefaultKey("cosmic.package." + Key.Value, Format.Value);
        }
    }

    public string GetFormatted()
    {
        return Language.Language.GetString("cosmic.package." + Key.Value);
    }

    public IEnumerable<CustomVariable> Contents()
    {
        yield return Key;
        yield return Format;
        yield return Priority;
    }

    public CustomPackage() : base()
    {
        Key = new CustomVString("Package");
        Format = new CustomVString("Format", true);
        Priority = new CustomVString("Priority");
    }
}

[HarmonyPatch]
public class CustomParts
{
    public static Material? hatShader;

    public static Dictionary<int, CustomHat> CustomHatRegistry = new Dictionary<int, CustomHat>();
    public static Dictionary<int, CustomNamePlate> CustomNamePlateRegistry = new Dictionary<int, CustomNamePlate>();
    public static Dictionary<int, CustomVisor> CustomVisorRegistry = new Dictionary<int, CustomVisor>();

    public static HatData CreateHatData(CustomHat ch, bool fromDisk = false)
    {
        if (hatShader == null)
            hatShader = DestroyableSingleton<HatManager>.Instance.PlayerMaterial;

        HatData hat = ScriptableObject.CreateInstance<HatData>();
        hat.hatViewData.viewData = ScriptableObject.CreateInstance<HatViewData>();

        ch.HatData = hat;

        hat.DontUnload();
        hat.hatViewData.viewData.DontUnload();

        HatViewData viewData = hat.hatViewData.viewData;
        try
        {
            foreach (var content in ch.Contents()) content.LoadImage("MoreCosmic/hats", fromDisk);

            viewData.MainImage = ch.I_Main.GetMainImage();
            if (ch.I_Flip)
                viewData.LeftMainImage = ch.I_Flip.GetMainImage();
            if (ch.I_Back)
            {
                viewData.BackImage = ch.I_Back.GetMainImage();
                ch.Behind.Value = true;
            }
            if (ch.I_Climb)
                viewData.ClimbImage = ch.I_Climb.GetMainImage();
            if (ch.I_ClimbFlip)
                viewData.LeftClimbImage = ch.I_ClimbFlip.GetMainImage();
            if (ch.I_BackFlip)
                viewData.LeftBackImage = ch.I_BackFlip.GetMainImage();

            ch.ReflectNameToVanillaData();
            
            //hat.Order = 99;
            hat.ProductId = "hat_" + ch.Name.Value.Replace(' ', '_');
            hat.InFront = !ch.Behind.Value;
            hat.NoBounce = !ch.Bounce.Value;
            hat.ChipOffset = new Vector2(0f, 0.2f);
            hat.Free = true;
            hat.NotInStore = true;

            if (ch.Adaptive.Value && hatShader != null)
                viewData.AltShader = hatShader;

            
            CustomHatRegistry.Add(hat.GetInstanceID(), ch);
            
            return hat;
        }
        catch (System.Exception e)
        {
            ScriptableObject.Destroy(hat.hatViewData.viewData);
            ScriptableObject.Destroy(hat);
            NebulaPlugin.Instance.Logger.Print("MoreCosmic",ch.Name.Value+ " is informal.");
            return null;
        }
    }

    public static NamePlateData CreateNamePlateData(CustomNamePlate ch, bool fromDisk = false)
    {
        NamePlateData np = ScriptableObject.CreateInstance<NamePlateData>();
        np.viewData.viewData = ScriptableObject.CreateInstance<NamePlateViewData>();

        ch.NamePlateData = np;

        np.DontUnload();
        np.viewData.viewData.DontUnload();

        try
        {
            foreach (var content in ch.Contents()) content.LoadImage("MoreCosmic/namePlates", fromDisk);

            np.viewData.viewData.Image = ch.I_Plate.GetMainImage();
            ch.ReflectNameToVanillaData();
            //np.Order = 99;
            np.ProductId = "nameplate_" + ch.Name.Value.Replace(' ', '_');
            np.ChipOffset = new Vector2(0f, 0.2f);
            np.Free = true;
            np.NotInStore = true;

            CustomNamePlateRegistry.Add(np.GetInstanceID(), ch);
            
            return np;
        }
        catch (System.Exception e)
        {
            ScriptableObject.Destroy(np.viewData.viewData);
            ScriptableObject.Destroy(np);
            NebulaPlugin.Instance.Logger.Print("MoreCosmic", ch.Name.Value + " is informal.");
            return null;
        }
    }

    public static VisorData CreateVisorData(CustomVisor ch, bool fromDisk = false)
    {
        if (hatShader == null)
            hatShader = DestroyableSingleton<HatManager>.Instance.PlayerMaterial;

        VisorData vd = ScriptableObject.CreateInstance<VisorData>();
        vd.viewData.viewData = ScriptableObject.CreateInstance<VisorViewData>();

        ch.VisorData = vd;

        vd.DontUnload();
        vd.viewData.viewData.DontUnload();

        try
        {
            foreach (var content in ch.Contents()) content.LoadImage("MoreCosmic/visors", fromDisk);

            vd.viewData.viewData.IdleFrame = ch.I_Main.GetMainImage();
            vd.viewData.viewData.LeftIdleFrame = ch.I_Flip.GetMainImage();
            ch.ReflectNameToVanillaData(); 
            vd.ProductId = "visor_" + ch.Name.Value.Replace(' ', '_');
            vd.ChipOffset = new Vector2(0f, 0.2f);
            vd.Free = true;
            vd.NotInStore = true;
            vd.viewData.viewData.BehindHats = ch.BehindHat.Value;

            if (ch.Adaptive.Value && hatShader != null)
                vd.viewData.viewData.AltShader = hatShader;

            CustomVisorRegistry.Add(vd.GetInstanceID(), ch);
            
            return vd;
        }
        catch (System.Exception e)
        {
            ScriptableObject.Destroy(vd.viewData.viewData);
            ScriptableObject.Destroy(vd);
            NebulaPlugin.Instance.Logger.Print("MoreCosmic", ch.Name.Value + " is informal.");
            return null;
        }
    }

    [HarmonyPatch(typeof(HatManager), nameof(HatManager.GetHatById))]
    private static class HatManagerPatch
    {
        private static bool LOADED;
        private static bool RUNNING;

        static void Prefix(HatManager __instance)
        {
            if (RUNNING) return;
            RUNNING = true; // prevent simultanious execution

            try
            {
                if (CosmicLoader.hatdetails.Count > 0)
                {
                    var lastArray = __instance.allHats;
                    int newCosmics = CosmicLoader.hatdetails.Count;
                    var newArray = new Il2CppReferenceArray<HatData>(lastArray.Count + newCosmics);
                    int newCount = lastArray.Count;
                    int lastCount = lastArray.Count;

                    for (int i = 0; i < lastCount; i++) newArray[i] = lastArray[i];
                    for (int i = 0; i < newCosmics; i++)
                    {
                        var data = CreateHatData(CosmicLoader.hatdetails[i], true);
                        if (data != null)
                        {
                            newArray[newCount] = data;
                            newCount++;
                        }
                    }
                    CosmicLoader.hatdetails.RemoveRange(0, newCosmics);

                    if (newArray.Count > newCount) newArray = new(newArray.ToArray().SubArray(0, newCount));

                    __instance.allHats = newArray;
                }
            }
            catch (System.Exception e)
            {
                if (!LOADED)
                    System.Console.WriteLine("Unable to add Custom Hats\n" + e);
            }
            LOADED = true;
        }
        static void Postfix(HatManager __instance)
        {
            RUNNING = false;
        }
    }


    [HarmonyPatch(typeof(HatManager), nameof(HatManager.GetNamePlateById))]
    private static class NamePlateManagerPatch
    {
        private static bool LOADED;
        private static bool RUNNING;

        static void Prefix(HatManager __instance)
        {
            if (RUNNING) return;
            RUNNING = true; // prevent simultanious execution

            try
            {
                if (CosmicLoader.namePlatedetails.Count > 0)
                {
                    var lastArray = __instance.allNamePlates;
                    int newCosmics = CosmicLoader.namePlatedetails.Count;
                    var newArray = new Il2CppReferenceArray<NamePlateData>(lastArray.Count + newCosmics);
                    int newCount = lastArray.Count;
                    int lastCount = lastArray.Count;

                    for (int i = 0; i < lastCount; i++) newArray[i] = lastArray[i];
                    for (int i = 0; i < newCosmics; i++)
                    {
                        var data = CreateNamePlateData(CosmicLoader.namePlatedetails[i], true);
                        if (data != null)
                        {
                            newArray[newCount] = data;
                            newCount++;
                        }
                    }
                    CosmicLoader.namePlatedetails.RemoveRange(0, newCosmics);

                    if (newArray.Count > newCount) newArray = new(newArray.ToArray().SubArray(0, newCount));

                    __instance.allNamePlates = newArray;
                }
            }
            catch (System.Exception e)
            {
                if (!LOADED)
                    System.Console.WriteLine("Unable to add Custom Hats\n" + e);
            }
            LOADED = true;
        }
        static void Postfix(HatManager __instance)
        {
            RUNNING = false;
        }
    }

    [HarmonyPatch(typeof(HatManager), nameof(HatManager.GetVisorById))]
    private static class VisorManagerPatch
    {
        private static bool LOADED;
        private static bool RUNNING;

        static void Prefix(HatManager __instance)
        {
            if (RUNNING) return;
            RUNNING = true; // prevent simultanious execution

            try
            {
                if (CosmicLoader.visordetails.Count > 0)
                {
                    var lastArray = __instance.allVisors;
                    int newCosmics = CosmicLoader.visordetails.Count;
                    var newArray = new Il2CppReferenceArray<VisorData>(lastArray.Count + newCosmics);
                    int newCount = lastArray.Count;
                    int lastCount = lastArray.Count;
                    
                    for (int i = 0; i < lastCount; i++) newArray[i] = lastArray[i];
                    for (int i = 0; i < newCosmics; i++)
                    {
                        var data =  CreateVisorData(CosmicLoader.visordetails[i], true);
                        if (data != null)
                        {
                            newArray[newCount] = data;
                            newCount++;
                        }
                    }
                    CosmicLoader.visordetails.RemoveRange(0, newCosmics);

                    if (newArray.Count > newCount) newArray = new(newArray.ToArray().SubArray(0, newCount));

                    __instance.allVisors = newArray;
                }
            }
            catch (System.Exception e)
            {
            }
            LOADED = true;
        }
        static void Postfix(HatManager __instance)
        {
            RUNNING = false;
        }
    }


    [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.PlayerFall))]
    private static class ExiledPlayerHideHandsPatch
    {
        private static void Postfix(AirshipExileController __instance)
        {
            HatParent hp = __instance.Player.cosmetics.hat;
            if (hp.Hat == null) return;
            CustomHat extend = hp.Hat.getHatData();
            if (extend == null) return;

            if (extend.HideHands.Value)
            {
                __instance.Player.gameObject.transform.FindChild("HandSlot").gameObject.SetActive(false);
            }
        }
    }

    public static class AnimationHandler
    {
        private static Sprite? UpdateAndGetSprite(Game.PlayerData.CosmicPartTimer cosmicTimer, Sprite?[] sprites)
        {
            if (sprites.Length == 0) return null;
            if (cosmicTimer.Index >= sprites.Length) cosmicTimer.Index = 0;
            return sprites[cosmicTimer.Index];
        }

        private static void HandleHat(PlayerAnimations animation, CosmeticsLayer cosmetics,Nebula.Game.PlayerData.CosmicTimer timer)
        {
            AnimationClip currentAnimation = animation.Animator.m_currAnim;
            if (currentAnimation == animation.group.ClimbUpAnim || currentAnimation == animation.group.ClimbDownAnim) return;
            HatParent hp = cosmetics.hat;
            if (hp.Hat == null) return;
            CustomHat extend = hp.Hat.getHatData();
            if (extend == null) return;

            var cosmicTimer = timer.Hat;
            cosmicTimer.Timer -= Time.deltaTime;
            if (cosmicTimer.Timer < 0f) { cosmicTimer.Timer = extend.SecPerFrame.SecPerFrame; cosmicTimer.Index++; }

            if (currentAnimation == animation.group.RunAnim)
            {
                if (extend.I_Move)
                    hp.FrontLayer.sprite = UpdateAndGetSprite(cosmicTimer, (cosmetics.FlipX && extend.I_MoveFlip) ? extend.I_MoveFlip.Images : extend.I_Move.Images);
                if (extend.I_MoveBack)
                    hp.BackLayer.sprite = UpdateAndGetSprite(cosmicTimer, (cosmetics.FlipX && extend.I_MoveBackFlip) ? extend.I_MoveBackFlip.Images : extend.I_MoveBack.Images);
            }
            else
            {
                hp.FrontLayer.sprite = UpdateAndGetSprite(cosmicTimer, (cosmetics.FlipX && extend.I_Flip) ? extend.I_Flip.Images : extend.I_Main.Images);
                hp.BackLayer.sprite = UpdateAndGetSprite(cosmicTimer, (cosmetics.FlipX && extend.I_BackFlip) ? extend.I_BackFlip.Images : extend.I_Back.Images);
            }
        }
        private static void HandleVisor(PlayerAnimations animation, CosmeticsLayer cosmetics, Nebula.Game.PlayerData.CosmicTimer timer)
        {
            AnimationClip currentAnimation = animation.Animator.m_currAnim;
            if (currentAnimation == animation.group.ClimbUpAnim || currentAnimation == animation.group.ClimbDownAnim) return;

            var visor = cosmetics.visor;
            if (visor.currentVisor == null) return;
            CustomVisor extend = visor.currentVisor.getVisorData();
            if (extend == null) return;

            var cosmicTimer = timer.Visor;
            cosmicTimer.Timer -= Time.deltaTime;
            if (cosmicTimer.Timer < 0f) { cosmicTimer.Timer = extend.SecPerFrame.SecPerFrame; cosmicTimer.Index++; }

            visor.Image.sprite = UpdateAndGetSprite(cosmicTimer, (cosmetics.FlipX && extend.I_Flip) ? extend.I_Flip.Images : extend.I_Main.Images);
        }

        public static void HandleAnimation(PlayerAnimations animation, CosmeticsLayer cosmetics, Nebula.Game.PlayerData.CosmicTimer timer)
        {
            HandleHat(animation, cosmetics, timer);
            HandleVisor(animation, cosmetics, timer);
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
    private static class PlayerPhysicsHandleAnimationPatch
    {
        private static void Postfix(PlayerPhysics __instance)
        {
            var timer = Game.PlayerData.GetCosmicTimer(__instance.myPlayer.PlayerId);
            AnimationHandler.HandleAnimation(__instance.Animations,__instance.myPlayer.cosmetics,timer);
        }
    }


    private static List<TMPro.TMP_Text> hatsTabCustomTexts = new List<TMPro.TMP_Text>();
    private static List<TMPro.TMP_Text> nameplatesTabCustomTexts = new List<TMPro.TMP_Text>();
    private static List<TMPro.TMP_Text> visorsTabCustomTexts = new List<TMPro.TMP_Text>();

    public static string innerslothHatPackageName = "innerslothHats";
    public static string innerslothNamePlatePackageName = "innerslothNameplates";
    public static string innerslothVisorPackageName = "innerslothVisors";
    private static float headerSize = 0.8f;
    private static float headerX = 0.8f;
    private static float inventoryTop = 1.5f;
    private static float inventoryBot = -2.5f;
    private static float inventoryZ = -2f;

    public static void calcItemBounds(InventoryTab __instance)
    {
        inventoryTop = __instance.scroller.Inner.position.y - 0.5f;
        inventoryBot = __instance.scroller.Inner.position.y - 4.5f;
    }

    private static Sprite AnimationMarkSprite;
    private static Sprite GetAnimationMark()
    {
        if (AnimationMarkSprite) return AnimationMarkSprite;
        AnimationMarkSprite = Helpers.loadSpriteFromResources("Nebula.Resources.AnimationIcon.png", 100f);
        return AnimationMarkSprite;
    }

    static void AddAnimationMark(ColorChip colorChip)
    {
        GameObject obj = new GameObject("AnimationMark");
        obj.transform.SetParent(colorChip.transform);
        obj.layer = colorChip.gameObject.layer;
        obj.transform.localPosition = new Vector3(-0.38f, 0.39f, -10f);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = GetAnimationMark();
        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }


    [HarmonyPatch(typeof(HatsTab), nameof(HatsTab.OnEnable))]
    public class HatsTabOnEnablePatch
    {
        public static TMPro.TMP_Text textTemplate;

        public static float createHatPackage(List<System.Tuple<HatData, CustomHat>> hats, string packageName, float YStart, HatsTab __instance)
        {
            float offset = YStart;

            if (textTemplate != null)
            {
                TMPro.TMP_Text title = UnityEngine.Object.Instantiate<TMPro.TMP_Text>(textTemplate, __instance.scroller.Inner);
                title.GetComponent<TextTranslatorTMP>().enabled = false;
                var mat = title.GetComponent<MeshRenderer>().material;
                mat.SetFloat("_StencilComp", 4f);
                mat.SetFloat("_Stencil", 1f);

                title.transform.parent = __instance.scroller.Inner;
                title.transform.localPosition = new Vector3(headerX, YStart, inventoryZ);
                title.text = Language.Language.GetString("cosmic.package." + packageName);
                title.alignment = TMPro.TextAlignmentOptions.Center;
                title.fontSize = 5f;
                title.fontWeight = TMPro.FontWeight.Thin;
                title.enableAutoSizing = false;
                title.autoSizeTextContainer = true;
                offset -= headerSize * __instance.YOffset;
                hatsTabCustomTexts.Add(title);
            }

            var numHats = hats.Count;

            for (int i = 0; i < hats.Count; i++)
            {
                HatData hat = hats[i].Item1;
                CustomHat ext = hats[i].Item2;

                float xpos = __instance.XRange.Lerp((i % __instance.NumPerRow) / (__instance.NumPerRow - 1f));
                float ypos = offset - (i / __instance.NumPerRow) * __instance.YOffset;
                ColorChip colorChip = UnityEngine.Object.Instantiate<ColorChip>(__instance.ColorTabPrefab, __instance.scroller.Inner);

                int color = __instance.HasLocalPlayer() ? PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId : AmongUs.Data.DataManager.Player.Customization.Color;

                colorChip.transform.localPosition = new Vector3(xpos, ypos, inventoryZ);
                if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
                {
                    colorChip.Button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() => { __instance.SelectHat(hat); colorChip.SelectionHighlight.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; }));
                    colorChip.Button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectHat(FastDestroyableSingleton<HatManager>.Instance.GetHatById(AmongUs.Data.DataManager.Player.Customization.Hat))));
                    colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.ClickEquip()));
                }
                else
                {
                    colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectHat(hat)));
                }

                if (ext != null && ext.HasAnimation()) AddAnimationMark(colorChip);

                colorChip.Inner.SetHat(hat, color);
                colorChip.Inner.transform.localPosition = hat.ChipOffset;
                colorChip.Tag = hat;
                colorChip.Button.ClickMask = __instance.scroller.Hitbox;
                __instance.ColorChips.Add(colorChip);

            }

            return offset - ((numHats - 1) / __instance.NumPerRow) * __instance.YOffset - headerSize;
        }

        public static bool Prefix(HatsTab __instance)
        {
            calcItemBounds(__instance);

            HatData[] unlockedHats = FastDestroyableSingleton<HatManager>.Instance.GetUnlockedHats();
            Dictionary<string, List<System.Tuple<HatData, CustomHat>>> packages = new Dictionary<string, List<System.Tuple<HatData, CustomHat>>>();

            Helpers.destroyList(hatsTabCustomTexts);
            Helpers.destroyList(__instance.ColorChips);

            __instance.ColorChips.Clear();

            textTemplate = __instance.transform.FindChild("Text").gameObject.GetComponent<TMPro.TMP_Text>();

            foreach (HatData hatBehaviour in unlockedHats)
            {
                CustomHat ext = hatBehaviour.getHatData();

                if (ext != null)
                {
                    if (!packages.ContainsKey(ext.Package.Value))
                        packages[ext.Package.Value] = new List<System.Tuple<HatData, CustomHat>>();
                    packages[ext.Package.Value].Add(new System.Tuple<HatData, CustomHat>(hatBehaviour, ext));
                }
                else
                {
                    if (!packages.ContainsKey(innerslothHatPackageName))
                        packages[innerslothHatPackageName] = new List<System.Tuple<HatData, CustomHat>>();
                    packages[innerslothHatPackageName].Add(new System.Tuple<HatData, CustomHat>(hatBehaviour, null));
                }
            }

            float YOffset = __instance.YStart;

            var orderedKeys = packages.Keys.OrderBy((string x) =>
            {
                if (x == innerslothHatPackageName) return 1000;
                if (x == "local") return 200;
                if (CustomPackage.orderDic.ContainsKey(x)) return CustomPackage.orderDic[x];
                return 500;
            });

            foreach (string key in orderedKeys)
            {
                List<System.Tuple<HatData, CustomHat>> value = packages[key];
                YOffset = createHatPackage(value, key, YOffset, __instance);
            }

            __instance.scroller.ContentYBounds.max = -(YOffset + 3.0f + headerSize);

            foreach (var cc in __instance.ColorChips)
            {
                cc.Inner.FrontLayer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                cc.Inner.BackLayer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                cc.SelectionHighlight.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
            return false;
        }
    }


    [HarmonyPatch(typeof(NameplatesTab), nameof(NameplatesTab.OnEnable))]
    public class NamePlatesTabOnEnablePatch
    {
        public static TMPro.TMP_Text textTemplate;

        public static float createNameplatePackage(List<System.Tuple<NamePlateData, CustomNamePlate>> nameplates, string packageName, float YStart, NameplatesTab __instance)
        {
            float offset = YStart;

            if (textTemplate != null)
            {
                TMPro.TMP_Text title = UnityEngine.Object.Instantiate<TMPro.TMP_Text>(textTemplate, __instance.scroller.Inner);
                title.GetComponent<TextTranslatorTMP>().enabled = false;
                var mat = title.GetComponent<MeshRenderer>().material;
                mat.SetFloat("_StencilComp", 4f);
                mat.SetFloat("_Stencil", 1f);

                title.transform.parent = __instance.scroller.Inner;
                title.transform.localPosition = new Vector3(headerX, YStart, inventoryZ);
                title.text = Language.Language.GetString("cosmic.package." + packageName);
                title.alignment = TMPro.TextAlignmentOptions.Center;
                title.fontSize = 5f;
                title.fontWeight = TMPro.FontWeight.Thin;
                title.enableAutoSizing = false;
                title.autoSizeTextContainer = true;
                offset -= headerSize * __instance.YOffset;
                nameplatesTabCustomTexts.Add(title);
            }

            var numNameplates = nameplates.Count;

            for (int i = 0; i < nameplates.Count; i++)
            {
                NamePlateData nameplate = nameplates[i].Item1;
                CustomNamePlate ext = nameplates[i].Item2;

                float xpos = __instance.XRange.Lerp((i % __instance.NumPerRow) / (__instance.NumPerRow - 1f));
                float ypos = offset - (i / __instance.NumPerRow) * __instance.YOffset;
                ColorChip colorChip = UnityEngine.Object.Instantiate<ColorChip>(__instance.ColorTabPrefab, __instance.scroller.Inner);


                colorChip.transform.localPosition = new Vector3(xpos, ypos, inventoryZ);
                if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
                {
                    colorChip.Button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectNameplate(nameplate)));
                    colorChip.Button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectNameplate(FastDestroyableSingleton<HatManager>.Instance.GetNamePlateById(AmongUs.Data.DataManager.Player.Customization.NamePlate))));
                    colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.ClickEquip()));
                }
                else
                {
                    colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectNameplate(nameplate)));
                }


                __instance.StartCoroutine(nameplate.CoLoadViewData((Il2CppSystem.Action<NamePlateViewData>)((n) =>
                {
                    colorChip.gameObject.GetComponent<NameplateChip>().image.sprite = n.Image;
                    colorChip.gameObject.GetComponent<NameplateChip>().ProductId = nameplate.ProductId;
                    __instance.ColorChips.Add(colorChip);
                })));

            }

            return offset - ((numNameplates - 1) / __instance.NumPerRow) * __instance.YOffset - headerSize;
        }

        public static bool Prefix(NameplatesTab __instance)
        {
            calcItemBounds(__instance);

            NamePlateData[] unlockedNameplates = FastDestroyableSingleton<HatManager>.Instance.GetUnlockedNamePlates();
            Dictionary<string, List<System.Tuple<NamePlateData, CustomNamePlate>>> packages = new Dictionary<string, List<System.Tuple<NamePlateData, CustomNamePlate>>>();

            Helpers.destroyList(nameplatesTabCustomTexts);
            Helpers.destroyList(__instance.ColorChips);

            nameplatesTabCustomTexts.Clear();
            __instance.ColorChips.Clear();

            textTemplate = __instance.transform.FindChild("Text").gameObject.GetComponent<TMPro.TMP_Text>();

            foreach (NamePlateData nameplate in unlockedNameplates)
            {
                CustomNamePlate ext = nameplate.getNamePlateData();

                if (ext != null)
                {
                    if (!packages.ContainsKey(ext.Package.Value))
                        packages[ext.Package.Value] = new List<System.Tuple<NamePlateData, CustomNamePlate>>();
                    packages[ext.Package.Value].Add(new System.Tuple<NamePlateData, CustomNamePlate>(nameplate, ext));
                }
                else
                {
                    if (!packages.ContainsKey(innerslothNamePlatePackageName))
                        packages[innerslothNamePlatePackageName] = new List<System.Tuple<NamePlateData, CustomNamePlate>>();
                    packages[innerslothNamePlatePackageName].Add(new System.Tuple<NamePlateData, CustomNamePlate>(nameplate, null));
                }
            }

            float YOffset = __instance.YStart;

            var orderedKeys = packages.Keys.OrderBy((string x) =>
            {
                if (x == innerslothNamePlatePackageName) return 1000;
                if (x == "local") return 200;
                if (CustomPackage.orderDic.ContainsKey(x)) return CustomPackage.orderDic[x];
                return 500;
            });

            foreach (string key in orderedKeys)
            {
                List<System.Tuple<NamePlateData, CustomNamePlate>> value = packages[key];
                YOffset = createNameplatePackage(value, key, YOffset, __instance);
            }

            __instance.scroller.ContentYBounds.max = -(YOffset + 3.0f + headerSize);

            return false;
        }
    }

    [HarmonyPatch(typeof(VisorsTab), nameof(VisorsTab.OnEnable))]
    public class VisorTabOnEnablePatch
    {
        public static TMPro.TMP_Text textTemplate;

        public static float createVisorPackage(List<System.Tuple<VisorData, CustomVisor>> visors, string packageName, float YStart, VisorsTab __instance)
        {
            float offset = YStart;

            if (textTemplate != null)
            {
                TMPro.TMP_Text title = UnityEngine.Object.Instantiate<TMPro.TMP_Text>(textTemplate, __instance.scroller.Inner);
                title.GetComponent<TextTranslatorTMP>().enabled = false;
                var mat = title.GetComponent<MeshRenderer>().material;
                mat.SetFloat("_StencilComp", 4f);
                mat.SetFloat("_Stencil", 1f);

                title.transform.parent = __instance.scroller.Inner;
                title.transform.localPosition = new Vector3(headerX, YStart, inventoryZ);
                title.text = Language.Language.GetString("cosmic.package." + packageName);
                title.alignment = TMPro.TextAlignmentOptions.Center;
                title.fontSize = 5f;
                title.fontWeight = TMPro.FontWeight.Thin;
                title.enableAutoSizing = false;
                title.autoSizeTextContainer = true;
                offset -= headerSize * __instance.YOffset;
                visorsTabCustomTexts.Add(title);
            }

            var numVisors = visors.Count;

            for (int i = 0; i < visors.Count; i++)
            {
                VisorData visor = visors[i].Item1;
                CustomVisor ext = visors[i].Item2;

                float xpos = __instance.XRange.Lerp((i % __instance.NumPerRow) / (__instance.NumPerRow - 1f));
                float ypos = offset - (i / __instance.NumPerRow) * __instance.YOffset;
                ColorChip colorChip = UnityEngine.Object.Instantiate<ColorChip>(__instance.ColorTabPrefab, __instance.scroller.Inner);


                colorChip.transform.localPosition = new Vector3(xpos, ypos, inventoryZ);
                if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
                {
                    colorChip.Button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectVisor(visor)));
                    colorChip.Button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectVisor(FastDestroyableSingleton<HatManager>.Instance.GetVisorById(AmongUs.Data.DataManager.Player.Customization.Visor))));
                    colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.ClickEquip()));
                }
                else
                {
                    colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectVisor(visor)));
                }

                if (ext != null && ext.HasAnimation()) AddAnimationMark(colorChip);

                colorChip.Button.ClickMask = __instance.scroller.Hitbox;
                colorChip.ProductId = visor.ProductId;
                colorChip.Inner.transform.localPosition = visor.ChipOffset;
                colorChip.Tag = visor.ProdId;
                colorChip.SelectionHighlight.gameObject.SetActive(false);

                __instance.StartCoroutine(visor.CoLoadViewData((Il2CppSystem.Action<VisorViewData>)((v) =>
                {
                    colorChip.Inner.FrontLayer.sprite = v.IdleFrame;
                    if (ext != null && ext.Adaptive.Value)
                        colorChip.Inner.FrontLayer.material = FastDestroyableSingleton<HatManager>.Instance.PlayerMaterial;
                })));


                __instance.ColorChips.Add(colorChip);
            }

            return offset - ((numVisors - 1) / __instance.NumPerRow) * __instance.YOffset - headerSize;
        }

        public static bool Prefix(VisorsTab __instance)
        {
            calcItemBounds(__instance);

            VisorData[] unlockedVisors = FastDestroyableSingleton<HatManager>.Instance.GetUnlockedVisors();
            Dictionary<string, List<System.Tuple<VisorData, CustomVisor>>> packages = new Dictionary<string, List<System.Tuple<VisorData, CustomVisor>>>();

            Helpers.destroyList(visorsTabCustomTexts);
            Helpers.destroyList(__instance.ColorChips);

            visorsTabCustomTexts.Clear();
            __instance.ColorChips.Clear();

            textTemplate = __instance.transform.FindChild("Text").gameObject.GetComponent<TMPro.TMP_Text>();

            foreach (VisorData visor in unlockedVisors)
            {
                CustomVisor ext = visor.getVisorData();

                if (ext != null)
                {
                    if (!packages.ContainsKey(ext.Package.Value))
                        packages[ext.Package.Value] = new List<System.Tuple<VisorData, CustomVisor>>();
                    packages[ext.Package.Value].Add(new System.Tuple<VisorData, CustomVisor>(visor, ext));
                }
                else
                {
                    if (!packages.ContainsKey(innerslothVisorPackageName))
                        packages[innerslothVisorPackageName] = new List<System.Tuple<VisorData, CustomVisor>>();
                    packages[innerslothVisorPackageName].Add(new System.Tuple<VisorData, CustomVisor>(visor, null));
                }
            }

            float YOffset = __instance.YStart;

            var orderedKeys = packages.Keys.OrderBy((string x) =>
            {
                if (x == innerslothVisorPackageName) return 1000;
                if (x == "local") return 200;
                if (CustomPackage.orderDic.ContainsKey(x)) return CustomPackage.orderDic[x];
                return 500;
            });

            foreach (string key in orderedKeys)
            {
                List<System.Tuple<VisorData, CustomVisor>> value = packages[key];
                YOffset = createVisorPackage(value, key, YOffset, __instance);
            }

            __instance.scroller.ContentYBounds.max = -(YOffset + 3.0f + headerSize);

            foreach (var cc in __instance.ColorChips)
            {
                cc.SelectionHighlight.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }

            return false;
        }
    }
}

public class CosmicLoader
{
    public static bool running = false;
    public static string userCosmicReposLink = "https://raw.githubusercontent.com/Dolly1016/MoreCosmic/master/UserCosmics.dat";
    public static string[] cosmicRepos = new string[]
    {
            "MoreCosmic"
    };

    public static List<CustomHat> hatdetails = new List<CustomHat>();
    public static List<CustomNamePlate> namePlatedetails = new List<CustomNamePlate>();
    public static List<CustomVisor> visordetails = new List<CustomVisor>();

    private static Task cosmicFetchTask = null;

    public static void GetUserCosmicRepos(ref List<string> list)
    {
        try
        {
            HttpStatusCode result;
            HttpClient? http = null;

            http = new HttpClient();
            http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            var responseTask = http.GetAsync(new System.Uri(userCosmicReposLink), HttpCompletionOption.ResponseContentRead);
            responseTask.Wait();
            var response = responseTask.Result;

            if (response.StatusCode != HttpStatusCode.OK) return;
            if (response.Content == null) return;


            var task = response.Content.ReadAsStringAsync();
            task.Wait();
            string reposText = task.Result;
            foreach (string repo in reposText.Split("\n")) list.Add(repo);
        }
        catch { }
    }

    public static bool cosmicLoad = false;
    public static void LaunchCosmicFetcher()
    {
        if (cosmicLoad) return;

        if (running)
            return;
        running = true;
        cosmicLoad = true;

        System.IO.Directory.CreateDirectory("MoreCosmic");
        System.IO.Directory.CreateDirectory("MoreCosmic/hats");
        System.IO.Directory.CreateDirectory("MoreCosmic/namePlates");
        System.IO.Directory.CreateDirectory("MoreCosmic/visors");

        if(!NebulaOption.GetGameControlArgument(1)) cosmicFetchTask = LaunchCosmicFetcherAsync();
    }

    private static async Task LaunchCosmicFetcherAsync()
    {
        List<string> repos;
        if (!NebulaOption.GetGameControlArgument(3))
            repos = new List<string>(cosmicRepos);
        else
            repos = new();
        if (!NebulaOption.GetGameControlArgument(2))
            GetUserCosmicRepos(ref repos);

        foreach (string repo in repos)
        {
            string? json;
            HttpClient? http = null;

            if (repo.StartsWith("https://"))
            {
                http = new HttpClient();
                json = await FetchOnlineItems(repo, http);
            }
            else
                json = FetchOfflineItems(repo);

            if (json == null) continue;

            try
            {
                HttpStatusCode status;

                status = await LoadPackage(json, repo, http == null);
                if (status != HttpStatusCode.OK)
                    NebulaPlugin.Instance.Logger.Print($"[Failed]Load MoreCosmic Packages {repo}\n");

                status = await FetchItems(http, json, repo, "hats", hatdetails);
                if (status != HttpStatusCode.OK)
                    NebulaPlugin.Instance.Logger.Print($"[Failed]Load MoreCosmic Hats {repo}\n");

                status = await FetchItems(http, json, repo, "namePlates", namePlatedetails);
                if (status != HttpStatusCode.OK)
                    NebulaPlugin.Instance.Logger.Print($"[Failed]Load MoreCosmic Nameplates {repo}\n");

                status = await FetchItems(http, json, repo, "visors", visordetails);
                if (status != HttpStatusCode.OK)
                    NebulaPlugin.Instance.Logger.Print($"[Failed]Load MoreCosmic Visors {repo}\n");

            }
            catch 
            {
                NebulaPlugin.Instance.Logger.Print($"[Failed]Load MoreCosmic {repo}");
            }
        }
        running = false;
    }

    public static async Task<string?> FetchOnlineItems(string repo, HttpClient http)
    {
        string? json = "";

        http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        var response = await http.GetAsync(new System.Uri($"{repo}/Contents.json"), HttpCompletionOption.ResponseContentRead);

        if (response.StatusCode != HttpStatusCode.OK) return null;
        if (response.Content == null)
        {
            NebulaPlugin.Instance.Logger.Print("Server returned no data: " + response.StatusCode.ToString());
            return null;
        }

        try
        {
            var task = response.Content.ReadAsStringAsync();
            task.Wait();
            json = task.Result;
        }
        catch (System.Exception ex)
        {
            NebulaPlugin.Instance.Logger.Print("[MoreCosmic]" + ex);
        }
        return json;

    }

    public static string? FetchOfflineItems(string repo)
    {
        try
        {
            return File.ReadAllText($"{repo}/Contents.json");
        }
        catch (System.Exception e)
        {
            return null;
        }
    }

    public static async Task<HttpStatusCode> FetchItems<Cosmic>(HttpClient? http, string json, string repo, string category, List<Cosmic> cosmics) where Cosmic : CustomCosmicItem, new()
    {
        try
        {
            JToken jobj = JObject.Parse(json)[category];
            if (!jobj.HasValues) return HttpStatusCode.OK;

            List<Cosmic> cosList = new List<Cosmic>();

            List<CustomVariable> markedfordownload = new List<CustomVariable>();
            string filePath = @"MoreCosmic/" + category + @"/";
            MD5 md5 = MD5.Create();

            for (JToken current = jobj.First; current != null; current = current.Next)
            {
                if (current.HasValues)
                {
                    Cosmic cos = new Cosmic();
                    cos.IsLocal = http == null;
                    foreach (var content in cos.Contents())
                    {
                        content.Load(current);
                        if (content.DoesResourceRequireDownload(filePath, md5)) markedfordownload.Add(content);
                    }
                    cosList.Add(cos);
                }
                else break;

            }

            if (http != null)
            {
                foreach (var content in markedfordownload)
                {
                    await content.Download(repo + "/" + category + "/", filePath, http);
                }
            }

            cosmics.AddRange(cosList);
        }
        catch
        {
            
        }
        return HttpStatusCode.OK;
    }

    public static async Task<HttpStatusCode> LoadPackage(string json, string repo,bool isLocal)
    {
        try
        {
            JToken jobj = JObject.Parse(json)["packages"];
            if (!jobj.HasValues) return HttpStatusCode.ExpectationFailed;

            for (JToken current = jobj.First; current != null; current = current.Next)
            {
                if (current.HasValues)
                {
                    CustomPackage cos = new CustomPackage();

                    foreach (var content in cos.Contents())
                    {
                        content.Load(current);
                    }

                    cos.IsLocal = isLocal;

                    CustomPackage.AllPackage.Add(cos);
                    cos.RegisterPackage();

                }
                else break;

            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine(ex);
        }
        return HttpStatusCode.OK;
    }
}

public static class CustomItemManager
{
    public static CustomHat getHatData(this HatData hat)
    {
        CustomHat ret = null;
        if (hat == CustomHatDesigner.Hat?.VanillaData) return CustomHatDesigner.Hat!.NebulaData;
        
        CustomParts.CustomHatRegistry.TryGetValue(hat.GetInstanceID(), out ret);
        return ret;
    }

    public static CustomNamePlate getNamePlateData(this NamePlateData namePlate)
    {
        CustomNamePlate ret = null;
       
        CustomParts.CustomNamePlateRegistry.TryGetValue(namePlate.GetInstanceID(), out ret);
        return ret;
    }

    public static CustomVisor getVisorData(this VisorData visorData)
    {
        CustomVisor ret = null;
        //if (visorData == CustomVisorDesigner.Visor?.VanillaData) return CustomVisorDesigner.Visor!.NebulaData;
        
        CustomParts.CustomVisorRegistry.TryGetValue(visorData.GetInstanceID(), out ret);
        return ret;
    }

}


//y座標のずれを修正
[HarmonyPatch(typeof(PoolablePlayer), nameof(PoolablePlayer.Awake))]
public static class PoolablePlayerEnablePatch
{
    public static void Postfix(PoolablePlayer __instance)
    {
        try
        {
            __instance.cosmetics.visor.transform.localPosition = new Vector3(
                __instance.cosmetics.visor.transform.localPosition.x,
                0.575f,
                __instance.cosmetics.visor.transform.localPosition.z
                );
            __instance.cosmetics.hat.transform.localPosition = new Vector3(
                __instance.cosmetics.hat.transform.localPosition.x,
                0.575f,
                __instance.cosmetics.hat.transform.localPosition.z
                );
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ResetAnimState))]
public static class MeetingHatFixPatch
{
    public static void Postfix(PlayerPhysics __instance)
    {
        try
        {
            __instance.myPlayer.cosmetics.SetHatColor(Game.GameData.data.GetPlayerData(__instance.myPlayer.PlayerId).CurrentOutfit.ColorId);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(HatParent), nameof(HatParent.PopulateFromHatViewData))]
public static class PopulateFromHatViewDataPatch
{
    public static void Postfix(HatParent __instance)
    {
        var extend = __instance.Hat.getHatData();
        if (extend != null && extend.IsSkinny.Value)
        {
            if (__instance.BackLayer.transform.localPosition.z < 0f)
                __instance.FrontLayer.transform.localPosition = new Vector3(0, 0, __instance.BackLayer.transform.localPosition.z - 0.00225f);
            else
                __instance.FrontLayer.transform.localPosition = new Vector3(0, 0, __instance.BackLayer.transform.localPosition.z * -1.25f);
        }
        else
        {
            if (__instance.BackLayer.transform.localPosition.z < 0f)
                __instance.FrontLayer.transform.localPosition = new Vector3(0, 0, __instance.BackLayer.transform.localPosition.z - 0.003f);
            else
                __instance.FrontLayer.transform.localPosition = new Vector3(0, 0, __instance.BackLayer.transform.localPosition.z * -2f);
        }
    }
}
