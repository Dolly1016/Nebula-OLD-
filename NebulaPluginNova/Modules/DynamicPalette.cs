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

[NebulaPreLoad(typeof(RemoteProcessBase))]
[NebulaRPCHolder]
public class DynamicPalette
{
    static public ColorPalette[] AllColorPalette = { new DefaultColorPalette() };
    static public ShadowPattern[] AllShadowPattern = { new DefaultShadowPattern() };
    static public Tuple<Color,Color>[] VanillaColors;

    static public Dictionary<int, Tuple<int, int>> ColorNameDic = new();

    static private DataSaver ColorData;
    static public ModColor MyColor;

    static public void Load()
    {
        ColorData = new DataSaver("DynamicColor");
        MyColor = new ModColor("myColor");
        VanillaColors = new Tuple<Color,Color>[18];
        for (int i = 0; i < 18; i++) VanillaColors[i] = new(Palette.PlayerColors[i], Palette.ShadowColors[i]);

        //カモフラージャーカラー
        Palette.PlayerColors[16] = Palette.PlayerColors[6].Multiply(new Color32(180, 180, 180, 255));
        Palette.ShadowColors[16] = Palette.ShadowColors[6].Multiply(new Color32(180, 180, 180, 255));
    }

    public class ColorParameters
    {
        public DataEntry<byte> hue, distance;
        public DataEntry<float> brightness;
        public DataEntry<byte> palette;

        public ColorParameters(string colorDataId)
        {
            hue = new ByteDataEntry(colorDataId + ".h", ColorData, 0);
            distance = new ByteDataEntry(colorDataId + ".d", ColorData, 8);
            palette = new ByteDataEntry(colorDataId + ".p", ColorData, 0);
            brightness = new FloatDataEntry(colorDataId + ".b", ColorData, 1f);
        }

        public void Edit(byte? hue, byte? distance, float? brightness, byte? palette)
        {
            if(hue.HasValue) this.hue.Value = hue.Value;
            if(distance.HasValue) this.distance.Value = distance.Value;
            if(brightness.HasValue) this.brightness.Value = brightness.Value;
            if(palette.HasValue) this.palette.Value = palette.Value;
        }

        public Color ToColor()
        {
            return AllColorPalette[palette.Value].GetColor(hue.Value, distance.Value, brightness.Value);
        }
    }
    public class ModColor
    {
        public Color mainColor, shadowColor;
        public Color MainColor { get => mainColor; }
        public Color ShadowColor { get => shadowColor; }
        private ColorParameters mainParameters, shadowParameters;
        private DataEntry<byte> shadowType;

        public ModColor(string colorDataId)
        {
            mainParameters = new ColorParameters(colorDataId + ".main");
            shadowParameters = new ColorParameters(colorDataId + ".shadow");
            shadowType = new ByteDataEntry(colorDataId + ".type", ColorData, 0);

            AllShadowPattern[shadowType.Value].GetShadowColor(mainParameters.ToColor(), shadowParameters.ToColor(), out mainColor, out shadowColor);
        }

        public void EditColor(bool isShadow,byte? hue, byte? distance, float? brightness, byte? palette)
        {
            var param = isShadow ? shadowParameters : mainParameters;

            param.Edit(hue, distance, brightness, palette);
            var tempColor = param.ToColor();
            AllShadowPattern[shadowType.Value].GetShadowColor(
                isShadow ? mainColor : tempColor,
                isShadow ? tempColor : shadowColor, 
                out mainColor, out shadowColor);
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

    public class ShareColorMessage
    {
        public Color mainColor, shadowColor;
        public byte playerId;
        public Tuple<int, int> colorName;

        public ShareColorMessage ReflectMyColor()
        {
            mainColor = MyColor.mainColor;
            shadowColor = MyColor.shadowColor;
            MyColor.GetMainParam(out var h, out var d, out _);
            colorName = new(h, d);
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
            writer.Write(message.colorName.Item1);
            writer.Write(message.colorName.Item2);
        },
        (reader) =>
        {
            ShareColorMessage message = new ShareColorMessage();
            message.playerId = reader.ReadByte();
            message.mainColor = new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            message.shadowColor = new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            message.colorName = new(reader.ReadInt32(), reader.ReadInt32());
            return message;
        },
        (message, isCalledByMe) =>
        {
            Palette.PlayerColors[message.playerId] = message.mainColor;
            Palette.ShadowColors[message.playerId] = message.shadowColor;
            ColorNameDic[message.playerId] = message.colorName;

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
            resultShadow = mainColor * 0.5f;
        }

        public bool AllowEditShadowColor => true;
    }

    public static bool IsLightColor(Color color)
    {
        return (color.r + color.g + color.b) > 1.2f;
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


    public PlayerTab playerTab;

    static private float BrightnessHeight = 2.6f;
    static private float ToBrightness(float y) => Mathf.Clamp01((y + BrightnessHeight * 0.5f) / BrightnessHeight);

    public void Start()
    {
        DynamicPaletteRenderer = UnityHelper.CreateObject<SpriteRenderer>("DynamicPalette",transform, new Vector3(1.3f, 0.2f, -80f));
        DynamicPaletteRenderer.sprite = spritePalette.GetSprite();
        DynamicPaletteRenderer.gameObject.layer = LayerExpansion.GetUILayer();
        var PaletteCollider = DynamicPaletteRenderer.gameObject.AddComponent<CircleCollider2D>();
        PaletteCollider.radius = 2.1f;
        PaletteCollider.isTrigger = true;
        var PaletteButton = DynamicPaletteRenderer.gameObject.SetUpButton();

        PaletteButton.OnClick.AddListener(() => {
            ToColorParam(GetOnPalettePosition(), out var h, out var d);

            DynamicPalette.MyColor.EditColor(edittingShadowColor, h, d, null, null);
            TargetRenderer.transform.localPosition = ToPalettePosition(h, d);

            if (AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId=PlayerControl.LocalPlayer.PlayerId}.ReflectMyColor());
        });
        PaletteButton.OnMouseOut.AddListener(() =>
        {
            PreviewColor(null, null, null);
        });

        TargetRenderer = UnityHelper.CreateObject<SpriteRenderer>("TargetIcon", DynamicPaletteRenderer.transform, new Vector3(0f, 0f, -10f));
        TargetRenderer.sprite = spriteTarget.GetSprite();
        TargetRenderer.gameObject.layer = LayerExpansion.GetUILayer();

        DynamicPalette.MyColor.GetParam(edittingShadowColor, out byte h, out byte d, out _);
        TargetRenderer.transform.localPosition = ToPalettePosition(h,d);

        BrightnessRenderer = UnityHelper.CreateObject<SpriteRenderer>("BrightnessPalette", transform, new Vector3(4.1f, 0.2f, -80f));
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
            var pos = UnityHelper.ScreenToWorldPoint(Input.mousePosition, LayerExpansion.GetUILayer()) - BrPaletteBackButton.transform.position;
            var b = ToBrightness(pos.y);
            DynamicPalette.MyColor.EditColor(edittingShadowColor,null, null, b, null);

            var targetLocPos = BrightnessTargetRenderer.transform.localPosition;
            targetLocPos.y = Mathf.Clamp(pos.y, -BrightnessHeight * 0.5f, BrightnessHeight * 0.5f);
            BrightnessTargetRenderer.transform.localPosition = targetLocPos;

            if (AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId = PlayerControl.LocalPlayer.PlayerId }.ReflectMyColor());

            PreviewColor(null, null, null);
        });

        var BrTargetCollider = BrightnessTargetRenderer.gameObject.AddComponent<BoxCollider2D>();
        BrTargetCollider.size = new Vector2(0.38f, 0.18f);
        BrTargetCollider.isTrigger = true;

        var BrTargetKnob = BrightnessTargetRenderer.gameObject.AddComponent<UIKnob>();
        BrTargetKnob.IsVert = true;
        BrTargetKnob.Range = (-BrightnessHeight * 0.5f, BrightnessHeight * 0.5f);
        BrTargetKnob.Renderer = BrightnessTargetRenderer;
        BrTargetKnob.OnRelease = () => {
            float b = ToBrightness(BrTargetKnob.transform.localPosition.y);

            DynamicPalette.MyColor.EditColor(edittingShadowColor,null, null, b, null);

            if (AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId = PlayerControl.LocalPlayer.PlayerId }.ReflectMyColor());

            PreviewColor(null, null, null);
        };
        BrTargetKnob.OnDragging = (y) => {
            float b = (y + (BrightnessHeight * 0.5f)) / BrightnessHeight;
            PreviewColor(null, null, b);
        };

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

    private Vector3 ToPalettePosition(byte hue,byte distance)
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

    private void PreviewColor(byte? concernedHue, byte? concernedDistance,float? concernedBrightness)
    {
        try
        {
            DynamicPalette.MyColor.GetParam(edittingShadowColor, out byte h, out byte d, out var b);
            concernedHue ??= h;
            concernedDistance ??= d;
            concernedBrightness ??= b;

            BrightnessRenderer.color = DynamicPalette.AllColorPalette[currentPalette].GetColor((byte)concernedHue, (byte)concernedDistance, 1f);

            Color color = DynamicPalette.AllColorPalette[currentPalette].GetColor(concernedHue.Value, concernedDistance.Value, concernedBrightness.Value);
            DynamicPalette.AllShadowPattern[DynamicPalette.MyColor.GetShadowPattern()].GetShadowColor(
                edittingShadowColor ? DynamicPalette.MyColor.MainColor : color,
                edittingShadowColor ? color : DynamicPalette.MyColor.ShadowColor,
                out var resultMain, out var resultShadow
                );

            Palette.PlayerColors[PreviewColorId] = resultMain;
            Palette.ShadowColors[PreviewColorId] = resultShadow;

            string colorName = Language.Translate(ColorNamePatch.ToTranslationKey(concernedHue.Value, concernedDistance.Value));
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
                __result = Language.Translate(ToTranslationKey(tuple.Item1, tuple.Item2));
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