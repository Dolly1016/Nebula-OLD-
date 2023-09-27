using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace Nebula.Modules;

[NebulaPreLoad(typeof(SerializableDocument),typeof(NebulaAddon))]
public class DocumentManager
{
    private static Dictionary<string, SerializableDocument> allDocuments = new();
    static public SerializableDocument? GetDocument(string id)
    {
        if(allDocuments.TryGetValue(id, out var document)) return document;
        return null;
    }

    public static IEnumerator CoLoad()
    {
        Patches.LoadPatch.LoadingText = "Loading Serializable Documents";
        yield return null;

        string Postfix = ".json";
        int PostfixLength = Postfix.Length;

        foreach (var addon in NebulaAddon.AllAddons)
        {
            string Prefix = addon.InZipPath + "Documents/";
            int PrefixLength = Prefix.Length;

            foreach (var entry in addon.Archive.Entries)
            {
                if (!entry.FullName.StartsWith(Prefix) || !entry.FullName.EndsWith(Postfix)) continue;

                var id = entry.FullName.Substring(PrefixLength, entry.FullName.Length - PrefixLength - PostfixLength).Replace('/', '.');

                using var stream = entry.Open();
                if (stream == null) continue;

                var doc = JsonStructure.Deserialize<SerializableDocument>(stream);
                if (doc == null) continue;

                allDocuments[id] = doc;
            }

            yield return null;
        }
    }
}

[NebulaPreLoad]
public class SerializableDocument
{
    public class SerializableColor
    {
        [JsonSerializableField]
        public byte R = 255;
        [JsonSerializableField]
        public byte G = 255;
        [JsonSerializableField]
        public byte B = 255;
        [JsonSerializableField]
        public byte A = 255;
        [JsonSerializableField]
        public string? Style = null;
        public Color AsColor => GetColor(Style) ?? new Color((float)R / 255f, (float)G / 255f, (float)B / 255f, (float)A / 255f);
    }

    private static Dictionary<string, TextAttribute> TextStyle = new();
    private static Dictionary<string, Color> ColorStyle = new();

    public static void RegisterColor(string style, Color color) => ColorStyle[style] = color;
    private static Color? GetColor(string? style)
    {
        if (style == null) return null;
        if (ColorStyle.TryGetValue(style, out var col)) return col;
        return null;
    }

    public static void Load()
    {
        TextStyle["Standard"] = new TextAttribute(TextAttribute.NormalAttr).EditFontSize(1.2f, 0.6f, 1.2f);
        TextStyle["Bold"] = new TextAttribute(TextAttribute.BoldAttr).EditFontSize(1.2f, 0.6f, 1.2f);
        TextStyle["Content"] = new TextAttribute(TextAttribute.ContentAttr).EditFontSize(1.2f, 0.6f, 1.2f);
        TextStyle["Title"] = new TextAttribute(TextAttribute.TitleAttr).EditFontSize(2.2f, 0.6f, 2.2f);
    }

    //子となるコンテンツ
    [JsonSerializableField]
    public List<SerializableDocument>? Contents = null;

    //横並びのコンテンツ
    [JsonSerializableField]
    public List<SerializableDocument>? Aligned = null;

    //テンプレートスタイルID
    public string? Style = null;

    //テキストの生文字列
    [JsonSerializableField]
    public string? RawText;

    //テキストの翻訳キー
    [JsonSerializableField]
    public string? TranslationKey;

    //太字
    [JsonSerializableField]
    public bool IsBold = false;

    //テキストカラー
    [JsonSerializableField]
    public SerializableColor? Color = null;

    //フォントサイズ
    [JsonSerializableField]
    public float? FontSize = null;

    //可変テキスト
    [JsonSerializableField]
    public bool IsVariable = false;

    //画像パス
    [JsonSerializableField]
    public string? Image = null;

    //横幅
    [JsonSerializableField]
    public float? Width = null;

    //縦方向余白
    [JsonSerializableField]
    public float? VSpace = null;

    //横方向余白
    [JsonSerializableField]
    public float? HSpace = null;

    private ISpriteLoader? imageLoader = null;

    public IMetaContext? Build(MetaScreen? myScreen)
    {
        if (Contents != null)
        {
            MetaContext context = new();
            foreach(var c in Contents)
            {
                var subContext = c.Build(myScreen);
                if (subContext == null) continue;

                context.Append(subContext);
            }
            return context;
        }

        if(Aligned != null)
        {
            List<IMetaParallelPlacable> list = new();
            foreach (var c in Aligned)
            {
                var tem = c.Build(myScreen);
                if(!(tem is IMetaParallelPlacable mpp))
                {
                    NebulaPlugin.Log.Print(NebulaLog.LogCategory.Document,"Document contains an unalignable content.");
                    continue;
                }
                list.Add(mpp);
            }
            return new CombinedContext(list.ToArray()) { Alignment = IMetaContext.AlignmentOption.Left };
        }

        if(TranslationKey != null || RawText != null)
        {
            string text = TranslationKey != null ? Language.Translate(TranslationKey!) : RawText!;

            TextAttribute? attr = null;
            if(Style == null || !TextStyle.TryGetValue(Style, out attr)) attr = IsVariable ? TextStyle["Content"] : TextStyle["Standard"];

            float fontSize = FontSize.HasValue ? FontSize.Value : attr.FontSize;
            attr = new(attr) {
                FontSize = fontSize,
                FontMinSize = Mathf.Min(fontSize, attr.FontMinSize),
                FontMaxSize = Mathf.Max(fontSize, attr.FontMaxSize),
                Color = Color?.AsColor ?? UnityEngine.Color.white,
                Styles = IsBold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal,
                Alignment = TMPro.TextAlignmentOptions.Left
            };

            void PostBuilder(TMPro.TextMeshPro text) {

                foreach(var linkInfo in text.textInfo.linkInfo)
                {
                    int begin = linkInfo.linkTextfirstCharacterIndex;
                    for (int i = 0; i < linkInfo.linkTextLength; i++)
                    {
                        int index = begin + i;
                        text.textInfo.characterInfo[i].color = new Color32(116, 132, 169, 255);
                    }
                }

                //スタイルシートが使える
                /*
                var styleSheet = new TMP_StyleSheet();
                styleSheet.styles.Add(new TMP_Style(,))
                */

                var collider = UnityHelper.CreateObject<BoxCollider2D>("TextCollider", text.transform.parent, text.transform.localPosition);
                collider.size = text.rectTransform.sizeDelta;
                var button = collider.gameObject.SetUpButton();
                button.OnClick.AddListener(() => {
                    var cam = UnityHelper.FindCamera(LayerExpansion.GetUILayer());
                    if (cam == null) return;
                    
                    int linkIdx = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition,cam);
                    if (linkIdx == -1) return;
                    
                    var action = text.textInfo.linkInfo[linkIdx].GetLinkID();
                    var args = action.Split(':', 2);
                    if (args.Length != 2) return;

                    switch (args[0])
                    {
                        case "to":
                            myScreen?.SetContext(DocumentManager.GetDocument(args[1])?.Build(myScreen) ?? null);
                            break;
                        default:
                            NebulaPlugin.Log.Print(NebulaLog.LogCategory.Document, $"Unknown link action \"{args[0]}\" is triggered.");
                            break;
                    }
                });
                
            }

            if (IsVariable)
            {
                return new MetaContext.VariableText(attr) { RawText = text, Alignment = IMetaContext.AlignmentOption.Left, PostBuilder = PostBuilder };
            }
            else
            {
                return new MetaContext.Text(attr) { RawText = text, Alignment = IMetaContext.AlignmentOption.Left, PostBuilder = PostBuilder };
            }
        }

        if(Image != null)
        {
            if(imageLoader == null)
            {
                imageLoader = SpriteLoader.FromResource("Nebula.Resources." + Image + ".png", 100f);
            }

            return new MetaContext.Image(imageLoader.GetSprite()) { Width = Width ?? 1f };
        }

        if (HSpace != null) return new MetaContext.HorizonalMargin(HSpace.Value);
        if (VSpace != null) return new MetaContext.VerticalMargin(VSpace.Value);

        //無効なコンテンツ
        return null;
    }
}
