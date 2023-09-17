using Il2CppInterop.Runtime.Injection;
using Nebula.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using static Nebula.Modules.IMetaContext;

namespace Nebula.Modules;

public interface IMetaContext
{
    public enum AlignmentOption
    {
        Left,
        Center,
        Right
    }
    public AlignmentOption Alignment { get; }
    public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width);
    public float Generate(GameObject screen, Vector2 cursor, Vector2 size) => Generate(screen, cursor, size, out var _);

    protected static (float,float) CalcWidth(AlignmentOption alignment, Vector2 cursor, Vector2 size,float width,float actualMin,float actualMax)
    {
        float center = 0;
        switch (alignment)
        {
            case AlignmentOption.Left:
                center = cursor.x + width / 2;
                break;
            case AlignmentOption.Right:
                center = cursor.x + size.x - width / 2;
                break;
            case AlignmentOption.Center:
            default:
                center = cursor.x + size.x / 2;
                break;
        }
        return new(center + actualMin, center + actualMax);
    }

    protected static Vector3 ReflectAlignment(AlignmentOption alignment,Vector2 mySize,Vector2 cursor,Vector2 size)
    {
        switch (alignment)
        {
            case AlignmentOption.Left:
                return new Vector3(cursor.x + mySize.x / 2, cursor.y - mySize.y / 2);
            case AlignmentOption.Right:
                return new Vector3(cursor.x + size.x - mySize.x / 2, cursor.y - mySize.y / 2);
            case AlignmentOption.Center:
            default:
                return new Vector3(cursor.x + size.x / 2, cursor.y - mySize.y / 2);
        }
    }
}

public interface IMetaParallelPlacable
{
    public void Generate(GameObject screen, Vector2 center);
    public float Width { get; }

}

public class MetaContext : IMetaContext
{
    List<IMetaContext> contents = new();

    public AlignmentOption Alignment => AlignmentOption.Center;
    public float Generate(GameObject screen, Vector2 cursor, Vector2 size,out (float,float) width)
    {
        float widthMin = size.x / 2;
        float widthMax = cursor.x;

        float heightSum = 0;
        foreach (var c in contents)
        {
            float height = c.Generate(screen, cursor, size,out var innerWidth);
            widthMin = Math.Min(widthMin, innerWidth.min);
            widthMax = Math.Max(widthMax, innerWidth.max);
            heightSum += height;
            cursor.y -= height;
            size.y -= height;
        }

        width = new(widthMin,widthMax);
        return heightSum;
    }

    public MetaContext Append(IMetaContext content)
    {
        contents.Add(content);
        return this;
    }

    //linesを-1にすると全部を並べる
    public MetaContext Append<T>(IEnumerable<T> enumerable,Func<T, IMetaParallelPlacable> converter,int perLine,int lines,int page,float height,bool fixedHeight = false)
    {
        int skip = lines > 0 ? page * lines : 0;
        int leftLines = lines;
        int index = 0;
        IMetaParallelPlacable[] contextList = new IMetaParallelPlacable[perLine];

        foreach(var content in enumerable)
        {
            if (skip > 0)
            {
                skip--;
                continue;
            }

            contextList[index] = converter.Invoke(content);
            index++;

            if(index == perLine)
            {
                Append(new CombinedContext(height, contextList));
                index = 0;
                leftLines--;

                if (leftLines == 0) break;
            }
        }

        if (index != 0)
        {
            for (; index < perLine; index++) contextList[index] = new HorizonalMargin(0f);
            Append(new CombinedContext(height, contextList));
        }

        if (fixedHeight && leftLines > 0) for (int i = 0; i < leftLines; i++) Append(new VerticalMargin(height));

        return this;
    }

    public MetaContext[] Split(params float[] ratios)
    {
        var contents = ratios.Select(ratio => new Tuple<IMetaContext,float>(new MetaContext(),ratio)).ToArray();
        Append(new ParallelContext(contents));
        return contents.Select(c => (MetaContext)c.Item1).ToArray();
    }

    public class Image : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public float Width { get; set; }
        public Sprite? Sprite { get; set; }
        public Action<SpriteRenderer>? PostBuilder { get; set; }

        public Image(Sprite? sprite)
        {
            Sprite = sprite;
        }

        public void Generate(GameObject screen, Vector2 center)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("Image", screen.transform,center);
            renderer.sprite = Sprite;
            if (Sprite) renderer.transform.localScale = Vector3.one * (Width / Sprite!.bounds.size.x);
            PostBuilder?.Invoke(renderer);
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            Vector2 mySize = Sprite ? Sprite!.bounds.size * (Width / Sprite!.bounds.size.x) : new(Width, Width);

            if (Sprite)
            {
                var renderer = UnityHelper.CreateObject<SpriteRenderer>("Image", screen.transform, ReflectAlignment(Alignment, mySize + new Vector2(0.1f, 0.1f), cursor, size));
                renderer.sprite = Sprite;
                renderer.transform.localScale = Vector3.one * (Width / Sprite!.bounds.size.x);
                PostBuilder?.Invoke(renderer);
            }

            width = CalcWidth(Alignment, cursor, size, mySize.x, -mySize.x / 2f, mySize.x / 2f);

            return mySize.y + 0.1f;
        }
    }

    public class Text : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public float Width { get => TextAttribute.Size.x; }
        public TextAttribute TextAttribute { get; set; }
        
        public ITextComponent? MyText { get; set; } = null;
        public string RawText { set { MyText = new RawTextComponent(value); } }
        public string TranslationKey { set { MyText = new TranslateTextComponent(value); } }

        public Text(TextAttribute attribute)
        {
            TextAttribute = attribute;
        }

        public void Generate(GameObject screen, Vector2 center)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            text.text = MyText?.Text ?? "";
            text.transform.localPosition = center;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            if (!(TextAttribute.Size.x > 0)) text.rectTransform.sizeDelta = new Vector2(size.x, TextAttribute.Size.y);
            text.text = MyText?.Text ?? "";
            text.transform.localPosition = ReflectAlignment(Alignment, TextAttribute.Size, cursor, size);

            text.ForceMeshUpdate();

            var bounds = text.GetTextBounds();
            width = CalcWidth(Alignment, cursor, size, TextAttribute.Size.x, bounds.min.x, bounds.max.x);

            return TextAttribute.Size.y;
        }
    }

    public class VariableText : IMetaContext
    {
        public AlignmentOption Alignment { get; set; }
        public TextAttribute TextAttribute { get; set; }
        public ITextComponent? MyText { get; set; } = null;
        public string RawText { set { MyText = new RawTextComponent(value); } }
        public string TranslationKey { set { MyText = new TranslateTextComponent(value); } }

        public VariableText(TextAttribute attribute)
        {
            TextAttribute = attribute;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            if (!(TextAttribute.Size.x > 0)) text.rectTransform.sizeDelta = new Vector2(size.x, TextAttribute.Size.y);
            text.text = MyText?.Text ?? "";

            text.ForceMeshUpdate();
            text.rectTransform.sizeDelta = new(text.rectTransform.sizeDelta.x, text.preferredHeight);

            text.ForceMeshUpdate();
            var bounds = text.GetTextBounds();
            width = CalcWidth(Alignment, cursor, size, TextAttribute.Size.x, bounds.min.x, bounds.max.x);

            text.transform.localPosition = ReflectAlignment(Alignment, text.rectTransform.sizeDelta, cursor, size);

            return text.rectTransform.sizeDelta.y;
        }
    }

    public class Button : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public float Width { get => TextAttribute.Size.x + TextMargin; }
        public TextAttribute TextAttribute { get; set; }
        public Action? OnClick { get; set; }
        public Action? OnMouseOver { get; set; }
        public Action? OnMouseOut { get; set; }
        public Color? Color { get; set; }
        
        public ITextComponent? Text { get; set; }
        public string RawText { set { Text = new RawTextComponent(value); } }
        public string TranslationKey { set { Text = new TranslateTextComponent(value); } }

        public Action<PassiveButton, SpriteRenderer,TMPro.TextMeshPro>? PostBuilder { get; set; }
        public Button(Action? onClick,TextAttribute attribute)
        {
            TextAttribute = attribute;
            OnClick = onClick;
        }

        private const float TextMargin = 0.26f;

        private void Generate(SpriteRenderer button,out PassiveButton passiveButton,out TMPro.TextMeshPro text)
        {
            button.sprite = VanillaAsset.TextButtonSprite;
            button.drawMode = SpriteDrawMode.Sliced;
            button.tileMode = SpriteTileMode.Continuous;
            button.size = TextAttribute.Size + new Vector2(TextMargin * 0.84f, TextMargin * 0.84f);
            button.gameObject.layer = LayerExpansion.GetUILayer();
            button.gameObject.AddComponent<SortingGroup>();

            text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, button.transform);
            TextAttribute.Reflect(text);
            text.text = Text?.Text ?? "";
            text.transform.localPosition = new Vector3(0,0,-0.1f);
            text.sortingOrder = 15;

            var collider = button.gameObject.AddComponent<BoxCollider2D>();
            collider.size = TextAttribute.Size + new Vector2(TextMargin * 0.6f, TextMargin * 0.6f);
            collider.isTrigger = true;

            passiveButton = button.gameObject.SetUpButton(true, button, Color);
            if (OnClick != null) passiveButton.OnClick.AddListener(OnClick);
            if (OnMouseOut != null) passiveButton.OnMouseOut.AddListener(OnMouseOut);
            if (OnMouseOver != null) passiveButton.OnMouseOver.AddListener(OnMouseOver);
        }

        public void Generate(GameObject screen, Vector2 center)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("Button", screen.transform, center);
            Generate(renderer,out var button,out var text);
            PostBuilder?.Invoke(button, renderer, text);
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("Button", screen.transform, 
                ReflectAlignment(Alignment, TextAttribute.Size + new Vector2(TextMargin, TextMargin), cursor, size));
            renderer.sortingOrder = 5;
            Generate(renderer, out var button, out var text);
            PostBuilder?.Invoke(button, renderer, text);

            var mySize = TextAttribute.Size.x + TextMargin;
            width = CalcWidth(Alignment, cursor, size, mySize, -mySize / 2f, mySize / 2f);

            return TextAttribute.Size.y + TextMargin;
        }
    }

    public class VerticalMargin : IMetaContext
    {
        public float Margin { get; set; }

        public AlignmentOption Alignment => AlignmentOption.Center;

        public VerticalMargin(float margin)
        {
            Margin = margin;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            width = new(size.x/2,cursor.x);
            return Margin;
        }
    }

    public class HorizonalMargin : IMetaParallelPlacable
    {
        public float Width { get; set; }

        public AlignmentOption Alignment => AlignmentOption.Center;

        public HorizonalMargin(float margin)
        {
            Width = margin;
        }

        public void Generate(GameObject screen, Vector2 center)
        {
            return;
        }
    }

    public class ScrollView : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public float Width { get => Size.x; }
        public IMetaContext? Inner;
        public Vector2 Size;
        public bool WithMask;
        public ScrollView(Vector2 size,IMetaContext inner,bool withMask=true)
        {
            this.Size = size;
            this.Inner = inner;
            this.WithMask = withMask;
        }

        public void Generate(GameObject screen, Vector2 center)
        {
            var view = UnityHelper.CreateObject("ScrollView", screen.transform, new Vector3(center.x, center.y, -0.01f));
            var inner = UnityHelper.CreateObject("Inner", view.transform, new Vector3(0, 0, 0));
            var innerSize = Size - new Vector2(0.4f, 0f);

            if (WithMask)
            {
                view.AddComponent<SortingGroup>();
                var mask = UnityHelper.CreateObject<SpriteMask>("Mask", view.transform, new Vector3(0, 0, 0));
                mask.sprite = VanillaAsset.FullScreenSprite;
                mask.transform.localScale = innerSize;
            }

            float height = Inner?.Generate(inner, new Vector2(-innerSize.x / 2f - 0.2f, innerSize.y / 2f), innerSize) ?? 0f;
            VanillaAsset.GenerateScroller(Size, view.transform, new Vector2(Size.x / 2 - 0.15f, 0f), inner.transform, new FloatRange(0, height - Size.y), Size.y);
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size,out (float min,float max) width)
        {
            var view = UnityHelper.CreateObject("ScrollView", screen.transform, ReflectAlignment(Alignment, Size, cursor, size));
            var inner = UnityHelper.CreateObject("Inner", view.transform, new Vector3(0, 0, 0));
            var innerSize = Size - new Vector2(0.2f, 0f);

            if (WithMask)
            {
                view.AddComponent<SortingGroup>();
                var mask = UnityHelper.CreateObject<SpriteMask>("Mask", view.transform, new Vector3(0, 0, 0));
                mask.sprite = VanillaAsset.FullScreenSprite;
                mask.transform.localScale = innerSize;
            }

            float height = Inner?.Generate(inner, new Vector2(-innerSize.x / 2f, innerSize.y / 2f), innerSize) ?? 0f;
            VanillaAsset.GenerateScroller(Size, view.transform, new Vector2(Size.x / 2 - 0.1f, 0f), inner.transform, new FloatRange(0, height - Size.y), Size.y);

            width = CalcWidth(Alignment, cursor, size, Size.x, -Size.x / 2f, Size.x / 2f);

            return Size.y;
        }
    }
}

public class ParallelContext : IMetaContext
{
    Tuple<IMetaContext, float>[] contents;
    public ParallelContext(params Tuple<IMetaContext,float>[] contents)
    {
        this.contents = contents;
    }

    public AlignmentOption Alignment { get; set; }

    public float Generate(GameObject screen, Vector2 cursor, Vector2 size,out (float min,float max) width)
    {
        float sum = contents.Sum(c => c.Item2);
        float height = 0;

        if (Alignment == AlignmentOption.Right)
            cursor.x += size.x - sum;
        else if (Alignment == AlignmentOption.Center)
            cursor.x += (size.x - sum) / 2f;

        float widthMin = size.x / 2f;
        float widthMax = cursor.x;

        foreach (var c in contents)
        {
            float myX = (c.Item2 / sum) * size.x;
            float temp = c.Item1.Generate(screen, cursor, new Vector2(myX, size.y),out var innerWidth);

            widthMin = Mathf.Min(widthMin, innerWidth.min);
            widthMax = Mathf.Min(widthMax, innerWidth.max);

            cursor.x += myX;
            if (temp > height) height = temp;
        }

        width = new(widthMin, widthMax);

        return height;
    }
}

public class CombinedContext : IMetaContext
{
    IMetaParallelPlacable[] contents;
    public AlignmentOption Alignment { get; set; }
    float height;

    public CombinedContext(float height, AlignmentOption alignment, params IMetaParallelPlacable[] contents)
    {
        this.contents = contents.ToArray();
        this.Alignment = alignment;
        this.height = height;
    }

    public CombinedContext(float height, params IMetaParallelPlacable[] contents) : this(height, AlignmentOption.Center, contents) { }

    public float Generate(GameObject screen, Vector2 cursor,Vector2 size, out (float min, float max) width)
    {
        float widthSum = contents.Sum(c => c.Width);

        float leftPos;
        switch (Alignment)
        {
            case AlignmentOption.Left:
                leftPos = cursor.x; break;
            case AlignmentOption.Right:
                leftPos = cursor.x + size.x - widthSum; break;
            case AlignmentOption.Center:
            default:
                leftPos = cursor.x + (size.x - widthSum) / 2f; break;
        }

        float centerY = cursor.y - height / 2f;
        foreach(var c in contents)
        {
            c.Generate(screen, new Vector2(leftPos + c.Width / 2f, centerY));
            leftPos += c.Width;
        }

        width = CalcWidth(Alignment, cursor, size, widthSum, -widthSum / 2f, widthSum / 2f);

        return height;
    }
}

public class MetaScreen : MonoBehaviour
{
    static MetaScreen()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MetaScreen>();
    }

    private GameObject? combinedObject = null;
    public LineRenderer? borderLine = null;
    private Vector2 border;
    public Vector2 Border { get => border; private set {
            bool lastShownFlag = ShowBorderLine;
            ShowBorderLine = false;
            border = value;
            ShowBorderLine = lastShownFlag;
        } }

    public bool ShowBorderLine { get => borderLine != null; set{ 
            if(borderLine == null && value)
            {
                var lineObj = new GameObject("BorderLine");
                lineObj.layer = LayerExpansion.GetUILayer();
                lineObj.transform.SetParent(transform);
                lineObj.transform.localPosition = new Vector3(0,0,0);
                borderLine = lineObj.AddComponent<LineRenderer>();
                borderLine.material.shader= Shader.Find("Sprites/Default");
                borderLine.positionCount = 4;
                borderLine.loop = true;
                float x = Border.x / 2, y = Border.y / 2;
                borderLine.SetPositions(new Vector3[] { new(-x, -y), new(-x, y), new(x, y), new(x, -y) });
                borderLine.SetWidth(0.1f, 0.1f);
                borderLine.SetColors(Color.cyan, Color.cyan);
                borderLine.useWorldSpace = false;
            }
            else if(borderLine != null && !value)
            {
                GameObject.Destroy(borderLine.gameObject);
                borderLine = null;
            }
        } }

    public float SetContext(Vector2? border, IMetaContext? context)
    {
        if(border != null) Border = border.Value;

        return SetContext(context);
    }

    public float SetContext(IMetaContext? context,out (float min,float max) width) {
        gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) =>
        {
            if (obj.name != "BorderLine") GameObject.Destroy(obj);
        }));

        if (context == null)
        {
            width = (0f, 0f);
            return 0f;
        }

        Vector2 cursor = Border / 2f;
        cursor.x *= -1f;
        return context.Generate(gameObject, cursor, Border, out width);
    }

    public float SetContext(IMetaContext? context) => SetContext(context, out var _);

    public void CloseScreen()
    {
        GameObject.Destroy(combinedObject ?? gameObject);
    }

    static public MetaScreen GenerateScreen(Vector2 size,Transform? parent,Vector3 localPos,bool withBackground,bool withBlackScreen,bool withClickGuard)
    {
        var window = UnityHelper.CreateObject("MetaWindow", parent, localPos);
        if (withBackground)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("Background", window.transform, new Vector3(0, 0, 0.1f));
            renderer.sprite = VanillaAsset.PopUpBackSprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size + new Vector2(0.45f, 0.35f);
            renderer.gameObject.layer = LayerExpansion.GetUILayer();
        }
        if (withBlackScreen)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("BlackScreen", window.transform, new Vector3(0, 0, 0.2f));
            renderer.sprite = VanillaAsset.FullScreenSprite;
            renderer.color = new Color(0, 0, 0, 0.4226f);
            renderer.transform.localScale = new Vector3(100f, 100f);
            renderer.gameObject.layer = LayerExpansion.GetUILayer();
        }
        if (withClickGuard)
        {
            var collider = UnityHelper.CreateObject<BoxCollider2D>("ClickGuard", window.transform, new Vector3(0, 0, 0.2f));
            collider.isTrigger = true;
            collider.gameObject.layer= LayerExpansion.GetUILayer();
            collider.size = new Vector2(100f,100f);
            collider.gameObject.SetUpButton();
        }
        var screen = UnityHelper.CreateObject<MetaScreen>("Screen", window.transform, Vector3.zero);
        screen.Border = size;
        screen.combinedObject = window;

        return screen;
    }

    static public MetaScreen GenerateWindow(Vector2 size, Transform? parent, Vector3 localPos, bool withBlackScreen,bool closeOnClickOutside,bool withCloseButton = true)
    {
        var screen = GenerateScreen(size, parent, localPos, true, withBlackScreen, true);
        
        var obj = screen.transform.parent.gameObject;

        var collider = UnityHelper.CreateObject<CircleCollider2D>("CloseButton", obj.transform, new Vector3(-size.x / 2f - 0.6f, size.y / 2f + 0.25f, 0f));
        collider.isTrigger = true;
        collider.gameObject.layer = LayerExpansion.GetUILayer();
        collider.radius = 0.25f;
        SpriteRenderer? renderer = null;
        if (withCloseButton)
        {
            renderer = collider.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = VanillaAsset.CloseButtonSprite;
        }
        var button = collider.gameObject.SetUpButton(true, renderer);
        button.OnClick.AddListener(() => GameObject.Destroy(obj));
        NebulaManager.Instance.RegisterUI(obj, button);

        if (closeOnClickOutside)
            obj.transform.FindChild("ClickGuard").GetComponent<PassiveButton>().OnClick.AddListener(() => GameObject.Destroy(obj));

        return screen;
    }
}
