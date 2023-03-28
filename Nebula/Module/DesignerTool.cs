using Nebula.Expansion;
using ExtremeSkins.Core;
using ExtremeSkins.Core.ExtremeHats;
using ExtremeSkins.Core.ExtremeVisor;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Events;
using static Nebula.Game.PlayerData;
using static Nebula.Module.CustomDesignerSublist;
using static Nebula.Module.CustomParts;
using static Rewired.Controller;

using ExHData = ExtremeSkins.Core.ExtremeHats.DataStructure;
using ExVData = ExtremeSkins.Core.ExtremeVisor.DataStructure;

namespace Nebula.Module;

public class DesignerFileAccepter : MonoBehaviour
{
    public delegate void GetFilePathEvent(List<string> pathes);
    private FileInput.UnityDragAndDropHook.DroppedFilesEvent OnGetFilePath;

    static DesignerFileAccepter()
    {
        ClassInjector.RegisterTypeInIl2Cpp<DesignerFileAccepter>();
    }

    static public void Show(GameObject parent, GetFilePathEvent onGetFilePath)
    {
        GameObject obj = new GameObject("FileAccepter");
        obj.transform.SetParent(parent.transform);
        obj.SetActive(false);
        obj.transform.localPosition = new Vector3(0, 0, -300);
        var accepter = obj.AddComponent<DesignerFileAccepter>();
        accepter.OnGetFilePath = (pathes, point) =>
        {
            onGetFilePath(pathes);
            GameObject.Destroy(obj);
        };

        obj.SetActive(true);
    }

    public void Awake()
    {
        Nebula.Module.FileInput.UnityDragAndDropHook.InstallHook();
        Nebula.Module.FileInput.UnityDragAndDropHook.OnDroppedFiles += OnGetFilePath;

        var backBlackPrefab = CustomDesignerBase.MainMenu.playerCustomizationPrefab.transform.GetChild(1);
        var background = GameObject.Instantiate(backBlackPrefab.gameObject, transform);
        background.gameObject.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.5f, 0.9f, 0.75f);
        var collider =gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(100f, 100f);
        gameObject.SetUpButton(() => GameObject.Destroy(gameObject));

        var screen = MetaScreen.OpenScreen(gameObject, new Vector2(1f,1f),Vector2.zero);
        var str = new MSString(3f, Language.Language.GetString("designers.ui.drag"), 4f, 4f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
        screen.AddTopic(str);
        str.text.outlineColor = Color.clear;
    }

    public void OnDestroy()
    {
        Nebula.Module.FileInput.UnityDragAndDropHook.UninstallHook();
        Nebula.Module.FileInput.UnityDragAndDropHook.OnDroppedFiles -= OnGetFilePath;
    }
}

public class DesignerTextureSplitter : MonoBehaviour
{
    private Texture2D Texture;
    private Action<int> OnDecideSplit;
    static DesignerTextureSplitter()
    {
        ClassInjector.RegisterTypeInIl2Cpp<DesignerTextureSplitter>();
    }

    static public void Show(GameObject parent,Texture2D texture,Action<int> onDecideSplit)
    {
        GameObject obj = new GameObject("TextureSplitter");
        obj.transform.SetParent(parent.transform);
        obj.SetActive(false);
        obj.transform.localPosition = new Vector3(0, 0, -300);
        var accepter = obj.AddComponent<DesignerTextureSplitter>();
        accepter.Texture = texture;
        accepter.OnDecideSplit = onDecideSplit;
        obj.SetActive(true);
    }

    public void Awake()
    {
        var backBlackPrefab = CustomDesignerBase.MainMenu.playerCustomizationPrefab.transform.GetChild(1);
        var background = GameObject.Instantiate(backBlackPrefab.gameObject, transform);
        background.gameObject.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.75f);
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(100f, 100f);
        gameObject.SetUpButton(() => GameObject.Destroy(gameObject));

        var screen = MetaScreen.OpenScreen(gameObject, new Vector2(4f, 2.5f), Vector2.zero);

        Sprite sprite = Helpers.loadSpriteFromTexture(Texture,Texture.height*0.8f);
        screen.AddTopic(new MSSprite(new SpriteLoader(sprite), 0.1f, 1f));

        int split = 4;
        MSString splitStr = new MSString(1f,split.ToString(),TMPro.TextAlignmentOptions.Center,TMPro.FontStyles.Bold);
        screen.AddTopic(
            new MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Normal, () => { if (split > 1) split--; splitStr.text.text = split.ToString(); }),
            splitStr,
            new MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Normal, () => { if (split < 32) split++; splitStr.text.text = split.ToString(); }));

        screen.AddTopic(new MSButton(1f, 0.4f, Language.Language.GetString("config.option.confirm"), TMPro.FontStyles.Normal, () => {
            OnDecideSplit(split);
            GameObject.Destroy(gameObject);
        }));


    }
}

public static class DesignersSaver
{

    private static void ExportCosmics(string exportTo, string category,MD5 md5, JsonObjectContent rootContent, CustomItem[] items)
    {
        string prePath = category + "/";
        foreach (var item in items)
        {
            foreach (var c in item.Contents())
            {
                if (c is CustomVImage image && image.Address!=null)
                {
                    using (var stream = File.OpenRead("MoreCosmic/" + prePath + image.Address))
                    {
                        var hash = System.BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                        image.Hash = hash;
                    }
                    string exPath = exportTo + "/" + prePath + image.Address;
                    Directory.CreateDirectory(Path.GetDirectoryName(exPath));
                    if(!File.Exists(exPath))File.Copy("MoreCosmic/"+prePath + image.Address, exPath);
                }
            }
        }
        AddCosmics(category,rootContent,items);
    }

    private static void AddCosmics(string category,JsonObjectContent rootContent, CustomItem[] items)
    {
        if(items.Length == 0) return;

        var categoryContent = new JsonArrayContent();
        rootContent.AddContent(category,categoryContent);
        foreach(var item in items)
        {
            var itemContent = new JsonObjectContent();
            categoryContent.AddContent(itemContent);
            foreach (var c in item.Contents())
            {
                c.ToJson(out string label, out JsonContent? jsonContent);
                if (jsonContent != null) itemContent.AddContent(label, jsonContent);
            }
        }
    }

    public static void RegenerateDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
        Directory.CreateDirectory(path);
    }

    public static void SaveAllLocalCosmics()
    {
        JsonGenerator json = new();

        AddCosmics("hats", json.RootContent, CustomParts.CustomHatRegistry.Values.Where((c) => c.IsLocal).ToArray<CustomItem>());
        AddCosmics("visors", json.RootContent, CustomParts.CustomVisorRegistry.Values.Where((c) => c.IsLocal).ToArray<CustomItem>());
        AddCosmics("namePlates", json.RootContent, CustomParts.CustomNamePlateRegistry.Values.Where((c) => c.IsLocal).ToArray<CustomItem>());
        AddCosmics("packages", json.RootContent, CustomPackage.AllPackage.Where((c) => c.IsLocal).ToArray<CustomItem>());

        var stream = new StreamWriter(File.Create("MoreCosmic/Contents.json"));

        stream.Write(json.Generate());
        stream.Close();
    }
    

    public static void ExportAllLocalCosmics()
    {
        MD5 md5 = MD5.Create();
        JsonGenerator json = new();

        RegenerateDirectory("GlobalCosmic");

        ExportCosmics("GlobalCosmic", "hats",md5, json.RootContent,CustomParts.CustomHatRegistry.Values.Where((c) => c.IsLocal).ToArray<CustomItem>());
        ExportCosmics("GlobalCosmic", "visors", md5, json.RootContent, CustomParts.CustomVisorRegistry.Values.Where((c) => c.IsLocal).ToArray<CustomItem>());
        ExportCosmics("GlobalCosmic", "namePlates", md5, json.RootContent, CustomParts.CustomNamePlateRegistry.Values.Where((c) => c.IsLocal).ToArray<CustomItem>());
        AddCosmics("packages", json.RootContent, CustomPackage.AllPackage.Where((c) => c.IsLocal).ToArray<CustomItem>());

        var stream = new StreamWriter(File.Create("GlobalCosmic/Contents.json"));

        stream.Write(json.Generate());
        stream.Close();
    }

    public static void ExportAllLocalCosmicsToSNR()
    {
        string CopyFile(string category, CustomVImage image, string destPrePath)
        {
            string prePath = category + "/";
            string fileName = Path.GetFileName(image.Address);
            string exPath = destPrePath + fileName;
            Directory.CreateDirectory(Path.GetDirectoryName(exPath));
            if (!File.Exists(exPath)) File.Copy("MoreCosmic/" + prePath + image.Address, exPath);
            return fileName;
        }

        RegenerateDirectory("GlobalCosmicSNR");

        //MD5 md5 = MD5.Create();
        JsonGenerator json;
        JsonArrayContent arrayContent;
        StreamWriter stream;

        json = new();
        var hats = CustomParts.CustomHatRegistry.Values.Where((c) => c.IsLocal).ToArray();
        arrayContent = new();
        json.RootContent.AddContent("hats",arrayContent);
        foreach(var hat in hats)
        {
            //メイン画像が無いあるいはアニメーションハットの場合はスルー
            if (!hat.I_Main || !hat.I_Climb) continue;
            if(hat.Contents().Any((v) => v is CustomVImage image && image.Length>1))continue;
            var content = new JsonObjectContent();
            content.AddContent("name", new JsonStringContent(hat.Name.Value));
            content.AddContent("author", new JsonStringContent(hat.Author.Value));
            content.AddContent("package", new JsonStringContent(hat.Package.Value));
            content.AddContent("condition", new JsonStringContent("None"));
            content.AddContent("resource", new JsonStringContent(hat.Name.Value + CopyFile("hats", hat.I_Main, "GlobalCosmicSNR/hats/" + hat.Name.Value)));
            content.AddContent("climbresource", new JsonStringContent(hat.Name.Value + CopyFile("hats", hat.I_Climb, "GlobalCosmicSNR/hats/" + hat.Name.Value)));
            if (hat.I_Flip) content.AddContent("flipresource", new JsonStringContent(hat.Name.Value + CopyFile("hats", hat.I_Flip, "GlobalCosmicSNR/hats/" + hat.Name.Value)));
            if (hat.I_Back) content.AddContent("backresource", new JsonStringContent(hat.Name.Value + CopyFile("hats", hat.I_Back, "GlobalCosmicSNR/hats/" + hat.Name.Value)));
            if (hat.I_BackFlip) content.AddContent("backflipresource", new JsonStringContent(hat.Name.Value + CopyFile("hats", hat.I_BackFlip, "GlobalCosmicSNR/hats/" + hat.Name.Value)));
            if (hat.Adaptive.Value) content.AddContent("adaptive", new JsonBooleanContent(true));
            arrayContent.AddContent(content);
        }
        if (arrayContent.Length != 0)
        {
            stream = new StreamWriter(File.Create("GlobalCosmicSNR/CustomHats.json"));
            stream.Write(json.Generate());
            stream.Close();
        }

        json = new();
        var visors = CustomParts.CustomVisorRegistry.Values.Where((c) => c.IsLocal).ToArray();
        arrayContent = new();
        json.RootContent.AddContent("Visors", arrayContent);
        foreach (var visor in visors)
        {
            //メイン画像が無いあるいはAdaptive,アニメーションバイザーの場合はスルー
            if (!visor.I_Main || visor.Adaptive.Value) continue;
            if(visor.Contents().Any((v) => v is CustomVImage image && image.Length > 1))continue;
            var content = new JsonObjectContent();
            content.AddContent("name", new JsonStringContent(visor.Name.Value));
            content.AddContent("author", new JsonStringContent(visor.Author.Value));
            content.AddContent("resource", new JsonStringContent(visor.Name.Value + CopyFile("visors", visor.I_Main, "GlobalCosmicSNR/Visors/" + visor.Name.Value)));
            arrayContent.AddContent(content);
        }
        if (arrayContent.Length != 0)
        {
            stream = new StreamWriter(File.Create("GlobalCosmicSNR/CustomVisors.json"));
            stream.Write(json.Generate());
            stream.Close();
        }

        json = new();
        var nameplates = CustomParts.CustomNamePlateRegistry.Values.Where((c) => c.IsLocal).ToArray();
        arrayContent = new();
        json.RootContent.AddContent("nameplates", arrayContent);
        foreach (var nameplate in nameplates)
        {
            //メイン画像が無い場合はスルー
            if (!nameplate.I_Plate) continue;
            var content = new JsonObjectContent();
            content.AddContent("name", new JsonStringContent(nameplate.Name.Value));
            content.AddContent("author", new JsonStringContent(nameplate.Author.Value));
            content.AddContent("resource", new JsonStringContent(nameplate.Name.Value + CopyFile("namePlates", nameplate.I_Plate, "GlobalCosmicSNR/NamePlates/" + nameplate.Name.Value)));
            arrayContent.AddContent(content);
        }
        if (arrayContent.Length != 0)
        {
            stream = new StreamWriter(File.Create("GlobalCosmicSNR/CustomNamePlates.json"));
            stream.Write(json.Generate());
            stream.Close();
        }
    }

    public static void ExportAllLocalCosmicsToExR()
    {
        void CopyFile(string category, CustomVImage image, string destPath)
        {
            string prePath = "MoreCosmic/" + category + "/";
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            if (!File.Exists(destPath)) File.Copy(prePath + image.Address, destPath);
        }

        string ConvertName(string name)
        {
            var builder = new StringBuilder();
            foreach (var c in name) builder.Append(((int)c).ToString("X4"));
            return builder.ToString();
        }

        Dictionary<string, string> translationDic = new();

        const string globalExportFolder = "GlobalCosmicExR";
        RegenerateDirectory(globalExportFolder);

        const string hatCategory = "hats";
        var hats = CustomParts.CustomHatRegistry.Values.Where((c) => c.IsLocal).ToArray();
        foreach (var hat in hats)
        {
            //メイン画像が無いあるいはアニメーションハットの場合はスルー
            if (!hat.I_Main) continue;
            if (hat.Contents().Any((v) => v is CustomVImage image && image.Length > 1)) continue;
            string name = ConvertName(hat.Author.Value + hat.Name.Value);
            string authorName = ConvertName(hat.Author.Value);
            translationDic[name] = hat.Name.Value;
            translationDic[authorName] = hat.Author.Value;

            string exportHatFolder = Path.Combine(globalExportFolder, ExHData.FolderName, name);

            CopyFile(hatCategory, hat.I_Main, Path.Combine(exportHatFolder, ExHData.FrontImageName));
            if (hat.I_Flip) CopyFile(hatCategory, hat.I_Flip, Path.Combine(exportHatFolder, ExHData.FrontFlipImageName));
            if (hat.I_Back) CopyFile(hatCategory, hat.I_Back, Path.Combine(exportHatFolder, ExHData.BackImageName));
            if (hat.I_BackFlip) CopyFile(hatCategory, hat.I_BackFlip, Path.Combine(exportHatFolder, ExHData.BackFlipImageName));
            if (hat.I_Climb) CopyFile(hatCategory, hat.I_Climb, Path.Combine(exportHatFolder, ExHData.ClimbImageName));

            HatInfo hatInfo = new(
                Name: name,
                Author: authorName,
                Bound: hat.Bounce.Value,
                Shader: hat.Adaptive.Value,
                Climb: hat.I_Climb,
                FrontFlip: hat.I_Flip,
                Back: hat.I_Back,
                BackFlip: hat.I_BackFlip
            );
            InfoBase.ExportToJson(hatInfo, exportHatFolder);
        }

        const string visorCategory = "visors";
        var visors = CustomParts.CustomVisorRegistry.Values.Where((c) => c.IsLocal).ToArray();
        foreach (var visor in visors)
        {
            //メイン画像が無いあるいはアニメーションバイザーの場合はスルー
            if (!visor.I_Main) continue;
            if (visor.Contents().Any((v) => v is CustomVImage image && image.Length > 1)) continue;

            string name = ConvertName(visor.Author.Value + visor.Name.Value);
            string authorName = ConvertName(visor.Author.Value);
            translationDic[name] = visor.Name.Value;
            translationDic[authorName] = visor.Author.Value;

            string exportVisorFolder = Path.Combine(globalExportFolder, ExHData.FolderName, name);

            CopyFile(visorCategory, visor.I_Main, Path.Combine(exportVisorFolder, ExVData.IdleImageName));
            if (visor.I_Flip) CopyFile(visorCategory, visor.I_Flip, Path.Combine(exportVisorFolder, ExVData.FlipIdleImageName));

            VisorInfo visorInfo = new(
                Name: visor.Name.Value,
                Author: visor.Name.Value,
                LeftIdle: visor.I_Flip,
                Shader: visor.Adaptive.Value,
                BehindHat: visor.BehindHat.Value
            );
            InfoBase.ExportToJson(visorInfo, exportVisorFolder);
        }

        if (translationDic.Count == 0) return;
        using (var stream = CreatorMode.CreateTranslationWriter(globalExportFolder))
        {
            foreach (var entry in translationDic) stream.WriteLine($"{entry.Key},,,,,,,,,,,,{entry.Value},,,,,");
        }

        //ネームプレートは作れないのでひとまずスルー
        /*
        json = new();
        var nameplates = CustomParts.CustomNamePlateRegistry.Values.Where((c) => c.IsLocal).ToArray();
        arrayContent = new();
        json.RootContent.AddContent("nameplates", arrayContent);
        foreach (var nameplate in nameplates)
        {
            //メイン画像が無い場合はスルー
            if (!nameplate.I_Plate) continue;
            json = new();
            var content = json.RootContent;
            string name = ConvertName(visor.Author.Value + visor.Name.Value);
            string authorName = ConvertName(visor.Author.Value);
            translationDic[name] = visor.Name.Value;
            translationDic[authorName] = visor.Author.Value;

            content.AddContent("name", new JsonStringContent(name));
            content.AddContent("author", new JsonStringContent(authorName));
            
            
        }
        */
    }

}

public class DesignersImageParameter
{
    public string Path;
    public Texture2D Texture;
    public int Split;

    public DesignersImageParameter(string path,Texture2D texture,int split) {
        Path = path;
        Texture = texture;
        Split = split;
    }

    public static DesignersImageParameter? Create(CustomVImage? image)
    {
        if (image != null && image.Address != null && image.Images != null && image.Texture!=null)
            return new(image.Address, image.Texture, image.Length);
        else
            return null;
    }
}

public class DesignersHat
{
    public class DesignersHatParameter
    {
        public DesignersImageParameter? Main;
        public DesignersImageParameter? Flip;
        public DesignersImageParameter? Back;
        public DesignersImageParameter? BackFlip;
        public DesignersImageParameter? Move;
        public DesignersImageParameter? MoveFlip;
        public DesignersImageParameter? MoveBack;
        public DesignersImageParameter? MoveBackFlip;
        public DesignersImageParameter? Climb;
        public DesignersImageParameter? ClimbFlip;
        public bool? Bounce;
        public bool? Adaptive;
        public bool? Behind;
        public bool? HideHands;
        public bool? IsSkinny;
        public float? SecPerFrame;

        public DesignersHatParameter() { }
        public DesignersHatParameter(CustomHat modData) {
            Main = DesignersImageParameter.Create(modData.I_Main);
            Flip = DesignersImageParameter.Create(modData.I_Flip);
            Back = DesignersImageParameter.Create(modData.I_Back);
            BackFlip = DesignersImageParameter.Create(modData.I_BackFlip);
            Move = DesignersImageParameter.Create(modData.I_Move);
            MoveFlip = DesignersImageParameter.Create(modData.I_MoveFlip);
            MoveBack = DesignersImageParameter.Create(modData.I_MoveBack);
            MoveBackFlip = DesignersImageParameter.Create(modData.I_MoveBackFlip);
            Climb = DesignersImageParameter.Create(modData.I_Climb);
            ClimbFlip = DesignersImageParameter.Create(modData.I_ClimbFlip);
            Bounce = modData.Bounce.Value;
            Adaptive = modData.Adaptive.Value;
            Behind = modData.Behind.Value;
            HideHands = modData.HideHands.Value;
            IsSkinny = modData.IsSkinny.Value;
            SecPerFrame = modData.SecPerFrame.SecPerFrame;
        }

        public void ReflectTo(HatData vanillaData,CustomHat modData)
        {
            if (Main != null)
            {
                modData.I_Main.LoadImage(Main);
                vanillaData.hatViewData.viewData.MainImage = modData.I_Main.GetMainImage();
            }
            if (Flip != null)
            {
                modData.I_Flip.LoadImage(Flip);
                vanillaData.hatViewData.viewData.LeftMainImage = modData.I_Flip.GetMainImage();
            }
            if (Back != null)
            {
                modData.I_Back.LoadImage(Back);
                vanillaData.hatViewData.viewData.BackImage = modData.I_Back.GetMainImage();
            }
            if (BackFlip != null)
            {
                modData.I_BackFlip.LoadImage(BackFlip);
                vanillaData.hatViewData.viewData.LeftBackImage = modData.I_BackFlip.GetMainImage();
            }
            if (Move != null)
            {
                modData.I_Move.LoadImage(Move);
                vanillaData.hatViewData.viewData.MainImage = modData.I_Move.GetMainImage();
            }
            if (MoveFlip != null)
            {
                modData.I_MoveFlip.LoadImage(MoveFlip);
                vanillaData.hatViewData.viewData.LeftMainImage = modData.I_MoveFlip.GetMainImage();
            }
            if (MoveBack != null)
            {
                modData.I_MoveBack.LoadImage(MoveBack);
                vanillaData.hatViewData.viewData.BackImage = modData.I_MoveBack.GetMainImage();
            }
            if (MoveBackFlip != null)
            {
                modData.I_MoveBackFlip.LoadImage(MoveBackFlip);
                vanillaData.hatViewData.viewData.LeftBackImage = modData.I_MoveBackFlip.GetMainImage();
            }
            if (Climb != null)
            {
                modData.I_Climb.LoadImage(Climb);
                vanillaData.hatViewData.viewData.ClimbImage = modData.I_Climb.GetMainImage();
            }
            if (ClimbFlip != null)
            {
                modData.I_ClimbFlip.LoadImage(ClimbFlip);
                vanillaData.hatViewData.viewData.LeftClimbImage = modData.I_ClimbFlip.GetMainImage();
            }
            if (Bounce.HasValue)
            {
                modData.Bounce.Value = Bounce.Value;
                vanillaData.NoBounce = !Bounce.Value;
            }
            if (Adaptive.HasValue)
            {
                modData.Adaptive.Value = Adaptive.Value;
                vanillaData.hatViewData.viewData.AltShader = Adaptive.Value ? DestroyableSingleton<HatManager>.Instance.PlayerMaterial : null;
            }
            if (Behind.HasValue)
            {
                if (vanillaData.hatViewData.viewData.BackImage != null) Behind = true;
                modData.Behind.Value = Behind.Value;
                vanillaData.InFront = !Behind.Value;
            }
            else if (vanillaData.hatViewData.viewData.BackImage != null)
            {
                modData.Behind.Value = true;
                vanillaData.InFront = false;
            }
            if (IsSkinny.HasValue)
            {
                modData.IsSkinny.Value = IsSkinny.Value;
            }
            if (HideHands.HasValue)
            {
                modData.HideHands.Value = HideHands.Value;
            }
            if (SecPerFrame.HasValue)
            {
                modData.SecPerFrame.SecPerFrame = SecPerFrame.Value;
            }
        }
    }

    public HatData VanillaData { get; private set; }
    public CustomHat NebulaData { get; private set; }

    public DesignersHat()
    {
        VanillaData = ScriptableObject.CreateInstance<HatData>();
        VanillaData.hatViewData.viewData = ScriptableObject.CreateInstance<HatViewData>();

        HatViewData viewData = VanillaData.hatViewData.viewData;

        viewData.MainImage = null;
        viewData.LeftMainImage = null;
        viewData.BackImage = null;
        viewData.ClimbImage = null;
        viewData.LeftClimbImage = null;
        viewData.LeftBackImage = null;

        VanillaData.StoreName = "designerHat";
        VanillaData.name = VanillaData.StoreName;
        VanillaData.ProductId = "designerHat";
        VanillaData.InFront = true;
        VanillaData.NoBounce = true;

        viewData.AltShader = null;

        NebulaData = new CustomHat();
        NebulaData.Behind.Value = false;
        NebulaData.Bounce.Value = false;
    }

    public void CopyFrom(PlayerDisplay? display, CustomHat hat)
    {
        UpdateHat(display, new DesignersHatParameter(hat));
        NebulaData.Name.Value = hat.Name.Value;
        NebulaData.Author.Value = hat.Author.Value;
        NebulaData.Package.Value = hat.Package.Value;
    }

    public void UpdateHat(PlayerDisplay? display, DesignersHatParameter parameter)
    {
        parameter.ReflectTo(VanillaData,NebulaData);

        if (display != null)
        {
            display.Cosmetics.SetHat(VanillaData, 0);
            if (display.Animations.IsPlayingClimbAnimation())
                display.Cosmetics.AnimateClimb(true);
            else
                display.Cosmetics.SetHatAndVisorIdle(0);
        }
    }
}

public class DesignersVisor
{
    public class DesignersVisorParameter
    {
        public DesignersImageParameter? Main;
        public DesignersImageParameter? Flip;
        public bool? BehindHat;
        public bool? Adaptive;
        public float? SecPerFrame;
   
        public DesignersVisorParameter() { }
        public DesignersVisorParameter(CustomVisor modData)
        {
            Main = DesignersImageParameter.Create(modData.I_Main);
            Flip = DesignersImageParameter.Create(modData.I_Flip);

            BehindHat = modData.BehindHat.Value;
            Adaptive = modData.Adaptive.Value;
            SecPerFrame = modData.SecPerFrame.SecPerFrame;
        }

        public void ReflectTo(VisorData vanillaData, CustomVisor modData)
        {
            if (Main != null)
            {
                modData.I_Main.LoadImage(Main);
                vanillaData.viewData.viewData.IdleFrame = modData.I_Main.GetMainImage();
            }
            if (Flip != null)
            {
                modData.I_Flip.LoadImage(Flip);
                vanillaData.viewData.viewData.LeftIdleFrame = modData.I_Flip.GetMainImage();
            }
           
            if (BehindHat.HasValue)
            {
                modData.BehindHat.Value = BehindHat.Value;
                vanillaData.viewData.viewData.BehindHats = BehindHat.Value;
            }
            if (Adaptive.HasValue)
            {
                modData.Adaptive.Value = Adaptive.Value;
                vanillaData.viewData.viewData.AltShader = Adaptive.Value ? DestroyableSingleton<HatManager>.Instance.PlayerMaterial : null;
            }
            if (SecPerFrame.HasValue)
            {
                modData.SecPerFrame.SecPerFrame = SecPerFrame.Value;
            }
        }
    }

    public VisorData VanillaData { get; private set; }
    public CustomVisor NebulaData { get; private set; }
    public DesignersVisor()
    {
        VanillaData = ScriptableObject.CreateInstance<VisorData>();
        VanillaData.viewData.viewData = ScriptableObject.CreateInstance<VisorViewData>();

        VanillaData.viewData.viewData.IdleFrame = null;
        VanillaData.viewData.viewData.LeftIdleFrame = null;
        VanillaData.name = "designerVisor";
        VanillaData.ProductId = "designerVisor";
        VanillaData.viewData.viewData.BehindHats = false;
        VanillaData.viewData.viewData.AltShader = null;

        NebulaData = new CustomVisor();
        NebulaData.BehindHat.Value = false;
    }

    public void CopyFrom(PlayerDisplay? display, CustomVisor visor)
    {
        UpdateVisor(display, new DesignersVisorParameter(visor));
        NebulaData.Name.Value = visor.Name.Value;
        NebulaData.Author.Value = visor.Author.Value;
        NebulaData.Package.Value = visor.Package.Value;
    }

    public void UpdateVisor(PlayerDisplay display, DesignersVisorParameter parameter)
    {
        parameter.ReflectTo(VanillaData, NebulaData);

        if (display != null)
        {
            display.Cosmetics.SetVisor(VanillaData, 0);
            if (display.Animations.IsPlayingClimbAnimation())
                display.Cosmetics.AnimateClimb(true);
            else
                display.Cosmetics.SetHatAndVisorIdle(0);
        }
    }
}


public class DesignersNameplate
{
    public class DesignersNameplateParameter
    {
        public DesignersImageParameter? Plate;

        public DesignersNameplateParameter() { }
        public DesignersNameplateParameter(CustomNamePlate modData)
        {
            Plate = DesignersImageParameter.Create(modData.I_Plate);
        }

        public void ReflectTo(NamePlateData vanillaData, CustomNamePlate modData)
        {
            if (Plate != null)
            {
                modData.I_Plate.LoadImage(Plate);
                vanillaData.viewData.viewData.Image = modData.I_Plate.GetMainImage();
            }
        }
    }

    public NamePlateData VanillaData { get; private set; }
    public CustomNamePlate NebulaData { get; private set; }
    public DesignersNameplate()
    {
        VanillaData = ScriptableObject.CreateInstance<NamePlateData>();
        VanillaData.viewData.viewData = ScriptableObject.CreateInstance<NamePlateViewData>();

        VanillaData.viewData.viewData.Image = null;
        VanillaData.name = "designerNameplate";
        VanillaData.ProductId = "designerNameplate";

        NebulaData = new CustomNamePlate();
    }

    public void CopyFrom(SpriteRenderer? display, CustomNamePlate nameplate)
    {
        UpdateNameplate(display, new DesignersNameplateParameter(nameplate));
        NebulaData.Name.Value = nameplate.Name.Value;
        NebulaData.Author.Value = nameplate.Author.Value;
        NebulaData.Package.Value = nameplate.Package.Value;
    }

    public void UpdateNameplate(SpriteRenderer? display, DesignersNameplateParameter parameter)
    {
        parameter.ReflectTo(VanillaData, NebulaData);

        if (display != null)
        {
            display.sprite = NebulaData.I_Plate.GetMainImage();
        }
    }
}

public class CustomDesignerBase : MonoBehaviour
{
    protected static SpriteLoader IconSprite = new SpriteLoader("Nebula.Resources.ReloadIcon.png", 100f);

    protected MetaScreen.MSDesigner ShowDialog(Vector2 size)
    {
        var designer = MetaScreen.OpenScreen(gameObject, size, Vector2.zero);
        designer.MakeIntoPseudoScreen().OnClick.AddListener((UnityAction)(() => { designer.screen.Close(); }));
        designer.screen.screen.transform.localPosition += new Vector3(0, 0, -100f);
        return designer;
    }

    protected MetaScreen.MSDesigner ShowCompleteDialog() { 
        var noticeDialog = ShowDialog(new Vector2(3f, 1.4f));
        noticeDialog.AddTopic(new MSString(3f, Language.Language.GetString("designers.dialog.exportSuccessfully"), 1.5f, 1.5f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));
        noticeDialog.AddTopic(new MSButton(1.2f, 0.4f, Language.Language.GetString("config.option.confirm"), TMPro.FontStyles.Bold, () => noticeDialog.screen.Close()));
        return noticeDialog;
    }

    static CustomDesignerBase()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomDesignerBase>();
    }

    static public MainMenuManager MainMenu;
    
    protected void Close()
    {
        DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(gameObject, null, (Il2CppSystem.Action)(() => { GameObject.Destroy(gameObject); }));
    }

    public void Awake()
    {
        var backBlackPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(1);
        GameObject.Instantiate(backBlackPrefab.gameObject, transform);
        var backGroundPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(2);
        var backGround = GameObject.Instantiate(backGroundPrefab.gameObject, transform);
        GameObject.Destroy(backGround.transform.GetChild(2).gameObject);

        var closeButtonPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(0).GetChild(0);
        var closeButton = GameObject.Instantiate(closeButtonPrefab.gameObject, transform);
        GameObject.Destroy(closeButton.GetComponent<AspectPosition>());
        var button = closeButton.GetComponent<PassiveButton>();
        button.gameObject.SetActive(true);
        button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.OnClick.AddListener((UnityEngine.Events.UnityAction)Close);
        button.transform.localPosition = new Vector3(-4.9733f, 2.6708f, -50f);
    }
}

public class CustomDesignerMainMenu : CustomDesignerBase
{
    static CustomDesignerMainMenu()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomDesignerMainMenu>();
    }

    static public CustomDesignerMainMenu Instance { get; private set; }

    public Il2CppReferenceField<CustomDesignerSublist> Sublist;

    public MSButton[] Packages = new MSButton[5];
    public CustomPackage?[] LinkedPackages = new CustomPackage?[5];

    static public void Open(MainMenuManager mainMenuManager)
    {
        MainMenu = mainMenuManager;

        var obj = new GameObject("DesignerToolMenu");
        obj.transform.localPosition = new Vector3(0, 0, -30f);
        obj.SetActive(false);
        var designer = obj.AddComponent<CustomDesignerMainMenu>();

        DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(null, obj, null);
    }

    protected void Close()
    {
        DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(gameObject, null, (Il2CppSystem.Action)(() => { GameObject.Destroy(gameObject); }));
    }

    public void Awake()
    {
        Instance = this;

        base.Awake();

        Sublist.Set(new GameObject("Sublist").AddComponent<CustomDesignerSublist>());
        var sublist = Sublist.Get();
        sublist.transform.SetParent(transform);
        sublist.transform.localPosition = new Vector3(-2f,0f);
        sublist.SetType(DesignerType.Hat);

        var packageScreen = MetaScreen.OpenScreen(gameObject, new Vector2(2.5f, 3.9f), new Vector2(2.9f, 0.5f));
        packageScreen.AddTopic(new MSString(2f, Language.Language.GetString("designers.packages"), TMPro.TextAlignmentOptions.Center,TMPro.FontStyles.Bold));
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            Packages[i] = new MSButton(2.5f, 0.5f, "", TMPro.FontStyles.Bold, () => {
                var customDesigner = DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade<CustomPackageDesigner>("PackageDesigner", -110f);
                if (LinkedPackages[index] != null)
                {
                    customDesigner.gameObject.SetActive(true);
                    customDesigner.SetTargetPackage(LinkedPackages[index]);
                    customDesigner.gameObject.SetActive(false);
                }
            });
            packageScreen.AddTopic(Packages[i]);
        }

        packageScreen.CustomUse(0.5f);
        packageScreen.AddTopic(new MSButton(1.7f, 0.6f, Language.Language.GetString("designers.button.export"), TMPro.FontStyles.Bold, () =>
        {
            DesignersSaver.ExportAllLocalCosmics();

            ShowCompleteDialog();
        }),
        new MSButton(0.4f, 0.4f, "...", TMPro.FontStyles.Bold, () =>
        {
            var designer = ShowDialog(new Vector2(4f,2.2f));
            designer.AddTopic(new MSString(1.7f, Language.Language.GetString("designers.external"),TMPro.TextAlignmentOptions.Center,TMPro.FontStyles.Normal));
            designer.CustomUse(0.2f);
            designer.AddTopic(new MSButton(3f, 0.4f, Language.Language.GetString("designers.external.superNewRoles"), TMPro.FontStyles.Bold, () =>
            {
                DesignersSaver.ExportAllLocalCosmicsToSNR();
                designer.screen.Close();

                ShowCompleteDialog();
            }));
            designer.AddTopic(new MSButton(3f, 0.4f, Language.Language.GetString("designers.external.extremeRoles"), TMPro.FontStyles.Bold, () =>
            {
                DesignersSaver.ExportAllLocalCosmicsToExR();
                designer.screen.Close();

                ShowCompleteDialog();
            }));
        }
        ));
        

        Reload();
    }

    public void Reload()
    {
        var linkedPackagesCandidate = CustomPackage.AllPackage.Where((p) => p.IsLocal).ToArray();
        for(int i = 0; i < 5; i++)
        {
            if (linkedPackagesCandidate.Length > i) 
                LinkedPackages[i] = linkedPackagesCandidate[i];
            else 
                LinkedPackages[i] = null;
            Packages[i].text.text = LinkedPackages[i]?.GetFormatted() ?? Language.Language.GetString("designers.packages.empty");
        }
    }
}

public class CustomDesignerChip
{
    public MSButton Button { get; set; }
    private SpriteRenderer FrontLayer { get; set; }
    private SpriteRenderer BackLayer { get; set; }
    private SpriteRenderer NameplateLayer { get; set; }
    public CustomCosmicItem Item { get; private set; }

    public CustomDesignerChip(MSButton button) {
        Button = button;
        Button.PostBuilder = (obj) =>
        {
            FrontLayer = new GameObject("FrontLayer").AddComponent<SpriteRenderer>();
            BackLayer = new GameObject("BackLayer").AddComponent<SpriteRenderer>();
            NameplateLayer = new GameObject("NameplateLayer").AddComponent<SpriteRenderer>();
            FrontLayer.transform.SetParent(obj.transform);
            BackLayer.transform.SetParent(obj.transform);
            NameplateLayer.transform.SetParent(obj.transform);
            FrontLayer.transform.localPosition = new Vector3(-0.92f, 0.05f, -10f);
            BackLayer.transform.localPosition = new Vector3(-0.92f, 0.05f, -9f);
            NameplateLayer.transform.localPosition = new Vector3(-0.92f, 0f, -10f);

            FrontLayer.transform.localScale = new Vector3(0.18f,0.18f,1f);
            BackLayer.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            NameplateLayer.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            NameplateLayer.transform.eulerAngles = new Vector3(0f, 0f, 35f);
        };
    }

    public void SetItem(CustomCosmicItem item)
    {
        Item = item;
        if (item is CustomHat hat)
        {
            FrontLayer.sprite = hat.I_Main.GetMainImage();
            BackLayer.sprite = hat.I_Back.GetMainImage();
            FrontLayer.material = hat.Adaptive.Value ? DestroyableSingleton<HatManager>.Instance.PlayerMaterial : DestroyableSingleton<HatManager>.Instance.DefaultShader;
            BackLayer.material = hat.Adaptive.Value ? DestroyableSingleton<HatManager>.Instance.PlayerMaterial : DestroyableSingleton<HatManager>.Instance.DefaultShader;
            if (hat.Adaptive.Value)
            {
                PlayerMaterial.SetColors(0, FrontLayer);
                PlayerMaterial.SetColors(0, BackLayer);
            }
            NameplateLayer.sprite = null;
        }else if (item is CustomVisor visor)
        {
            FrontLayer.sprite = visor.I_Main.GetMainImage();
            BackLayer.sprite = null;
            FrontLayer.material = visor.Adaptive.Value ? DestroyableSingleton<HatManager>.Instance.PlayerMaterial : DestroyableSingleton<HatManager>.Instance.DefaultShader;
            BackLayer.material = visor.Adaptive.Value ? DestroyableSingleton<HatManager>.Instance.PlayerMaterial : DestroyableSingleton<HatManager>.Instance.DefaultShader;
            if (visor.Adaptive.Value)
            {
                PlayerMaterial.SetColors(0, FrontLayer);
                PlayerMaterial.SetColors(0, BackLayer);
            }
            NameplateLayer.sprite = null;
        }else if (item is CustomNamePlate nameplate)
        {
            FrontLayer.sprite = null;
            BackLayer.sprite = null;
            NameplateLayer.sprite = nameplate.I_Plate.GetMainImage();
        }
        else
        {
            FrontLayer.sprite = null;
            BackLayer.sprite = null;
            NameplateLayer.sprite = null;
        }
    }
}

public class CustomDesignerSublist : MonoBehaviour
{
    static CustomDesignerSublist()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomDesignerSublist>();
    }

    public enum DesignerType
    {
        None,
        Hat,
        Nameplate,
        Visor
    }

    public DesignerType Type { get; private set; } = DesignerType.None;
    CustomDesignerChip[] Elements = new CustomDesignerChip[10];

    int scroll = 0;
    CustomCosmicItem[] Contents = new CustomCosmicItem[0];

    GameObject NewCosmeticsButton;

    public void Awake()
    {
        var designer = MetaScreen.OpenScreen(gameObject,new Vector2(5.6f,5.4f),Vector2.zero);

        designer.AddTopic(
            new MSButton(1.4f, 0.35f, Language.Language.GetString("designers.category.hat"), TMPro.FontStyles.Bold, () => SetType(DesignerType.Hat)),
            new MSButton(1.4f, 0.35f, Language.Language.GetString("designers.category.visor"), TMPro.FontStyles.Bold, () => SetType(DesignerType.Visor)),
            new MSButton(1.4f, 0.35f, Language.Language.GetString("designers.category.nameplate"), TMPro.FontStyles.Bold, () => SetType(DesignerType.Nameplate))
            );
        designer.CustomUse(0.1f);

        var Temp = new MSButton[2];
        var chipPrefab = CustomDesignerBase.MainMenu.playerCustomizationPrefab.Tabs[0].Tab.ColorTabPrefab;

        for (int i = 0; i < 10; i++)
        {
            int index = i;
            Temp[i % 2] = new(2.5f, 0.7f, "", TMPro.FontStyles.Bold, () =>
            {
                var content = Contents[index + (scroll * 2)];
                if(content is CustomHat hat)
                {
                    var customDesigner = DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade<CustomHatDesigner>("HatDesigner", -110f);
                    customDesigner.gameObject.SetActive(true);
                    customDesigner.SetTargetHat(hat);
                    customDesigner.gameObject.SetActive(false);
                }
                if (content is CustomVisor visor)
                {
                    var customDesigner = DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade<CustomVisorDesigner>("VisorDesigner", -110f);
                    customDesigner.gameObject.SetActive(true);
                    customDesigner.SetTargetVisor(visor);
                    customDesigner.gameObject.SetActive(false);
                }
                if (content is CustomNamePlate nameplate)
                {
                    var customDesigner = DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade<CustomNameplateDesigner>("NameplateDesigner", -110f);
                    customDesigner.gameObject.SetActive(true);
                    customDesigner.SetTargetNameplate(nameplate);
                    customDesigner.gameObject.SetActive(false);
                }
            });
            Elements[i] = new CustomDesignerChip(Temp[i % 2]);
            if (i % 2 == (2 - 1))
            {
                designer.AddTopic(Temp);
                foreach (var e in Temp)
                {
                    e.text.alignment = TMPro.TextAlignmentOptions.Left;
                    e.text.transform.localPosition += new Vector3(0.45f, 0f, 0f);
                    e.text.rectTransform.sizeDelta = new Vector2(2f, 0.4f);
                }
            }
        }

        var newCosmeticsDesigner = MetaScreen.OpenScreen(gameObject, new Vector2(1f, 1f), Vector2.zero);
        newCosmeticsDesigner.AddTopic(new MSButton(3.2f, 0.45f, Language.Language.GetString("designers.button.addCosmetics"), TMPro.FontStyles.Bold, () => {
            switch (Type)
            {
                case DesignerType.Hat:
                    DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade<CustomHatDesigner>("HatDesigner", -110f);
                    break;
                case DesignerType.Visor:
                    DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade<CustomVisorDesigner>("VisorDesigner", -110f);
                    break;
                case DesignerType.Nameplate:
                    DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade<CustomNameplateDesigner>("NameplateDesigner", -110f);
                    break;
            }
            
        }));
        NewCosmeticsButton = newCosmeticsDesigner.screen.screen;
    }

    public void Update()
    {
        if (Contents == null) return;

        int delta = (int)Input.mouseScrollDelta.y;
        if (delta != 0)
        {
            scroll -= delta;
            if (scroll > (Contents.Length / 2) - 5) scroll = (Contents.Length / 2) - 5;
            if (scroll < 0) scroll = 0;
            UpdateButtons();
        }

        //右クリックが押されたとき
        if (Input.GetMouseButtonDown(1))
        {
            foreach (var e in Elements)
            {
                //ボタンが選択中
                if (e.Button.button.gameObject.GetInstanceID() == PassiveButtonManager.Instance.currentOver.gameObject.GetInstanceID())
                {
                    var designer = MetaScreen.OpenScreen(CustomDesignerMainMenu.Instance.gameObject, new Vector2(2.8f, 1.2f), Vector2.zero);
                    designer.CustomUse(-0.4f);
                    designer.AddTopic(new MSString(2.8f,Language.Language.GetString("designers.dialog.confirmErase"),1.5f,1.5f,TMPro.TextAlignmentOptions.Center,TMPro.FontStyles.Bold));
                    designer.AddTopic(new MSString(2.8f, e.Item.Name.Value, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold));
                    var element = e;
                    designer.AddTopic(
                        new MSButton(0.8f, 0.4f, Language.Language.GetString("config.option.no"), TMPro.FontStyles.Bold, () => designer.screen.Close()),
                        new MSButton(0.8f, 0.4f, Language.Language.GetString("config.option.yes"), TMPro.FontStyles.Bold, () => { 
                            designer.screen.Close();
                            element.Item.Remove();
                            DesignersSaver.SaveAllLocalCosmics();
                            Reload();
                        })
                        );
                    designer.MakeIntoPseudoScreen();
                    designer.screen.screen.transform.localPosition += new Vector3(0f, 0f, -50f);
                }
            }
        }
    }

    private void UpdateButtons()
    {
        float newButtonY = 0;

        for(int i=0;i<Elements.Length;i++)
        {
            if (i + (scroll * 2) >= Contents.Length)
            {
                Elements[i].Button.button.gameObject.SetActive(false);
            }
            else
            {
                var content = Contents[i + (scroll * 2)];
                Elements[i].Button.button.gameObject.SetActive(true);
                Elements[i].Button.text.text = Contents[i + (scroll * 2)].Name.Value;
                Elements[i].SetItem(content);
                newButtonY = Elements[i].Button.button.gameObject.transform.position.y - 0.7f;
            }
        }
        NewCosmeticsButton.transform.localPosition = new Vector3(0f, newButtonY, -20f);
    }

    public void Reload()
    {
        scroll = 0;
        switch (Type)
        {
            case DesignerType.None:
                Contents = null;
                break;
            case DesignerType.Hat:
                Contents = CustomHatRegistry.Values.Where((item) => item.IsLocal).ToArray<CustomCosmicItem>();
                break;
            case DesignerType.Visor:
                Contents = CustomVisorRegistry.Values.Where((item) => item.IsLocal).ToArray<CustomCosmicItem>();
                break;
            case DesignerType.Nameplate:
                Contents = CustomNamePlateRegistry.Values.Where((item) => item.IsLocal).ToArray<CustomCosmicItem>();
                break;
        }
        UpdateButtons();
    }
    public void SetType(DesignerType type)
    {
        if (Type == type) return;
        Type = type;

        Reload();
    }
}

public class CustomPlayerDesigner : CustomDesignerBase
{
    static CustomPlayerDesigner()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomPlayerDesigner>();
    }

    public PlayerDisplay Display;
    GameObject PlayerDisplayGroup;

    Game.PlayerData.CosmicTimer CosmicTimer;

    public void AcceptImage(string path, int minWidthToAskDivision, Action<string, Texture2D, int> action)
    {
        try
        {
            var texture = Helpers.loadTextureFromDisk(path);
            if (texture.width >= minWidthToAskDivision)
            {
                DesignerTextureSplitter.Show(gameObject, texture, (split) =>
                {
                    action(path, texture, split);
                });
            }
            else
            {
                action(path, texture, 1);
            }
        }
        catch { }
    }
    public void AcceptImage(int minWidthToAskDivision, Action<string, Texture2D, int> action)
    {
        DesignerFileAccepter.Show(gameObject, (pathes) => AcceptImage(pathes[0], minWidthToAskDivision, action));
    }

    public void Close()
    {
        DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(gameObject);
    }

    public void Awake()
    {
        CosmicTimer = new CosmicTimer();

        var backBlackPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(1);
        GameObject.Instantiate(backBlackPrefab.gameObject, transform);
        var backGroundPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(2);
        var backGround = GameObject.Instantiate(backGroundPrefab.gameObject, transform);
        GameObject.Destroy(backGround.transform.GetChild(2).gameObject);

        var closeButtonPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(0).GetChild(0);
        var closeButton = GameObject.Instantiate(closeButtonPrefab.gameObject, transform);
        GameObject.Destroy(closeButton.GetComponent<AspectPosition>());
        var button = closeButton.GetComponent<PassiveButton>();
        button.gameObject.SetActive(true);
        button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.OnClick.AddListener((UnityEngine.Events.UnityAction)Close);
        button.transform.localPosition = new Vector3(-4.9733f, 2.6708f, -50f);

        PlayerDisplayGroup = new GameObject("Player");
        PlayerDisplayGroup.transform.SetParent(transform);
        PlayerDisplayGroup.transform.localPosition = new Vector3(-2.7f, 0f, 0f);

        Display = GameObject.Instantiate(RuntimePrefabs.PlayerDisplayPrefab, PlayerDisplayGroup.transform);
        Display.gameObject.SetActive(true);
        Display.SetLayer(LayerExpansion.GetUILayer());
        Display.UpdateFromDefault();
        Display.transform.localPosition = new Vector3(0f, 0.6f, -10f);
        Display.transform.localScale = new Vector3(2f, 2f, 1f);

        var leftScreen = MetaScreen.OpenScreen(PlayerDisplayGroup, new Vector2(3f, 2f), new Vector2(0f, -2f));
        leftScreen.AddTopic(
            new MSButton(2f, 0.4f, Language.Language.GetString("designers.state.idle"), TMPro.FontStyles.Bold, () =>
            {
                Display.Animations.PlayIdleAnimation();
                Display.Cosmetics.SetHatAndVisorIdle(0);
            }),
            new MSButton(2f, 0.4f, Language.Language.GetString("designers.state.walk"), TMPro.FontStyles.Bold, () =>
            {
                Display.Cosmetics.SetHatAndVisorIdle(0);
                Display.Animations.PlayRunAnimation();
            }));
        leftScreen.AddTopic(
            new MSButton(2f, 0.4f, Language.Language.GetString("designers.state.climbUp"), TMPro.FontStyles.Bold, () =>
            {
                Display.Animations.PlayClimbAnimation(false);
                Display.Cosmetics.AnimateClimb(false);
            }),
            new MSButton(2f, 0.4f, Language.Language.GetString("designers.state.climbDown"), TMPro.FontStyles.Bold, () =>
            {
                Display.Animations.PlayClimbAnimation(true);
                Display.Cosmetics.AnimateClimb(true);
            }));
        var flipButton = new MSRadioButton(false, 0.7f, Language.Language.GetString("designers.state.flip"), 2f, 2f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);
        leftScreen.AddTopic(flipButton);
        flipButton.FlagUpdateAction = (flag) => { Display.Cosmetics.SetFlipX(flag); };
    }

    public void Update()
    {
        AnimationHandler.HandleAnimation(Display.Animations, Display.Cosmetics, CosmicTimer);
    }

}


public class CustomHatDesigner : CustomPlayerDesigner
{
    static public DesignersHat Hat = null;
    TextInputField NameField;
    TextInputField AuthorField;
    TextInputField CategoryField;
    TextInputField FPSField;
    MSRadioButton BounceButton;
    MSRadioButton AdaptiveButton;
    MSRadioButton BehindButton;
    MSRadioButton IsSkinnyButton;
    MSRadioButton HideHandsButton;

    CustomHat? targetHat = null;

    static CustomHatDesigner()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomHatDesigner>();
    }

    public void SetTargetHat(CustomHat hat)
    {
        Hat.CopyFrom(Display,hat);
        targetHat=hat;
        NameField.InputText = hat.Name.Value;
        AuthorField.InputText = hat.Author.Value;
        CategoryField.InputText = hat.Package.Value;
        BounceButton.Flag = hat.Bounce.Value;
        AdaptiveButton.Flag = hat.Adaptive.Value;
        BehindButton.Flag = hat.Behind.Value;
        IsSkinnyButton.Flag = hat.IsSkinny.Value;
        HideHandsButton.Flag = hat.HideHands.Value;
    }

    public void Awake()
    {
        base.Awake();

        Hat = new DesignersHat();

        var nameInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var authorInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var categoryInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var fpsInputContent = new MSTextInput(0.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };

        BounceButton = new MSRadioButton(false, 1.4f, Language.Language.GetString("designers.options.bounce"), 1.7f, 1.7f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);
        AdaptiveButton = new MSRadioButton(false, 1.4f, Language.Language.GetString("designers.options.adaptive"), 1.7f, 1.7f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);
        BehindButton = new MSRadioButton(false, 1.4f, Language.Language.GetString("designers.options.behind"), 1.7f, 1.7f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);
        IsSkinnyButton = new MSRadioButton(false, 1.4f, Language.Language.GetString("designers.options.isSkinny"), 1.7f, 1.7f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);
        HideHandsButton = new MSRadioButton(false, 1.4f, Language.Language.GetString("designers.options.hideHands"), 1.7f, 1.7f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);

        var rightScreen = MetaScreen.OpenScreen(gameObject, new Vector2(3f, 6.4f), new Vector2(2.5f, 0f));
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.name") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), nameInputContent);
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.author") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), authorInputContent);
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.package") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), categoryInputContent);
        rightScreen.AddTopic(BounceButton, AdaptiveButton);
        rightScreen.CustomUse(-0.15f);
        rightScreen.AddTopic(BehindButton, IsSkinnyButton, HideHandsButton);

        BounceButton.FlagUpdateAction = (bounce) => Hat.UpdateHat(Display, new DesignersHat.DesignersHatParameter() { Bounce = bounce });
        AdaptiveButton.FlagUpdateAction = (adaptive) => Hat.UpdateHat(Display, new DesignersHat.DesignersHatParameter() { Adaptive = adaptive });
        BehindButton.FlagUpdateAction = (behind) => Hat.UpdateHat(Display, new DesignersHat.DesignersHatParameter() { Behind = behind });
        IsSkinnyButton.FlagUpdateAction = (isSkinny) => Hat.UpdateHat(Display, new DesignersHat.DesignersHatParameter() { IsSkinny = isSkinny });
        HideHandsButton.FlagUpdateAction = (hideHands) => Hat.UpdateHat(Display, new DesignersHat.DesignersHatParameter() { HideHands = hideHands });

        MSButton GeneratePartButton(string label,Action<DesignersHat.DesignersHatParameter,DesignersImageParameter> parameterEditor)
        {
            return new MSButton(2f, 0.36f, label, TMPro.FontStyles.Bold, () =>
            {
                AcceptImage(400, (path, texture, split) =>
                {
                    var parameter = new DesignersHat.DesignersHatParameter();
                    parameterEditor(parameter,new(path,texture,split));
                    Hat.UpdateHat(Display,parameter);
                });
            });
        }

        MSButton GenerateReloadButton(Func<CustomHat,CustomVImage> imageGetter, Action<DesignersHat.DesignersHatParameter, DesignersImageParameter> parameterEditor)
        {
            var button = new MSButton(0.36f, 0.36f, "", TMPro.FontStyles.Bold, () =>
            {
                string? path = imageGetter(Hat.NebulaData).Address;
                if (path == null) return;
                AcceptImage(path, 400, (path, texture, split) =>
                {
                    var parameter = new DesignersHat.DesignersHatParameter();
                    parameterEditor(parameter, new(path, texture, split));
                    Hat.UpdateHat(Display, parameter);
                });
            });
            button.PostBuilder = (obj) => {
                var iconObj = new GameObject("Icon");
                iconObj.layer = LayerExpansion.GetUILayer();
                iconObj.transform.SetParent(obj.transform,false);
                iconObj.transform.localPosition = new Vector3(0, 0, -10f);
                iconObj.transform.localScale = Vector3.one * 0.54f;
                iconObj.AddComponent<SpriteRenderer>().sprite = CustomDesignerBase.IconSprite.GetSprite();
            };
            return button;
        }


        rightScreen.AddTopic(
            GeneratePartButton(Language.Language.GetString("designers.parts.idle"), (parameter, img) => parameter.Main = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat)=>hat.I_Main, (parameter, img) => parameter.Main = img),
            GeneratePartButton(Language.Language.GetString("designers.parts.idleFlip"), (parameter, img) => parameter.Flip = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_Flip, (parameter, img) => parameter.Flip = img)
        );
        rightScreen.AddTopic(
            GeneratePartButton(Language.Language.GetString("designers.parts.back"), (parameter, img) => parameter.Back = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_Back, (parameter, img) => parameter.Back = img),
            GeneratePartButton(Language.Language.GetString("designers.parts.backFlip"), (parameter, img) => parameter.BackFlip = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_BackFlip, (parameter, img) => parameter.BackFlip = img)
        );
        rightScreen.AddTopic(
            GeneratePartButton(Language.Language.GetString("designers.parts.move"), (parameter, img) => parameter.Move = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_Move, (parameter, img) => parameter.Move = img),
            GeneratePartButton(Language.Language.GetString("designers.parts.moveFlip"), (parameter, img) => parameter.MoveFlip = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_MoveFlip, (parameter, img) => parameter.MoveFlip = img)
        );
        rightScreen.AddTopic(
            GeneratePartButton(Language.Language.GetString("designers.parts.moveBackFlip"), (parameter, img) => parameter.MoveBack = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_MoveBack, (parameter, img) => parameter.MoveBack = img),
            GeneratePartButton(Language.Language.GetString("designers.parts.moveBackFlip"), (parameter, img) => parameter.MoveBackFlip = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_MoveBackFlip, (parameter, img) => parameter.MoveBackFlip = img)
        );
        rightScreen.AddTopic(
            GeneratePartButton(Language.Language.GetString("designers.parts.climb"), (parameter, img) => parameter.Climb = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_Climb, (parameter, img) => parameter.Climb = img),
            GeneratePartButton(Language.Language.GetString("designers.parts.climbFlip"), (parameter, img) => parameter.ClimbFlip = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_ClimbFlip, (parameter, img) => parameter.ClimbFlip = img)
        );

        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.fps") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), fpsInputContent);

        rightScreen.AddTopic(new MSButton(2.6f, 0.4f, Language.Language.GetString("designers.button.save"), TMPro.FontStyles.Bold, () => {
            if (NameField.InputText.Length == 0 || AuthorField.InputText.Length == 0)
            {
                NameField.HintText = Language.Language.GetString("designers.requiredField");
                AuthorField.HintText = Language.Language.GetString("designers.requiredField");
                return;
            }
            if (CategoryField.InputText.Length == 0) CategoryField.InputText = "NoSCollection";
            if (targetHat != null)
            {
                //既存のハットを上書き
                var param= new DesignersHat.DesignersHatParameter(Hat.NebulaData);
                param.ReflectTo(targetHat.HatData,targetHat);
                targetHat.UpdateCommonInfo(NameField.InputText, AuthorField.InputText, CategoryField.InputText);
                targetHat.ResaveAllImages();
            }
            else
            {
                //新たなハットを追加
                Hat.NebulaData.UpdateCommonInfo(NameField.InputText, AuthorField.InputText, CategoryField.InputText);
                Hat.NebulaData.ResaveAllImages();
                var data = CustomParts.CreateHatData(Hat.NebulaData, true);
                var list = HatManager.Instance.allHats.ToList();
                list.Add(data);
                HatManager.Instance.allHats = new(list.ToArray());
                Hat = null;
            }
            DesignersSaver.SaveAllLocalCosmics();
            DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(gameObject);
            if (CustomDesignerMainMenu.Instance) CustomDesignerMainMenu.Instance.Sublist.Get().Reload();
        }));

        rightScreen.screen.screen.transform.localScale = Vector3.one * 0.9f;

        NameField = nameInputContent.TextInputField;
        AuthorField = authorInputContent.TextInputField;
        CategoryField = categoryInputContent.TextInputField;
        FPSField= fpsInputContent.TextInputField;
        FPSField.AllowCharacters = (c) => '0' <= c && c <= '9';
        FPSField.DecisionAction = (s) =>
        {
            if (int.TryParse(s, out int fps) && fps>0)
            {
                Hat.UpdateHat(Display, new DesignersHat.DesignersHatParameter() { SecPerFrame = 1f / fps });
            }
            else
            {
                FPSField.InputText = "";
            }
        };

        NameField.UseIME = true;
        AuthorField.UseIME = true;        
    }

    public void OnDestroy()
    {
        if (Hat != null) Hat.NebulaData.Remove();
    }
}



public class CustomVisorDesigner : CustomPlayerDesigner
{
    static public DesignersVisor Visor = null;
    TextInputField NameField;
    TextInputField AuthorField;
    TextInputField CategoryField;
    TextInputField FPSField;
    MSRadioButton BehindHatButton;
    MSRadioButton AdaptiveButton;

    CustomVisor? targetVisor = null;

    static CustomVisorDesigner()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomVisorDesigner>();
    }

    public void SetTargetVisor(CustomVisor visor)
    {
        Visor.CopyFrom(Display, visor);
        targetVisor = visor;
        NameField.InputText = visor.Name.Value;
        AuthorField.InputText = visor.Author.Value;
        CategoryField.InputText = visor.Package.Value;
        BehindHatButton.Flag = visor.BehindHat.Value;
        AdaptiveButton.Flag = visor.Adaptive.Value;
    }

    public void Awake()
    {
        base.Awake();

        Visor = new DesignersVisor();

        var nameInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var authorInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var categoryInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var fpsInputContent = new MSTextInput(0.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };

        BehindHatButton = new MSRadioButton(false, 1.4f, Language.Language.GetString("designers.options.behindHat"), 1.7f, 1.7f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);
        AdaptiveButton = new MSRadioButton(false, 1.4f, Language.Language.GetString("designers.options.adaptive"), 1.7f, 1.7f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold);

        var rightScreen = MetaScreen.OpenScreen(gameObject, new Vector2(3f, 4.6f), new Vector2(2.5f, 0f));
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.name") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), nameInputContent);
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.author") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), authorInputContent);
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.package") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), categoryInputContent);
        rightScreen.AddTopic(BehindHatButton, AdaptiveButton);

        BehindHatButton.FlagUpdateAction = (behindHat) => Visor.UpdateVisor(Display, new DesignersVisor.DesignersVisorParameter() { BehindHat = behindHat });
        AdaptiveButton.FlagUpdateAction = (adaptive) => Visor.UpdateVisor(Display, new DesignersVisor.DesignersVisorParameter() { Adaptive = adaptive });
       
        MSButton GeneratePartButton(string label, Action<DesignersVisor.DesignersVisorParameter, DesignersImageParameter> parameterEditor)
        {
            return new MSButton(2f, 0.36f, label, TMPro.FontStyles.Bold, () =>
            {
                AcceptImage(400, (path, texture, split) =>
                {
                    var parameter = new DesignersVisor.DesignersVisorParameter();
                    parameterEditor(parameter, new(path, texture, split));
                    Visor.UpdateVisor(Display, parameter);
                });
            });
        }

        MSButton GenerateReloadButton(Func<CustomVisor, CustomVImage> imageGetter, Action<DesignersVisor.DesignersVisorParameter, DesignersImageParameter> parameterEditor)
        {
            var button = new MSButton(0.36f, 0.36f, "", TMPro.FontStyles.Bold, () =>
            {
                string? path = imageGetter(Visor.NebulaData).Address;
                if (path == null) return;
                AcceptImage(path, 400, (path, texture, split) =>
                {
                    var parameter = new DesignersVisor.DesignersVisorParameter();
                    parameterEditor(parameter, new(path, texture, split));
                    Visor.UpdateVisor(Display, parameter);
                });
            });
            button.PostBuilder = (obj) => {
                var iconObj = new GameObject("Icon");
                iconObj.layer = LayerExpansion.GetUILayer();
                iconObj.transform.SetParent(obj.transform, false);
                iconObj.transform.localPosition = new Vector3(0, 0, -10f);
                iconObj.transform.localScale = Vector3.one * 0.54f;
                iconObj.AddComponent<SpriteRenderer>().sprite = IconSprite.GetSprite();
            };
            return button;
        }


        rightScreen.AddTopic(
            GeneratePartButton(Language.Language.GetString("designers.parts.idle"), (parameter, img) => parameter.Main = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_Main, (parameter, img) => parameter.Main = img),
            GeneratePartButton(Language.Language.GetString("designers.parts.idleFlip"), (parameter, img) => parameter.Flip = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((hat) => hat.I_Flip, (parameter, img) => parameter.Flip = img)
        );

        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.fps") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), fpsInputContent);

        rightScreen.AddTopic(new MSButton(2.6f, 0.4f, Language.Language.GetString("designers.button.save"), TMPro.FontStyles.Bold, () => {
            if (NameField.InputText.Length == 0 || AuthorField.InputText.Length == 0)
            {
                NameField.HintText = Language.Language.GetString("designers.requiredField");
                AuthorField.HintText = Language.Language.GetString("designers.requiredField");
                return;
            }
            if (CategoryField.InputText.Length == 0) CategoryField.InputText = "NoSCollection";
            if (targetVisor != null)
            {
                //既存のバイザーを上書き
                var param = new DesignersVisor.DesignersVisorParameter(Visor.NebulaData);
                param.ReflectTo(targetVisor.VisorData, targetVisor);
                targetVisor.UpdateCommonInfo(NameField.InputText, AuthorField.InputText, CategoryField.InputText);
                targetVisor.ResaveAllImages();
            }
            else
            {
                //新たなバイザーを追加
                Visor.NebulaData.UpdateCommonInfo(NameField.InputText, AuthorField.InputText, CategoryField.InputText);
                Visor.NebulaData.ResaveAllImages();
                var data = CustomParts.CreateVisorData(Visor.NebulaData, true);
                var list = HatManager.Instance.allVisors.ToList();
                list.Add(data);
                HatManager.Instance.allVisors = new(list.ToArray());
                Visor = null;
            }
            DesignersSaver.SaveAllLocalCosmics();
            DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(gameObject);
            if (CustomDesignerMainMenu.Instance) CustomDesignerMainMenu.Instance.Sublist.Get().Reload();
        }));

        rightScreen.screen.screen.transform.localScale = Vector3.one * 0.9f;

        NameField = nameInputContent.TextInputField;
        AuthorField = authorInputContent.TextInputField;
        CategoryField = categoryInputContent.TextInputField;
        FPSField = fpsInputContent.TextInputField;
        FPSField.AllowCharacters = (c) => '0' <= c && c <= '9';
        FPSField.DecisionAction = (s) =>
        {
            if (int.TryParse(s, out int fps) && fps > 0)
            {
                Visor.UpdateVisor(Display, new DesignersVisor.DesignersVisorParameter() { SecPerFrame = 1f / fps });
            }
            else
            {
                FPSField.InputText = "";
            }
        };

        NameField.UseIME = true;
        AuthorField.UseIME = true;
    }

    public void OnDestroy()
    {
        if (Visor != null) Visor.NebulaData.Remove();
    }
}

public class CustomNameplateDesigner : CustomDesignerBase
{
    static public DesignersNameplate Nameplate = null;
    TextInputField NameField;
    TextInputField AuthorField;
    TextInputField CategoryField;

    CustomNamePlate? targetNameplate = null;
    
    SpriteRenderer PlateRenderer;

    static CustomNameplateDesigner()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomNameplateDesigner>();
    }

    public void AcceptImage(string path,Action<string, Texture2D> action)
    {
        var texture = Helpers.loadTextureFromDisk(path);
        action(path, texture);
    }

    public void AcceptImage(Action<string, Texture2D> action)
    {
        DesignerFileAccepter.Show(gameObject, (pathes) => AcceptImage(pathes[0],action));
    }


    public void SetTargetNameplate(CustomNamePlate nameplate)
    {
        Nameplate.CopyFrom(PlateRenderer,nameplate);

        targetNameplate = nameplate;
        NameField.InputText = nameplate.Name.Value;
        AuthorField.InputText = nameplate.Author.Value;
        CategoryField.InputText = nameplate.Package.Value;
    }

    public void Awake()
    {
        base.Awake();

        Nameplate = new DesignersNameplate();

        var nameInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var authorInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var categoryInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };

        PlateRenderer = new GameObject("NameplateSample").AddComponent<SpriteRenderer>();
        PlateRenderer.transform.SetParent(transform);
        PlateRenderer.transform.localPosition = new Vector3(-2.7f, 0f, -20f);
        PlateRenderer.gameObject.layer = LayerExpansion.GetUILayer();

        var rightScreen = MetaScreen.OpenScreen(gameObject, new Vector2(3f, 3f), new Vector2(2.5f, 0f));
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.name") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), nameInputContent);
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.author") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), authorInputContent);
        rightScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.package") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), categoryInputContent);


        MSButton GeneratePartButton(string label, Action<DesignersNameplate.DesignersNameplateParameter, DesignersImageParameter> parameterEditor)
        {
            return new MSButton(2f, 0.36f, label, TMPro.FontStyles.Bold, () =>
            {
                AcceptImage((path, texture) =>
                {
                    var parameter = new DesignersNameplate.DesignersNameplateParameter();
                    parameterEditor(parameter, new(path, texture, 1));
                    Nameplate.UpdateNameplate(PlateRenderer, parameter);
                });
            });
        }

        MSButton GenerateReloadButton(Func<CustomNamePlate, CustomVImage> imageGetter, Action<DesignersNameplate.DesignersNameplateParameter, DesignersImageParameter> parameterEditor)
        {
            var button = new MSButton(0.36f, 0.36f, "", TMPro.FontStyles.Bold, () =>
            {
                string? path = imageGetter(Nameplate.NebulaData).Address;
                if (path == null) return;
                AcceptImage(path, (path, texture) =>
                {
                    var parameter = new DesignersNameplate.DesignersNameplateParameter();
                    parameterEditor(parameter, new(path, texture, 1));
                    Nameplate.UpdateNameplate(PlateRenderer, parameter);
                });
            });
            button.PostBuilder = (obj) => {
                var iconObj = new GameObject("Icon");
                iconObj.layer = LayerExpansion.GetUILayer();
                iconObj.transform.SetParent(obj.transform, false);
                iconObj.transform.localPosition = new Vector3(0, 0, -10f);
                iconObj.transform.localScale = Vector3.one * 0.54f;
                iconObj.AddComponent<SpriteRenderer>().sprite = IconSprite.GetSprite();
            };
            return button;
        }

        rightScreen.AddTopic(
            GeneratePartButton(Language.Language.GetString("designers.parts.image"), (parameter, img) => parameter.Plate = img),
            new MSMargin(-0.05f),
            GenerateReloadButton((nameplate) => nameplate.I_Plate, (parameter, img) => parameter.Plate = img)
        );

        rightScreen.AddTopic(new MSButton(2.6f, 0.4f, Language.Language.GetString("designers.button.save"), TMPro.FontStyles.Bold, () => {
            if (NameField.InputText.Length == 0 || AuthorField.InputText.Length == 0) {
                NameField.HintText = Language.Language.GetString("designers.requiredField");
                AuthorField.HintText = Language.Language.GetString("designers.requiredField");
                return; 
            }
            if (CategoryField.InputText.Length == 0) CategoryField.InputText = "NoSCollection";
            if (targetNameplate != null)
            {
                //既存のネームプレートを上書き
                var param = new DesignersNameplate.DesignersNameplateParameter(Nameplate.NebulaData);
                param.ReflectTo(targetNameplate.NamePlateData, targetNameplate);
                targetNameplate.UpdateCommonInfo(NameField.InputText, AuthorField.InputText, CategoryField.InputText);
                targetNameplate.ResaveAllImages();
            }
            else
            {
                //新たなネームプレートを追加
                Nameplate.NebulaData.UpdateCommonInfo(NameField.InputText, AuthorField.InputText, CategoryField.InputText);
                Nameplate.NebulaData.ResaveAllImages();
                var data = CustomParts.CreateNamePlateData(Nameplate.NebulaData, true);
                var list = HatManager.Instance.allNamePlates.ToList();
                list.Add(data);
                HatManager.Instance.allNamePlates = new(list.ToArray());
                Nameplate = null;
            }
            DesignersSaver.SaveAllLocalCosmics();
            DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(gameObject);
            if (CustomDesignerMainMenu.Instance) CustomDesignerMainMenu.Instance.Sublist.Get().Reload();
        }));

        rightScreen.screen.screen.transform.localScale = Vector3.one * 0.9f;

        NameField = nameInputContent.TextInputField;
        AuthorField = authorInputContent.TextInputField;
        CategoryField = categoryInputContent.TextInputField;

        NameField.UseIME = true;
        AuthorField.UseIME = true;
    }

    public void OnDestroy()
    {
        Nameplate.NebulaData.Remove();
    }
}

public class CustomPackageDesigner : CustomDesignerBase
{
    TextInputField IdField;
    TextInputField FormatField;
    TextInputField PriorityField;

    CustomPackage? targetPackage;

    static CustomPackageDesigner()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CustomPackageDesigner>();
    }

    public void SetTargetPackage(CustomPackage package)
    {
        targetPackage = package;
        IdField.InputText = package.Key.Value;
        FormatField.InputText = package.Format.Value;
        PriorityField.InputText = package.Priority.Value;
    }

    public void Awake()
    {
        base.Awake();

        var idInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var formatInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };
        var priorityInputContent = new MSTextInput(2.8f, 0.27f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal) { FontSize = 2f };

        var editScreen = MetaScreen.OpenScreen(gameObject, new Vector2(3f, 2.5f), new Vector2(0f, 0f));
        editScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.id") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), idInputContent);
        editScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.format") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), formatInputContent);
        editScreen.AddTopic(new MSString(0.4f, Language.Language.GetString("designers.label.priority") + " :", 1.7f, 1.7f, TMPro.TextAlignmentOptions.Right, TMPro.FontStyles.Bold), priorityInputContent);

        editScreen.AddTopic(new MSButton(2.6f, 0.4f, Language.Language.GetString("designers.button.save"), TMPro.FontStyles.Bold, () => {
            if (IdField.InputText.Length == 0)
            {
                if (targetPackage != null)
                {
                    CustomPackage.AllPackage.Remove(targetPackage);
                }
            }
            else
            {
                if (IdField.InputText.Length == 0 || FormatField.InputText.Length == 0)
                {
                    IdField.HintText = Language.Language.GetString("designers.requiredField");
                    FormatField.HintText = Language.Language.GetString("designers.requiredField");
                    return;
                }
                int priority;
                if (!int.TryParse(PriorityField.InputText, out priority))
                {
                    priority = 100;
                    PriorityField.InputText = "100";
                }
                if (targetPackage != null)
                {
                    //既存のパッケージを上書き
                    targetPackage.Key.Value = IdField.InputText;
                    targetPackage.Format.Value = FormatField.InputText;
                    targetPackage.Priority.Value = PriorityField.InputText;
                }
                else
                {
                    //新たなパッケージを追加
                    CustomPackage package = new CustomPackage();
                    package.IsLocal = true;
                    package.Key.Value = IdField.InputText;
                    package.Format.Value = FormatField.InputText;
                    package.Priority.Value = PriorityField.InputText;
                    CustomPackage.AllPackage.Add(package);
                }
            }
            CustomPackage.ReloadPackages();
            DesignersSaver.SaveAllLocalCosmics();
            DestroyableSingleton<TransitionFade>.Instance.DoTransitionFade(gameObject);
            if (CustomDesignerMainMenu.Instance) CustomDesignerMainMenu.Instance.Reload();
        }));

        editScreen.screen.screen.transform.localScale = Vector3.one * 0.9f;

        IdField = idInputContent.TextInputField;
        FormatField = formatInputContent.TextInputField;
        PriorityField = priorityInputContent.TextInputField;

        IdField.UseIME = false;
        FormatField.UseIME = true;
        PriorityField.UseIME = false;
        PriorityField.AllowCharacters = (c) => '0' <= c && c <= '9';
    }
}
