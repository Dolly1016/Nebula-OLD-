using Il2CppSystem.Text.Json;
using Nebula.Behaviour;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Nebula.Modules;

public interface IDocumentVariable
{
    public class EnumeratorView<T> where T : class
    {
        IEnumerator<T> myEnumerator;
        T? storedVal;
        EnumeratorView<T>? prev, next;
        bool isValid;

        public EnumeratorView(IEnumerator<T> myEnumerator) : this(myEnumerator, null){}

        private EnumeratorView(IEnumerator<T> myEnumerator, EnumeratorView<T>? prev)
        {
            this.myEnumerator = myEnumerator;
            this.prev = prev;
            isValid = myEnumerator.MoveNext();
            storedVal = isValid ? myEnumerator.Current : null;
        }

        public EnumeratorView<T>? GetPrev() => prev;
        public EnumeratorView<T>? GetNext()
        {
            next ??= new(myEnumerator, this);
            return next;
        }

        public T? Get() => storedVal;
        public bool IsValid => isValid;
    }

    public string? ToString() => AsString();
    public string AsString();
    public bool AsBool() => false;
    public int AsInteger() => 0;
    public EnumeratorView<string>? AsStringEnumerator() => null;
    public IEnumerable<string>? AsStringEnumerable() => null;
    public bool Equals(IDocumentVariable variable) => AsString().Equals(variable.AsString());

    public class DocumentVariableString : IDocumentVariable
    {
        private string val;
        public DocumentVariableString(string val) { this.val = val; }
        public string AsString() => val;
        public int AsInteger() => int.TryParse(val, out int num) ? num : 0;
    }

    public class DocumentVariableInteger : IDocumentVariable
    {
        private int val;
        public DocumentVariableInteger(int val) { this.val = val; }
        public string AsString() => val.ToString();
        public int AsInteger() => val;
    }

    public class DocumentVariableBool : IDocumentVariable
    {
        private bool val;
        public DocumentVariableBool(bool val) { this.val = val; }
        public string AsString() => val.ToString();
        public bool AsBool() => val;
    }

    public class DocumentVariableStringEnumerator : IDocumentVariable
    {
        private EnumeratorView<string> val;
        public DocumentVariableStringEnumerator(EnumeratorView<string> val) { this.val = val; }
        public string AsString() => "Enumerator";
        public EnumeratorView<string>? AsStringEnumerator() => val;
    }

    public class DocumentVariableStringEnumerable : IDocumentVariable
    {
        private IEnumerable<string> val;
        public DocumentVariableStringEnumerable(IEnumerable<string> val) { this.val = val; }
        public string AsString() => "Enumerator";
        public IEnumerable<string>? AsStringEnumerable() => val;
    }

    public static IDocumentVariable Generate(string val) => new DocumentVariableString(val);
    public static IDocumentVariable Generate(int val) => new DocumentVariableInteger(val);
    public static IDocumentVariable Generate(bool val) => new DocumentVariableBool(val);
    public static IDocumentVariable Generate(EnumeratorView<string> val) => new DocumentVariableStringEnumerator(val);
    public static IDocumentVariable Generate(IEnumerable<string> val)=>new DocumentVariableStringEnumerable(val);
}
public class DocumentFunction
{
    public Predicate<int> RequiredArguments { get; private set; }
    private Func<IDocumentVariable[], IDocumentVariable> myfunc;
    private DocumentFunction? overloadedLink = null;

    public DocumentFunction(int requiredArguments, Func<IDocumentVariable[], IDocumentVariable> function)
    {
        myfunc = function;
        RequiredArguments = (num) => num == requiredArguments;
    }

    public DocumentFunction(Predicate<int> requiredArguments, Func<IDocumentVariable[], IDocumentVariable> function)
    {
        myfunc = function;
        RequiredArguments = requiredArguments;
    }

    public bool Execute(IDocumentVariable[] arguments,out IDocumentVariable? result)
    {
        result = null;

        if(RequiredArguments.Invoke(arguments.Length))
        {
            result = myfunc.Invoke(arguments);
            return true;
        }

        return overloadedLink?.Execute(arguments, out result) ?? false;
    }

    public void Overload(DocumentFunction function)
    {
        if (overloadedLink != null)
            overloadedLink.Overload(function);
        else
            overloadedLink = function;
    }
}

[NebulaPreLoad(typeof(SerializableDocument),typeof(NebulaAddon))]
public class DocumentManager
{
    private static Dictionary<string, SerializableDocument> allDocuments = new();
    private static Dictionary<string, DocumentFunction> allFunctions = new();
    static public SerializableDocument? GetDocument(string id)
    {
        if(allDocuments.TryGetValue(id, out var document)) return document;
        return null;
    }

    static public void LoadFunction(string name,DocumentFunction function)
    {
        if(allFunctions.TryGetValue(name,out var val))
            val.Overload(function);
        else
            allFunctions[name] = function;
    }

    static public IDocumentVariable CallFunction(string name, IDocumentVariable[] argument)
    {
        if (allFunctions.TryGetValue(name, out var func))
            return (func.Execute(argument, out var result) ? result : IDocumentVariable.Generate($"BADARGS({name})")) ?? IDocumentVariable.Generate($"BADFUNC({name})");
        return IDocumentVariable.Generate($"BADCALL({name})");
    }

    public static IEnumerator CoLoad()
    {
        Patches.LoadPatch.LoadingText = "Loading Serializable Documents";
        yield return null;

        LoadFunction("Concat", new(n => n >= 2, (args) => {
            StringBuilder builder = new();
            foreach (var arg in args) builder.Append(arg.AsString());
            return IDocumentVariable.Generate(builder.ToString());
        }));
        LoadFunction("ToRoleName", new(1, (args) => IDocumentVariable.Generate("role." + args[0].AsString())));
        LoadFunction("IfConfig", new(3, (args) => {
            try
            {
                if (NebulaConfiguration.AllConfigurations.FirstOrDefault(option => option.Id == args[0].AsString())?.GetBool() ?? false)
                    return args[1];
                else
                    return args[2];
            }
            catch {
                return IDocumentVariable.Generate($"BADCONF({args[0]})");
            }
        }));
        LoadFunction("SwitchConfig", new(n => n >= 2, (args) =>
        {
            try
            {
                int index = NebulaConfiguration.AllConfigurations.FirstOrDefault(option => option.Id == args[0].AsString())?.CurrentValue ?? 0;
                return args[index + 1];
            }
            catch
            {
                return IDocumentVariable.Generate($"BADCONF({args[0]})");
            }
        }));
        LoadFunction("Translate", new(1, (args) => IDocumentVariable.Generate(Language.Translate(args[0].AsString()))));
        LoadFunction("ConfigVal", new(1, (args) => IDocumentVariable.Generate(NebulaConfiguration.AllConfigurations.FirstOrDefault(option => option.Id == args[0].AsString())?.ToDisplayString() ?? $"BADCONF({args[0]})")));
        LoadFunction("ConfigBool", new(1, (args) => IDocumentVariable.Generate(NebulaConfiguration.AllConfigurations.FirstOrDefault(option => option.Id == args[0].AsString())?.GetBool() ?? false)));
        LoadFunction("ConfigRaw", new(1, (args) => IDocumentVariable.Generate(NebulaConfiguration.AllConfigurations.FirstOrDefault(option => option.Id == args[0].AsString())?.CurrentValue ?? 0)));
        LoadFunction("Replace", new(3, (args) => IDocumentVariable.Generate(args[0].AsString().Replace(args[1].AsString(), args[2].AsString()))));
        LoadFunction("Property", new(1, (args)=> IDocumentVariable.Generate(PropertyManager.GetProperty(args[0].AsString())?.GetString() ?? $"BADPROP({args[0]})")));

        //整数演算

        //オーバーライド
        LoadFunction("Equal", new(2, (args) =>
        {
            bool equal = false;
            if (args[0] is IDocumentVariable.DocumentVariableInteger)
            {
                equal = args[0].AsInteger() == args[1].AsInteger();
            }
            return IDocumentVariable.Generate(equal);
        }));

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

                foreach(var c in doc.AllConents())c.RelatedNamespace = addon;

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
        foreach (var option in NebulaConfiguration.AllConfigurations) yield return option.Id + ".detail";
    }
}

public class ArgumentTable
{
    public Dictionary<string, IDocumentVariable> Arguments;

    public ArgumentTable(Dictionary<string, string>? arguments,ArgumentTable? baseTable)
    {
        Arguments = new();
        if (arguments == null) return;
        foreach (var entry in arguments) {
            Arguments[baseTable.GetValueOrRaw(entry.Key).AsString()] = baseTable.GetValueOrRaw(entry.Value);
        }
    }
}

static public class ArgumentTableHelper
{
    public static string GetString(this ArgumentTable? table, string rawString) => GetValueOrRaw(table, rawString).AsString();

    public static IDocumentVariable GetValueOrRaw(this ArgumentTable? table, string rawString)
    {
        if (!rawString.StartsWith("#")) return IDocumentVariable.Generate(rawString);

        string inStr = rawString.Substring(1);
        return GetValue(table, inStr);
    }

    public static IDocumentVariable GetValue(this ArgumentTable? table, string programStr)
    {
        try
        {
            return GetValueInternal(table, programStr, out _);
        }
        catch
        {
            return IDocumentVariable.Generate($"BADTEXTCODE({programStr})");
        }
    }

    private static IDocumentVariable GetValueInternal(ArgumentTable? table, string innerString, out int progress)
    {
        //生文字列
        string tokenString = innerString.TrimStart();
        if (tokenString.StartsWith('\''))
        {
            string[] splitted = tokenString.Split('\'', 3);
            if (splitted.Length == 3)
            {
                progress = (innerString.Length - tokenString.Length) + 2 + splitted[1].Length;
                return IDocumentVariable.Generate(splitted[1]);
            }
        }

        int diff = innerString.Length - tokenString.Length;

        //引数・関数と一致する場合
        int lastIndex = 0;
        while (tokenString.Length > lastIndex && TextField.IdPredicate.Invoke(tokenString[lastIndex])) lastIndex++;

        //関数
        if(lastIndex < tokenString.Length && tokenString[lastIndex] is '(')
        {
            string funcName = tokenString.Substring(0, lastIndex);

            string argStr = tokenString.Substring(lastIndex + 1);
            progress = lastIndex + 1 + diff;
            List<IDocumentVariable> args = new();
            while (true)
            {
                
                args.Add(GetValueInternal(table, argStr, out int p));
                if (p == -1)
                {
                    progress = -1;
                    return IDocumentVariable.Generate($"BADFORMAT({tokenString}) at ({argStr})");
                }

                argStr = argStr.Substring(p);
                progress += p;

                var temp = argStr.TrimStart();
                progress += argStr.Length - temp.Length;
                argStr = temp;

                if (argStr.Length == 0) return IDocumentVariable.Generate($"BADFORMAT({tokenString})");


                if (argStr[0] is ',')
                {
                    progress++;
                    argStr = argStr.Substring(1);
                }
                else if (argStr[0] is ')')
                {
                    progress++;
                    return DocumentManager.CallFunction(funcName, args.ToArray());
                }

            }
        }

        //引数
        string valString = lastIndex == 0 ? "" : tokenString.Substring(0, lastIndex);
        progress = lastIndex + diff;
        if (table != null && table!.Arguments.TryGetValue(valString.Trim(), out var val))
            return val;
        else
            return IDocumentVariable.Generate($"UNKNOWN({valString.Trim()})");
    }
}

[NebulaPreLoad]
public class SerializableDocument
{
    public class DocumentReference
    {
        [JsonSerializableField]
        public string Id = null!;
        [JsonSerializableField]
        public Dictionary<string,string>? Arguments;
    }

    public class SerializableColor
    {
        [JsonSerializableField(true)]
        public byte? R;
        [JsonSerializableField(true)]
        public byte? G;
        [JsonSerializableField(true)]
        public byte? B;
        [JsonSerializableField(true)]
        public byte? A;
        [JsonSerializableField(true)]
        public string? Style = null;
        public Color AsColor(ArgumentTable? table) => GetColor(Style != null ? table.GetString(Style) : null) ?? new Color((float)(R ?? 255) / 255f, (float)(G ?? 255) / 255f, (float)(B ?? 255) / 255f, (float)(A ?? 255) / 255f);
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

    public IEnumerable<SerializableDocument> AllConents()
    {
        yield return this;
        if (Contents != null) foreach (var doc in Contents) foreach (var c in doc.AllConents()) yield return c;
        if (Aligned != null) foreach (var doc in Aligned) foreach (var c in doc.AllConents()) yield return c;
    }

    //使用している引数(任意)
    [JsonSerializableField(true)]
    public List<string>? Arguments = null;

    //子となるコンテンツ
    [JsonSerializableField(true)]
    public List<SerializableDocument>? Contents = null;

    //横並びのコンテンツ
    [JsonSerializableField(true)]
    public List<SerializableDocument>? Aligned = null;

    //表示条件
    [JsonSerializableField(true)]
    public string? Predicate = null;

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

    //ドキュメント参照
    [JsonSerializableField(true)]
    public DocumentReference? Document = null;

    public INameSpace? RelatedNamespace = null;

    private ISpriteLoader? imageLoader = null;
    private string? lastImagePath;

    public List<SerializableDocument>? MyContainer => Contents ?? Aligned;
    public void ReplaceContent(SerializableDocument content, bool moveToHead)
    {
        List<SerializableDocument>? list = MyContainer;
        if (list == null) return;

        int index = list.IndexOf(content);
        if (index == -1) return;

        index += moveToHead ? -1 : 1;

        if (0 <= index && index < list.Count)
        {
            list.Remove(content);
            list.Insert(index, content);
        }
    }

    public void RemoveContent(SerializableDocument content)
    {
        MyContainer?.Remove(content);
    }

    public void AppendContent(SerializableDocument content)
    {
        MyContainer?.Add(content);
    }

    private const int MaxNesting = 32;

    public IMetaContext? BuildForDev(Action<PassiveButton,SerializableDocument, SerializableDocument?> editorBuilder, SerializableDocument? parent = null, INameSpace? nameSpace = null)
    {
        var context = BuildInternal(nameSpace ?? RelatedNamespace, null, null, c => c.BuildForDev(editorBuilder, this, nameSpace ?? RelatedNamespace), false, true, MaxNesting);

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

    public IMetaContext? Build(Reference<MetaContext.ScrollView.InnerScreen>? myScreen, bool useMaskedMaterial = true, int leftNesting = MaxNesting, INameSpace? nameSpace = null) => BuildInternal(nameSpace ?? RelatedNamespace, null, myScreen, c => c.Build(myScreen, useMaskedMaterial, leftNesting, nameSpace ?? RelatedNamespace), true, useMaskedMaterial, leftNesting);
    public IMetaContext? BuildReference(ArgumentTable? table, INameSpace? nameSpace, Reference<MetaContext.ScrollView.InnerScreen>? myScreen, int leftNesting = MaxNesting) => BuildInternal(nameSpace, table, myScreen, c => c.BuildReference(table, c.RelatedNamespace, myScreen, leftNesting), false, true, leftNesting);


    public IMetaContext? BuildInternal(INameSpace? nameSpace, ArgumentTable? arguments, Reference<MetaContext.ScrollView.InnerScreen>? myScreen, Func<SerializableDocument, IMetaContext?> builder, bool buildHyperLink,bool useMaskedMaterial, int leftNesting)
    {
        if(Predicate != null && Predicate.Length > 0)
        {
            if (!(arguments?.GetValue(Predicate[0] is '#' ? Predicate.Substring(1) : Predicate).AsBool() ?? true))
                return new MetaContext();
        }

        string ConsiderArgumentAsStr(string str) => arguments.GetString(str);

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
            string text = TranslationKey != null ? Language.Translate(ConsiderArgumentAsStr(TranslationKey!)) : ConsiderArgumentAsStr(RawText!);

            TextAttribute? attr = null;
            if(Style == null || !TextStyle.TryGetValue(ConsiderArgumentAsStr(Style), out attr)) attr = (IsVariable ?? false) ? TextStyle["Content"] : TextStyle["Standard"];

            float fontSize = FontSize.HasValue ? FontSize.Value : attr.FontSize;
            attr = new(attr) {
                FontSize = fontSize,
                FontMinSize = Mathf.Min(fontSize, attr.FontMinSize),
                FontMaxSize = Mathf.Max(fontSize, attr.FontMaxSize),
                Color = Color?.AsColor(arguments) ?? UnityEngine.Color.white,
                Styles = IsBold.HasValue ? (IsBold.Value ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal) : attr.Styles,
                Alignment = TMPro.TextAlignmentOptions.Left,
                FontMaterial = useMaskedMaterial ? VanillaAsset.StandardMaskedFontMaterial : null
               
            };

            void PostBuilder(TMPro.TextMeshPro text) {
                if (myScreen != null && buildHyperLink)
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
            string image = ConsiderArgumentAsStr(Image);
            if (imageLoader == null || image != lastImagePath)
            {
                if (image.Contains("::"))
                {
                    var splitted = image.Split("::", 2);
                    imageLoader = new SpriteLoader(new AddressedTextureLoader(NameSpaceManager.ResolveOrGetDefault(splitted[0]), splitted[1]+".png"), 100f);
                }
                else
                {
                    imageLoader = new SpriteLoader(new AddressedTextureLoader(nameSpace ?? NameSpaceManager.DefaultNameSpace, image + ".png"), 100f);
                }
                lastImagePath = image;
            }

            Sprite sprite = null!;
            try
            {
                sprite = imageLoader?.GetSprite()!;
            }
            catch { }
            if (sprite)
                return new MetaContext.Image(sprite) { Width = Width ?? 1f, PostBuilder = image => image.maskInteraction = useMaskedMaterial ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None };
            else
                return new MetaContext.VariableText(new TextAttribute(TextAttribute.BoldAttrLeft) { FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(1.4f)) { RawText = lastImagePath.Color(UnityEngine.Color.gray), Alignment = IMetaContext.AlignmentOption.Left };
        }

        if (HSpace != null) return new MetaContext.HorizonalMargin(HSpace.Value);
        if (VSpace != null) return new MetaContext.VerticalMargin(VSpace.Value);

        if(Document != null)
        {
            if (leftNesting == 0)
            {
                return new MetaContext.VariableText(new TextAttribute(TextAttribute.BoldAttrLeft) { FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(1f)) { MyText = ITextComponent.From("ui.document.tooLongNesting", UnityEngine.Color.red), Alignment = IMetaContext.AlignmentOption.Left };
            }
            else
            {
                SerializableDocument? doc = null;

                if (nameSpace is DevAddon addon)
                {
                    string path = "Documents/" + Document.Id + ".json";
                    var stream = nameSpace?.OpenRead(path);
                    if (stream != null) {
                        doc = JsonStructure.Deserialize<SerializableDocument>(new StreamReader(stream).ReadToEnd());
                    }
                }
                doc ??= DocumentManager.GetDocument(ConsiderArgumentAsStr(Document.Id));
                return doc?.BuildReference(new ArgumentTable(Document.Arguments, arguments), nameSpace, myScreen, leftNesting - 1) ?? new MetaContext();
            }
        }
        //無効なコンテンツ
        return null;
    }
}
