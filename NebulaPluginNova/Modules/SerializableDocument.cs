using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

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

    //ゲーム内で使用しているID
    public static IEnumerable<string> GetAllUsingId()
    {
        foreach (var role in Roles.Roles.AllRoles) yield return "role." + role.InternalName;
        foreach (var modifier in Roles.Roles.AllModifiers) yield return "role." + modifier.InternalName;
    }
}

[NebulaPreLoad]
public class SerializableDocument
{
    public class SerializableColor
    {
        [JsonSerializableField]
        public byte? R;
        [JsonSerializableField]
        public byte? G;
        [JsonSerializableField]
        public byte? B;
        [JsonSerializableField]
        public byte? A;
        [JsonSerializableField(true)]
        public string? Style = null;
        public Color AsColor => GetColor(Style) ?? new Color((float)(R ?? 255) / 255f, (float)(G ?? 255) / 255f, (float)(B ?? 255) / 255f, (float)(A ?? 255) / 255f);
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
    [JsonSerializableField(true)]
    public List<SerializableDocument>? Contents = null;

    //横並びのコンテンツ
    [JsonSerializableField(true)]
    public List<SerializableDocument>? Aligned = null;

    //テンプレートスタイルID
    [JsonSerializableField(true)]
    public string? Style = null;

    //テキストの生文字列
    [JsonSerializableField(true)]
    public string? RawText;

    //テキストの翻訳キー
    [JsonSerializableField(true)]
    public string? TranslationKey;

    //太字
    [JsonSerializableField(true)]
    public bool? IsBold = null;

    //テキストカラー
    [JsonSerializableField(true)]
    public SerializableColor? Color = null;

    //フォントサイズ
    [JsonSerializableField(true)]
    public float? FontSize = null;

    //可変テキスト
    [JsonSerializableField(true)]
    public bool? IsVariable = null;

    //画像パス
    [JsonSerializableField(true)]
    public string? Image = null;

    //横幅
    [JsonSerializableField(true)]
    public float? Width = null;

    //縦方向余白
    [JsonSerializableField(true)]
    public float? VSpace = null;

    //横方向余白
    [JsonSerializableField(true)]
    public float? HSpace = null;

    private ISpriteLoader? imageLoader = null;
    private string? lastImagePath;

    public List<SerializableDocument>? MyContainer => Contents ?? Aligned;
    public void ReplaceContent(SerializableDocument content, bool moveToHead)
    {
        List<SerializableDocument>? list = MyContainer;
        if (list == null) return;

        int index = list.IndexOf(content);
        if (index == -1) return;

        list.RemoveAt(index);
        index += moveToHead ? -1 : 1;

        if (0 <= index && index <= list.Count) list.Insert(index, content);
    }

    public void RemoveContent(SerializableDocument content)
    {
        MyContainer?.Remove(content);
    }

    public void AppendContent(SerializableDocument content)
    {
        MyContainer?.Add(content);
    }

    public IMetaContext? BuildForDev(Action<PassiveButton,SerializableDocument, SerializableDocument?> editorBuilder, SerializableDocument? parent = null)
    {
        var context = BuildInternal(null, c => c.BuildForDev(editorBuilder, this), false);

        if (context != null) context = new MetaContext.FramedContext(context, new Vector2(0.15f, 0.15f)) { 
            HighlightColor = UnityEngine.Color.cyan.AlphaMultiplied(0.25f), 
            PostBuilder = renderer =>
            {
                renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                var button = renderer.gameObject.SetUpButton(true, renderer, UnityEngine.Color.white.AlphaMultiplied(0.15f), UnityEngine.Color.Lerp(UnityEngine.Color.cyan, UnityEngine.Color.green, 0.4f).AlphaMultiplied(0.3f));
                var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
                collider.size = renderer.size;
                editorBuilder.Invoke(button,this,parent);
            } };

        return context;
    }

    public IMetaContext? Build(Reference<MetaContext.ScrollView.InnerScreen> myScreen) => BuildInternal(myScreen, c => c.Build(myScreen),true);
    

    public IMetaContext? BuildInternal(Reference<MetaContext.ScrollView.InnerScreen>? myScreen, Func<SerializableDocument, IMetaContext?> builder, bool buildHyperLink)
    {
        if (Contents != null)
        {
            MetaContext context = new();
            foreach(var c in Contents)
            {
                var subContext = builder.Invoke(c);
                if (subContext != null) context.Append(subContext);
            }
            return context;
        }

        if(Aligned != null)
        {
            List<IMetaParallelPlacable> list = new();
            foreach (var c in Aligned)
            {
                var tem = builder.Invoke(c);
                if (!(tem is IMetaParallelPlacable mpp))
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
            if(Style == null || !TextStyle.TryGetValue(Style, out attr)) attr = (IsVariable ?? false) ? TextStyle["Content"] : TextStyle["Standard"];

            float fontSize = FontSize.HasValue ? FontSize.Value : attr.FontSize;
            attr = new(attr) {
                FontSize = fontSize,
                FontMinSize = Mathf.Min(fontSize, attr.FontMinSize),
                FontMaxSize = Mathf.Max(fontSize, attr.FontMaxSize),
                Color = Color?.AsColor ?? UnityEngine.Color.white,
                Styles = IsBold.HasValue ? (IsBold.Value ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal) : attr.Styles,
                Alignment = TMPro.TextAlignmentOptions.Left,
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial
               
            };

            void PostBuilder(TMPro.TextMeshPro text) {
                if (buildHyperLink)
                {
                    foreach (var linkInfo in text.textInfo.linkInfo)
                    {
                        int begin = linkInfo.linkTextfirstCharacterIndex;
                        for (int i = 0; i < linkInfo.linkTextLength; i++)
                        {
                            int index = begin + i;
                            text.textInfo.characterInfo[i].color = new Color32(116, 132, 169, 255);
                        }
                    }

                    var collider = UnityHelper.CreateObject<BoxCollider2D>("TextCollider", text.transform.parent, text.transform.localPosition);
                    collider.size = text.rectTransform.sizeDelta;
                    var button = collider.gameObject.SetUpButton();
                    button.OnClick.AddListener(() =>
                    {
                        var cam = UnityHelper.FindCamera(LayerExpansion.GetUILayer());
                        if (cam == null) return;

                        int linkIdx = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, cam);
                        if (linkIdx == -1) return;

                        var action = text.textInfo.linkInfo[linkIdx].GetLinkID();
                        var args = action.Split(':', 2);
                        if (args.Length != 2) return;

                        switch (args[0])
                        {
                            case "to":
                                myScreen?.Value?.SetContext(DocumentManager.GetDocument(args[1])?.Build(myScreen) ?? null);
                                break;
                            default:
                                NebulaPlugin.Log.Print(NebulaLog.LogCategory.Document, $"Unknown link action \"{args[0]}\" is triggered.");
                                break;
                        }
                    });
                }
            }

            if (IsVariable ?? false)
            {
                return new MetaContext.VariableText(attr) { RawText = text, Alignment = IMetaContext.AlignmentOption.Left, PostBuilder =  PostBuilder };
            }
            else
            {
                return new MetaContext.Text(attr) { RawText = text, Alignment = IMetaContext.AlignmentOption.Left, PostBuilder = PostBuilder };
            }
        }

        if(Image != null)
        {
            if(imageLoader == null || Image != lastImagePath)
            {
                imageLoader = SpriteLoader.FromResource("Nebula.Resources." + Image + ".png", 100f);
                lastImagePath = Image;
            }

            Sprite sprite = null!;
            try
            {
                sprite = imageLoader?.GetSprite()!;
            }
            catch { }
            return new MetaContext.Image(sprite) { Width = Width ?? 1f, PostBuilder = image => image.maskInteraction = SpriteMaskInteraction.VisibleInsideMask };
        }

        if (HSpace != null) return new MetaContext.HorizonalMargin(HSpace.Value);
        if (VSpace != null) return new MetaContext.VerticalMargin(VSpace.Value);

        //無効なコンテンツ
        return null;
    }
}
