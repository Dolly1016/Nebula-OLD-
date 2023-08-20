using AmongUs.Data;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Nebula;
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

    static public Dictionary<int, Tuple<int, int>> ColorNameDic = new();

    static private DataSaver ColorData;
    static public ModColor MyColor;

    static public void Load()
    {
        ColorData = new DataSaver("DynamicColor");
        MyColor = new ModColor("myColor");
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

        public void Edit(byte hue, byte distance, float brightness, byte palette)
        {
            this.hue.Value = hue;
            this.distance.Value = distance;
            this.brightness.Value = brightness;
            this.palette.Value = palette;
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

        public void EditMainColor(byte hue, byte distance, float brightness, byte palette)
        {
            mainParameters.Edit(hue, distance, brightness, palette);
            var tempColor = mainParameters.ToColor();
            AllShadowPattern[shadowType.Value].GetShadowColor(tempColor, shadowColor, out mainColor, out shadowColor);
        }

        public void EditShadowColor(byte hue, byte distance, float brightness, byte palette)
        {
            shadowParameters.Edit(hue, distance, brightness, palette);
            var tempColor = shadowParameters.ToColor();
            AllShadowPattern[shadowType.Value].GetShadowColor(mainColor, tempColor, out mainColor, out shadowColor);
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

    

    static NebulaPlayerTab()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaPlayerTab>();
    }

    int currentPalette = 0;
    bool edittingShadowColor = false;

    SpriteRenderer DynamicPaletteRenderer;
    SpriteRenderer TargetRenderer;
    

    public PlayerTab playerTab;


    public void Start()
    {
        DynamicPaletteRenderer = UnityHelper.CreateObject<SpriteRenderer>("DynamicPalette",transform, new Vector3(1.3f, 0.2f, -80f));
        DynamicPaletteRenderer.sprite = spritePalette.GetSprite();
        DynamicPaletteRenderer.gameObject.layer = LayerExpansion.GetUILayer();
        var PaletteCollider = DynamicPaletteRenderer.gameObject.AddComponent<CircleCollider2D>();
        PaletteCollider.radius = 2.1f;
        var PaletteButton = DynamicPaletteRenderer.gameObject.AddComponent<PassiveButton>();
        PaletteButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        PaletteButton.OnMouseOut = new UnityEngine.Events.UnityEvent();
        PaletteButton.OnMouseOver = new UnityEngine.Events.UnityEvent();

        PaletteButton.OnClick.AddListener(() => {
            ToColorParam(GetOnPalettePosition(), out var h, out var d);

            if (edittingShadowColor)
                DynamicPalette.MyColor.EditShadowColor(h, d, 1.0f, 0);
            else
                DynamicPalette.MyColor.EditMainColor(h, d, 1.0f, 0);
            TargetRenderer.transform.localPosition = ToPalettePosition(h, d);

            if (AmongUsClient.Instance.IsInGame && PlayerControl.LocalPlayer) DynamicPalette.RpcShareColor.Invoke(new DynamicPalette.ShareColorMessage() { playerId=PlayerControl.LocalPlayer.PlayerId}.ReflectMyColor());
        });
        PaletteButton.OnMouseOut.AddListener(() =>
        {
            DynamicPalette.MyColor.GetMainParam(out var h, out var d, out _);
            PreviewColor(h, d, DynamicPalette.MyColor.MainColor, DynamicPalette.MyColor.ShadowColor);
        });

        TargetRenderer = UnityHelper.CreateObject<SpriteRenderer>("TargetIcon", DynamicPaletteRenderer.transform, new Vector3(0f, 0f, -10f));
        TargetRenderer.sprite = spriteTarget.GetSprite();
        TargetRenderer.gameObject.layer = LayerExpansion.GetUILayer();

        byte h, d;
        if(edittingShadowColor)
            DynamicPalette.MyColor.GetShadowParam(out h,out d,out float b);
        else
            DynamicPalette.MyColor.GetMainParam(out h, out d, out float b);
        TargetRenderer.transform.localPosition = ToPalettePosition(h,d);
    }

    public void OnEnable()
    {
        DynamicPalette.MyColor.GetMainParam(out var h,out var d,out _);
        string colorName = Language.Translate(ColorNamePatch.ToTranslationKey(h, d));
        PlayerCustomizationMenu.Instance.SetItemName(colorName);
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
            Color color = DynamicPalette.AllColorPalette[currentPalette].GetColor(h, d, 1f);

            DynamicPalette.AllShadowPattern[DynamicPalette.MyColor.GetShadowPattern()].GetShadowColor(
                edittingShadowColor ? DynamicPalette.MyColor.MainColor : color,
                edittingShadowColor ? color : DynamicPalette.MyColor.ShadowColor,
                out var resultMain,out var resultShadow
                );
            PreviewColor(h, d, resultMain, resultShadow);
        }
    }

    static byte PreviewColorId = 15;

    private void PreviewColor(int concernedHue, int concernedDistance, Color mainColor,Color shadowColor)
    {
        Palette.PlayerColors[PreviewColorId] = mainColor;
        Palette.ShadowColors[PreviewColorId] = shadowColor;

        string colorName = Language.Translate(ColorNamePatch.ToTranslationKey(concernedHue,concernedDistance));
        PlayerCustomizationMenu.Instance.SetItemName(colorName);
        playerTab.currentColor = PreviewColorId;
        playerTab.PlayerPreview.SetBodyColor(PreviewColorId);
        playerTab.PlayerPreview.SetPetColor(PreviewColorId);
        playerTab.PlayerPreview.SetSkin(DataManager.Player.Customization.Skin, PreviewColorId);
        playerTab.PlayerPreview.SetHat(DataManager.Player.Customization.Hat, PreviewColorId);
        playerTab.PlayerPreview.SetVisor(DataManager.Player.Customization.Visor, PreviewColorId);
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