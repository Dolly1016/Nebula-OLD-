using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Reflection.Internal;
using Innersloth.Assets;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine.AddressableAssets;
using static Rewired.Controller;

namespace Nebula.Modules;

public class CustomItemGrouped
{
    public CustomItemBundle MyBundle;
}

public class CustomCosmicItem : CustomItemGrouped
{
    [JsonSerializableField]
    public string Name = "Undefined";
    [JsonSerializableField]
    public string Author = "Unknown";
    [JsonSerializableField]
    public string Package = "None";

    public virtual string Category { get => "Undefined"; }
    public bool IsValid { get; private set; } = true;
    public bool IsActive { get; private set; } = false;

    public IEnumerable<CustomImage> AllImage() {

        foreach(var f in this.GetType().GetFields())
        {
            if (!f.FieldType.Equals(typeof(CustomImage))) continue;
            var image = (CustomImage?)f.GetValue(this);
            if (image != null) yield return image;
        }
    }

    public async Task Preactivate()
    {
        foreach(var image in AllImage())
        {
            var stream = MyBundle.OpenStream(Category+"/"+ image.Address);
            string? hash = null;
            if (stream != null)
            {
                hash = System.BitConverter.ToString(CustomItemBundle.MD5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                stream.Close();
            }
            if(hash == null || !(image.Hash?.Equals(hash) ?? true))
            {
                //更新を要する場合
                await MyBundle.DownloadAsset(Category, image.Address);
            }   
        }
    }

    public virtual void Activate()
    {
        foreach (var image in AllImage())
        {
            try
            {
                if (!image.TryLoadImage(MyBundle.GetTextureLoader(Category, image.Address)))
                {
                    IsValid = false;
                    //Debug.LogWarning("[MoreCosmic] Cosmic item \"" + Name + "\" is requiring invalid image.");
                    break;
                }
            }
            catch
            {
                IsValid = false;
                break;
            }
        }
    }


    public void Abandom()
    {

    }
}

public class CustomImage
{
    [JsonSerializableField]
    public string? Hash = "-";
    [JsonSerializableField]
    public string Address = "";
    [JsonSerializableField]
    public int Length = 1;

    private IDividedSpriteLoader? spriteLoader { get; set; }

    public bool TryLoadImage(ITextureLoader textureLoader)
    {
        this.spriteLoader = new DividedSpriteLoader(textureLoader,100f,Length,1);
        for (int i = 0; i < Length; i++) if (!spriteLoader.GetSprite(i)) return false;
        return true;
    }

    public Sprite? GetSprite(int index)
    {
        return spriteLoader?.GetSprite(index) ?? null;
    }
}

public class CustomHat : CustomCosmicItem
{
    [JsonSerializableField]
    public CustomImage? Main;
    [JsonSerializableField]
    public CustomImage? Flip;
    [JsonSerializableField]
    public CustomImage? Back;
    [JsonSerializableField]
    public CustomImage? BackFlip;
    [JsonSerializableField]
    public CustomImage? Move;
    [JsonSerializableField]
    public CustomImage? MoveFlip;
    [JsonSerializableField]
    public CustomImage? MoveBack;
    [JsonSerializableField]
    public CustomImage? MoveBackFlip;
    [JsonSerializableField]
    public CustomImage? Climb;
    [JsonSerializableField]
    public CustomImage? ClimbFlip;
    [JsonSerializableField]
    public bool Bounce = false;
    [JsonSerializableField]
    public bool Adaptive = false;
    [JsonSerializableField]
    public bool HideHands = false;
    [JsonSerializableField]
    public bool IsSkinny = false;
    [JSFieldAmbiguous]
    public int FPS = 1;

    public HatData MyHat { get; private set; }
    public HatViewData MyView { get; private set; }
    public override void Activate()
    {
        base.Activate();
        if (!IsValid) return;

        var viewdata = ScriptableObject.CreateInstance<HatViewData>();
        HatData hat = ScriptableObject.CreateInstance<HatData>();
        MyHat = hat;
        MyView = viewdata;
        viewdata.MarkDontUnload();
        hat.MarkDontUnload();

        viewdata.MainImage = Main?.GetSprite(0);
        viewdata.FloorImage = Main?.GetSprite(0);
        viewdata.LeftMainImage = Flip?.GetSprite(0);
        viewdata.LeftFloorImage = Flip?.GetSprite(0);

        viewdata.BackImage = Back?.GetSprite(0);
        viewdata.LeftBackImage = BackFlip?.GetSprite(0);
        
        viewdata.ClimbImage = Climb?.GetSprite(0);
        viewdata.LeftClimbImage = ClimbFlip?.GetSprite(0) ?? viewdata.ClimbImage;


        hat.name = Name.Replace('_', ' ');
        hat.displayOrder = 99;
        hat.ProductId = "noshat_" + Author + "_" + Name;
        hat.InFront = true;
        hat.NoBounce = !Bounce;
        hat.ChipOffset = new Vector2(0f, 0.2f);
        hat.Free = true;
        hat.PreviewCrewmateColor = Adaptive;
        hat.SpritePreview = Main?.GetSprite(0) ?? Back?.GetSprite(0) ?? Move?.GetSprite(0);

        if (Adaptive) viewdata.AltShader = MoreCosmic.AdaptiveShader;

        var assetRef = new AssetReference(viewdata.Pointer);
        hat.ViewDataRef = assetRef;
        hat.CreateAddressableAsset();

        MoreCosmic.AllHats.Add(hat.ProductId, this);        
    }

    public override string Category { get => "hats"; }
}

public class CustomVisor : CustomCosmicItem
{
    [JsonSerializableField]
    public CustomImage? Main;
    [JsonSerializableField]
    public CustomImage? Flip;
    [JsonSerializableField]
    public bool Adaptive = false;
    [JsonSerializableField]
    public bool BehindHat = false;
    [JSFieldAmbiguous]
    public int FPS = 1;

    public VisorData MyVisor { get; private set; }
    public VisorViewData MyView { get; private set; }

    public override string Category { get => "visors"; }
}

public class CustomNamePlate : CustomCosmicItem
{
    [JsonSerializableField]
    public CustomImage? Plate;

    public NamePlateData MyPlate { get; private set; }
    public NamePlateViewData MyView { get; private set; }
    public override string Category { get => "namePlates"; }
}

public class CustomPackage : CustomItemGrouped
{
    [JsonSerializableField]
    public string Package = "None";
    [JsonSerializableField]
    public string Format = "Custom Package";
    [JsonSerializableField]
    public int Priority = 1;
}

public class CustomItemBundle
{
    static public MD5 MD5 = MD5.Create();
    static public HttpClient HttpClient { get {
            if (httpClient == null) httpClient = new HttpClient();
            return httpClient;
        } }
    static private HttpClient? httpClient = null;

    static Dictionary<string, CustomItemBundle> AllBundles = new();

    [JSFieldAmbiguous]
    public string? BundleName = null;

    [JSFieldAmbiguous]
    public List<CustomHat> Hats = new();
    [JSFieldAmbiguous]
    public List<CustomVisor> Visors = new();
    [JSFieldAmbiguous]
    public List<CustomNamePlate> NamePlates = new();
    [JSFieldAmbiguous]
    public List<CustomPackage> Packages = new();

    public string? RelatedLocalAddress { get; private set; } = null;
    public string? RelatedRemoteAddress { get; private set; } = null;
    public ZipArchive? RelatedZip { get; private set; } = null;

    public bool IsActive { get;private set; } = false;

    private IEnumerable<CustomCosmicItem> AllCosmicItem()
    {
        foreach (var item in Hats) yield return item;
        foreach (var item in Visors) yield return item;
        foreach (var item in NamePlates) yield return item;
    }

    private IEnumerable<CustomItemGrouped> AllContents()
    {
        foreach (var item in AllCosmicItem()) yield return item;
        foreach (var item in Packages) yield return item;
    }
    public async Task Load()
    {
        if (IsActive) return;

        foreach (var item in AllContents()) item.MyBundle = this;
        foreach (var item in AllCosmicItem()) await item.Preactivate();

        if (AllBundles.ContainsKey(BundleName)) throw new Exception("Duplicated Bundle Error");
    }

    public void Activate()
    {
        IsActive = true;
        AllBundles[BundleName] = this;

        foreach (var item in AllCosmicItem()) item.Activate();

        var hatList = HatManager.Instance.allHats.ToList();
        foreach (var item in Hats) if (item.IsValid) hatList.Add(item.MyHat);
        HatManager.Instance.allHats = hatList.ToArray();
        /*
        var visorList = HatManager.Instance.allVisors.ToList();
        foreach (var item in Hats) if (item.IsValid) visorList.Add(item.MyVisor);
        HatManager.Instance.allVisors = visorList.ToArray();

        var nameplateList = HatManager.Instance.allNamePlates.ToList();
        foreach (var item in Hats) if (item.IsValid) nameplateList.Add(item.MyNamePlate);
        HatManager.Instance.allNamePlates = nameplateList.ToArray();
        */
    }

    public Stream? OpenStream(string path)
    {
        if (RelatedLocalAddress != null)
        {
            string address = RelatedLocalAddress + "/" + path;
            if (!File.Exists(address)) return null;
            return File.OpenRead(address);
        }
        return null;
    }

    public async Task DownloadAsset(string category,string address)
    {
        //リモートリポジトリやローカルの配置先が無い場合はダウンロードできない
        if (RelatedRemoteAddress == null || RelatedLocalAddress == null) return;

        var hatFileResponse = await HttpClient.GetAsync(RelatedRemoteAddress + "/" + category + "/" + address, HttpCompletionOption.ResponseContentRead);
        if (hatFileResponse.StatusCode != HttpStatusCode.OK) return;

        using var responseStream = await hatFileResponse.Content.ReadAsStreamAsync();
        //サブディレクトリまでを作っておく
        string localPath = RelatedLocalAddress + "/" + category + "/" + address;

        var dir = Path.GetDirectoryName(localPath);
        if(dir!=null)Directory.CreateDirectory(dir);

        using var fileStream = File.Create(localPath);
        responseStream.CopyTo(fileStream);
    }

    public ITextureLoader GetTextureLoader(string category,string address)
    {
        if (RelatedZip != null)
            return null;
        else
            return new DiskTextureLoader(RelatedLocalAddress + "/" + category + "/" + address);
    }

    static public async Task<CustomItemBundle?> LoadOnline(string url)
    {
        HttpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        var response = await HttpClient.GetAsync(new System.Uri($"{url}/Contents.json"), HttpCompletionOption.ResponseContentRead);
        if (response.StatusCode != HttpStatusCode.OK) return null;

        string json = await response.Content.ReadAsStringAsync();
        
        CustomItemBundle? bundle = (CustomItemBundle?)JsonStructure.Deserialize(json, typeof(CustomItemBundle));
        
        if (bundle == null) return null;

        bundle.RelatedRemoteAddress = url;
        bundle.RelatedLocalAddress = "MoreCosmic";
        if (bundle.BundleName == null) bundle.BundleName = url;
        await bundle.Load();

        return bundle;
    }

    static public CustomItemBundle LoadOffline()
    {
        return null;
    }
}

public static class MoreCosmic
{
    public static Dictionary<string, CustomHat> AllHats = new();
    public static Dictionary<string, CustomVisor> AllVisors = new();
    public static Dictionary<string, CustomNamePlate> AllNameplates = new();
    public static Dictionary<string, CustomPackage> AllPackages = new();

    private static Material? adaptiveShader = null;
    public static Material AdaptiveShader { get {
            if (adaptiveShader == null) adaptiveShader = HatManager.Instance.PlayerMaterial;
            return adaptiveShader;
        } }
    private static List<Task<CustomItemBundle?>> allTasks = new();
    private static bool isLoaded = false;
    public static void Load()
    {
        if (isLoaded) return;
        allTasks.Add(Modules.CustomItemBundle.LoadOnline("https://raw.githubusercontent.com/Dolly1016/MoreCosmic/master"));
        isLoaded = true;
    }

    public static void Update()
    {
        allTasks.RemoveAll((task) => {
            if (task.IsFaulted)
            {
                Debug.LogError("[MoreCosmic]" + (task.Exception?.InnerException?.Message ?? "Unknown Error"));
                return true;
            }
            if (task.IsCompleted)
            {
                task.Result?.Activate();
                return true;
            }
            return false;
        });

    }
}

[NebulaPreLoad]
public static class WrappedAddressableAssetLoader
{
    public static void Load()
    {
        try
        {
            ClassInjector.RegisterTypeInIl2Cpp<WrappedHatAsset>();
            ClassInjector.RegisterTypeInIl2Cpp<WrappedVisorAsset>();
            ClassInjector.RegisterTypeInIl2Cpp<WrappedNamePlateAsset>();
        }catch(Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }
}

public class WrappedHatAsset : AddressableAsset<HatViewData> {
    public HatViewData? viewData = null;
    public WrappedHatAsset(System.IntPtr ptr) : base(ptr) { }
    public WrappedHatAsset() : base(ClassInjector.DerivedConstructorPointer<WrappedHatAsset>())
    { ClassInjector.DerivedConstructorBody(this); }
    public override HatViewData GetAsset() => viewData;
    public override void LoadAsync(Il2CppSystem.Action? onSuccessCb = null, Il2CppSystem.Action? onErrorcb = null, Il2CppSystem.Action? onFinishedcb = null)
    {
        if (onSuccessCb != null) onSuccessCb.Invoke();
        if (onFinishedcb != null) onFinishedcb.Invoke();
    }
    public override void Unload() { }
    public override void Destroy() { }
    public override AssetLoadState GetState() => AssetLoadState.Success;
}
public class WrappedVisorAsset : AddressableAsset<VisorViewData> {
    public VisorViewData viewData;
    public WrappedVisorAsset(System.IntPtr ptr) : base(ptr) { }
    public WrappedVisorAsset() : base(ClassInjector.DerivedConstructorPointer<WrappedVisorAsset>())
    { ClassInjector.DerivedConstructorBody(this); }
    public override VisorViewData GetAsset() => viewData;
    public override void LoadAsync(Il2CppSystem.Action? onSuccessCb = null, Il2CppSystem.Action? onErrorcb = null, Il2CppSystem.Action? onFinishedcb = null)
    {
        if (onSuccessCb != null) onSuccessCb.Invoke();
        if (onFinishedcb != null) onFinishedcb.Invoke();
    }
    public override void Unload() { }
    public override void Destroy() { }
    public override AssetLoadState GetState() => AssetLoadState.Success;
}
public class WrappedNamePlateAsset : AddressableAsset<NamePlateViewData> {
    public NamePlateViewData viewData;
    public WrappedNamePlateAsset(System.IntPtr ptr) : base(ptr) { }
    public WrappedNamePlateAsset() : base(ClassInjector.DerivedConstructorPointer<WrappedNamePlateAsset>())
    { ClassInjector.DerivedConstructorBody(this); }
    public override NamePlateViewData GetAsset() => viewData;
    public override void LoadAsync(Il2CppSystem.Action? onSuccessCb = null, Il2CppSystem.Action? onErrorcb = null, Il2CppSystem.Action? onFinishedcb = null)
    {
        if (onSuccessCb != null) onSuccessCb.Invoke();
        if (onFinishedcb != null) onFinishedcb.Invoke();
    }
    public override void Unload() { }
    public override void Destroy() { }
    public override AssetLoadState GetState() => AssetLoadState.Success;
}

[HarmonyPatch(typeof(HatManager),nameof(HatManager.Initialize))]
public class HatManagerPatch
{
    public static void Postfix(HatManager __instance)
    {
        //MoreCosmic.Load();
    }
}

[HarmonyPatch(typeof(CosmeticsCache), nameof(CosmeticsCache.GetHat))]
public class CosmeticsCacheGetHatPatch
{
    public static bool Prefix(string id, ref HatViewData __result)
    {
        if (MoreCosmic.AllHats.TryGetValue(id,out var hat))
        {
            __result = hat.MyView;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CosmeticsCache), nameof(CosmeticsCache.GetVisor))]
public class CosmeticsCacheGetVisorPatch
{
    public static bool Prefix(string id, ref VisorViewData __result)
    {
        if (MoreCosmic.AllVisors.TryGetValue(id, out var visor))
        {
            __result = visor.MyView;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CosmeticsCache), nameof(CosmeticsCache.GetNameplate))]
public class CosmeticsCacheGetNameplatePatch
{
    public static bool Prefix(string id, ref NamePlateViewData __result)
    {
        if (MoreCosmic.AllNameplates.TryGetValue(id, out var plate))
        {
            __result = plate.MyView;
            return false;
        }
        return true;
    }
}


[HarmonyPatch(typeof(HatData), nameof(HatData.CreateAddressableAsset))]
public class HatAssetPatch
{
    public static bool Prefix(HatData __instance, ref AddressableAsset<HatViewData> __result)
    {
        if (!MoreCosmic.AllHats.TryGetValue(__instance.ProductId, out var value)) return true;
        var asset =  new WrappedHatAsset(); 
        asset.viewData = value.MyView;
        __result = asset.Cast<AddressableAsset<HatViewData>>();
        return false;
    }
}

[HarmonyPatch(typeof(VisorData), nameof(VisorData.CreateAddressableAsset))]
public class VisorAssetPatch
{
    public static bool Prefix(VisorData __instance, ref AddressableAsset<VisorViewData> __result)
    {
        if (!MoreCosmic.AllVisors.TryGetValue(__instance.ProductId, out var value)) return true;
        var asset = new WrappedVisorAsset();
        asset.viewData = value.MyView ;
        __result = asset.Cast<AddressableAsset<VisorViewData>>();
        return false;
    }
}

[HarmonyPatch(typeof(NamePlateData), nameof(NamePlateData.CreateAddressableAsset))]
public class NameplateAssetPatch
{
    public static bool Prefix(NamePlateData __instance, ref AddressableAsset<NamePlateViewData> __result)
    {
        if (!MoreCosmic.AllNameplates.TryGetValue(__instance.ProductId, out var value)) return true;
        var asset = new WrappedNamePlateAsset();
        asset.viewData = value.MyView;
        __result = asset.Cast<AddressableAsset<NamePlateViewData>>();
        return false;
    }
}