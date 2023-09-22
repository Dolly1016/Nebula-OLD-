using AmongUs.Data;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Nebula;
using Nebula.Behaviour;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Nebula.Modules.NebulaPlayerTab;
using static Nebula.Player.PlayerModInfo;
using static Rewired.Demos.PressStartToJoinExample_Assigner;

namespace Nebula.Modules;

[NebulaPreLoad(typeof(RemoteProcessBase),typeof(NebulaAddon))]
[NebulaRPCHolder]
public class DynamicPalette
{
    static public ColorPalette[] AllColorPalette = { new DefaultColorPalette() };
    static public ShadowPattern[] AllShadowPattern = { new DefaultShadowPattern() };

    static public Dictionary<int, (int h, int d, string? name)> ColorNameDic = new();

    static private DataSaver ColorData;
    static public ModColor MyColor;
    static public ModColor[] SavedColor;

    public static Dictionary<string, List<RestorableColor>> ColorCatalogue = new();

    static public void Load()
    {
        ColorData = new DataSaver("DynamicColor");
        MyColor = new ModColor("myColor");
        SavedColor = new ModColor[] { new("savedColor0"), new("savedColor1"), new("savedColor2"), new("savedColor3"), new("savedColor4") };

        List<RestorableColor> vanilaCatalogue = new();
        for (int i = 0; i < 18; i++)
        {
            vanilaCatalogue.Add(new RestorableColor()
            {
                MainColor = new(Palette.PlayerColors[i]),
                ShadowColor = new(Palette.ShadowColors[i]),
                ShadowType = byte.MaxValue,
                Category = "innersloth",
                TranslationKey = "inventory.color.vanilla" + i
            });
        }
        ColorCatalogue.Add("innersloth",vanilaCatalogue);


        //カモフラージャーカラー
        Palette.PlayerColors[16] = Palette.PlayerColors[6].Multiply(new Color32(180, 180, 180, 255));
        Palette.ShadowColors[16] = Palette.ShadowColors[6].Multiply(new Color32(180, 180, 180, 255));
    }

    static public IEnumerator CoLoad()
    {
        Patches.LoadPatch.LoadingText = "Loading Color Catalogue";
        yield return null;

        foreach (var addon in NebulaAddon.AllAddons)
        {
            using var stream = addon.OpenStream("Color/ColorCatalogue.json");
            if (stream == null) continue;

            var colors = JsonStructure.Deserialize<List<RestorableColor>>(stream);
            if (colors == null) continue;

            foreach (var c in colors)
            {
                if (!ColorCatalogue.ContainsKey(c.Category))
                    ColorCatalogue[c.Category] = new();
                ColorCatalogue[c.Category].Add(c);
            }

            yield return null;
        }
    }

    public class ColorParameters
    {
        public DataEntry<byte> hue, distance;
        public DataEntry<float> brightness;
        public DataEntry<byte> palette;
        public DataEntry<byte>[] color;
        public ColorParameters(string colorDataId)
        {
            hue = new ByteDataEntry(colorDataId + ".h", ColorData, 0);
            distance = new ByteDataEntry(colorDataId + ".d", ColorData, 8);
            palette = new ByteDataEntry(colorDataId + ".p", ColorData, 0);
            brightness = new FloatDataEntry(colorDataId + ".b", ColorData, 1f);
            color = new DataEntry<byte>[3] {
                new ByteDataEntry(colorDataId + ".col.r",ColorData,0),
                new ByteDataEntry(colorDataId + ".col.g",ColorData,0),
                new ByteDataEntry(colorDataId + ".col.b",ColorData,0),
            };
        }

        public void Edit(byte? hue, byte? distance, float? brightness, byte? palette)
        {
            if(hue.HasValue) this.hue.Value = hue.Value;
            if(distance.HasValue) this.distance.Value = distance.Value;
            if(brightness.HasValue) this.brightness.Value = brightness.Value;
            if(palette.HasValue) this.palette.Value = palette.Value;
        }

        public void Edit(Color color)
        {
            hue.Value = byte.MaxValue;
            this.color[0].Value = (byte)(color.r * byte.MaxValue);
            this.color[1].Value = (byte)(color.g * byte.MaxValue);
            this.color[2].Value = (byte)(color.b * byte.MaxValue);
        }

        public void Edit(SerializableColor color)
        {
            hue.Value = color.Hue;
            distance.Value = color.Distance;
            brightness.Value = color.Brightness;
            this.color[0].Value = color.R;
            this.color[1].Value = color.G;
            this.color[2].Value = color.B;
        }

        public Color ToColor()
        {
            if (hue.Value == byte.MaxValue)
                return new Color((float)color[0].Value/255f, (float)color[1].Value / 255f, (float)color[2].Value / 255f);
            else
                return AllColorPalette[palette.Value].GetColor(hue.Value, distance.Value, brightness.Value);
        }
    }
    public class ModColor
    {
        public Color mainColor, shadowColor;
        public Color MainColor { get => mainColor; }
        public Color ShadowColor { get => shadowColor; }
        public string Name { get => nameEntry.Value; set => nameEntry.Value = value; }
        private ColorParameters mainParameters, shadowParameters;
        private DataEntry<byte> shadowType;
        private DataEntry<string> nameEntry;

        public ModColor(string colorDataId)
        {
            mainParameters = new ColorParameters(colorDataId + ".main");
            shadowParameters = new ColorParameters(colorDataId + ".shadow");
            shadowType = new ByteDataEntry(colorDataId + ".type", ColorData, 0);
            nameEntry = new StringDataEntry(colorDataId + ".name", ColorData, "none");

            if (shadowType.Value != byte.MaxValue)
            {
                AllShadowPattern[shadowType.Value].GetShadowColor(mainParameters.ToColor(), shadowParameters.ToColor(), out mainColor, out shadowColor);
            }
            else
            {
                mainColor = mainParameters.ToColor();
                shadowColor = shadowParameters.ToColor();
            }
        }

        public void EditColor(bool isShadow,byte? hue, byte? distance, float? brightness, byte? palette)
        {
            var param = isShadow ? shadowParameters : mainParameters;

            param.Edit(hue, distance, brightness, palette);

            if (shadowType.Value != byte.MaxValue)
            {
                var tempColor = param.ToColor();
                AllShadowPattern[shadowType.Value].GetShadowColor(
                    isShadow ? mainColor : tempColor,
                    isShadow ? tempColor : shadowColor,
                    out mainColor, out shadowColor);
            }
        }

        public void RestoreColor(Color mainColor,Color shadowColor,byte shadowType,(byte hue,byte disatance,float brightness)? mainParam, (byte hue, byte disatance, float brightness)? shadowParam,string? name)
        {
            this.mainColor = mainColor;
            this.shadowColor = shadowColor;
            this.shadowType.Value = shadowType;

            if (mainParam != null)
                mainParameters.Edit(mainParam.Value.hue, mainParam.Value.disatance, mainParam.Value.brightness, 0);
            else
                mainParameters.Edit(mainColor);

            if (shadowParam != null)
                shadowParameters.Edit(shadowParam.Value.hue, shadowParam.Value.disatance, shadowParam.Value.brightness, 0);
            else
                shadowParameters.Edit(shadowColor);

            if (name != null) Name = name;
        }

        public void Restore(RestorableColor color)
        {
            mainColor = color.MainColor.AsColor;
            shadowColor = color.ShadowColor.AsColor;
            shadowType.Value = color.ShadowType;
            mainParameters.Edit(color.MainColor);
            shadowParameters.Edit(color.ShadowColor);
            Name = color.DisplayName;

            if (shadowType.Value != byte.MaxValue)
            {
                var tempColor = mainParameters.ToColor();
                AllShadowPattern[shadowType.Value].GetShadowColor(tempColor,shadowColor,
                    out mainColor, out shadowColor);
            }
        }

        public void Restore(ModColor color)
        {
            mainColor = color.mainColor;
            shadowColor = color.shadowColor;
            shadowType.Value = color.shadowType.Value;
    
            if(color.mainParameters.hue.Value == byte.MaxValue)
                mainParameters.Edit(color.mainColor);
            else
                mainParameters.Edit(color.mainParameters.hue.Value, color.mainParameters.distance.Value, color.mainParameters.brightness.Value, color.mainParameters.palette.Value);

            if (color.shadowParameters.hue.Value == byte.MaxValue)
                shadowParameters.Edit(color.shadowColor);
            else
                shadowParameters.Edit(color.shadowParameters.hue.Value, color.shadowParameters.distance.Value, color.shadowParameters.brightness.Value, color.shadowParameters.palette.Value);
            
            Name = color.Name;

            if (shadowType.Value != byte.MaxValue)
            {
                var tempColor = mainParameters.ToColor();
                AllShadowPattern[shadowType.Value].GetShadowColor(tempColor, shadowColor,
                    out mainColor, out shadowColor);
            }
        }

        public byte GetMainHue()
        {
            return mainParameters.hue.Value;
        }
        public void GetMainParam(out byte hue, out byte distance,out float brightness) {
            hue = mainParameters.hue.Value;
            distance = mainParameters.distance.Value;
            brightness = mainParameters.brightness.Value;
        }

        public void GetShadowParam(out byte hue, out byte distance, out float brightness) {
            hue = shadowParameters.hue.Value;
            distance = shadowParameters.distance.Value;
            brightness = shadowParameters.brightness.Value;
        }

        public void GetParam(bool isShadow, out byte hue, out byte distance, out float brightness)
        {
            var param = isShadow ? shadowParameters : mainParameters;

            hue = param.hue.Value;
            distance = param.distance.Value;
            brightness = param.brightness.Value;
        }

        public void SetShadowPattern(byte pattern) {
            shadowType.Value = pattern;
            AllShadowPattern[shadowType.Value].GetShadowColor(mainColor, shadowColor, out mainColor, out shadowColor);
        }
        public byte GetShadowPattern() => shadowType.Value;
    }

    public class SerializableColor
    {
        [JsonSerializableField]
        public byte R, G, B;
        [JsonSerializableField]
        public byte Hue = byte.MaxValue, Distance = 0;
        [JsonSerializableField]
        public float Brightness = 1f;

        public SerializableColor() { }
        public SerializableColor(Color color)
        {
            R = (byte)(color.r * byte.MaxValue);
            G = (byte)(color.g * byte.MaxValue);
            B = (byte)(color.b * byte.MaxValue);
            Hue = byte.MaxValue;
            Distance = 0;
            Brightness = 1f;
        }

        public SerializableColor(ColorParameters color)
        {
            var rawColor = color.ToColor();
            (R, G, B) = ((byte)(rawColor.r * byte.MaxValue), (byte)(rawColor.g * byte.MaxValue), (byte)(rawColor.b * byte.MaxValue));
            Hue = color.hue.Value;
            Distance = color.distance.Value;
            Brightness = color.brightness.Value;
        }

        public Color AsColor => new Color(R / 255f, G / 255f, B / 255f);
    }

    public class RestorableColor
    {
        [JsonSerializableField]
        public SerializableColor MainColor;
        [JsonSerializableField]
        public SerializableColor ShadowColor;
        [JsonSerializableField]
        public string Name = "";
        [JsonSerializableField]
        public string? TranslationKey = null;
        [JsonSerializableField]
        public byte ShadowType = byte.MaxValue;
        [JsonSerializableField]
        public string Category = "";
        public string DisplayName { get => TranslationKey != null ? Language.Translate(TranslationKey) : Name; }
    }
    public class ShareColorMessage
    {
        public Color mainColor, shadowColor;
        public byte playerId;
        public Tuple<int, int>? colorNameParam;
        public string? colorName;

        public ShareColorMessage ReflectMyColor()
        {
            mainColor = MyColor.mainColor;
            shadowColor = MyColor.shadowColor;
            MyColor.GetMainParam(out var h, out var d, out _);
            colorNameParam = new(h, d);
            colorName = h == byte.MaxValue ? MyColor.Name : null;
            return this;
        }
    }

    public readonly static RemoteProcess<ShareColorMessage> RpcShareColor = new RemoteProcess<ShareColorMessage>(
        "ShareColor",
        (writer, message) =>
        {
            writer.Write(message.playerId);
            writer.Write(message.mainColor.r);
            writer.Write(message.mainColor.g);
            writer.Write(message.mainColor.b);
            writer.Write(message.shadowColor.r);
            writer.Write(message.shadowColor.g);
            writer.Write(message.shadowColor.b);
            if (message.colorName != null)
            {
                writer.Write(false);
                writer.Write(message.colorName);
            }
            else
            {
                writer.Write(true);
                writer.Write(message.colorNameParam!.Item1);
                writer.Write(message.colorNameParam!.Item2);
            }
        },
        (reader) =>
        {
            ShareColorMessage message = new ShareColorMessage();
            message.playerId = reader.ReadByte();
            message.mainColor = new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            message.shadowColor = new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            if (reader.ReadBoolean())
                message.colorNameParam = new(reader.ReadInt32(), reader.ReadInt32());
            else
                message.colorName = reader.ReadString();
            return message;
        },
        (message, isCalledByMe) =>
        {
            Palette.PlayerColors[message.playerId] = message.mainColor;
            Palette.ShadowColors[message.playerId] = message.shadowColor;
            ColorNameDic[message.playerId] = (message.colorNameParam?.Item1 ?? byte.MaxValue, message.colorNameParam?.Item2 ?? 0, message.colorName);

            //まだプレイヤーが追加されていない場合は即座に反映させなくても大丈夫
            try
            {
                PlayerControl.AllPlayerControls.Find((Il2CppSystem.Predicate<PlayerControl>)(p => p.PlayerId == message.playerId))?.RawSetColor(message.playerId);
            }
            catch (Exception e){ }
        }
        );

    public interface ColorPalette
    {
        //hue 0～63 distance 0～23 brightness 0f～1f
        Color GetColor(byte hue, byte distance, float brightness);
    }

    public interface ShadowPattern
    {
        void GetShadowColor(Color mainColor, Color shadowColor, out Color resultMain, out Color resultShadow);
        abstract bool AllowEditShadowColor { get; }
    }

    public class DefaultColorPalette : ColorPalette
    {
        public Color GetColor(byte hue, byte distance, float brightness)
        {
            var color = Color.HSVToRGB((float)hue / 64f, 1f, 1f);
            if (distance < 6)
            {
                float d = 1f - (float)distance / 6f;
                color += new Color(d, d, d);
            }
            else if (distance > 9)
            {
                float s = (float)(distance - 9f) / 14f;
                float sum = (color.r + color.g + color.b) / 3f;
                color = new Color(sum, sum, sum) * s + color * (1 - s);
            }
            return color * (brightness * 0.5f + 0.5f);
        }
    }

    public class DefaultShadowPattern : ShadowPattern
    {
        public void GetShadowColor(Color mainColor, Color shadowColor, out Color resultMain, out Color resultShadow)
        {
            resultMain = mainColor;
            resultShadow = mainColor.RGBMultiplied(0.5f);
        }

        public bool AllowEditShadowColor => true;
    }

    public static bool IsLightColor(Color color)
    {
        return (color.r + color.g + color.b) > 1.2f;
    }

    public static SpriteLoader colorButtonSprite = SpriteLoader.FromResource("Nebula.Resources.ColorHalfButton.png", 100f);
    public static SpriteLoader colorBackSprite = SpriteLoader.FromResource("Nebula.Resources.ColorFullBase.png", 100f);
    public static void OpenCatalogue(SpriteRenderer TargetRenderer,Action ShownColor)
    {
        var screen = MetaScreen.GenerateWindow(new Vector2(6f, 3.6f), PlayerCustomizationMenu.Instance.transform, new Vector3(0f, 0f, 0f), true, false, true);

        MetaContext inner = new();
        MetaContext.ScrollView scrollView = new(new(6f, 3.6f), inner, true);
        
        foreach(var category in ColorCatalogue)
        {
            inner.Append(new MetaContext.Text(new(TextAttribute.BoldAttr) { FontMaterial = VanillaAsset.StandardMaskedFontMaterial}) { TranslationKey = "inventory.catalogue." + category.Key });
            inner.Append(category.Value, (col) =>
                new MetaContext.Image(colorButtonSprite.GetSprite())
                {
                    Width = 0.96f,
                    PostBuilder = (renderer) =>
                    {
                        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                        renderer.sortingOrder = 20;
                        renderer.color = col.MainColor.AsColor;

                        var baseRenderer= GameObject.Instantiate(renderer,renderer.transform.parent);
                        baseRenderer.sprite = colorBackSprite.GetSprite();
                        baseRenderer.transform.localPosition += new Vector3(0, 0, 0.1f);
                        baseRenderer.sortingOrder = 10;
                        baseRenderer.color = col.ShadowColor.AsColor;
                        

                        var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
                        collider.size = new(0.7f, 0.5f);
                        collider.isTrigger = true;
                        var button = renderer.gameObject.SetUpButton(true);
                        button.OnClick.AddListener(() =>
                        {
                            screen.CloseScreen();
                            MyColor.Restore(col);
                            TargetRenderer.gameObject.SetActive(col.MainColor.Hue != byte.MaxValue);
                            TargetRenderer.transform.localPosition = NebulaPlayerTab.ToPalettePosition(col.MainColor.Hue, col.MainColor.Distance);

                            ShownColor();

                            if (AmongUsClient.Instance && AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId = PlayerControl.LocalPlayer.PlayerId }.ReflectMyColor());
                        });
                        button.OnMouseOver.AddListener(() =>
                        {
                            MetaContext context = new();
                            context.Append(new MetaContext.VariableText(new(TextAttribute.BoldAttr) { Alignment = TextAlignmentOptions.Left }) { RawText = col.DisplayName });
                            if(col.TranslationKey != null)
                            {
                                var detail = Language.Find(col.TranslationKey + ".detail");
                                if (detail != null) context.Append(new MetaContext.VariableText(TextAttribute.ContentAttr) { RawText = detail });
                            }
                            NebulaManager.Instance.SetHelpContext(button,context);
                        });
                        button.OnMouseOut.AddListener(NebulaManager.Instance.HideHelpContext);
                    }
                }
            , 6, -1, 0, 0.65f);
        }
        screen.SetContext(scrollView);
    }
}

public class NebulaPlayerTab : MonoBehaviour
{
    static ISpriteLoader spritePalette = SpriteLoader.FromResource("Nebula.Resources.Palette.png", 100f);
    static ISpriteLoader spriteTarget = SpriteLoader.FromResource("Nebula.Resources.TargetIcon.png", 100f);

    static ISpriteLoader spriteBrPalette = SpriteLoader.FromResource("Nebula.Resources.PaletteBrightness.png", 100f);
    static ISpriteLoader spriteBrTarget = SpriteLoader.FromResource("Nebula.Resources.PaletteKnob.png", 100f);


    static NebulaPlayerTab()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaPlayerTab>();
    }

    int currentPalette = 0;
    bool edittingShadowColor = false;

    SpriteRenderer DynamicPaletteRenderer;
    SpriteRenderer TargetRenderer;

    SpriteRenderer BrightnessRenderer;
    SpriteRenderer BrightnessTargetRenderer;
    UIKnob BrTargetKnob;

    public PlayerTab playerTab;

    static private float BrightnessHeight = 2.6f;
    static private float ToBrightness(float y) => Mathf.Clamp01((y + BrightnessHeight * 0.5f) / BrightnessHeight);

    static private SpriteLoader saveButtonSprite = SpriteLoader.FromResource("Nebula.Resources.ColorSave.png", 100f);
    public void Start()
    {
        new MetaContext.Button(() => DynamicPalette.OpenCatalogue(TargetRenderer, () => PreviewColor(null, null, null)), TextAttribute.BoldAttr) { TranslationKey = "inventory.palette.catalogue" }.Generate(gameObject, new Vector2(2.9f, 2.25f));

        DynamicPaletteRenderer = UnityHelper.CreateObject<SpriteRenderer>("DynamicPalette",transform, new Vector3(0.4f, -0.1f, -80f));
        DynamicPaletteRenderer.sprite = spritePalette.GetSprite();
        DynamicPaletteRenderer.gameObject.layer = LayerExpansion.GetUILayer();
        var PaletteCollider = DynamicPaletteRenderer.gameObject.AddComponent<CircleCollider2D>();
        PaletteCollider.radius = 2.1f;
        PaletteCollider.isTrigger = true;
        var PaletteButton = DynamicPaletteRenderer.gameObject.SetUpButton();

        PaletteButton.OnClick.AddListener(() => {
            if (DynamicPalette.MyColor.GetShadowPattern() == byte.MaxValue) DynamicPalette.MyColor.SetShadowPattern(0);

            ToColorParam(GetOnPalettePosition(), out var h, out var d);

            DynamicPalette.MyColor.EditColor(edittingShadowColor, h, d, null, null);
            TargetRenderer.gameObject.SetActive(h != byte.MaxValue);
            TargetRenderer.transform.localPosition = ToPalettePosition(h, d);

            if (AmongUsClient.Instance && AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId=PlayerControl.LocalPlayer.PlayerId}.ReflectMyColor());
        });
        PaletteButton.OnMouseOut.AddListener(() =>
        {
            PreviewColor(null, null, null);
        });

        TargetRenderer = UnityHelper.CreateObject<SpriteRenderer>("TargetIcon", DynamicPaletteRenderer.transform, new Vector3(0f, 0f, -10f));
        TargetRenderer.sprite = spriteTarget.GetSprite();
        TargetRenderer.gameObject.layer = LayerExpansion.GetUILayer();

        DynamicPalette.MyColor.GetParam(edittingShadowColor, out byte h, out byte d, out _);
        TargetRenderer.gameObject.SetActive(h != byte.MaxValue);
        TargetRenderer.transform.localPosition = ToPalettePosition(h,d);

        BrightnessRenderer = UnityHelper.CreateObject<SpriteRenderer>("BrightnessPalette", transform, new Vector3(3.1f, -0.1f, -80f));
        BrightnessRenderer.sprite = spriteBrPalette.GetSprite();
        BrightnessRenderer.gameObject.layer = LayerExpansion.GetUILayer();

        BrightnessTargetRenderer = UnityHelper.CreateObject<SpriteRenderer>("BrightnessPalette", BrightnessRenderer.transform, new Vector3(0f, 0.0f, -1f));
        BrightnessTargetRenderer.sprite = spriteBrTarget.GetSprite();
        BrightnessTargetRenderer.gameObject.layer = LayerExpansion.GetUILayer();

        var BrPaletteCollider = BrightnessRenderer.gameObject.AddComponent<BoxCollider2D>();
        BrPaletteCollider.size = new Vector2(0.31f, BrightnessHeight);
        BrPaletteCollider.isTrigger = true;

        var BrPaletteBackButton = BrightnessRenderer.gameObject.SetUpButton();

        BrPaletteBackButton.OnClick.AddListener(() => {
            if (DynamicPalette.MyColor.GetShadowPattern() == byte.MaxValue) return;

            var pos = UnityHelper.ScreenToWorldPoint(Input.mousePosition, LayerExpansion.GetUILayer()) - BrPaletteBackButton.transform.position;
            var b = ToBrightness(pos.y);
            DynamicPalette.MyColor.EditColor(edittingShadowColor,null, null, b, null);

            var targetLocPos = BrightnessTargetRenderer.transform.localPosition;
            targetLocPos.y = Mathf.Clamp(pos.y, -BrightnessHeight * 0.5f, BrightnessHeight * 0.5f);
            BrightnessTargetRenderer.transform.localPosition = targetLocPos;

            if (AmongUsClient.Instance && AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId = PlayerControl.LocalPlayer.PlayerId }.ReflectMyColor());

            PreviewColor(null, null, null);
        });

        var BrTargetCollider = BrightnessTargetRenderer.gameObject.AddComponent<BoxCollider2D>();
        BrTargetCollider.size = new Vector2(0.38f, 0.18f);
        BrTargetCollider.isTrigger = true;

        BrTargetKnob = BrightnessTargetRenderer.gameObject.AddComponent<UIKnob>();
        BrTargetKnob.IsVert = true;
        BrTargetKnob.Range = (-BrightnessHeight * 0.5f, BrightnessHeight * 0.5f);
        BrTargetKnob.Renderer = BrightnessTargetRenderer;
        BrTargetKnob.OnRelease = () => {
            if (DynamicPalette.MyColor.GetShadowPattern() == byte.MaxValue) DynamicPalette.MyColor.SetShadowPattern(0);

            float b = ToBrightness(BrTargetKnob.transform.localPosition.y);

            DynamicPalette.MyColor.EditColor(edittingShadowColor,null, null, b, null);

            if (AmongUsClient.Instance && AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId = PlayerControl.LocalPlayer.PlayerId }.ReflectMyColor());

            PreviewColor(null, null, null);
        };
        BrTargetKnob.OnDragging = (y) => {
            float b = (y + (BrightnessHeight * 0.5f)) / BrightnessHeight;
            PreviewColor(null, null, b);
        };

        for(int i = 0; i < DynamicPalette.SavedColor.Length; i++)
        {
            int copiedIndex = i;
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("SavedColor", transform, new(4.45f + (float)i * 0.81f, 2.25f, -50f));
            renderer.sprite = DynamicPalette.colorButtonSprite.GetSprite();
            renderer.color = DynamicPalette.SavedColor[copiedIndex].MainColor;
            var baseRenderer = UnityHelper.CreateObject<SpriteRenderer>("ShadowColor", renderer.transform, new(0f, 0f, 1f));
            baseRenderer.sprite = DynamicPalette.colorBackSprite.GetSprite();
            baseRenderer.color = DynamicPalette.SavedColor[copiedIndex].ShadowColor;
            var saveRenderer = UnityHelper.CreateObject<SpriteRenderer>("SaveButton", renderer.transform, new(0f, -0.38f, 0f));
            saveRenderer.sprite = saveButtonSprite.GetSprite();

            var restoreButton = renderer.gameObject.SetUpButton(true);
            restoreButton.OnClick.AddListener(() => {
                DynamicPalette.MyColor.Restore(DynamicPalette.SavedColor[copiedIndex]);
                TargetRenderer.gameObject.SetActive(h != byte.MaxValue);
                TargetRenderer.transform.localPosition = ToPalettePosition(h, d);

                if (AmongUsClient.Instance && AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId = PlayerControl.LocalPlayer.PlayerId }.ReflectMyColor());

                PreviewColor(null, null, null);
            });
            restoreButton.OnMouseOver.AddListener(() => PreviewColor(DynamicPalette.SavedColor[copiedIndex]));
            restoreButton.OnMouseOut.AddListener(() => PreviewColor(null, null, null));
            restoreButton.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 0.5f);

            var saveButon = saveRenderer.gameObject.SetUpButton(true,saveRenderer);
            saveButon.OnClick.AddListener(() =>
            {
                DynamicPalette.SavedColor[copiedIndex].Restore(DynamicPalette.MyColor);
                renderer.color = DynamicPalette.MyColor.mainColor;
                baseRenderer.color = DynamicPalette.MyColor.shadowColor;
            });
            saveButon.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.25f, 0.25f);
        }


        PreviewColor(null, null, null);
    }

    public void OnEnable()
    {
        PreviewColor(null, null, null);
    }

    private Vector2 GetOnPalettePosition()
    {
        return UnityHelper.ScreenToWorldPoint(Input.mousePosition,LayerExpansion.GetUILayer()) - DynamicPaletteRenderer.transform.position;
    }

    public static Vector3 ToPalettePosition(byte hue,byte distance)
    {
        float magnitude = (float)distance / 24f * 2.1f;
        float angle = (float)hue / 64 * (2f * Mathf.PI) + Mathf.PI * 0.5f;
        return new Vector3(Mathf.Cos(angle) * magnitude, Mathf.Sin(angle) * magnitude, -1f);
    }

    private void ToColorParam(Vector2 pos,out byte hue,out byte distance)
    {
        distance = (byte)(pos.magnitude / 2.1f * 24);
        if (distance > 23) distance = 23;
        hue = (byte)(Mathf.Atan2(-pos.x, pos.y) / (2f * Mathf.PI) * 64);
        while (hue < 0) hue += 64;
        while (hue >= 64) hue -= 64;
    }

    public void Update()
    {
        BrTargetKnob.gameObject.SetActive(DynamicPalette.MyColor.GetShadowPattern() != byte.MaxValue);

        var pos = GetOnPalettePosition();
        float distance = pos.magnitude;
        
        if (distance < 2.06f)
        {
            //DynamicPaletteによる色の更新
            ToColorParam(pos, out var h, out var d);
            PreviewColor(h, d, null);
        }
    }

    static byte PreviewColorId = 15;

    private void AfterPreviewColor(byte concernedHue, byte concernedDistance,string? displayName = null)
    {
        try
        {
            string colorName;
            if (concernedHue == byte.MaxValue)
                colorName = displayName ?? DynamicPalette.MyColor.Name;
            else
                colorName = Language.Translate(ColorNamePatch.ToTranslationKey(concernedHue, concernedDistance));
            PlayerCustomizationMenu.Instance.SetItemName(colorName);


            playerTab.PlayerPreview.SetBodyColor(PreviewColorId);
            playerTab.PlayerPreview.SetPetColor(PreviewColorId);
            if (playerTab.currentColor != PreviewColorId)
            {
                playerTab.currentColor = PreviewColorId;
                playerTab.PlayerPreview.SetSkin(DataManager.Player.Customization.Skin, PreviewColorId);
                playerTab.PlayerPreview.SetHat(DataManager.Player.Customization.Hat, PreviewColorId);
                playerTab.PlayerPreview.SetVisor(DataManager.Player.Customization.Visor, PreviewColorId);
            }
            else
            {
                playerTab.PlayerPreview.cosmetics.skin.UpdateMaterial();
                playerTab.PlayerPreview.cosmetics.hat.UpdateMaterial();
                playerTab.PlayerPreview.cosmetics.visor.UpdateMaterial();
            }
        }
        catch { }
    }
    private void PreviewColor(byte? concernedHue, byte? concernedDistance, float? concernedBrightness)
    {
        try
        {
            DynamicPalette.MyColor.GetParam(edittingShadowColor, out byte h, out byte d, out var b);
            concernedHue ??= h;
            concernedDistance ??= d;
            concernedBrightness ??= b;

            BrightnessRenderer.color = DynamicPalette.AllColorPalette[currentPalette].GetColor((byte)concernedHue, (byte)concernedDistance, 1f);

            if (DynamicPalette.MyColor.GetShadowPattern() != byte.MaxValue)
            {
                Color color = DynamicPalette.AllColorPalette[currentPalette].GetColor(concernedHue.Value, concernedDistance.Value, concernedBrightness.Value);
                DynamicPalette.AllShadowPattern[DynamicPalette.MyColor.GetShadowPattern()].GetShadowColor(
                    edittingShadowColor ? DynamicPalette.MyColor.MainColor : color,
                    edittingShadowColor ? color : DynamicPalette.MyColor.ShadowColor,
                    out var resultMain, out var resultShadow
                    );

                Palette.PlayerColors[PreviewColorId] = resultMain;
                Palette.ShadowColors[PreviewColorId] = resultShadow;
            }
            else
            {
                Palette.PlayerColors[PreviewColorId] = DynamicPalette.MyColor.MainColor;
                Palette.ShadowColors[PreviewColorId] = DynamicPalette.MyColor.ShadowColor;
            }

            AfterPreviewColor(concernedHue.Value,concernedDistance.Value);
        }
        catch { }
    }

    private void PreviewColor(DynamicPalette.ModColor color)
    {
        try
        {
            Palette.PlayerColors[PreviewColorId] = color.MainColor;
            Palette.ShadowColors[PreviewColorId] = color.ShadowColor;

            color.GetMainParam(out var h,out var d,out _);
            AfterPreviewColor(h, d, color.Name);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(Palette), nameof(Palette.GetColorName))]
public static class ColorNamePatch
{
    static public string ToTranslationKey(int h,int d)
    {
        return "color." + h + "." + d;
    }

    static bool Prefix(ref string __result, [HarmonyArgument(0)]int colorId)
    {
        if (colorId < 15)
        {
            if (DynamicPalette.ColorNameDic.TryGetValue(colorId, out var tuple))
            {
                if (tuple.h == byte.MaxValue)
                    __result = tuple.name ?? "";
                else
                    __result = Language.Translate(ToTranslationKey(tuple.Item1, tuple.Item2));
            }
            else
                __result = "";
        }
        else if (colorId == 15)
        {
            DynamicPalette.MyColor.GetMainParam(out var h, out var d, out var b);
            __result = Language.Translate(ToTranslationKey(h, d));
        }
        else
        {
            __result = "";
        }
           
        return false;
    }
}