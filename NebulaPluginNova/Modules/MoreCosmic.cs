using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Reflection.Internal;
using Il2CppSystem.Text.RegularExpressions;
using Innersloth.Assets;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.XR;
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

    public string UnescapedName => Regex.Unescape(Name).Replace('_', ' ');
    public string UnescapedAuthor => Regex.Unescape(Author).Replace('_', ' ');

    public virtual string Category { get => "Undefined"; }
    public bool IsValid { get; private set; } = true;
    public bool IsActive { get; private set; } = false;

    public IEnumerable<CosmicImage> AllImage() {

        foreach(var f in this.GetType().GetFields())
        {
            if (!f.FieldType.Equals(typeof(CosmicImage))) continue;
            var image = (CosmicImage?)f.GetValue(this);
            if (image != null) yield return image;
        }
    }

    public string SubholderPath => Author.ToByteString() + "/" + Name.ToByteString();

    public async Task Preactivate()
    {
        string holder = SubholderPath;
        foreach (var image in AllImage())
        {
            var stream = MyBundle.OpenStream(Category + "/" + holder + "/" + image.Address);
            string? hash = null;
            if (stream != null)
            {
                hash = System.BitConverter.ToString(CustomItemBundle.MD5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                stream.Close();
            }
            if(hash == null || !(image.Hash?.Equals(hash) ?? true))
            {
                //更新を要する場合
                await MyBundle.DownloadAsset(Category, holder, image.Address);
            }   
        }
    }

    public virtual void Activate()
    {
        string holder = SubholderPath;
        foreach (var image in AllImage())
        {
            try
            {
                if (!image.TryLoadImage(MyBundle.GetTextureLoader(Category, SubholderPath, image.Address)))
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

public class CosmicImage
{
    [JsonSerializableField]
    public string? Hash = "-";
    [JsonSerializableField]
    public string Address = "";
    [JsonSerializableField]
    public int Length = 1;

    public float PixelsPerUnit = 100f;
    public Vector2 Pivot = new Vector2(0.5f, 0.5f);

    public bool RequirePlayFirstState = false;
    private IDividedSpriteLoader? spriteLoader { get; set; }

    public bool TryLoadImage(ITextureLoader textureLoader)
    {
        this.spriteLoader = new XOnlyDividedSpriteLoader(textureLoader, PixelsPerUnit, Length) { Pivot = Pivot };
        for (int i = 0; i < Length; i++) if (!spriteLoader.GetSprite(i)) return false;
        return true;
    }

    public Sprite? GetSprite(int index)
    {
        return spriteLoader?.GetSprite(index) ?? null;
    }
}

public class CosmicHat : CustomCosmicItem
{
    [JsonSerializableField]
    public CosmicImage? Main;
    [JsonSerializableField]
    public CosmicImage? Flip;
    [JsonSerializableField]
    public CosmicImage? Back;
    [JsonSerializableField]
    public CosmicImage? BackFlip;
    [JsonSerializableField]
    public CosmicImage? Move;
    [JsonSerializableField]
    public CosmicImage? MoveFlip;
    [JsonSerializableField]
    public CosmicImage? MoveBack;
    [JsonSerializableField]
    public CosmicImage? MoveBackFlip;
    [JsonSerializableField]
    public CosmicImage? Climb;
    [JsonSerializableField]
    public CosmicImage? ClimbFlip;
    [JsonSerializableField]
    public CosmicImage? ClimbDown;
    [JsonSerializableField]
    public CosmicImage? ClimbDownFlip;
    [JsonSerializableField]
    public CosmicImage? EnterVent;
    [JsonSerializableField]
    public CosmicImage? EnterVentFlip;
    [JsonSerializableField]
    public CosmicImage? ExitVent;
    [JsonSerializableField]
    public CosmicImage? ExitVentFlip;
    [JsonSerializableField]
    public CosmicImage? EnterVentBack;
    [JsonSerializableField]
    public CosmicImage? EnterVentBackFlip;
    [JsonSerializableField]
    public CosmicImage? ExitVentBack;
    [JsonSerializableField]
    public CosmicImage? ExitVentBackFlip;
    [JsonSerializableField]
    public CosmicImage? Preview;
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
        foreach (var image in AllImage()) {
            image.Pivot = new Vector2(0.53f, 0.575f);
            image.PixelsPerUnit = 112.875f;
        }

        base.Activate();

        if (!IsValid) return;

        var viewdata = ScriptableObject.CreateInstance<HatViewData>();
        HatData hat = ScriptableObject.CreateInstance<HatData>();
        MyHat = hat;
        MyView = viewdata;
        viewdata.MarkDontUnload();
        hat.MarkDontUnload();

        hat.name = UnescapedName + "\n<size=1.6>by " + UnescapedAuthor + "</size>";
        hat.displayOrder = 99;
        hat.ProductId = "noshat_" + Author + "_" + Name;
        hat.InFront = true;
        hat.NoBounce = !Bounce;
        hat.ChipOffset = new Vector2(0f, 0.2f);
        hat.Free = true;
        hat.PreviewCrewmateColor = Adaptive;
        hat.SpritePreview = Preview?.GetSprite(0) ?? Main?.GetSprite(0) ?? Back?.GetSprite(0) ?? Move?.GetSprite(0);

        if (Adaptive) viewdata.AltShader = MoreCosmic.AdaptiveShader;

        var assetRef = new AssetReference(viewdata.Pointer);
        hat.ViewDataRef = assetRef;
        hat.CreateAddressableAsset();

        if (EnterVent != null) EnterVent.RequirePlayFirstState = true;
        if (EnterVentBack != null) EnterVentBack.RequirePlayFirstState = true;
        if (EnterVentFlip != null) EnterVentFlip.RequirePlayFirstState = true;
        if (EnterVentBackFlip != null) EnterVentBackFlip.RequirePlayFirstState = true;
        if (ExitVent != null) ExitVent.RequirePlayFirstState = true;
        if (ExitVentBack != null) ExitVentBack.RequirePlayFirstState = true;
        if (ExitVentFlip != null) ExitVentFlip.RequirePlayFirstState = true;
        if (ExitVentBackFlip != null) ExitVentBackFlip.RequirePlayFirstState = true;
        if (Climb != null) Climb.RequirePlayFirstState = true;
        if (ClimbFlip != null) ClimbFlip.RequirePlayFirstState = true;
        if (ClimbDown != null) ClimbDown.RequirePlayFirstState = true;
        if (ClimbDownFlip != null) ClimbDownFlip.RequirePlayFirstState = true;

        MoreCosmic.AllHats.Add(hat.ProductId, this);        
    }

    public override string Category { get => "hats"; }
}

public class CosmicVisor : CustomCosmicItem
{
    [JsonSerializableField]
    public CosmicImage? Main;
    [JsonSerializableField]
    public CosmicImage? Flip;
    [JsonSerializableField]
    public CosmicImage? Move;
    [JsonSerializableField]
    public CosmicImage? MoveFlip;
    [JsonSerializableField]
    public CosmicImage? EnterVent;
    [JsonSerializableField]
    public CosmicImage? EnterVentFlip;
    [JsonSerializableField]
    public CosmicImage? ExitVent;
    [JsonSerializableField]
    public CosmicImage? ExitVentFlip;
    [JsonSerializableField]
    public CosmicImage? Preview;
    [JsonSerializableField]
    public CosmicImage? Climb;
    [JsonSerializableField]
    public CosmicImage? ClimbFlip;
    [JsonSerializableField]
    public CosmicImage? ClimbDown;
    [JsonSerializableField]
    public CosmicImage? ClimbDownFlip;
    [JsonSerializableField]
    public bool Adaptive = false;
    [JsonSerializableField]
    public bool BehindHat = false;
    [JSFieldAmbiguous]
    public int FPS = 1;

    public VisorData MyVisor { get; private set; }
    public VisorViewData MyView { get; private set; }
    public bool HasClimbUpImage => Climb != null;
    public bool HasClimbDownImage => (ClimbDown ?? Climb) != null;

    public override void Activate()
    {
        foreach (var image in AllImage())
        {
            image.Pivot = new Vector2(0.53f, 0.575f);
            image.PixelsPerUnit = 112.875f;
        }

        base.Activate();

        if (!IsValid) return;

        var viewdata = ScriptableObject.CreateInstance<VisorViewData>();
        VisorData visor = ScriptableObject.CreateInstance<VisorData>();
        MyVisor = visor;
        MyView = viewdata;
        viewdata.MarkDontUnload();
        visor.MarkDontUnload();

        visor.name = UnescapedName + "\n<size=1.6>by " + UnescapedAuthor + "</size>";
        visor.displayOrder = 99;
        visor.ProductId = "nosvisor_" + Author + "_" + Name;
        visor.ChipOffset = new Vector2(0f, 0.2f);
        visor.Free = true;
        visor.PreviewCrewmateColor = Adaptive;
        visor.SpritePreview = Preview?.GetSprite(0) ?? Main?.GetSprite(0);

        if (Adaptive) viewdata.AltShader = MoreCosmic.AdaptiveShader;

        var assetRef = new AssetReference(viewdata.Pointer);
        visor.ViewDataRef = assetRef;
        visor.CreateAddressableAsset();

        if (EnterVent != null) EnterVent.RequirePlayFirstState = true;
        if (EnterVentFlip != null) EnterVentFlip.RequirePlayFirstState = true;
        if (ExitVent != null) ExitVent.RequirePlayFirstState = true;
        if (ExitVentFlip != null) ExitVentFlip.RequirePlayFirstState = true;
        if (Climb != null) Climb.RequirePlayFirstState = true;
        if (ClimbFlip != null) ClimbFlip.RequirePlayFirstState = true;
        if (ClimbDown != null) ClimbDown.RequirePlayFirstState = true;
        if (ClimbDownFlip != null) ClimbDownFlip.RequirePlayFirstState = true;

        MoreCosmic.AllVisors.Add(visor.ProductId, this);
    }
    public override string Category { get => "visors"; }
}

public class CosmicNamePlate : CustomCosmicItem
{
    [JsonSerializableField]
    public CosmicImage? Plate;

    public NamePlateData MyPlate { get; private set; }
    public NamePlateViewData MyView { get; private set; }
    public override void Activate()
    {
        base.Activate();
        if (!IsValid) return;

        var viewdata = ScriptableObject.CreateInstance<NamePlateViewData>();
        NamePlateData nameplate = ScriptableObject.CreateInstance<NamePlateData>();
        MyPlate = nameplate;
        MyView = viewdata;
        viewdata.MarkDontUnload();
        nameplate.MarkDontUnload();

        viewdata.Image = Plate?.GetSprite(0);

        nameplate.name = UnescapedName + "\n<size=1.6>by " + UnescapedAuthor + "</size>";
        nameplate.displayOrder = 99;
        nameplate.ProductId = "nosplate_" + Author + "_" + Name;
        nameplate.ChipOffset = new Vector2(0f, 0.2f);
        nameplate.Free = true;
        nameplate.SpritePreview = Plate?.GetSprite(0);
        var assetRef = new AssetReference(viewdata.Pointer);
        nameplate.ViewDataRef = assetRef;
        nameplate.CreateAddressableAsset();

        MoreCosmic.AllNameplates.Add(nameplate.ProductId, this);
    }
    public override string Category { get => "namePlates"; }
}

public class CosmicPackage : CustomItemGrouped
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
    public List<CosmicHat> Hats = new();
    [JSFieldAmbiguous]
    public List<CosmicVisor> Visors = new();
    [JSFieldAmbiguous]
    public List<CosmicNamePlate> NamePlates = new();
    [JSFieldAmbiguous]
    public List<CosmicPackage> Packages = new();

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
        
        var visorList = HatManager.Instance.allVisors.ToList();
        foreach (var item in Visors) if (item.IsValid) visorList.Add(item.MyVisor);
        HatManager.Instance.allVisors = visorList.ToArray();

        var nameplateList = HatManager.Instance.allNamePlates.ToList();
        foreach (var item in NamePlates) if (item.IsValid) nameplateList.Add(item.MyPlate);
        HatManager.Instance.allNamePlates = nameplateList.ToArray();
    }

    public Stream? OpenStream(string path)
    {
        if (RelatedLocalAddress != null)
        {
            string address = RelatedLocalAddress + path;
            if (!File.Exists(address)) return null;
            return File.OpenRead(address);
        }
        return null;
    }

    public async Task DownloadAsset(string category,string localHolder,string address)
    {
        //リモートリポジトリやローカルの配置先が無い場合はダウンロードできない
        if (RelatedRemoteAddress == null || RelatedLocalAddress == null) return;

        var hatFileResponse = await HttpClient.GetAsync(RelatedRemoteAddress + category + "/" + address, HttpCompletionOption.ResponseContentRead);
        if (hatFileResponse.StatusCode != HttpStatusCode.OK) return;

        using var responseStream = await hatFileResponse.Content.ReadAsStreamAsync();
        //サブディレクトリまでを作っておく
        string localPath = RelatedLocalAddress + category + "/" + localHolder + "/" + address;

        var dir = Path.GetDirectoryName(localPath);
        if(dir!=null)Directory.CreateDirectory(dir);

        using var fileStream = File.Create(localPath);
        responseStream.CopyTo(fileStream);
    }

    public ITextureLoader GetTextureLoader(string category,string subholder,string address)
    {
        if (RelatedZip != null)
            return null;
        else
            return new DiskTextureLoader(RelatedLocalAddress + category + "/" + subholder + "/" + address);
    }

    static public async Task<CustomItemBundle?> LoadOnline(string url)
    {
        HttpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        var response = await HttpClient.GetAsync(new System.Uri($"{url}/Contents.json"), HttpCompletionOption.ResponseContentRead);
        if (response.StatusCode != HttpStatusCode.OK) return null;

        using StreamReader stream = new(await response.Content.ReadAsStreamAsync(),Encoding.UTF8);
        string json = stream.ReadToEnd();
        CustomItemBundle? bundle = (CustomItemBundle?)JsonStructure.Deserialize(json, typeof(CustomItemBundle));
        
        if (bundle == null) return null;

        bundle.RelatedRemoteAddress = url;
        bundle.RelatedLocalAddress = "MoreCosmic/";
        if (bundle.BundleName == null) bundle.BundleName = url;
        await bundle.Load();

        return bundle;
    }

    static public CustomItemBundle LoadOffline()
    {
        return null;
    }
}

[NebulaPreLoad]
public static class MoreCosmic
{
    public static Dictionary<string, CosmicHat> AllHats = new();
    public static Dictionary<string, CosmicVisor> AllVisors = new();
    public static Dictionary<string, CosmicNamePlate> AllNameplates = new();
    public static Dictionary<string, CosmicPackage> AllPackages = new();

    private static Material? adaptiveShader = null;
    public static Material AdaptiveShader { get {
            if (adaptiveShader == null) adaptiveShader = HatManager.Instance.PlayerMaterial;
            return adaptiveShader;
        } }

    private static bool isLoaded = false;
    private static List<CustomItemBundle?> loadedBundles = new();
    private static async Task LoadOnline()
    {
        var response = await CustomItemBundle.HttpClient.GetAsync(new System.Uri("https://raw.githubusercontent.com/Dolly1016/MoreCosmic/master/UserCosmics.dat"), HttpCompletionOption.ResponseContentRead);
        if (response.StatusCode != HttpStatusCode.OK) return;

        string repos = await response.Content.ReadAsStringAsync();

        while (!HatManager.InstanceExists) await Task.Delay(1000);

        foreach (string repo in repos.Split("\n"))
        {
            try
            {
                var result = await Modules.CustomItemBundle.LoadOnline(repo);

                lock (loadedBundles)
                {
                    loadedBundles.Add(result);
                }
            }
            catch { }
        }
    }

    public static void Update()
    {
        lock (loadedBundles)
        {
            if (loadedBundles.Count > 0)
            {
                foreach (var bundle in loadedBundles) bundle.Activate();
                loadedBundles.Clear();
            }
        }
    }

    public static void Load()
    {
        if (isLoaded) return;

        var detached = LoadOnline();

        isLoaded = true;
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
        asset.viewData = value.MyView;
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

[HarmonyPatch(typeof(HatParent), nameof(HatParent.LateUpdate))]
public class HatLateUpdatePatch
{
    public static bool Prefix(HatParent __instance)
    {
        try
        {
            if (!__instance.Hat) return true;
            return !MoreCosmic.AllHats.ContainsKey(__instance.Hat.ProductId);
        }catch { return true; }
    }
}

[HarmonyPatch(typeof(VisorLayer), nameof(VisorLayer.SetVisor), new Type[] { typeof(VisorData), typeof(int) })]
public class SetVisorPatch
{
    public static bool Prefix(VisorLayer __instance,[HarmonyArgument(0)] VisorData data,[HarmonyArgument(1)] int colorId)
    {
        if (!MoreCosmic.AllVisors.TryGetValue(data.ProductId, out var value)) return true;

        __instance.currentVisor = data;
        __instance.UnloadAsset();

        var asset = new WrappedVisorAsset();
        asset.viewData = value.MyView;
        __instance.viewAsset = asset.Cast<AddressableAsset<VisorViewData>>();

        __instance.LoadAssetAsync(__instance.viewAsset, (Il2CppSystem.Action)(()=>
        {
            if (__instance.viewAsset.GetAsset() == null) return;
            if (__instance.IsDestroyedOrNull() || __instance.gameObject.IsDestroyedOrNull()) return;
            __instance.SetVisor(__instance.currentVisor, __instance.viewAsset.GetAsset(), colorId);
        }), null);
        return false;
    }
}

[HarmonyPatch(typeof(VisorData), nameof(VisorData.CoLoadIcon))]
public class CoLoadIconPatch
{
    public static bool Prefix(VisorData __instance, ref Il2CppSystem.Collections.IEnumerator __result, [HarmonyArgument(0)] Il2CppSystem.Action<Sprite, AddressableAsset> onLoaded)
    {
        if (!MoreCosmic.AllVisors.TryGetValue(__instance.ProductId, out var value)) return true;

        IEnumerator GetEnumerator()
        {
            var asset = new WrappedVisorAsset();
            asset.viewData = value.MyView;
            yield return asset.CoLoadAsync(null);
            VisorViewData viewData = asset.GetAsset();
            Sprite sprite = ((viewData != null) ? viewData.IdleFrame : null);
            onLoaded.Invoke(sprite, asset);
            yield break;
        }
        __result = GetEnumerator().WrapToIl2Cpp();
        return false;
    }
}

[HarmonyPatch(typeof(CosmeticsCache), nameof(CosmeticsCache.CoAddVisor))]
public class CoAddVisorPatch
{
    public static bool Prefix(CosmeticsCache __instance, ref Il2CppSystem.Collections.IEnumerator __result,[HarmonyArgument(0)] string visorId)
    {
        if (!MoreCosmic.AllVisors.TryGetValue(visorId, out var value)) return true;

        IEnumerator GetEnumerator()
        {
            var asset = new WrappedVisorAsset();
            asset.viewData = value.MyView;
            __instance.allCachedAssets.Add(asset.Cast<AddressableAsset<VisorViewData>>());
            yield return asset.CoLoadAsync(null);
            __instance.visors[visorId] = asset;

            asset = null;

            yield break;
        }
        __result = GetEnumerator().WrapToIl2Cpp();
        return false;
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.StartClimb))]
public class ClimbVisorPatch
{
    private static void Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] bool down)
    {
        
        try
        {
            if (!MoreCosmic.AllVisors.TryGetValue(__instance.myPlayer.cosmetics.visor.currentVisor.ProductId, out var value)) return;

            if(down ? value.HasClimbDownImage : value.HasClimbUpImage) __instance.myPlayer.cosmetics.ToggleVisor(true);
        }
        catch { }
        
        __instance.myPlayer.cosmetics.ToggleVisor(true);
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ResetAnimState))]
public static class HatFixPatch
{
    public static void Postfix(PlayerPhysics __instance)
    {
        if (NebulaGameManager.Instance == null) return;
        if (NebulaGameManager.Instance.GameState == NebulaGameStates.NotStarted) return;

        try
        {
            __instance.myPlayer.cosmetics.SetHatAndVisorIdle(__instance.myPlayer.GetModInfo().CurrentOutfit.ColorId);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.PlayerFall))]
public static class ExiledPlayerHideHandsPatch
{
    public static void Postfix(AirshipExileController __instance)
    {
        HatParent hp = __instance.Player.cosmetics.hat;
        if (hp.Hat == null) return;

        if (MoreCosmic.AllHats.TryGetValue(hp.Hat.ProductId, out var modHat))
            if (modHat.HideHands) foreach (var hand in __instance.Player.Hands) hand.gameObject.SetActive(false);
    }
}

[HarmonyPatch(typeof(CosmeticsLayer), nameof(CosmeticsLayer.EnsureInitialized))]
public class NebulaCosmeticsLayerPatch
{
    private static void Postfix(CosmeticsLayer __instance)
    {
        if (!__instance.gameObject.TryGetComponent<NebulaCosmeticsLayer>(out var c))__instance.gameObject.AddComponent<NebulaCosmeticsLayer>();
    }
}

public enum PlayerAnimState
{
    Idle,
    Run,
    ClimbUp,
    ClimbDown,
    EnterVent,
    ExitVent
}

public class NebulaCosmeticsLayer : MonoBehaviour
{
    public CosmeticsLayer MyLayer;
    public PlayerPhysics? MyPhysics;

    public HatData? CurrentHat;
    public VisorData? CurrentVisor;
    public CosmicHat? CurrentModHat;
    public CosmicVisor? CurrentModVisor;

    public float HatTimer = 0f;
    public int HatFrontIndex = 0;
    public int HatBackIndex = 0;

    public float VisorTimer = 0f;
    public int VisorIndex = 0;

    private CosmicImage? lastHatFrontImage = null;
    private CosmicImage? lastHatBackImage = null;
    private CosmicImage? lastVisorImage = null;

    static NebulaCosmeticsLayer()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaCosmeticsLayer>();
    }

    public void Awake()
    {
        MyLayer = gameObject.GetComponent<CosmeticsLayer>();
        transform.parent?.gameObject.TryGetComponent<PlayerPhysics>(out MyPhysics);
    }

    public void LateUpdate()
    {
        try
        {

            if (MyLayer.hat != null && CurrentHat != MyLayer.hat.Hat)
            {
                CurrentHat = MyLayer.hat.Hat;
                MoreCosmic.AllHats.TryGetValue(MyLayer.hat.Hat.ProductId, out CurrentModHat);
                HatFrontIndex = HatBackIndex = 0;
            }
            if (MyLayer.visor != null && CurrentVisor != MyLayer.visor.currentVisor)
            {
                CurrentVisor = MyLayer.visor.currentVisor;
                MoreCosmic.AllVisors.TryGetValue(MyLayer.visor.currentVisor.ProductId, out CurrentModVisor);
                VisorIndex = 0;
            }

            PlayerAnimState animState = PlayerAnimState.Idle;
            bool flip = MyLayer.FlipX;

            if (MyPhysics)
            {
                var anim = MyPhysics.Animations;
                if (anim)
                {
                    var current = anim.Animator.m_currAnim;
                    if (current == anim.group.ClimbUpAnim)
                        animState = PlayerAnimState.ClimbUp;
                    else if (current == anim.group.ClimbDownAnim)
                        animState = PlayerAnimState.ClimbDown;
                    else if (current == anim.group.RunAnim)
                        animState = PlayerAnimState.Run;
                    else if (current == anim.group.EnterVentAnim)
                        animState = PlayerAnimState.EnterVent;
                    else if (current == anim.group.ExitVentAnim)
                        animState = PlayerAnimState.ExitVent;
                }
            }

            void SetImage(ref CosmicImage? current, CosmicImage? normal, CosmicImage? flipped)
            {
                current = (flip ? normal : flipped) ?? normal ?? flipped ?? current;
            }

            if (CurrentModHat != null)
            {
                //タイマーの更新
                HatTimer -= Time.deltaTime;
                if (HatTimer < 0f)
                {
                    HatTimer = 1f / (float)CurrentModHat.FPS;
                    HatBackIndex++;
                    HatFrontIndex++;
                }

                //表示する画像の選定
                CosmicImage? frontImage = null;
                CosmicImage? backImage = null;
                if (animState is not PlayerAnimState.ClimbUp and not PlayerAnimState.ClimbDown)
                {
                    SetImage(ref frontImage, CurrentModHat.Main, CurrentModHat.Flip);
                    SetImage(ref backImage, CurrentModHat.Back, CurrentModHat.BackFlip);
                }

                switch (animState)
                {
                    case PlayerAnimState.Run:
                        SetImage(ref frontImage, CurrentModHat.Move, CurrentModHat.MoveFlip);
                        SetImage(ref backImage, CurrentModHat.MoveBack, CurrentModHat.MoveBackFlip);
                        break;
                    case PlayerAnimState.ClimbUp:
                        SetImage(ref frontImage, CurrentModHat.Climb, CurrentModHat.ClimbFlip);
                        backImage = null;
                        break;
                    case PlayerAnimState.ClimbDown:
                        SetImage(ref frontImage, CurrentModHat.Climb, CurrentModHat.ClimbFlip);
                        SetImage(ref frontImage, CurrentModHat.ClimbDown, CurrentModHat.ClimbDownFlip);
                        backImage = null;
                        break;
                    case PlayerAnimState.EnterVent:
                        SetImage(ref frontImage, CurrentModHat.EnterVent, CurrentModHat.EnterVentFlip);
                        SetImage(ref backImage, CurrentModHat.EnterVentBack, CurrentModHat.EnterVentBackFlip);
                        break;
                    case PlayerAnimState.ExitVent:
                        SetImage(ref frontImage, CurrentModHat.ExitVent, CurrentModHat.ExitVentFlip);
                        SetImage(ref backImage, CurrentModHat.ExitVentBack, CurrentModHat.ExitVentBackFlip);
                        break;
                }

                //インデックスの調整
                HatFrontIndex %= frontImage?.Length ?? 1;
                HatBackIndex %= backImage?.Length ?? 1;
                if (lastHatFrontImage != frontImage && (frontImage?.RequirePlayFirstState ?? true)) HatFrontIndex = 0;
                if (lastHatBackImage != backImage && (backImage?.RequirePlayFirstState ?? true)) HatBackIndex = 0;
                lastHatFrontImage = frontImage;
                lastHatBackImage = backImage;

                MyLayer.hat.FrontLayer.sprite = frontImage?.GetSprite(HatFrontIndex) ?? null;
                MyLayer.hat.BackLayer.sprite = backImage?.GetSprite(HatBackIndex) ?? null;

                MyLayer.hat.FrontLayer.enabled = true;
                MyLayer.hat.BackLayer.enabled = true;

                MyLayer.hat.FrontLayer.transform.SetLocalZ(MyLayer.visor.Image.transform.localPosition.z * (CurrentModHat.IsSkinny ? 0.5f : 1.3f));
            }
            else
            {
                MyLayer.hat.FrontLayer.transform.SetLocalZ(MyLayer.visor.Image.transform.localPosition.z * 1.3f);
            }
            if (CurrentModVisor != null)
            {
                //タイマーの更新
                VisorTimer -= Time.deltaTime;
                if (VisorTimer < 0f)
                {
                    VisorTimer = 1f / (float)CurrentModVisor.FPS;
                    VisorIndex++;
                }

                //表示する画像の選定
                CosmicImage? image = null;

                if (animState is not PlayerAnimState.ClimbUp and not PlayerAnimState.ClimbDown)
                    SetImage(ref image, CurrentModVisor.Main, CurrentModVisor.Flip);

                switch (animState)
                {
                    case PlayerAnimState.Run:
                        SetImage(ref image, CurrentModVisor.Move, CurrentModVisor.MoveFlip);
                        break;
                    case PlayerAnimState.EnterVent:
                        SetImage(ref image, CurrentModVisor.EnterVent, CurrentModVisor.EnterVentFlip);
                        break;
                    case PlayerAnimState.ExitVent:
                        SetImage(ref image, CurrentModVisor.ExitVent, CurrentModVisor.ExitVentFlip);
                        break;
                    case PlayerAnimState.ClimbUp:
                        SetImage(ref image, CurrentModVisor.Climb, CurrentModVisor.ClimbFlip);
                        break;
                    case PlayerAnimState.ClimbDown:
                        SetImage(ref image, CurrentModVisor.Climb, CurrentModVisor.ClimbFlip);
                        SetImage(ref image, CurrentModVisor.ClimbDown, CurrentModVisor.ClimbDownFlip);
                        break;
                }

                //インデックスの調整
                VisorIndex %= image?.Length ?? 1;
                if (lastVisorImage != image && (image?.RequirePlayFirstState ?? true)) VisorIndex = 0;
                lastVisorImage = image;

                MyLayer.visor.Image.sprite = image?.GetSprite(VisorIndex) ?? null;


            }
        }
        catch { }
    }
}