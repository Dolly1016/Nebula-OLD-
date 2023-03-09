using BepInEx.Configuration;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Linq;
using static Nebula.Module.DynamicColors;
using static UnityEngine.UI.StencilMaterial;
using Newtonsoft.Json.Bson;

namespace Nebula.Module;

[HarmonyPatch]
public static class DynamicColors
{
    static private SpriteLoader ShareIconSprite = new SpriteLoader("Nebula.Resources.ShareIcon.png", 100f);
    static private SpriteLoader PaletteSprite = new SpriteLoader("Nebula.Resources.Palette.png", 100f);
    static private SpriteLoader PaletteDisabledSprite = new SpriteLoader("Nebula.Resources.PaletteDisabled.png", 100f);
    static private SpriteLoader PaletteDisabledFrameSprite = new SpriteLoader("Nebula.Resources.PaletteDisabledFrame.png", 100f);
    static private SpriteLoader MaskSprite = new SpriteLoader("Nebula.Resources.PaletteDisabledMask.png", 100f);
    static private SpriteLoader MaskCircleSprite = new SpriteLoader("Nebula.Resources.PaletteDisabledCircleMask.png", 100f);
    static private SpriteLoader LPaletteSprite = new SpriteLoader("Nebula.Resources.PaletteBrightness.png", 100f);
    static private SpriteLoader BaseButtonSprite = new SpriteLoader("Nebula.Resources.ColorHalfButton.png", 100f);
    static private SpriteLoader ButtonSprite = new SpriteLoader("Nebula.Resources.ColorFullBase.png", 100f);
    static private SpriteLoader SaveButtonSprite = new SpriteLoader("Nebula.Resources.ColorSaveButton.png", 100f);
    static private SpriteLoader ColorTargetSprite = new SpriteLoader("Nebula.Resources.TargetIcon.png", 140f);
    static private SpriteLoader LTargetSprite = new SpriteLoader("Nebula.Resources.PaletteKnob.png", 100f);
    static private SpriteLoader[] BrightnessSprite = new SpriteLoader[2] {
        new SpriteLoader("Nebula.Resources.ColorLight.png", 100f),
        new SpriteLoader("Nebula.Resources.ColorDark.png", 100f)
    };
    static private SpriteLoader[] SelectedSprite = new SpriteLoader[3] {
        new SpriteLoader("Nebula.Resources.ColorButtonSelected.png", 100f),
        new SpriteLoader("Nebula.Resources.ColorHalfButtonSelected0.png", 100f),
        new SpriteLoader("Nebula.Resources.ColorHalfButtonSelected1.png", 100f)
    };

    static private Sprite ModeVanillaSprite, ModeDynamicSprite;
    static private Sprite GetModeChangeSprite(bool showVanillaColorsNow)
    {
        if (showVanillaColorsNow)
        {
            if (ModeDynamicSprite) return ModeDynamicSprite;
            ModeDynamicSprite = Helpers.loadSpriteFromResources(
                Helpers.loadTextureFromResources("Nebula.Resources.PaletteChangeButton.png"), 100f,
                new Rect(64f, 0f, 64f, -64f));
            return ModeDynamicSprite;
        }
        else
        {
            if (ModeVanillaSprite) return ModeVanillaSprite;
            ModeVanillaSprite = Helpers.loadSpriteFromResources(
                Helpers.loadTextureFromResources("Nebula.Resources.PaletteChangeButton.png"), 100f,
                new Rect(0f, 0f, 64f, -64f));
            return ModeVanillaSprite;
        }
    }

    static public bool IsLightColor(Color color)
    {
        return (color.r + color.g * 1.15f + color.b * 0.5f > 1.05f);
    }

    public class CustomColor
    {
        public class CustomOriginalColor
        {
            private FloatDataEntry EntryR;
            private FloatDataEntry EntryG;
            private FloatDataEntry EntryB;
            private ByteDataEntry EntryH;
            private ByteDataEntry EntryD;
            private FloatDataEntry EntryL;

            public Color OriginalColor { get; private set; }
            public Color Color { get; private set; }

            public CustomOriginalColor(string saveCategory, string identifier, float multiplier)
            {
                EntryR = new FloatDataEntry(saveCategory + "." + identifier + ".R", colorSaver, (float)multiplier);
                EntryG = new FloatDataEntry(saveCategory + "." + identifier + ".G", colorSaver, 0f);
                EntryB = new FloatDataEntry(saveCategory + "." + identifier + ".B", colorSaver, 0f);
                EntryL = new FloatDataEntry(saveCategory + "." + identifier + ".L", colorSaver, (float)1f);
                EntryH = new ByteDataEntry(saveCategory + "." + identifier + ".H", colorSaver, (byte)0);
                EntryD = new ByteDataEntry(saveCategory + "." + identifier + ".D", colorSaver, (byte)0);

                OriginalColor = new Color(EntryR.Value, EntryG.Value, EntryB.Value, 1f);
                Color = GetColor(OriginalColor, EntryL.Value);
            }

            public void SetColor(Color originalColor, float l,byte h,byte d)
            {
                EntryR.SetValueWithoutSave(originalColor.r);
                EntryG.SetValueWithoutSave(originalColor.g);
                EntryB.SetValueWithoutSave(originalColor.b);
                EntryL.SetValueWithoutSave(l);

                if (h < 80)
                    EntryH.SetValueWithoutSave((byte)(h % 64));
                else
                    EntryH.SetValueWithoutSave((byte)h);
                if (d < 24)
                    EntryD.SetValueWithoutSave(d);
                else
                    EntryD.SetValueWithoutSave(23);

                colorSaver.Save();

                OriginalColor = originalColor;
                Color = GetColor(originalColor, l);
            }

            public void Transcribe(CustomOriginalColor source)
            {
                SetColor(source.OriginalColor, source.EntryL.Value,source.EntryH.Value,source.EntryD.Value);
            }

            public void Simulate(Color? originalColor,float? l, out Color color)
            {
                if (!originalColor.HasValue) originalColor = this.OriginalColor;
                if (!l.HasValue) l = this.EntryL.Value;

                color = GetColor(originalColor.Value,l.Value);
            }

            static private Color GetColor(Color originalColor, float l)
            {
                originalColor = new Color(
                    originalColor.r > 1f ? 1f : originalColor.r,
                    originalColor.g > 1f ? 1f : originalColor.g,
                    originalColor.b > 1f ? 1f : originalColor.b);
                return originalColor.RGBMultiplied(l * 0.85f + 0.15f);
            }

            public float GetLuminosity()
            {
                return EntryL.Value;
            }

            public byte GetHue()
            {
                return EntryH.Value;
            }

            public byte GetDistance()
            {
                return EntryD.Value;
            }
        }

        private CustomOriginalColor mainColor;
        private CustomOriginalColor shadowColor;
        private ConfigEntry<byte> EntryS;
        private ConfigEntry<byte> EntryPH;
        private ConfigEntry<byte> EntryPD;

        public CustomColor(string saveCategory,float initialMul=0.6f)
        {
            mainColor = new CustomOriginalColor(saveCategory, "main", initialMul > 0f ? 1f : 0f);
            shadowColor = new CustomOriginalColor(saveCategory, "shadow", initialMul);

            //陰タイプ
            EntryS = NebulaPlugin.Instance.Config.Bind(saveCategory, "S", (byte)0);
            if (EntryS.Value > 3) EntryS.Value = 0;

            EntryPD = NebulaPlugin.Instance.Config.Bind(saveCategory, "PD", (byte)14);
            EntryPH = NebulaPlugin.Instance.Config.Bind(saveCategory, "PH", (byte)63);
        }

        public void SetMainColor(Color originalColor, float luminosity, byte h, byte d)
        {
            mainColor.SetColor(originalColor, luminosity,h,d);
        }

        public void SetShadowColor(Color originalColor, float luminosity, byte h, byte d)
        {
            shadowColor.SetColor(originalColor, luminosity, h, d);
        }

        public void SetShadowType(byte type)
        {
            EntryS.Value = type;
        }

        public void SetMainPosInfo(byte ph, byte pd)
        {
            EntryPH.Value = ph;
            EntryPD.Value = pd;
        }

        public void Set(Color originalColor, byte ph,byte pd,float l, byte h, byte d, Color shadowColor, float sl, byte sh, byte sd, byte shadowType)
        {
            SetMainPosInfo(ph,pd);
            SetMainColor(originalColor, l, h, d);
            SetShadowColor(shadowColor, sl, sh, sd);
            SetShadowType(shadowType);
        }

        public void Transcribe(CustomColor source)
        {
            mainColor.Transcribe(source.mainColor);
            shadowColor.Transcribe(source.shadowColor);
            EntryS.Value = source.EntryS.Value;
            EntryPD.Value = source.EntryPD.Value;
            EntryPH.Value = source.EntryPH.Value;
        }

        public void Simulate(Color? mainOriginalColor,float? mL,Color? shadowOriginalColor,float? sL,ref byte? h,ref byte? d,out Color mainColor,out Color shadowColor)
        {
            this.mainColor.Simulate(mainOriginalColor, mL, out mainColor);
            this.shadowColor.Simulate(shadowOriginalColor, sL, out shadowColor);
            shadowColor = CustomShadow.allShadows[EntryS.Value].GetShadowColor(mainColor,shadowColor);
            if (!h.HasValue) h = GetMainHue();
            if (!d.HasValue) d = GetMainDistance();
        }

        public byte GetShadowType() { return EntryS.Value; }

        public Color GetMainColor() { return mainColor.Color; }
        public Color GetShadowColor() { return shadowColor.Color; }

        public Color GetMainOriginalColor() { return mainColor.OriginalColor; }
        public Color GetShadowOriginalColor() { return shadowColor.OriginalColor; }

        public float GetMainLuminosity() { return mainColor.GetLuminosity(); }
        public float GetShadowLuminosity() { return shadowColor.GetLuminosity(); }

        public byte GetMainHue() { return mainColor.GetHue(); }
        public byte GetShadowHue() { return shadowColor.GetHue(); }

        public byte GetMainDistance() { return mainColor.GetDistance(); }
        public byte GetShadowDistance() { return shadowColor.GetDistance(); }

        public byte GetMainPosDistance() { return EntryPD.Value; }
        public byte GetMainPosHue() { return EntryPH.Value; }
    }

    public class CustomShadow
    {
        public static CustomShadow?[] allShadows = new CustomShadow?[4] { null, null, null, null };

        public bool HasUniqueShadow { private set; get; }
        public Func<Color, Color> shadowColorGenerater;

        public CustomShadow(bool hasUniqueShadow, Func<Color, Color> shadowColorGenerater, byte type)
        {
            this.HasUniqueShadow = hasUniqueShadow;
            this.shadowColorGenerater = shadowColorGenerater;
            allShadows[type] = this;
        }

        public static void Load()
        {
            new CustomShadow(false, (c) => c.RGBMultiplied(0.6f), 0);
            new CustomShadow(false, (c) => c.RGBMultiplied(0.8f) * c * c * c, 1);
            new CustomShadow(true, (c) => c.RGBMultiplied(0.6f), 2);
        }

        public Color GetShadowColor(Color mainColor,Color shadowColor)
        {
            if (HasUniqueShadow) return shadowColor;
            return shadowColorGenerater(mainColor);
        }
    }

    public class SaveButton
    {
        GameObject ButtonObject;
        ColorButton colorButton;

        public SaveButton(PlayerTab __instance, ColorButton relateButton)
        {
            colorButton = relateButton;

            ButtonObject = new GameObject("SaveButton");
            ButtonObject.transform.SetParent(relateButton.ButtonObject.transform);

            BoxCollider2D Collider = ButtonObject.AddComponent<BoxCollider2D>();
            PassiveButton PassiveButton = ButtonObject.AddComponent<PassiveButton>();

            SpriteRenderer Renderer = ButtonObject.AddComponent<SpriteRenderer>();
            Renderer.sprite = SaveButtonSprite.GetSprite();
            Renderer.transform.localPosition = new Vector3(0.6f, -0.01f, 0);

            Collider.size = new Vector2(0.45f, 0.45f);

            PassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
            PassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
            PassiveButton.OnClick=new Button.ButtonClickedEvent();
            PassiveButton.OnClick.AddListener((System.Action)(() =>
            {
                relateButton.customColor.Transcribe(MyColor);
                relateButton.Reflect();
            }));
            PassiveButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
            PassiveButton.enabled = true;

            ButtonObject.layer = LayerExpansion.GetUILayer();
        }
    }

    public class ColorButton
    {
        public GameObject ButtonObject;
        SpriteRenderer BaseRenderer, ShadowRenderer;
        public CustomColor? customColor { get; private set; }

        //CustomColorの形式で保持していない場合
        Color mainColor, shadowColor;
        Color mainOrigColor, shadowOrigColor;
        byte h, d, sh, sd, ph, pd;
        float l,sl;

        public void SetPosInfo(byte posHue,byte posDistance)
        {
            if (customColor != null) customColor.SetMainPosInfo(posHue,posDistance);
            else
            {
                ph = posHue;
                pd = posDistance;
            }
        }

        private void SetUp(PlayerTab __instance, GameObject layer, Vector3 position, System.Action? onClick)
        {
            ButtonObject = new GameObject("ColorButton");

            BoxCollider2D Collider = ButtonObject.AddComponent<BoxCollider2D>();
            PassiveButton PassiveButton = ButtonObject.AddComponent<PassiveButton>();

            ShadowRenderer = ButtonObject.AddComponent<SpriteRenderer>();
            ShadowRenderer.sprite = ButtonSprite.GetSprite();

            GameObject SubObject = new GameObject("ColorButtonBase");
            SubObject.transform.SetParent(ButtonObject.transform);
            BaseRenderer = SubObject.AddComponent<SpriteRenderer>();
            BaseRenderer.sprite = BaseButtonSprite.GetSprite();
            BaseRenderer.transform.position += new Vector3(0, 0, -1f);

            Collider.size = new Vector2(0.68f, 0.45f);

            PassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
            PassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
            PassiveButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
            PassiveButton.OnMouseOver.AddListener((System.Action)(() =>
            {
                if (customColor != null)
                {
                    Palette.PlayerColors[32] = customColor.GetMainColor();
                    Palette.ShadowColors[32] = customColor.GetShadowColor();
                    PlayerCustomizationMenu.Instance.SetItemName(GetColorName(customColor.GetMainHue(), customColor.GetMainDistance()));
                }
                else
                {
                    Palette.PlayerColors[32] = mainColor;
                    Palette.ShadowColors[32] = shadowColor;
                    PlayerCustomizationMenu.Instance.SetItemName(GetColorName(h,d));
                }

                PlayerCustomizationMenu.Instance.PreviewArea.cosmetics.SetColor(32);
                 

            }));

            PassiveButton.OnClick = new Button.ButtonClickedEvent();
            PassiveButton.OnClick.AddListener((System.Action)(() =>
            {
                if (customColor != null)
                {
                    Palette.PlayerColors[AmongUs.Data.DataManager.Player.Customization.Color] = customColor.GetMainColor();
                    Palette.ShadowColors[AmongUs.Data.DataManager.Player.Customization.Color] = customColor.GetShadowColor();
                    MyColor.Transcribe(customColor);
                }
                else
                {
                    Palette.PlayerColors[AmongUs.Data.DataManager.Player.Customization.Color] = mainColor;
                    Palette.ShadowColors[AmongUs.Data.DataManager.Player.Customization.Color] = shadowColor;
                    MyColor.Set(mainOrigColor, ph, pd, l, h, d, shadowOrigColor, sl, sh, sd, 2);
                }

                PlayerCustomizationMenu.Instance.PreviewArea.cosmetics.SetColor(AmongUs.Data.DataManager.Player.Customization.Color);
                PlayerCustomizationMenu.Instance.SetItemName(GetColorName());

                if (MyColorChangedAction != null) MyColorChangedAction();
            }));
            if (onClick is not null) PassiveButton.OnClick.AddListener(onClick);
            PassiveButton.enabled = true;

            ButtonObject.transform.SetParent(layer.transform);
            ButtonObject.transform.localPosition = position;
            ButtonObject.layer = LayerExpansion.GetUILayer();
            SubObject.layer = LayerExpansion.GetUILayer();
        }

        public ColorButton(PlayerTab __instance, GameObject layer, Vector3 position,  CustomColor? color = null)
        {
            customColor = color;

            SetUp(__instance,layer,position,null);

            Reflect();
        }

        public ColorButton(PlayerTab __instance, GameObject layer, Vector3 position, System.Action? onClick, Color mainColor, Color shadowColor, Color mainOrigColor, Color shadowOrigColor, byte h, byte d, float l, byte sh,byte sd,float sl)
        {
            customColor = null;

            SetUp(__instance, layer, position, onClick);

            this.mainColor = mainColor;
            this.shadowColor = shadowColor;
            this.mainOrigColor = mainOrigColor;
            this.shadowOrigColor = shadowOrigColor;
            this.h = h;
            this.d = d;
            this.l = l;
            this.sh = sh;
            this.sd = sd;
            this.sl = sl;

            BaseRenderer.color = mainColor;
            ShadowRenderer.color = shadowColor;
        }

        public void Reflect()
        {
            if (customColor is not null)
            {
                BaseRenderer.color = customColor.GetMainColor();
                ShadowRenderer.color = customColor.GetShadowColor();
            }
            else
            {
                BaseRenderer.color = mainColor;
                ShadowRenderer.color = shadowColor;
            }
        }

        public void Reflect(Color mainColor,Color shadowColor, Color mainOrigColor,Color shadowOrigColor,byte h, byte d, float l,byte sh, byte sd,float sl)
        {
            BaseRenderer.color = mainColor;
            ShadowRenderer.color = shadowColor;
            if (customColor == null)
            {
                this.mainColor = mainColor;
                this.shadowColor = shadowColor;
                this.mainOrigColor = mainOrigColor;
                this.shadowOrigColor = shadowOrigColor;
                this.h = h;
                this.d = d;
                this.sh = sh;
                this.sd = sd;
                this.l = l;
                this.sl = sl;
            }
        }
    }

    static public CustomColor MyColor;
    static public Action? MyColorChangedAction=null;
    static private CustomColor[] SaveColor;
    static private CustomColor[] SharedColor;
    static private Tuple<Color, Color>[] VanillaColor;

    static DataSaver colorSaver;

    static private ColorButton[] VanillaVariations = null;
    static private ColorButton[] ShadowVariations = null;
    static private SpriteRenderer[] ShadowVariationsSelected = null;
    static private ColorButton[] SaveVariations = null;
    static private SaveButton[] WriteSaveVariations = null;
    static private ColorButton[] SharedVariations = null;
    static private Dictionary<byte, GameObject> OtherPlayersArea = new();

    static private GameObject DynamicLayer = null;
    static private GameObject LegacyLayer = null;

    private static string GetColorName()
    {
        return Language.Language.GetString("color." + (MyColor.GetMainHue()) + "." + MyColor.GetMainDistance());
    }

    private static string GetColorName(byte h, byte d)
    {
        return Language.Language.GetString("color." + h + "." + d);
    }

   

    static public void Load()
    {
        CustomShadow.Load();

        colorSaver = new DataSaver("dynamicColor.dat");

        var PlayerColors = Enumerable.ToList<Color32>(Palette.PlayerColors);
        var ShadowColors = Enumerable.ToList<Color32>(Palette.ShadowColors);

        //31以降をModの使用領域とする
        while (PlayerColors.Count < 64) PlayerColors.Add(new Color32());
        while (ShadowColors.Count < 64) ShadowColors.Add(new Color32());

        //Camo Color
        PlayerColors[31] = PlayerColors[6];
        //Preview Color
//      PlayerColors[32]

        Palette.PlayerColors = PlayerColors.ToArray();
        Palette.ShadowColors = ShadowColors.ToArray();

        MyColor = new CustomColor("Color");
        SaveColor = new CustomColor[5];
        SharedColor = new CustomColor[6];
        VanillaColor = new Tuple<Color, Color>[18];
        for (int i = 0; i < 18; i++) VanillaColor[i] = new(PlayerColors[i], ShadowColors[i]);
        for (int i = 0; i < SaveColor.Length; i++) SaveColor[i] = new CustomColor("SaveColor" + i);
        for (int i = 0; i < SharedColor.Length; i++) SharedColor[i] = new CustomColor("SharedColor" + i, 0f);

        AmongUs.Data.DataManager.Player.Customization.Color = 0;
        Palette.PlayerColors[AmongUs.Data.DataManager.Player.Customization.Color] = MyColor.GetMainColor();
        Palette.ShadowColors[AmongUs.Data.DataManager.Player.Customization.Color] = MyColor.GetShadowColor();
    }

    //他人の色名を覚えておく
    static public Dictionary<int, Tuple<byte, byte>> ColorNameDic = new();
    //他人の色座標を覚えておく
    static public Dictionary<int, Tuple<byte, byte>> ColorPosDic = new();

    static public void SetOthersColor(byte hue, byte dis, byte posHue,byte posDis,Color color, Color shadowColor, byte playerId)
    {
        ColorNameDic[(int)playerId] = new Tuple<byte, byte>(hue, dis);
        ColorPosDic[(int)playerId] = new Tuple<byte, byte>(posHue, posDis);
        Palette.PlayerColors[playerId] = color;
        Palette.ShadowColors[playerId] = shadowColor;
        var p = Helpers.playerById(playerId);
        if (p) p.RawSetColor(playerId);
    }

    [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), new[] {
                typeof(StringNames),
                typeof(Il2CppReferenceArray<Il2CppSystem.Object>)
            })]
    private class ColorStringPatch
    {
        public static bool Prefix(ref string __result, [HarmonyArgument(0)] StringNames name)
        {
            if (((int)StringNames.ColorOrange <= (int)name && (int)StringNames.ColorLime >= (int)name) ||
                ((int)StringNames.ColorMaroon <= (int)name && (int)StringNames.ColorSunset >= (int)name) ||
                StringNames.ColorCoral == name)
            {
                __result = GetColorName();
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(CosmeticsLayer), nameof(CosmeticsLayer.GetColorBlindText))]
    private class CosmeticsLayerColorStringPatch
    {
        public static bool Prefix(CosmeticsLayer __instance,ref string __result)
        {
            int color = __instance.bodyMatProperties.ColorId;

            string colorName;
            if (ColorNameDic.ContainsKey(color))
            {
                var tuple = ColorNameDic[color];
                colorName = GetColorName(tuple.Item1, tuple.Item2);
            }
            else
            {
                colorName = GetColorName();
            }

            char[] array = colorName.ToCharArray();
            if (array.Length != 0)
            {
                array[0] = char.ToUpper(array[0]);
                bool afterSpace = false;
                for (int i = 1; i < array.Length; i++)
                {
                    array[i] = afterSpace ? char.ToUpper(array[i]) : char.ToLower(array[i]);
                    afterSpace = array[i] == ' ';
                }
            }
            
            __result = new string(array);

            return false;
        }
    }

    static private GameObject PaletteObject = null;
    static private SpriteRenderer PaletteRenderer = null;
    static private CircleCollider2D PaletteCollider = null;

    

    static private GameObject LPaletteObject = null;
    static private SpriteRenderer LPaletteRenderer = null;
    static private BoxCollider2D LPaletteCollider = null;

    static private SpriteRenderer BrightnessRenderer = null;

    private static void DetectColor(out Color? detectedColor,out byte? detectedH, out byte? detectedD, out float? detectedL)
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - PaletteObject.transform.position;
        float dis = Mathf.Sqrt(pos.x * pos.x + pos.y * pos.y);

        float angle = Mathf.Atan2(pos.y, pos.x) - (float)System.Math.PI / 2f;
        if (angle < 0) angle += (float)(System.Math.PI * 2f);
        Color color;
        float h = angle / (float)(2f * System.Math.PI);
        color = Color.HSVToRGB(h, 1f, 1f);
        if (dis < 0.45)
        {
            float v = 1f - (dis / 0.45f);
            color += new Color(v, v, v);
        }
        else if (dis > 1.1f)
        {
            float s = dis - 1.1f;
            float sum = (color.r + color.g + color.b) / 3f;
            color = new Color(sum, sum, sum) * s + color * (1 - s);
        }

        pos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - LPaletteObject.transform.position;
        float l = pos.y / LPaletteCollider.size.y + 0.5f;

        pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (PaletteCollider.OverlapPoint(pos))
        {
            detectedL = null;
            detectedColor = color;
            detectedH = (byte)((h * 64) % 64);
            detectedD = (byte)((dis / 2.1f) * 24);
        }
        else if (LPaletteCollider.OverlapPoint(pos))
        {
            detectedL = l;
            detectedColor = null;
            detectedH = null;
            detectedD = null;
        }
        else
        {
            detectedL = null;
            detectedColor = null;
            detectedH = null;
            detectedD = null;
        }
    }

    static bool ShowVanillaColorFlag = false;
    static bool EditMainColorFlag = true;

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.Update))]
    private static class PlayerTabUpdatePatch
    {
        public static void Postfix(PlayerTab __instance)
        {
            if (!__instance.enabled) return;

            if (!ShowVanillaColorFlag)
            {
                Color? c;
                byte? h, d;
                float? l;
                DetectColor(out c,out h,out d,out l);

                if(c.HasValue || l.HasValue)
                {
                    Color main, shadow;
                    MyColor.Simulate(EditMainColorFlag ? c : null, EditMainColorFlag ? l : null, EditMainColorFlag ? null : c, EditMainColorFlag ? null : l, ref h, ref d, out main, out shadow);
                    Palette.PlayerColors[32] = main;
                    Palette.ShadowColors[32] = shadow;
                    if (c.HasValue) PlayerCustomizationMenu.Instance.SetItemName(GetColorName(h!.Value, d!.Value));

                    PlayerCustomizationMenu.Instance.PreviewArea.cosmetics.SetColor(32);
                }

                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.NotJoined)
                {
                    foreach (var key in OtherPlayersArea.Keys.Where((b) => !PlayerControl.AllPlayerControls.Find((Il2CppSystem.Predicate<PlayerControl>)((p) => p.PlayerId == b))))
                    {
                        GameObject.Destroy(OtherPlayersArea[key]);
                        OtherPlayersArea.Remove(key);
                    }
                    foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
                    {
                        if (OtherPlayersArea.ContainsKey(p.PlayerId)) continue;
                        if (PlayerControl.LocalPlayer.PlayerId == p.PlayerId) continue;
                        var obj = new GameObject("Mask");
                        obj.transform.SetParent(PaletteObject.transform);
                        obj.transform.localPosition = Vector3.zero;
                        OtherPlayersArea[p.PlayerId] = obj;

                        SpriteMask mask;

                        var AreaObj = new GameObject("AreaMask");
                        AreaObj.layer = LayerExpansion.GetUILayer();
                        AreaObj.transform.SetParent(obj.transform);
                        AreaObj.transform.localPosition = Vector3.zero;

                        mask = AreaObj.AddComponent<SpriteMask>();
                        mask.backSortingOrder = 9;
                        mask.backSortingOrder = 10;
                        mask.sprite = MaskSprite.GetSprite();

                        var circleObj = new GameObject("CircleMask");
                        circleObj.layer = LayerExpansion.GetUILayer();
                        circleObj.transform.SetParent(obj.transform);
                        circleObj.transform.localPosition = Vector3.zero;

                        mask = circleObj.AddComponent<SpriteMask>();
                        mask.backSortingOrder = 9;
                        mask.backSortingOrder = 10;
                        mask.sprite = MaskCircleSprite.GetSprite();
                    }
                    foreach (var entry in OtherPlayersArea)
                    {
                        if (ColorPosDic.TryGetValue(entry.Key, out var tuple))
                        {
                            float p = (0.04f + (float)tuple.Item2 / 24f * 1.6f);
                            var t = entry.Value.transform;
                            t.eulerAngles = new Vector3(0, 0, (float)tuple.Item1 / 64f * 360f);

                            t.GetChild(0).localScale = new Vector3(p > 1f ? Mathf.Pow(Mathf.Clamp(p, 1f, 5f), 2.7f) : (p * 0.7f + 0.3f), 0.1f + Mathf.Pow(p, 1.3f), 1f);
                            t.GetChild(1).localScale = Vector3.one * (p > 1f ? 1f / Mathf.Clamp(p, 1f, 10f) : Mathf.Clamp(p, 0.4f, 1f));
                            t.GetChild(1).localPosition = Vector3.up * (p - 0.02f) * 1.35f;
                        }
                    }
                }
            }

            BrightnessRenderer.sprite = BrightnessSprite[IsLightColor(Palette.PlayerColors[32]) ? 0 : 1].GetSprite();
        }
    }

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
    private static class PlayerTabEnablePatch
    {
        public static void Postfix(PlayerTab __instance)
        {
            //全てのColorChipを無効化
            foreach (ColorChip colorChip in __instance.ColorChips)
            {
                colorChip.transform.localScale *= 0f;
                colorChip.enabled = false;
                colorChip.Button.enabled = false;
                colorChip.Button.OnClick.RemoveAllListeners();
            }

            //パレット
            if (!PaletteObject)
            {
                var VanillaLayer = new GameObject("VanillaLayer");
                var ModLayer = new GameObject("ModLayer");
                VanillaLayer.transform.SetParent(__instance.transform);
                ModLayer.transform.SetParent(__instance.transform);
                VanillaLayer.transform.localPosition = new Vector3(0, 0, 0);
                ModLayer.transform.localPosition = new Vector3(0, 0, 0);

                PaletteObject = new GameObject("DynamicPalette");
                PaletteObject.transform.SetParent(ModLayer.transform);
                PaletteRenderer = PaletteObject.AddComponent<SpriteRenderer>();
                PaletteCollider = PaletteObject.AddComponent<CircleCollider2D>();
                PaletteObject.AddComponent<SortingGroup>();
                var PalettePassiveButton = PaletteObject.AddComponent<PassiveButton>();

                var PaletteDisabled = new GameObject("DynamicPaletteDisabled");
                PaletteDisabled.layer = LayerExpansion.GetUILayer();
                PaletteDisabled.transform.SetParent(PaletteObject.transform);
                PaletteDisabled.transform.localScale = Vector3.one;
                PaletteDisabled.transform.localPosition = Vector3.zero;
                var PDRenderer = PaletteDisabled.AddComponent<SpriteRenderer>();
                PDRenderer.sortingOrder = 10;
                PDRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                PDRenderer.sprite = PaletteDisabledSprite.GetSprite();
                var PaletteDisabledFrame = new GameObject("DynamicPaletteDisabledFrame");
                PaletteDisabledFrame.layer = LayerExpansion.GetUILayer();
                PaletteDisabledFrame.transform.SetParent(PaletteObject.transform);
                PaletteDisabledFrame.transform.localScale = Vector3.one;
                PaletteDisabledFrame.transform.localPosition = Vector3.zero;
                var DFRenderer = PaletteDisabledFrame.AddComponent<SpriteRenderer>();
                DFRenderer.sortingOrder = 20;
                DFRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                //DFRenderer.sprite = PaletteDisabledFrameSprite.GetSprite();

                OtherPlayersArea.Clear();

                LPaletteObject = new GameObject("LuminosityPalette");
                LPaletteObject.transform.SetParent(ModLayer.transform);
                LPaletteRenderer = LPaletteObject.AddComponent<SpriteRenderer>();
                LPaletteCollider = LPaletteObject.AddComponent<BoxCollider2D>();
                var LPalettePassiveButton = LPaletteObject.AddComponent<PassiveButton>();

                var ChangeModeObject = new GameObject("ChangeButton");
                var ChangeModeRenderer = ChangeModeObject.AddComponent<SpriteRenderer>();
                var ChangeModeCollider = ChangeModeObject.AddComponent<BoxCollider2D>();
                var ChangeModePassiveButton = ChangeModeObject.AddComponent<PassiveButton>();

                var ColorTargetObject = new GameObject("ColorTarget");
                ColorTargetObject.transform.SetParent(ModLayer.transform);
                var ColorTargetRenderer = ColorTargetObject.AddComponent<SpriteRenderer>();

                var LTargetObject = new GameObject("LTarget");
                LTargetObject.transform.SetParent(ModLayer.transform);
                var LTargetRenderer = LTargetObject.AddComponent<SpriteRenderer>();

                var BrightnessObject = new GameObject("Brightness");
                BrightnessObject.transform.SetParent(__instance.transform);
                BrightnessRenderer = BrightnessObject.AddComponent<SpriteRenderer>();

                ShadowVariations = new ColorButton[3];
                ShadowVariationsSelected = new SpriteRenderer[3];
                for (int i = 0; i < ShadowVariations.Length; i++)
                {
                    int index = i;
                    ShadowVariations[i] =
                        new ColorButton(__instance, ModLayer, new Vector3(1.8f + (((float)i) - 1f) * 0.8f, -2.2f, -75f),
                        () =>
                        {
                            if (CustomShadow.allShadows[index].HasUniqueShadow && MyColor.GetShadowType() == index)
                                EditMainColorFlag = !EditMainColorFlag;
                            else
                            {
                                EditMainColorFlag = true;
                                MyColor.SetShadowType((byte)index);
                            }

                            if (MyColorChangedAction is not null) MyColorChangedAction();
                        }, MyColor.GetMainColor(), MyColor.GetShadowColor(), MyColor.GetMainOriginalColor(), MyColor.GetShadowOriginalColor(), MyColor.GetMainHue(), MyColor.GetMainDistance(), MyColor.GetMainLuminosity(), MyColor.GetShadowHue(), MyColor.GetShadowDistance(), MyColor.GetShadowLuminosity());
                    GameObject selectedObject = new GameObject("Selected");
                    selectedObject.layer = LayerExpansion.GetUILayer();
                    selectedObject.transform.SetParent(ShadowVariations[i].ButtonObject.transform);
                    selectedObject.transform.localPosition = new Vector3(0,0,-2f);
                    ShadowVariationsSelected[i] = selectedObject.AddComponent<SpriteRenderer>();
                    ShadowVariationsSelected[i].sprite = SelectedSprite[0].GetSprite();
                }

                SaveVariations = new ColorButton[5];
                WriteSaveVariations = new SaveButton[5];
                Vector3 pos;
                for (int i = 0; i < SaveVariations.Length; i++)
                {
                    int index = i;
                    pos = new Vector3(-1.5f, 1.2f - (float)i * 0.6f, -75f);
                    SaveVariations[i] =
                        new ColorButton(__instance, __instance.gameObject, pos, SaveColor[i]);
                    WriteSaveVariations[i] = new SaveButton(__instance, SaveVariations[i]);
                }

                VanillaVariations = new ColorButton[18];
                for (int i = 0; i < VanillaVariations.Length; i++)
                {
                    int index = i;
                    VanillaVariations[i] =
                        new ColorButton(__instance, VanillaLayer, new Vector3(0.2f + (float)(i % 6) * 0.8f, 1.3f - 0.5f * (float)(i / 6), -75f),
                        null, VanillaColor[i].Item1, VanillaColor[i].Item2, VanillaColor[i].Item1, VanillaColor[i].Item2, 100, (byte)i, 1f, 100, (byte)0, 1f);
                }
                VanillaVariations[0].SetPosInfo(63, 14);
                VanillaVariations[1].SetPosInfo(42, 14);
                VanillaVariations[2].SetPosInfo(24, 18);
                VanillaVariations[3].SetPosInfo(57, 2);
                VanillaVariations[4].SetPosInfo(5, 6);
                VanillaVariations[5].SetPosInfo(10, 3);
                VanillaVariations[6].SetPosInfo(42, 23);
                VanillaVariations[7].SetPosInfo(0, 0);
                VanillaVariations[8].SetPosInfo(47, 18);
                VanillaVariations[9].SetPosInfo(5, 17);
                VanillaVariations[10].SetPosInfo(29, 11);
                VanillaVariations[11].SetPosInfo(19, 4);
                VanillaVariations[12].SetPosInfo(61, 19);
                VanillaVariations[13].SetPosInfo(62, 1);
                VanillaVariations[14].SetPosInfo(4, 1);
                VanillaVariations[15].SetPosInfo(35, 22);
                VanillaVariations[16].SetPosInfo(6, 22);
                VanillaVariations[17].SetPosInfo(0, 3);

                SharedVariations = new ColorButton[6];
                for (int i = 0; i < SharedVariations.Length; i++)
                {
                    int index = i;
                    SharedVariations[i] =
                        new ColorButton(__instance, VanillaLayer, new Vector3(0.2f + (float)(i % 6) * 0.8f, -0.8f - 0.5f * (float)(i / 6), -75f),SharedColor[i]);
                    SharedVariations[i].ButtonObject.SetActive(false);
                }

                PaletteObject.transform.localPosition = new Vector3(1.8f, 0.35f, -40f);
                PaletteObject.layer = __instance.ColorChips[0].gameObject.layer;

                PaletteRenderer.sprite = PaletteSprite.GetSprite();
                PaletteCollider.radius = 2.1f;
                PalettePassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                PalettePassiveButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
                PalettePassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                PalettePassiveButton.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    PlayerCustomizationMenu.Instance.PreviewArea.cosmetics.SetColor(AmongUs.Data.DataManager.Player.Customization.Color);
                    PlayerCustomizationMenu.Instance.SetItemName(GetColorName());
                }));
                PalettePassiveButton.OnClick.RemoveAllListeners();
                PalettePassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    Color? c;
                    byte? h, d;
                    float? l;
                    DetectColor(out c,out h,out d,out l);
                    if (!CustomShadow.allShadows[MyColor.GetShadowType()]!.HasUniqueShadow)
                    {
                        MyColor.SetMainColor(c!.Value, MyColor.GetMainLuminosity(), h!.Value, d!.Value);
                        MyColor.SetShadowColor(CustomShadow.allShadows[MyColor.GetShadowType()]!.GetShadowColor(MyColor.GetMainColor(), MyColor.GetShadowColor()), 1f,0,0);
                        MyColor.SetMainPosInfo(h!.Value,d!.Value);
                    }
                    else
                    {
                        if (EditMainColorFlag)
                        {
                            MyColor.SetMainColor(c!.Value, MyColor.GetMainLuminosity(), h!.Value, d!.Value);
                            MyColor.SetMainPosInfo(h!.Value,d!.Value);
                        }
                        else
                            MyColor.SetShadowColor(c!.Value, MyColor.GetShadowLuminosity(), h!.Value, d!.Value);
                    }

                    if (MyColorChangedAction is not null) MyColorChangedAction();
                }));
                PalettePassiveButton.enabled = true;

                // ------------------------------------------------------ //

                LPaletteObject.transform.localPosition = new Vector3(4.5f, 0.35f, -40f);
                LPaletteObject.layer = __instance.ColorChips[0].gameObject.layer;

                LPaletteRenderer.sprite = LPaletteSprite.GetSprite();
                LPaletteCollider.size = new Vector2(0.3f, 2.7f);
                LPalettePassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                LPalettePassiveButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
                LPalettePassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                LPalettePassiveButton.OnClick.RemoveAllListeners();
                LPalettePassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    Color? c;
                    byte? h, d;
                    float? l;
                    DetectColor(out c, out h, out d, out l);

                    if (!CustomShadow.allShadows[MyColor.GetShadowType()]!.HasUniqueShadow)
                    {
                        MyColor.SetMainColor(MyColor.GetMainOriginalColor(), l.Value, MyColor.GetMainHue(), MyColor.GetMainDistance());
                        MyColor.SetShadowColor(CustomShadow.allShadows[MyColor.GetShadowType()]!.GetShadowColor(MyColor.GetMainColor(), MyColor.GetShadowColor()), 1f, 0, 0);
                    }
                    else
                    {
                        if (EditMainColorFlag)
                            MyColor.SetMainColor(MyColor.GetMainOriginalColor(), l.Value, MyColor.GetMainHue(), MyColor.GetMainDistance());
                        else
                            MyColor.SetShadowColor(MyColor.GetShadowOriginalColor(), l.Value, MyColor.GetShadowHue(), MyColor.GetShadowDistance());
                    }

                    if (MyColorChangedAction is not null) MyColorChangedAction();
                }));
                LPalettePassiveButton.enabled = true;

                // ------------------------------------------------------ //


                ChangeModeObject.transform.SetParent(__instance.ColorChips[0].transform.parent);
                ChangeModeObject.transform.localPosition = new Vector3(4.3f, -1.6f, -40f);
                ChangeModeObject.layer = __instance.ColorChips[0].gameObject.layer;

                ChangeModeRenderer.sprite = GetModeChangeSprite(ShowVanillaColorFlag);
                ChangeModeCollider.size = new Vector2(0.55f, 0.55f);
                ChangeModePassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                ChangeModePassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                ChangeModePassiveButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
                ChangeModePassiveButton.OnClick.RemoveAllListeners();
                ChangeModePassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    ShowVanillaColorFlag = !ShowVanillaColorFlag;
                    ChangeModeRenderer.sprite = GetModeChangeSprite(ShowVanillaColorFlag);
                    VanillaLayer.SetActive(ShowVanillaColorFlag);
                    ModLayer.SetActive(!ShowVanillaColorFlag);
                }));
                ChangeModePassiveButton.enabled = true;

                // ------------------------------------------------------ //

                ColorTargetObject.transform.SetParent(PaletteObject.transform);
                ColorTargetObject.layer = __instance.ColorChips[0].gameObject.layer;
                ColorTargetRenderer.sprite = ColorTargetSprite.GetSprite();

                LTargetObject.transform.SetParent(LPaletteObject.transform);
                LTargetObject.layer = __instance.ColorChips[0].gameObject.layer;
                LTargetRenderer.sprite = LTargetSprite.GetSprite();

                BrightnessObject.transform.localPosition = new Vector3(6.7f, -0.5f, -75f);
                BrightnessObject.layer = __instance.ColorChips[0].gameObject.layer;
                BrightnessRenderer.sprite = BrightnessSprite[IsLightColor(MyColor.GetMainColor()) ? 0 : 1].GetSprite();

                MyColorChangedAction = () =>
                {
                    if (!CustomShadow.allShadows[MyColor.GetShadowType()].HasUniqueShadow && !EditMainColorFlag) EditMainColorFlag = true;

                    Palette.PlayerColors[AmongUs.Data.DataManager.Player.Customization.Color] = MyColor.GetMainColor();
                    Palette.ShadowColors[AmongUs.Data.DataManager.Player.Customization.Color] = MyColor.GetShadowColor();

                    if (AmongUsClient.Instance.AmConnected)RPCEventInvoker.SetMyColor();
                    

                    float l = (EditMainColorFlag ? MyColor.GetMainLuminosity(): MyColor.GetShadowLuminosity()) * 0.85f + 0.15f;
                    PaletteRenderer.color = new Color(l, l, l);

                    LPaletteRenderer.color = EditMainColorFlag ? MyColor.GetMainOriginalColor() : MyColor.GetShadowOriginalColor();


                    float angle = (EditMainColorFlag ? MyColor.GetMainHue() : MyColor.GetShadowHue()) / 64f * 2f * (float)System.Math.PI;
                    float r = (EditMainColorFlag ? MyColor.GetMainDistance() : MyColor.GetShadowDistance()) / 10f * 0.88f;
                    ColorTargetObject.transform.localPosition = new Vector3(-MathF.Sin(angle) * r, MathF.Cos(angle) * r, -200f);

                    l = ((EditMainColorFlag ? MyColor.GetMainLuminosity() : MyColor.GetShadowLuminosity()) - 0.5f) * 2f;
                    LTargetObject.transform.localPosition = new Vector3(0f, l * 1.35f, -200f);

                    for(int i=0;i< ShadowVariations.Length; i++)
                    {
                        ShadowVariations[i].Reflect(MyColor.GetMainColor(),CustomShadow.allShadows[i].GetShadowColor(MyColor.GetMainColor(),MyColor.GetShadowColor()),
                            MyColor.GetMainOriginalColor(),MyColor.GetShadowOriginalColor(),
                            MyColor.GetMainHue(),MyColor.GetMainDistance(), MyColor.GetMainLuminosity(), MyColor.GetShadowHue(), MyColor.GetShadowDistance(),MyColor.GetShadowLuminosity());
                        
                        ShadowVariationsSelected[i].gameObject.SetActive(i == MyColor.GetShadowType());
                        if (i == MyColor.GetShadowType() && CustomShadow.allShadows[i].HasUniqueShadow)
                        {
                            ShadowVariationsSelected[i].sprite = SelectedSprite[EditMainColorFlag ? 1 : 2].GetSprite();
                        }
                    }
                };
                MyColorChangedAction();
                UpdateSharedColor();

                if (AmongUsClient.Instance.AmConnected) {
                    GameObject shareButton = GameObject.Instantiate(PlayerCustomizationMenu.Instance.equipButton);
                    shareButton.transform.SetParent(__instance.transform);
                    shareButton.transform.localPosition = new Vector3(6.069f, -1.92f, -1f);
                    shareButton.transform.GetChild(1).gameObject.SetActive(false);
                    var bRenderer = shareButton.transform.GetChild(0).GetComponent<SpriteRenderer>();
                    bRenderer.size = new Vector2(bRenderer.size.y, bRenderer.size.y);
                    var bCollider = shareButton.GetComponent<BoxCollider2D>();
                    bCollider.size = new Vector2(bCollider.size.y, bCollider.size.y);

                    GameObject shareIconRenderer = new GameObject("ShareIcon");
                    shareIconRenderer.layer = LayerExpansion.GetUILayer();
                    shareIconRenderer.transform.SetParent(shareButton.transform);
                    shareIconRenderer.transform.localPosition = new Vector3(0, 0, -0.5f);
                    shareIconRenderer.transform.localScale = new Vector3(0.85f,0.85f,1f);
                    shareIconRenderer.AddComponent<SpriteRenderer>().sprite = ShareIconSprite.GetSprite();

                    var sButton = shareButton.GetComponent<PassiveButton>();
                    sButton.OnClick = new Button.ButtonClickedEvent();
                    sButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
                        RPCEventInvoker.ShareColor(MyColor);
                    }));

                    shareButton.SetActive(true);
                }

                VanillaLayer.SetActive(ShowVanillaColorFlag);
                ModLayer.SetActive(!ShowVanillaColorFlag);
            }
        }
    }

    static public void UpdateSharedColor()
    {
        for(int i = 0; i < 6; i++) 
        {
            if(SharedColor[i].GetMainColor().r>0f|| SharedColor[i].GetMainColor().g > 0f|| SharedColor[i].GetMainColor().b > 0f)
            {
                SharedVariations[i].ButtonObject.SetActive(true);
                SharedVariations[i].Reflect();
            }
            else
            {
                SharedVariations[i].ButtonObject.SetActive(false);
            }
        }
    }

    static public void ReceiveSharedColor(byte shadowType,byte mainPosHue,byte mainPosDis,Color mainColor,float mainLum, byte mainHue,byte mainDis,Color shadowColor,float shadowLum,byte shadowHue,byte shadowDis)
    {
        for(int i = 5; i >= 1; i--)
        {
            SharedColor[i].Transcribe(SharedColor[i - 1]);
        }
        SharedColor[0].Set(mainColor, mainPosHue,mainPosDis,mainLum, mainHue, mainDis, shadowColor, shadowLum, shadowHue, shadowDis, shadowType);

        if (PlayerCustomizationMenu.Instance)
        {
            UpdateSharedColor();
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizationMenu), nameof(PlayerCustomizationMenu.Update))]
    class PlayerCustomizationMenuUpdatePatch
    {
        public static void Postfix(PlayerCustomizationMenu __instance)
        {
            if (__instance.selectedTab == 0)
            {
                __instance.equippedText.SetActive(false);
            }
        }
    }

}