using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Il2CppSystem;
using HarmonyLib;
using UnityEngine;
using UnhollowerBaseLib;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Nebula.Module
{
    public class CustomVariable
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

        public CustomVariable(string Id)
        {
            this.Id = Id;
        }
    }

    public class CustomVHatImage : CustomVImage
    {
        public override void LoadImage(string parent, bool fromDisk = false)
        {
            Texture2D texture = fromDisk ? Helpers.loadTextureFromDisk(Path.GetDirectoryName(Application.dataPath) + "/" + parent + "/" + Address) : Helpers.loadTextureFromResources(parent + "." + Address);
            if (texture == null)
                return;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.53f, 0.575f), texture.width * 0.375f);
            if (sprite == null)
                return;
            texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            Image = sprite;
        }

        public CustomVHatImage(string Id) : base(Id)
        {
        }
    }

    public class CustomVImage : CustomVariable
    {
        public static implicit operator bool(CustomVImage image) { return image.Address != null; }
        public string Hash { get; set; }
        public string Address { get; set; }
        public Sprite Image { get; set; }

        private string SanitizeResourcePath(string res)
        {
            if (res == null || !res.EndsWith(".png"))
                return null;

            res = res.Replace("\\", "")
                     .Replace("/", "")
                     .Replace("*", "")
                     .Replace("..", "");
            return res;
        }

        protected override void LoadValue(JToken? token)
        {
            Address = SanitizeResourcePath(token?["Address"]?.ToString());
            Hash = SanitizeResourcePath(token?["Hash"]?.ToString());
        }

        public override void LoadImage(string parent, bool fromDisk = false)
        {
            Texture2D texture = fromDisk ? Helpers.loadTextureFromDisk(Path.GetDirectoryName(Application.dataPath) + "/" + parent + "/" + Address) : Helpers.loadTextureFromResources(parent + "." + Address);
            if (texture == null)
                return;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            if (sprite == null)
                return;
            texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            Image = sprite;
        }

        public override bool DoesResourceRequireDownload(string directoryPath, MD5 md5)
        {
            if (NebulaPlugin.DebugMode.HasToken("OutputHash"))
            {
                if (File.Exists(directoryPath + Address))
                {
                    using (var stream = File.OpenRead(directoryPath + Address))
                    {
                        var hash = System.BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                        NebulaPlugin.Instance.Logger.Print("HASH: " + hash + " (" + directoryPath + Address + ")");
                    }
                }
            }

            if (!this) return false;
            if (Hash == null || !File.Exists(directoryPath+Address))
                return true;

            using (var stream = File.OpenRead(directoryPath + Address))
            {
                var hash = System.BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                return !Hash.Equals(hash);
            }
        }

        public override async Task Download(string repoPath,string directoryPath, HttpClient http)
        {
            var hatFileResponse = await http.GetAsync($"{repoPath}{Address}", HttpCompletionOption.ResponseContentRead);
            if (hatFileResponse.StatusCode != HttpStatusCode.OK) return;
            using (var responseStream = await hatFileResponse.Content.ReadAsStreamAsync())
            {
                using (var fileStream = File.Create($"{directoryPath}{Address}"))
                {
                    responseStream.CopyTo(fileStream);
                }
            }
        }

        public CustomVImage(string Id):base(Id)
        {
            Hash = Address = null;
            Image = null;
        }
    }

    public class CustomVBool : CustomVariable
    {
        public bool Value { get; set; }

        protected override void LoadValue(JToken? token)
        {
            string str = token?.ToString();
            if (str != null) Value = bool.Parse(str);
        }

        public CustomVBool(string Id) : base(Id)
        {
            Value = false;
        }
    }

    public class CustomVString : CustomVariable
    {
        public string Value { get; set; }

        protected override void LoadValue(JToken? token)
        {
            Value = token?.ToString();
        }

        public CustomVString(string Id) : base(Id)
        {
            Value = "";
        }

        public CustomVString(string Id,string initialValue) : base(Id)
        {
            Value = initialValue;
        }
    }

    public class CustomItem
    {
        public CustomVString Author { get; set; }
        public CustomVString Package { get; set; }
        public CustomVString Condition { get; set; }
        public CustomVString Name { get; set; }

        public IEnumerable<CustomVariable> Contents()
        {
            yield return Author;
            yield return Package;
            yield return Condition;
            yield return Name;
            foreach (var content in ExtendedContents()) yield return content;
            yield break;
        }

        protected virtual IEnumerable<CustomVariable> ExtendedContents()
        {
            yield break;
        }

        public CustomItem()
        {
            Author = new CustomVString("Author","Unknown");
            Package = new CustomVString("Package","Misc.");
            Condition = new CustomVString("Condition","none");
            Name = new CustomVString("Name");
        }
    }

    public class CustomHat : CustomItem
    {
        public CustomVHatImage I_Main { get; set; }
        public CustomVHatImage I_Flip { get; set; }
        public CustomVHatImage I_BackFlip { get; set; }
        public CustomVHatImage I_Back { get; set; }
        public CustomVHatImage I_Climb { get; set; }
        public CustomVBool Bounce { get; set; }
        public CustomVBool Adaptive { get; set; }
        public CustomVBool Behind { get; set; }

        protected override IEnumerable<CustomVariable> ExtendedContents()
        {
            yield return I_Main;
            yield return I_Flip;
            yield return I_BackFlip;
            yield return I_Back;
            yield return I_Climb;
            yield return Bounce;
            yield return Adaptive;
            yield return Behind;
            yield break;
        }

        public CustomHat():base()
        {
            I_Main = new CustomVHatImage("Main");
            I_Flip = new CustomVHatImage("Flip");
            I_BackFlip = new CustomVHatImage("BackFlip");
            I_Back = new CustomVHatImage("Back");
            I_Climb = new CustomVHatImage("Climb");
            Bounce = new CustomVBool("Bounce");
            Adaptive = new CustomVBool("Adaptive");
            Behind = new CustomVBool("Behind");
        }
    }

    public class CustomNamePlate : CustomItem
    {
        public CustomVImage I_Plate { get; set; }

        protected override IEnumerable<CustomVariable> ExtendedContents()
        {
            yield return I_Plate;
            yield break;
        }

        public CustomNamePlate() : base()
        {
            I_Plate = new CustomVImage("Plate");
        }
    }

    [HarmonyPatch]
    public class CustomParts
    {
        public static Material hatShader;

        public static Dictionary<string, CustomHat> CustomHatRegistry = new Dictionary<string, CustomHat>();
        public static Dictionary<string, CustomNamePlate> CustomNamePlateRegistry = new Dictionary<string, CustomNamePlate>();
        public static CustomHat TestHat = null;
        public static CustomNamePlate TestNamePlate = null;

        private static HatData CreateHatBehaviour(CustomHat ch, bool fromDisk = false, bool testOnly = false)
        {
            if (hatShader == null && DestroyableSingleton<HatManager>.InstanceExists)
            {
                foreach (HatData h in DestroyableSingleton<HatManager>.Instance.allHats)
                {
                    if (h.hatViewData.viewData.AltShader != null)
                    {
                        hatShader = h.hatViewData.viewData.AltShader;
                        break;
                    }
                }
            }

            foreach(var content in ch.Contents())
            {
                content.LoadImage("MoreCosmic/hats", fromDisk);
            }

            HatData hat = new HatData();
            HatViewData viewData = hat.hatViewData.viewData;

            viewData.MainImage = ch.I_Main.Image;
            if (ch.I_Back)
            {
                viewData.BackImage = ch.I_Back.Image;
                ch.Behind.Value = true;
            }
            if (ch.I_Climb)
                viewData.ClimbImage = ch.I_Climb.Image;
            hat.StoreName = ch.Name.Value + "\nby " + ch.Author.Value;
            //hat.Order = 99;
            hat.ProductId = "hat_" + ch.Name.Value.Replace(' ', '_');
            hat.InFront = !ch.Behind.Value;
            hat.NoBounce = !ch.Bounce.Value;
            hat.ChipOffset = new Vector2(0f, 0.2f);
            hat.Free = true;
            hat.NotInStore = true;


            if (ch.Adaptive.Value && hatShader != null)
                viewData.AltShader = hatShader;

            if (testOnly)
            {
                TestHat = ch;
                TestHat.Condition.Value = hat.name;
            }
            else
            {
                CustomHatRegistry.Add(hat.name, ch);
            }

            return hat;
        }

        private static NamePlateData CreateNamePlateData(CustomNamePlate ch, bool fromDisk = false, bool testOnly = false)
        {
            foreach (var content in ch.Contents())
            {
                content.LoadImage("MoreCosmic/namePlates", fromDisk);
            }

            NamePlateData np = new NamePlateData();
            np.viewData.viewData = new NamePlateViewData();
            np.viewData.viewData.Image = ch.I_Plate.Image;
            np.name = ch.Name.Value + "\nby " + ch.Author.Value;
            //np.Order = 99;
            np.ProductId = "nameplate_" + ch.Name.Value.Replace(' ', '_');
            np.ChipOffset = new Vector2(0f, 0.2f);
            np.Free = true;
            np.NotInStore = true;
            
            if (testOnly)
            {
                TestNamePlate = ch;
                TestNamePlate.Condition.Value = np.name;
            }
            else
            {
                CustomNamePlateRegistry.Add(np.name, ch);
            }

            return np;
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
                    while (CosmicLoader.hatdetails.Count > 0)
                    {
                        __instance.allHats.Add(CreateHatBehaviour(CosmicLoader.hatdetails[0],true));
                        CosmicLoader.hatdetails.RemoveAt(0);
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
                    while (CosmicLoader.namePlatedetails.Count > 0)
                    {
                        __instance.allNamePlates.Add(CreateNamePlateData(CosmicLoader.namePlatedetails[0], true));
                        CosmicLoader.namePlatedetails.RemoveAt(0);
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

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
        private static class PlayerPhysicsHandleAnimationPatch
        {
            private static void Postfix(PlayerPhysics __instance)
            {
                AnimationClip currentAnimation = __instance.Animator.GetCurrentAnimation();
                if (currentAnimation == __instance.CurrentAnimationGroup.ClimbAnim || currentAnimation == __instance.CurrentAnimationGroup.ClimbDownAnim) return;
                HatParent hp = __instance.myPlayer.HatRenderer;
                if (hp.Hat == null) return;
                CustomHat extend = hp.Hat.getHatData();
                if (extend == null) return;
                if (extend.I_Flip)
                {
                    if (__instance.rend.flipX)
                    {
                        hp.FrontLayer.sprite = extend.I_Flip.Image;
                    }
                    else
                    {
                        hp.FrontLayer.sprite = hp.Hat.hatViewData.viewData.MainImage;
                    }
                }
                if (extend.I_BackFlip != null)
                {
                    if (__instance.rend.flipX)
                    {
                        hp.BackLayer.sprite = extend.I_BackFlip.Image;
                    }
                    else
                    {
                        hp.BackLayer.sprite = hp.Hat.hatViewData.viewData.BackImage;
                    }
                }
            }
        }


        private static List<TMPro.TMP_Text> hatsTabCustomTexts = new List<TMPro.TMP_Text>();
        private static List<TMPro.TMP_Text> nameplatesTabCustomTexts = new List<TMPro.TMP_Text>();

        public static string innerslothHatPackageName = "innerslothHats";
        public static string innerslothNamePlatePackageName = "innerslothNameplates";
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
                    title.transform.parent = __instance.scroller.Inner;
                    title.transform.localPosition = new Vector3(headerX, YStart, inventoryZ);
                    title.alignment = TMPro.TextAlignmentOptions.Center;
                    title.fontSize *= 1.25f;
                    title.fontWeight = TMPro.FontWeight.Thin;
                    title.enableAutoSizing = false;
                    title.autoSizeTextContainer = true;
                    title.text = Language.Language.GetString("cosmic.package."+packageName);
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

                    int color = __instance.HasLocalPlayer() ? PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId : SaveManager.BodyColor;

                    colorChip.transform.localPosition = new Vector3(xpos, ypos, inventoryZ);
                    if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
                    {
                        colorChip.Button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectHat(hat)));
                        colorChip.Button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectHat(DestroyableSingleton<HatManager>.Instance.GetHatById(SaveManager.LastHat))));
                        colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.ClickEquip()));
                    }
                    else
                    {
                        colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectHat(hat)));
                    }

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

                HatData[] unlockedHats = DestroyableSingleton<HatManager>.Instance.GetUnlockedHats();
                Dictionary<string, List<System.Tuple<HatData, CustomHat>>> packages = new Dictionary<string, List<System.Tuple<HatData, CustomHat>>>();

                Helpers.destroyList(hatsTabCustomTexts);
                Helpers.destroyList(__instance.ColorChips);

                hatsTabCustomTexts.Clear();
                __instance.ColorChips.Clear();

                textTemplate = PlayerCustomizationMenu.Instance.itemName;

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

                var orderedKeys = packages.Keys.OrderBy((string x) => {
                    if (x == innerslothHatPackageName) return 1000;
                    if (x == "developerHats") return 200;
                    return 500;
                });

                foreach (string key in orderedKeys)
                {
                    List<System.Tuple<HatData, CustomHat>> value = packages[key];
                    YOffset = createHatPackage(value, key, YOffset, __instance);
                }

                __instance.scroller.ContentYBounds.max = -(YOffset + 3.0f + headerSize);
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
                    title.transform.parent = __instance.scroller.Inner;
                    title.transform.localPosition = new Vector3(headerX, YStart, inventoryZ);
                    title.alignment = TMPro.TextAlignmentOptions.Center;
                    title.fontSize *= 1.25f;
                    title.fontWeight = TMPro.FontWeight.Thin;
                    title.enableAutoSizing = false;
                    title.autoSizeTextContainer = true;
                    title.text = Language.Language.GetString("cosmic.package." + packageName);
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
                        colorChip.Button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectNameplate(DestroyableSingleton<HatManager>.Instance.GetNamePlateById(SaveManager.LastNamePlate))));
                        colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.ClickEquip()));
                    }
                    else
                    {
                        colorChip.Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => __instance.SelectNameplate(nameplate)));
                    }


                    __instance.StartCoroutine(nameplate.CoLoadViewData((Il2CppSystem.Action<NamePlateViewData>)((n) => {
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

                NamePlateData[] unlockedNameplates = DestroyableSingleton<HatManager>.Instance.GetUnlockedNamePlates();
                Dictionary<string, List<System.Tuple<NamePlateData, CustomNamePlate>>> packages = new Dictionary<string, List<System.Tuple<NamePlateData, CustomNamePlate>>>();

                Helpers.destroyList(nameplatesTabCustomTexts);
                Helpers.destroyList(__instance.ColorChips);

                nameplatesTabCustomTexts.Clear();
                __instance.ColorChips.Clear();

                textTemplate = PlayerCustomizationMenu.Instance.itemName;

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

                var orderedKeys = packages.Keys.OrderBy((string x) => {
                    if (x == innerslothNamePlatePackageName) return 1000;
                    if (x == "developerNamePlates") return 200;
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

        [HarmonyPatch]
        public class TabUpdatePatch {
            private static void TabPostFix(InventoryTab __instance)
            {
                // Manually hide all custom TMPro.TMP_Text objects that are outside the ScrollRect
                foreach (TMPro.TMP_Text customText in hatsTabCustomTexts)
                {
                    if (customText != null && customText.transform != null && customText.gameObject != null)
                    {
                        bool active = customText.transform.position.y <= inventoryTop && customText.transform.position.y >= inventoryBot;
                        float epsilon = Mathf.Min(Mathf.Abs(customText.transform.position.y - inventoryTop), Mathf.Abs(customText.transform.position.y - inventoryBot));
                        if (active != customText.gameObject.active && epsilon > 0.1f) customText.gameObject.SetActive(active);
                    }
                }
            }

            [HarmonyPatch(typeof(HatsTab), nameof(HatsTab.Update))]
            public class HatsTabUpdatePatch
            {
                public static bool Prefix()
                {
                    //return false;
                    return true;
                }

                public static void Postfix(HatsTab __instance)
                {
                    TabPostFix(__instance);
                }
            }

            [HarmonyPatch(typeof(NameplatesTab), nameof(NameplatesTab.Update))]
            public class NamePlateTabUpdatePatch
            {
                public static bool Prefix()
                {
                    //return false;
                    return true;
                }

                public static void Postfix(NameplatesTab __instance)
                {
                    TabPostFix(__instance);
                }
            }
        }


    }

    public class CosmicLoader
    {
        public static bool running = false;

        public static string[] cosmicRepos = new string[]
        {
            "https://raw.githubusercontent.com/Dolly1016/MoreCosmic/master"
        };

        public static List<CustomHat> hatdetails = new List<CustomHat>();
        public static List<CustomNamePlate> namePlatedetails = new List<CustomNamePlate>();

        private static Task cosmicFetchTask = null;

        public static void LaunchCosmicFetcher()
        {
            if (running)
                return;
            running = true;
            cosmicFetchTask = LaunchCosmicFetcherAsync();
        }

        private static async Task LaunchCosmicFetcherAsync()
        {
            System.IO.Directory.CreateDirectory("MoreCosmic");
            System.IO.Directory.CreateDirectory("MoreCosmic/hats");
            System.IO.Directory.CreateDirectory("MoreCosmic/namePlates");

            List<string> repos = new List<string>(cosmicRepos);

            foreach (string repo in repos)
            {
                try
                {
                    HttpStatusCode status;

                    status = await FetchItems(repo, "hats", hatdetails);
                    if (status != HttpStatusCode.OK)
                        System.Console.WriteLine($"Repo was not found. : {repo}\n");

                    status = await FetchItems(repo, "namePlates", namePlatedetails);
                    if (status != HttpStatusCode.OK)
                        System.Console.WriteLine($"Repo was not found. : {repo}\n");
                }
                catch (System.Exception e)
                {
                    System.Console.WriteLine($"Unable to fetch from repo: {repo}\n" + e.Message);
                }
            }
            running = false;
        }

        public static async Task<HttpStatusCode> FetchItems<Cosmic>(string repo,string category,List<Cosmic> cosmics) where Cosmic : CustomItem, new()
        {
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            var response = await http.GetAsync(new System.Uri($"{repo}/Contents.json"), HttpCompletionOption.ResponseContentRead);
            try
            {
                if (response.StatusCode != HttpStatusCode.OK) return response.StatusCode;
                if (response.Content == null)
                {
                    System.Console.WriteLine("Server returned no data: " + response.StatusCode.ToString());
                    return HttpStatusCode.ExpectationFailed;
                }

                string json = await response.Content.ReadAsStringAsync();
                JToken jobj = JObject.Parse(json)[category];
                if (!jobj.HasValues) return HttpStatusCode.ExpectationFailed;

                List<Cosmic> cosList = new List<Cosmic>();
                
                List<CustomVariable> markedfordownload = new List<CustomVariable>();
                string filePath = Path.GetDirectoryName(Application.dataPath) + @"\MoreCosmic\"+ category+@"\";
                MD5 md5 = MD5.Create();


                for (JToken current = jobj.First; current != null; current = current.Next)
                {
                    if (current.HasValues)
                    {
                        Cosmic cos = new Cosmic();

                        foreach (var content in cos.Contents())
                        {
                            content.Load(current);
                            if (content.DoesResourceRequireDownload(filePath, md5)) markedfordownload.Add(content);
                        }
                        cosList.Add(cos);
                    }
                    else break;

                }


                foreach (var content in markedfordownload)
                {
                    await content.Download(repo+"/"+category+"/", filePath,http);
                }

                cosmics.AddRange(cosList);
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
            if (CustomParts.TestHat != null && CustomParts.TestHat.Condition.Value.Equals(hat.name))
            {
                return CustomParts.TestHat;
            }
            CustomParts.CustomHatRegistry.TryGetValue(hat.name, out ret);
            return ret;
        }

        public static CustomNamePlate getNamePlateData(this NamePlateData namePlate)
        {
            CustomNamePlate ret = null;
            if (CustomParts.TestNamePlate != null && CustomParts.TestNamePlate.Condition.Value.Equals(namePlate.name))
            {
                return CustomParts.TestNamePlate;
            }
            CustomParts.CustomNamePlateRegistry.TryGetValue(namePlate.name, out ret);
            return ret;
        }
    }

    [HarmonyPatch(typeof(PoolablePlayer), nameof(PoolablePlayer.UpdateFromPlayerOutfit))]
    public static class PoolablePlayerPatch
    {
        public static void Postfix(PoolablePlayer __instance)
        {
            if (__instance.VisorSlot?.transform == null || __instance.HatSlot?.transform == null) return;

            // fixes a bug in the original where the visor will show up beneath the hat,
            // instead of on top where it's supposed to be
            __instance.VisorSlot.transform.localPosition = new Vector3(
                __instance.VisorSlot.transform.localPosition.x,
                __instance.VisorSlot.transform.localPosition.y,
                __instance.HatSlot.transform.localPosition.z - 1
                );
        }
    }
}
