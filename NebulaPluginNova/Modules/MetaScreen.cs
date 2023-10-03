using Iced.Intel;
using Il2CppInterop.Runtime.Injection;
using Nebula.Behaviour;
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

    protected static (float, float) CalcWidth(AlignmentOption alignment, Vector2 cursor, Vector2 size, float width) => CalcWidth(alignment, cursor, size, width, -width / 2f, width / 2f);
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
    public float Generate(GameObject screen, Vector2 center,out float width);

}

public class MetaContext : IMetaContext, IMetaParallelPlacable
{
    List<IMetaContext> contents = new();

    public AlignmentOption Alignment => AlignmentOption.Center;
    public float Generate(GameObject screen, Vector2 cursor, Vector2 size,out (float,float) width)
    {
        if (contents.Count == 0)
        {
            width = (cursor.x,cursor.x);
            return 0f;
        }

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

    public float Generate(GameObject screen, Vector2 center, out float width)
    {
        if (contents.Count == 0)
        {
            width = 0f;
            return 0f;
        }

        var subscreen = UnityHelper.CreateObject("MetaContext", screen.transform, new Vector3(center.x, center.y, -0.1f));

        float widthMin = float.MaxValue;
        float widthMax = float.MinValue;

        float heightSum = 0;
        foreach (var c in contents)
        {
            float height = c.Generate(subscreen, new Vector2(0, -heightSum), new Vector2(0, 0), out var innerWidth);
            widthMin = Math.Min(widthMin, innerWidth.min);
            widthMax = Math.Max(widthMax, innerWidth.max);
            heightSum += height;
        }

        width = widthMax - widthMin;

        subscreen.transform.localPosition -= new Vector3(width * 0.5f, -heightSum * 0.5f);
        return heightSum;
    }

    public MetaContext Append(IMetaContext content)
    {
        contents.Add(content);
        return this;
    }

    //linesを-1にすると全部を並べる
    public MetaContext Append<T>(IEnumerable<T> enumerable,Func<T, IMetaParallelPlacable> converter,int perLine,int lines,int page,float height,bool fixedHeight = false, AlignmentOption alignment = AlignmentOption.Center)
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
                Append(new CombinedContext(height, contextList) { Alignment = alignment });
                index = 0;
                leftLines--;

                if (leftLines == 0) break;
            }
        }

        if (index != 0)
        {
            for (; index < perLine; index++) contextList[index] = new HorizonalMargin(0f);
            Append(new CombinedContext(height, contextList) { Alignment = alignment });
            leftLines--;
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

        public float Generate(GameObject screen, Vector2 center, out float width)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("Image", screen.transform,center);
            renderer.sprite = Sprite;
            renderer.sortingOrder = 10;
            if (Sprite) renderer.transform.localScale = Vector3.one * (Width / Sprite!.bounds.size.x);
            PostBuilder?.Invoke(renderer);
            width = Width;
            return Sprite ? renderer.transform.localScale.y * Sprite!.bounds.size.y : 0f;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            Vector2 mySize = Sprite ? Sprite!.bounds.size * (Width / Sprite!.bounds.size.x) : new(Width, Width);

            if (Sprite)
            {
                var renderer = UnityHelper.CreateObject<SpriteRenderer>("Image", screen.transform, ReflectAlignment(Alignment, mySize + new Vector2(0.1f, 0.1f), cursor, size));
                renderer.sprite = Sprite;
                renderer.transform.localScale = Vector3.one * (Width / Sprite!.bounds.size.x);
                renderer.sortingOrder = 10;
                PostBuilder?.Invoke(renderer);
            }

            width = CalcWidth(Alignment, cursor, size, mySize.x, -mySize.x / 2f, mySize.x / 2f);

            return mySize.y + 0.1f;
        }
    }

    public class Text : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public TextAttribute TextAttribute { get; set; }
        
        public ITextComponent? MyText { get; set; } = null;
        public string RawText { set { MyText = new RawTextComponent(value); } }
        public string TranslationKey { set { MyText = new TranslateTextComponent(value); } }
        
        public Action<TMPro.TextMeshPro>? PostBuilder { get; set; }

        public Text(TextAttribute attribute)
        {
            TextAttribute = attribute;
        }

        public float Generate(GameObject screen, Vector2 center, out float width)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            text.text = MyText?.Text ?? "";
            text.transform.localPosition = center;
            text.sortingOrder = 10;

            PostBuilder?.Invoke(text);

            width = TextAttribute.Size.x;

            return TextAttribute.Size.y;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            if (!(TextAttribute.Size.x > 0)) text.rectTransform.sizeDelta = new Vector2(size.x, TextAttribute.Size.y);
            text.text = MyText?.Text ?? "";
            text.transform.localPosition = ReflectAlignment(Alignment, TextAttribute.Size, cursor, size);
            text.sortingOrder = 10;

            text.ForceMeshUpdate();

            var bounds = text.GetTextBounds();
            width = CalcWidth(Alignment, cursor, size, TextAttribute.Size.x, bounds.min.x, bounds.max.x);

            PostBuilder?.Invoke(text);

            return TextAttribute.Size.y;
        }
    }

    public class VariableText : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public TextAttribute TextAttribute { get; set; }
        public ITextComponent? MyText { get; set; } = null;
        public string RawText { set { MyText = new RawTextComponent(value); } }
        public string TranslationKey { set { MyText = new TranslateTextComponent(value); } }
        public Action<TMPro.TextMeshPro>? PostBuilder { get; set; } = null;

        public VariableText(TextAttribute attribute)
        {
            TextAttribute = attribute;
        }

        public float Generate(GameObject screen, Vector2 center, out float width)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            text.text = MyText?.Text ?? "";
            text.transform.localPosition = center;
            text.sortingOrder = 10;

            text.ForceMeshUpdate();
            text.rectTransform.sizeDelta = new(text.preferredWidth, text.preferredHeight);

            text.ForceMeshUpdate();

            PostBuilder?.Invoke(text);

            width = text.rectTransform.sizeDelta.x;

            return text.rectTransform.sizeDelta.y;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            if (!(TextAttribute.Size.x > 0)) text.rectTransform.sizeDelta = new Vector2(size.x, TextAttribute.Size.y);
            text.text = MyText?.Text ?? "";
            text.sortingOrder = 10;

            text.ForceMeshUpdate();
            text.rectTransform.sizeDelta = new(text.preferredWidth, text.preferredHeight);

            text.ForceMeshUpdate();
            var bounds = text.GetTextBounds();
            width = CalcWidth(Alignment, cursor, text.rectTransform.sizeDelta, text.rectTransform.sizeDelta.x, bounds.min.x, bounds.max.x);

            text.transform.localPosition = ReflectAlignment(Alignment, text.rectTransform.sizeDelta, cursor, size);

            PostBuilder?.Invoke(text);

            return text.rectTransform.sizeDelta.y;
        }
    }

    public class Button : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public TextAttribute TextAttribute { get; set; }
        public Action? OnClick { get; set; }
        public Action? OnMouseOver { get; set; }
        public Action? OnMouseOut { get; set; }
        public Color? Color { get; set; }

        public ITextComponent? Text { get; set; }
        public string RawText { set { Text = new RawTextComponent(value); } }
        public string TranslationKey { set { Text = new TranslateTextComponent(value); } }

        public Action<PassiveButton, SpriteRenderer, TMPro.TextMeshPro>? PostBuilder { get; set; }
        public Button(Action? onClick, TextAttribute attribute)
        {
            TextAttribute = attribute;
            OnClick = onClick;
        }

        public Button SetAsMaskedButton()
        {
            PostBuilder = (_, renderer, _) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            return this;
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

        public float Generate(GameObject screen, Vector2 center, out float width)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("Button", screen.transform, center);
            Generate(renderer,out var button,out var text);
            PostBuilder?.Invoke(button, renderer, text);

            width = TextAttribute.Size.x + TextMargin;

            return TextAttribute.Size.y + TextMargin;
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

    public class StateButton : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public Action<bool>? OnChanged { get; set; }
        public Reference<bool>? StateRef { get; set; }
        public bool WithMaskMaterial { get; set; } = false;

        public StateButton(bool state) {}

        private void Generate(GameObject obj)
        {
            var attr = new TextAttribute(TextAttribute.NormalAttr) { Size = new(0.36f, 0.36f), FontMaterial = WithMaskMaterial ? VanillaAsset.StandardMaskedFontMaterial : null }.EditFontSize(2f, 1f, 2f);

            var checkMark = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, obj.transform);
            attr.Reflect(checkMark);
            checkMark.text = "<b>✓</b>";
            checkMark.transform.localPosition = new Vector3(0, 0, -0.2f);
            checkMark.sortingOrder = 16;
            checkMark.color = Color.green;
            checkMark.gameObject.SetActive(StateRef?.Value ?? false);

            var box = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, obj.transform);
            attr.Reflect(box);
            box.text = "□";
            box.transform.localPosition = new Vector3(0, 0, -0.1f);
            box.sortingOrder = 15;

            var collider = obj.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.2f,0.2f);
            collider.isTrigger = true;

            var copiedState = StateRef;
            var copiedChanged = OnChanged;
            void SetState(bool state, bool callEvent = true)
            {
                if (copiedState != null) copiedState.Value = state;
                checkMark.gameObject.SetActive(state);
                if (callEvent) copiedChanged?.Invoke(state);
            }
            void SwitchState() => SetState(!checkMark.gameObject.activeSelf);

            var passiveButton = obj.SetUpButton(true);
            passiveButton.OnClick.AddListener(() => SwitchState());
            passiveButton.OnMouseOut.AddListener(() => box.color = Color.white);
            passiveButton.OnMouseOver.AddListener(()=>box.color = Color.green);
        }

        public float Generate(GameObject screen, Vector2 center, out float width)
        {
            var obj = UnityHelper.CreateObject("CheckBox",screen.transform,center);
            Generate(obj);

            width = 0.25f;
            return 0.25f;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            var obj = UnityHelper.CreateObject("CheckBox", screen.transform, ReflectAlignment(Alignment, new Vector2(0.25f, 0.25f), cursor, size));
            Generate(obj);

            width = CalcWidth(Alignment, cursor, size, 0.25f);

            return 0.25f;
        }

        public static CombinedContext CheckBox(string translateKey,float width,bool isBold,bool state,Action<bool> onChanged, bool maskMaterial = false)
        {
            return new CombinedContext(
                new StateButton(state) { OnChanged = onChanged, WithMaskMaterial = maskMaterial },
                new Text(new(isBold ? TextAttribute.BoldAttr : TextAttribute.NormalAttr) { FontMaterial = maskMaterial ? VanillaAsset.StandardMaskedFontMaterial : null, Size = new(width, 0.3f), Alignment = TMPro.TextAlignmentOptions.Left }) { TranslationKey = translateKey }
                );
        }

        public static CombinedContext TopLabelCheckBox(string translateKey, float? width, bool isBold, bool state, Action<bool> onChanged,bool maskMaterial = false)
        {
            IMetaParallelPlacable label;
            if (width == null)
                label = new VariableText(new(isBold ? TextAttribute.BoldAttr : TextAttribute.NormalAttr) { FontMaterial = maskMaterial ? VanillaAsset.StandardMaskedFontMaterial : null, Size = new(5f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Left }) { TranslationKey = translateKey };
            else
                label = new Text(new(isBold ? TextAttribute.BoldAttr : TextAttribute.NormalAttr) { FontMaterial = maskMaterial ? VanillaAsset.StandardMaskedFontMaterial : null, Size = new(width.Value, 0.3f), Alignment = TMPro.TextAlignmentOptions.Left }) { TranslationKey = translateKey };
        
            return new CombinedContext(
                label,
                new Text(new(isBold ? TextAttribute.BoldAttr : TextAttribute.NormalAttr) { FontMaterial = maskMaterial ? VanillaAsset.StandardMaskedFontMaterial : null, Size = new(0.15f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center }) { RawText = ":" },
                new StateButton(state) { OnChanged = onChanged, WithMaskMaterial = maskMaterial }                
                );
        }
    }


    public class VerticalMargin : IMetaContext, IMetaParallelPlacable
    {
        public float Margin { get; set; }

        public AlignmentOption Alignment => AlignmentOption.Center;

        public VerticalMargin(float margin)
        {
            Margin = margin;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            width = new(cursor.x, cursor.x);
            return Margin;
        }

        public float Generate(GameObject screen, Vector2 center, out float width)
        {
            width = 0f;
            return Margin;
        }
    }

    public class HorizonalMargin : IMetaContext, IMetaParallelPlacable
    {
        public float Width { get; set; }

        public AlignmentOption Alignment => AlignmentOption.Center;

        public HorizonalMargin(float margin)
        {
            Width = margin;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            width = new(cursor.x, cursor.x + Width);
            return 0f;
        }
        public float Generate(GameObject screen, Vector2 center,out float width)
        {
            width = Width;
            return 0f;
        }
    }

    public class ScrollView : IMetaContext, IMetaParallelPlacable
    {
        //ビュー内のコンテンツが後から変更できる
        public class InnerScreen
        {
            private GameObject screen;
            private Vector2 innerSize;
            private float scrollViewSizeY;
            private Scroller scroller;
            private Collider2D scrollerCollider;
            public void SetContext(IMetaContext? context)
            {
                //子を削除
                screen.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) => GameObject.Destroy(obj)));

                float height = context?.Generate(screen, new Vector2(-innerSize.x / 2f, innerSize.y / 2f), innerSize) ?? 0f;
                scroller.SetBounds(new FloatRange(0, height - scrollViewSizeY), null);

                foreach (var button in screen.GetComponentsInChildren<PassiveButton>()) button.ClickMask = scrollerCollider;
            }

            public void SetStaticContext(IMetaParallelPlacable? context)
            {
                //子を削除
                screen.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) => GameObject.Destroy(obj)));

                context?.Generate(screen, new Vector2(0f, 0f), out _);
                scroller.SetBounds(new FloatRange(0f, 0f), null);
            }

            public void SetLoadingContext() => SetStaticContext(new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr).EditFontSize(2.8f)) { TranslationKey = "ui.common.loading" });
            

            public InnerScreen(GameObject screen,Vector2 innerSize, Scroller scroller, Collider2D scrollerCollider, float scrollViewSizeY)
            {
                this.screen = screen;
                this.innerSize = innerSize;
                this.scroller = scroller;
                this.scrollerCollider = scrollerCollider;
                this.scrollViewSizeY = scrollViewSizeY;
            }
        }

        public AlignmentOption Alignment { get; set; }
        public IMetaContext? Inner = null;
        public Vector2 Size;
        public bool WithMask;
        public Reference<InnerScreen>? InnerRef = null;

        public ScrollView(Vector2 size,IMetaContext inner,bool withMask=true)
        {
            this.Size = size;
            this.Inner = inner;
            this.WithMask = withMask;
        }

        public ScrollView(Vector2 size, Reference<InnerScreen> reference, bool withMask = true)
        {
            this.Size = size;
            this.InnerRef = reference;
            this.WithMask = withMask;
        }

        public float Generate(GameObject screen, Vector2 center, out float width)
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

            float height = Inner?.Generate(inner, new Vector2(-innerSize.x / 2f, innerSize.y / 2f), innerSize) ?? 0f;
            var scroller = VanillaAsset.GenerateScroller(Size, view.transform, new Vector2(Size.x / 2 - 0.15f, 0f), inner.transform, new FloatRange(0, height - Size.y), Size.y);

            var hitBox = scroller.GetComponent<Collider2D>();
            foreach(var button in inner.GetComponentsInChildren<PassiveButton>()) button.ClickMask = hitBox;

            InnerRef?.Set(new(inner, innerSize, scroller, hitBox, Size.y));

            width = Size.x;

            return Size.y;
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
            var scroller = VanillaAsset.GenerateScroller(Size, view.transform, new Vector2(Size.x / 2 - 0.1f, 0f), inner.transform, new FloatRange(0, height - Size.y), Size.y);

            var hitBox = scroller.GetComponent<Collider2D>();
            foreach (var button in inner.GetComponentsInChildren<PassiveButton>()) button.ClickMask = hitBox;

            width = CalcWidth(Alignment, cursor, size, Size.x, -Size.x / 2f, Size.x / 2f);

            InnerRef?.Set(new(inner, innerSize, scroller, hitBox, Size.y));

            return Size.y;
        }
    }

    public class TextInput : IMetaContext, IMetaParallelPlacable
    {

        public AlignmentOption Alignment { get; set; } = AlignmentOption.Left;
        private Vector2 fieldSize;
        private int maxLines;
        private float fontSize;
        private bool useSharpFieldFlag;
        public Reference<TextField>? TextFieldRef;
        public Predicate<char>? TextPredicate;
        public Action<TextField>? PostBuilder;
        public string DefaultText = "";

        public TextInput(int maxLines,float fontSize,Vector2 size,bool useSharpField = false)
        {
            this.maxLines = maxLines;
            this.fontSize = fontSize;
            fieldSize = size;
            useSharpFieldFlag = useSharpField;
        }

        private Vector2 ActualSize => fieldSize + new Vector2(0.15f, 0.15f);
        private void Generate(Transform screen,Vector2 center)
        {
            Vector2 actualSize = ActualSize;
            var obj = UnityHelper.CreateObject("TextField", screen, center);

            var field = UnityHelper.CreateObject<TextField>("Text", obj.transform, new Vector3(0, 0, -0.5f));
            field.SetSize(fieldSize, fontSize, maxLines);
            field.InputPredicate = TextPredicate;

            var background = UnityHelper.CreateObject<SpriteRenderer>("Background", obj.transform, Vector3.zero);
            background.sprite = useSharpFieldFlag ? NebulaAsset.SharpWindowBackgroundSprite.GetSprite() : VanillaAsset.TextButtonSprite;
            background.drawMode = SpriteDrawMode.Sliced;
            background.tileMode = SpriteTileMode.Continuous;
            background.size = actualSize;
            background.sortingOrder = 5;
            var collider = background.gameObject.AddComponent<BoxCollider2D>();
            collider.size = actualSize;
            collider.isTrigger = true;
            var button = background.gameObject.SetUpButton(true,background);
            button.OnClick.AddListener(() => field.GainFocus());
            TextFieldRef?.Set(field);
            if (DefaultText.Length > 0) field.SetText(DefaultText);

            PostBuilder?.Invoke(field);
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            Vector2 actualSize = ActualSize;
            Generate(screen.transform, ReflectAlignment(Alignment, actualSize, cursor, size));
            width = CalcWidth(Alignment, cursor, size, actualSize.x, -actualSize.x / 2f, actualSize.x / 2f);

            return actualSize.y + 0.05f;
        }
        public float Generate(GameObject screen, Vector2 center, out float width)
        {
            Generate(screen.transform, center);
            width = ActualSize.x;

            return ActualSize.y + 0.05f;
        }
    }

    public class FramedContext : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment => (context as IMetaContext)?.Alignment ?? AlignmentOption.Center;
        object context { get; set; }
        Vector2 extended { get; set; }
        public Color? HighlightColor { get; set; }
        public Action<SpriteRenderer>? PostBuilder { get; set; }

        public FramedContext(IMetaContext context,Vector2 extended) { 
            this.context = context;
            this.extended = extended;
        }

        public FramedContext(IMetaParallelPlacable context,Vector2 extended)
        {
            this.context = context;
            this.extended = extended;
        }


        public float Generate(GameObject screen, Vector2 cursor, Vector2 size, out (float min, float max) width)
        {
            var frame = UnityHelper.CreateObject("SizedFrame", screen.transform, new(0f, 0f, -0.5f));

            IMetaContext mc = (context as IMetaContext) ?? new MetaContext();


            float height = mc.Generate(frame, cursor + new Vector2(extended.x, -extended.y), size - new Vector2(extended.x, extended.y) * 2f, out width);
            if (HighlightColor != null)
            {
                var backGround = NebulaAsset.CreateSharpBackground(new Vector2(width.max - width.min, height) + extended * 1.8f, HighlightColor.Value, screen.transform);
                backGround.transform.localPosition += new Vector3((width.min + width.max) * 0.5f, cursor.y - height * 0.5f - extended.y, 0.5f);
                backGround.sortingOrder = 7;
                PostBuilder?.Invoke(backGround);
            }
            width = (width.min - extended.x, width.max + extended.x);
            return extended.y * 2f + height;
        }

        public float Generate(GameObject screen, Vector2 center, out float width)
        {
            float height = 0f;
            var frame = UnityHelper.CreateObject("SizedFrame", screen.transform, new(center.x, center.y, -1f));
            width = 0f;
            if(context is IMetaParallelPlacable mpp)
            {
                height = mpp.Generate(frame, Vector2.zero,out width);
            }

            if(HighlightColor != null)
            {
                var backGround = NebulaAsset.CreateSharpBackground(new Vector2(width + extended.x * 1.8f, height + extended.y * 1.8f), HighlightColor.Value, frame.transform);
                backGround.transform.localPosition += new Vector3(0, 0, 0.5f);
                backGround.sortingOrder = 7;
                PostBuilder?.Invoke(backGround);
            }

            height += extended.y * 2f;
            width += extended.x * 2f;
            return height;
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

public class CombinedContext : IMetaContext, IMetaParallelPlacable
{
    IMetaParallelPlacable[] contents;
    public AlignmentOption Alignment { get; set; }
    float? height;

    public CombinedContext(float height, AlignmentOption alignment, params IMetaParallelPlacable[] contents)
    {
        this.contents = contents.ToArray();
        this.Alignment = alignment;
        this.height = height < 0f ? null : height;
    }

    public CombinedContext(float height, params IMetaParallelPlacable[] contents) : this(height, AlignmentOption.Center, contents) { }
    public CombinedContext(params IMetaParallelPlacable[] contents) : this(-1f, AlignmentOption.Center, contents) { }

    public float Generate(GameObject screen, Vector2 cursor,Vector2 size, out (float min, float max) width)
    {
        var combinedScreen = UnityHelper.CreateObject("CombinedScreen", screen.transform, Vector3.zero);
        float x = 0f;
        float height = 0f;
        foreach(var c in contents)
        {
            var alloc = UnityHelper.CreateObject("Allocator", combinedScreen.transform, new Vector3(x, 0f, 0f));
            height = Mathf.Max(height, c.Generate(alloc, Vector2.zero,out float cWidth));
            alloc.transform.localPosition += new Vector3(cWidth * 0.5f, 0f);
            x += cWidth;
        }
        if (this.height.HasValue) height = this.height.Value;

        float centerY = cursor.y - height / 2f;

        float leftPos;
        switch (Alignment)
        {
            case AlignmentOption.Left:
                leftPos = cursor.x; break;
            case AlignmentOption.Right:
                leftPos = cursor.x + size.x - x; break;
            case AlignmentOption.Center:
            default:
                leftPos = cursor.x + (size.x - x) / 2f; break;
        }

        combinedScreen.transform.localPosition += new Vector3(leftPos, centerY, 0f);

        width = CalcWidth(Alignment, cursor, size, x, -x / 2f, x / 2f);

        return height;
    }

    public float Generate(GameObject screen, Vector2 center, out float width)
    {
        var combinedScreen = UnityHelper.CreateObject("CombinedScreen", screen.transform, Vector3.zero);
        float x = 0f;
        float height = 0f;
        foreach (var c in contents)
        {
            var alloc = UnityHelper.CreateObject("Allocator", combinedScreen.transform, new Vector3(x, 0f, 0f));
            height = Mathf.Max(height, c.Generate(alloc, Vector2.zero, out float cWidth));
            alloc.transform.localPosition += new Vector3(cWidth * 0.5f, 0f);
            x += cWidth;
        }
        if (this.height.HasValue) height = this.height.Value;


        combinedScreen.transform.localPosition += new Vector3(-x / 2f, 0f, 0f);

        width = x;

        return height;
    }

}

public class MetaScreen : MonoBehaviour
{
    static MetaScreen()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MetaScreen>();
    }

    public void Start()
    {
        gameObject.AddComponent<SortingGroup>();
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
        {
            obj.transform.FindChild("ClickGuard").GetComponent<PassiveButton>().OnClick.AddListener(() => GameObject.Destroy(obj));
            var myCollider = UnityHelper.CreateObject<BoxCollider2D>("MyScreenCollider", obj.transform, new Vector3(0f, 0f, 0.1f));
            myCollider.isTrigger = false;
            myCollider.size = size;
            myCollider.gameObject.SetUpButton();
        }

        return screen;
    }
}
