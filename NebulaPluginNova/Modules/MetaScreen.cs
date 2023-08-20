﻿using Il2CppInterop.Runtime.Injection;
using UnityEngine;
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
    public float Generate(GameObject screen, Vector2 cursor, Vector2 size);
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
    public float Generate(GameObject screen,Vector2 cursor,Vector2 size)
    {
        float heightSum = 0;
        foreach(var c in contents)
        {
            float height = c.Generate(screen, cursor, size);
            heightSum += height;
            cursor.y -= height;
            size.y -= height;
        }
        return heightSum;
    }

    public MetaContext Append(IMetaContext content)
    {
        contents.Add(content);
        return this;
    }

    public MetaContext[] Split(params float[] ratios)
    {
        var contents = ratios.Select(ratio => new Tuple<IMetaContext,float>(new MetaContext(),ratio)).ToArray();
        Append(new ParallelContext(contents));
        return contents.Select(c => (MetaContext)c.Item1).ToArray();
    }

    public class Text : IMetaContext, IMetaParallelPlacable
    {
        public AlignmentOption Alignment { get; set; }
        public float Width { get => TextAttribute.Size.x; }
        public TextAttribute TextAttribute { get; set; }
        public string RawText { get; set; } = "";
        public Text(TextAttribute attribute)
        {
            TextAttribute = attribute;
        }

        public void Generate(GameObject screen, Vector2 center)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            text.text = RawText;
            text.transform.localPosition = center;
        }

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size)
        {
            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, screen.transform);
            TextAttribute.Reflect(text);
            if (!(TextAttribute.Size.x > 0)) text.rectTransform.sizeDelta = new Vector2(size.x, TextAttribute.Size.y);
            text.text = RawText;
            text.transform.localPosition = ReflectAlignment(Alignment, TextAttribute.Size, cursor, size);
            return TextAttribute.Size.y;
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
        public string RawText { get; set; } = "";
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

            text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, button.transform);
            TextAttribute.Reflect(text);
            text.text = RawText;
            text.transform.localPosition = new Vector3(0,0,-0.1f);

            var collider = button.gameObject.AddComponent<BoxCollider2D>();
            collider.size = TextAttribute.Size + new Vector2(TextMargin * 0.6f, TextMargin * 0.6f);
            collider.isTrigger = true;

            passiveButton = button.gameObject.SetUpButton(true,button);
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

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("Button", screen.transform, 
                ReflectAlignment(Alignment, TextAttribute.Size + new Vector2(TextMargin, TextMargin), cursor, size));
            Generate(renderer, out var button, out var text);
            PostBuilder?.Invoke(button, renderer, text);
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

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size)
        {
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

        public float Generate(GameObject screen, Vector2 cursor, Vector2 size)
        {
            return 0f;
        }

        public void Generate(GameObject screen, Vector2 center)
        {
            return;
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

    public float Generate(GameObject screen, Vector2 cursor, Vector2 size)
    {
        float sum = contents.Sum(c => c.Item2);
        float height = 0;

        if (Alignment == AlignmentOption.Right)
            cursor.x += size.x - sum;
        else if (Alignment == AlignmentOption.Center)
            cursor.x += (size.x - sum) / 2f;

        foreach (var c in contents)
        {
            float myX = (c.Item2 / sum) * size.x;
            float temp = c.Item1.Generate(screen, cursor, new Vector2(myX, size.y));
            cursor.x += myX;
            if (temp > height) height = temp;
        }
        return height;
    }
}

public class CombinedContent : IMetaContext
{
    IMetaParallelPlacable[] contents;
    public AlignmentOption Alignment { get; set; }
    float height;

    public CombinedContent(float height, AlignmentOption alignment, params IMetaParallelPlacable[] contents)
    {
        this.contents = contents;
        this.Alignment = alignment;
        this.height = height;
    }

    public CombinedContent(float height, params IMetaParallelPlacable[] contents) : this(height, AlignmentOption.Center, contents) { }

    public float Generate(GameObject screen, Vector2 cursor,Vector2 size)
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

    public float SetContext(Vector2? border, IMetaContext context)
    {
        if(border != null) Border = border.Value;

        return SetContext(context);
    }

    public float SetContext(IMetaContext context)
    {
        gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) =>
        {
            if (obj.name != "BorderLine") GameObject.Destroy(obj);
        }));

        Vector2 cursor = Border / 2f;
        cursor.x *= -1f;
        return context.Generate(gameObject, cursor, Border);
    }

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

    static public MetaScreen GenerateWindow(Vector2 size, Transform? parent, Vector3 localPos, bool withBlackScreen,bool closeOnClickOutside)
    {
        var screen = GenerateScreen(size, parent, localPos, true, withBlackScreen, true);
        
        var obj = screen.transform.parent.gameObject;

        var collider = UnityHelper.CreateObject<CircleCollider2D>("CloseButton", obj.transform, new Vector3(-size.x / 2f - 0.6f, size.y / 2f + 0.25f, 0f));
        collider.isTrigger = true;
        collider.gameObject.layer = LayerExpansion.GetUILayer();
        collider.radius = 0.25f;
        var renderer = collider.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = VanillaAsset.CloseButtonSprite;
        var button = collider.gameObject.SetUpButton(true, renderer);
        button.OnClick.AddListener(() => GameObject.Destroy(obj));
        NebulaManager.Instance.RegisterUI(obj, button);

        if (closeOnClickOutside)
            obj.transform.FindChild("ClickGuard").GetComponent<PassiveButton>().OnClick.AddListener(() => GameObject.Destroy(obj));

        return screen;
    }
}