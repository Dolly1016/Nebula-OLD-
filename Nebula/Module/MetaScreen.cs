using Nebula.Expansion;
using Steamworks;

namespace Nebula.Module;

public class MetaScreenContent
{
    public virtual Vector2 GetSize() { return new Vector3(0f, 0f); }

    public MetaScreenContent()
    {
    }

    public virtual void Generate(GameObject obj) { }
    public Action<GameObject>? PostBuilder = null;
}

public class MSMargin : MetaScreenContent
{
    protected float width { get; }

    public override Vector2 GetSize() => new Vector2(width, 0f);

    public MSMargin(float width)
    {
        this.width = width;
    }
}

public class MSString : MetaScreenContent
{
    protected float width { get; }
    protected string rawText { get; }
    protected TMPro.TextAlignmentOptions alignment { get; }
    protected TMPro.FontStyles style { get; }
    public TMPro.TextMeshPro? text { get; protected set; }
    public override Vector2 GetSize() => new Vector2(width + (omitMargin ? 0f : 0.06f), 0.5f);
    protected float fontSize = 3f;
    protected float fontSizeMin = 2f;
    private bool dontAllowWrapping = false;
    private bool omitMargin = false;

    public MSString(float width, string text, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style)
    {
        this.width = width;
        this.rawText = text;
        this.alignment = alignment;
        this.style = style;
    }

    public MSString(float width, string text, float fontSize, float fontSizeMin, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style,bool dontAllowWrapping=false,bool omitMargin=false)
        : this(width, text, alignment, style)
    {
        this.fontSize = fontSize;
        this.fontSizeMin = fontSizeMin;
        this.dontAllowWrapping = dontAllowWrapping;
        this.omitMargin = omitMargin;
    }

    public MSString EditFontSize(float fontSize,float fontSizeMin)
    {
        this.fontSize = fontSize;
        this.fontSizeMin = fontSizeMin;
        return this;
    }

    public MSString SetFontSizeMin(float fontSizeMin)
    {
        this.fontSizeMin = fontSizeMin;
        return this;
    }

    public override void Generate(GameObject obj)
    {
        this.text = GameObject.Instantiate(RuntimePrefabs.TextPrefab/*HudManager.Instance.Dialogue.target*/);
        text.transform.SetParent(obj.transform);
        text.transform.localScale = new Vector3(1f, 1f, 1f);
        text.transform.localPosition = new Vector3(0f, 0f, -1f);

        text.alignment = alignment;
        text.fontStyle = style;
        text.rectTransform.sizeDelta = new Vector2(width - (omitMargin ? 0f : 0.2f), 0.36f);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.text = rawText;
        text.fontSize = text.fontSizeMax = fontSize;
        text.fontSizeMin = fontSizeMin;
        text.m_fontSizeBase = 3f;
        if (dontAllowWrapping) text.enableWordWrapping = false;
    }
}

public class MSTextArea : MetaScreenContent
{
    protected Vector2 size;
    protected string rawText { get; }
    protected TMPro.TextAlignmentOptions alignment { get; }
    protected TMPro.FontStyles style { get; }
    public TMPro.TextMeshPro? text { get; protected set; }
    public float fontSize { get; protected set; }
    public override Vector2 GetSize() => new Vector2(size.x + 0.06f, size.y + 0.1f);

    public MSTextArea(Vector2 size, string text, float fontSize, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style)
    {
        this.size = size;
        this.rawText = text;
        this.fontSize = fontSize;
        this.alignment = alignment;
        this.style = style;
    }

    public override void Generate(GameObject obj)
    {
        this.text = GameObject.Instantiate(RuntimePrefabs.TextPrefab/*HudManager.Instance.Dialogue.target*/);
        text.transform.SetParent(obj.transform);
        text.transform.localScale = new Vector3(1f, 1f, 1f);
        text.transform.localPosition = new Vector3(0f, 0f, -1f);

        text.alignment = alignment;
        text.fontStyle = style;
        text.rectTransform.sizeDelta = size - new Vector2(0.2f, 0f);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.text = rawText;
        text.fontSize = text.fontSizeMax = fontSize;
        text.fontSizeMin = fontSize / 2;
        text.m_fontSizeBase = 3f;
    }
}

public class MSMultiString : MetaScreenContent
{
    protected float width { get; }
    protected string rawText { get; }
    protected TMPro.TextAlignmentOptions alignment { get; }
    protected TMPro.FontStyles style { get; }
    public TMPro.TextMeshPro? text { get; protected set; }
    public float fontSize { get; protected set; }
    public override Vector2 GetSize() => new Vector2(width + 0.06f, 0.1f + 0.74f * fontSize / 6f * (float)(1 + rawText.Count((c) => c == '\n')));

    public MSMultiString(float width, float size, string text, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style)
    {
        this.width = width;
        this.fontSize = size;
        this.rawText = text;
        this.alignment = alignment;
        this.style = style;
    }

    public override void Generate(GameObject obj)
    {
        this.text = GameObject.Instantiate(RuntimePrefabs.TextPrefab/*HudManager.Instance.Dialogue.target*/);
        text.transform.SetParent(obj.transform);
        text.transform.localScale = new Vector3(1f, 1f, 1f);
        text.transform.localPosition = new Vector3(0f, 0f, -1f);

        text.alignment = alignment;
        text.fontStyle = style;
        text.rectTransform.sizeDelta = GetSize() - new Vector2(0.206f, 0.1f);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.text = rawText;
        text.fontSize = text.fontSizeMax = text.fontSizeMin = fontSize;
        text.m_fontSizeBase = 3f;
    }
}

public class MSSprite : MetaScreenContent
{
    protected Utilities.SpriteLoader sprite;
    protected float margin;
    protected float scale;
    public override Vector2 GetSize() => (Vector2)sprite.GetSprite().bounds.size * scale + new Vector2(margin, margin);
    public SpriteRenderer renderer;

    public MSSprite(Utilities.SpriteLoader sprite, float margin, float scale)
    {
        this.sprite = sprite;
        this.margin = margin;
        this.scale = scale;
    }

    public override void Generate(GameObject obj)
    {
        this.renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite.GetSprite();
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}


public class MSButton : MSString
{
    public Color? color { get; }
    private float height { get; }
    private Action onClick { get; }
    public override Vector2 GetSize() => new Vector2(width + 0.1f, height + 0.12f);
    public PassiveButton? button { get; private set; }
    public MSButton(float width, float height, string text, TMPro.FontStyles style, Action onClick, Color? color = null) :
        base(width, text, TMPro.TextAlignmentOptions.Center, style)
    {
        this.color = color;
        this.height = height;
        this.onClick = onClick;
    }

    public override void Generate(GameObject obj)
    {
        button = MetaDialog.MSDesigner.SetUpButton(obj, new Vector2(width, height), rawText, color);
        button.OnClick.AddListener((UnityEngine.Events.UnityAction)onClick);
        var text = button.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.fontStyle = style;
        this.text = text;
    }
}

public class MSTextInput : MetaScreenContent
{
    private float width;
    private float height;
    public override Vector2 GetSize() => new Vector2(width + 0.3f, height + 0.3f);
    private TMPro.TextAlignmentOptions alignmentOptions;
    private TMPro.FontStyles fontStyles;
    public float FontSize;
    public TextInputField TextInputField;
    public MSTextInput(float width, float height, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style) 
    {
        this.width = width;
        this.height = height;
        this.alignmentOptions = alignment;
        this.fontStyles = style;
        this.FontSize = 1.5f;
    }

    public override void Generate(GameObject obj)
    {
        TextInputField = obj.AddComponent<TextInputField>();
        TextInputField.SetTextProperty(new Vector2(width,height), FontSize, alignmentOptions,fontStyles);
    }
}

public class MSRadioButton : MSString
{
    public Action<bool>? FlagUpdateAction = null;
    private bool flag;
    public bool Flag { get { return flag; } set { flag = value; OnFlagChanged(); } }
    private void OnFlagChanged()
    {
        RadioButton.text = Flag ? "◉" : "○";
        if (FlagUpdateAction != null) FlagUpdateAction.Invoke(flag);
    }
    public override Vector2 GetSize() => new Vector2(width + 0.36f, 0.5f);

    private TMPro.TextMeshPro RadioButton;
    
    public MSRadioButton(bool flag,float width, string text,float fontSize,float fontSizeMin, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style):
        base(width,text,fontSize,fontSizeMin,alignment,style)
    {
        this.flag = flag;
    }


    public override void Generate(GameObject obj)
    {
        base.Generate(obj);

        text.transform.localPosition += new Vector3(0.15f,0f);

        RadioButton = GameObject.Instantiate(RuntimePrefabs.TextPrefab/*HudManager.Instance.Dialogue.target*/);
        RadioButton.transform.SetParent(obj.transform);
        RadioButton.transform.localScale = new Vector3(1f, 1f, 1f);
        RadioButton.transform.localPosition = new Vector3(0.08f - width / 2f, 0f, -1f);

        RadioButton.alignment = TMPro.TextAlignmentOptions.Center;
        RadioButton.fontStyle = TMPro.FontStyles.Bold;
        RadioButton.rectTransform.sizeDelta = new Vector2(0.4f, 0.4f);
        RadioButton.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        RadioButton.fontSize = RadioButton.fontSizeMax = RadioButton.fontSizeMin = fontSize;
        RadioButton.m_fontSizeBase = 3f;
        OnFlagChanged();

        PassiveButton button = RadioButton.gameObject.AddComponent<PassiveButton>();
        button.OnMouseOut = new UnityEngine.Events.UnityEvent();
        button.OnMouseOver = new UnityEngine.Events.UnityEvent();
        button.OnClick.RemoveAllListeners();
        button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
            Flag = !Flag;
            OnFlagChanged();
            SoundManager.Instance.PlaySound(MetaDialog.getSelectClip(), false, 0.8f);
        }));
        button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() => {
            RadioButton.color = Palette.AcceptedGreen;
            SoundManager.Instance.PlaySound(MetaDialog.getHoverClip(), false, 0.8f);
        }));
        button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => {
            RadioButton.color = Color.white;
        }));

        BoxCollider2D collider = RadioButton.gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.25f, 0.25f);
    }
}

public class MetaScreen
{
    static private Sprite? buttonSprite = null;
    static private AudioClip? audioHover = null;
    static private AudioClip? audioSelect = null;
    static private SpriteLoader playerMask = new SpriteLoader("Nebula.Resources.PlayerMask.png", 100f);
    static public Sprite GetButtonBackSprite()
    {
        if (buttonSprite == null) buttonSprite = Helpers.getSpriteFromAssets("buttonClick");
        return buttonSprite!;
    }

    static public AudioClip? getHoverClip()
    {
        if (audioHover == null) audioHover = Helpers.FindSound("UI_Hover");
        return audioHover;
    }

    static public AudioClip? getSelectClip()
    {
        if (audioSelect == null) audioSelect = Helpers.FindSound("UI_Select");
        return audioSelect;
    }

    public class MSDesigner
    {
        public MetaScreen screen { get; private set; }
        public Vector2 size { get; private set; }
        private Vector2 origin;
        private float used;
        private Vector2 center;
        public Vector2 CurrentOrigin { get { return origin - new Vector2(0f, used); } }
        public float Used { get => used; }

        public void CustomUse(float used) { this.used += used; }

        private MSDesigner(MetaScreen screen, Vector2 size, Vector2 origin)
        {
            this.screen = screen;
            this.size = size;
            this.origin = origin;
            this.used = 0f;
            center = new Vector2(origin.x + size.x * 0.5f, origin.y - size.y * 0.5f);
        }

        public MSDesigner(MetaScreen screen, Vector2 size, float titleHeight)
        {
            this.screen = screen;
            this.size = size;
            origin = new Vector2(-size.x / 2f, size.y / 2f);
            used += titleHeight;
            center = new Vector2(origin.x + size.x * 0.5f, origin.y - size.y * 0.5f);
        }

        static private SpriteLoader PseudoBackgroundSprite = new SpriteLoader("Nebula.Resources.ColorFullBase.png", 100f);
        public PassiveButton MakeIntoPseudoScreen()
        {
            var renderer = screen.screen.AddComponent<SpriteRenderer>();
            renderer.sprite = MetaScreen.GetButtonBackSprite();
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size + new Vector2(0.08f, 0.08f);

            var collider2D = screen.screen.AddComponent<BoxCollider2D>();
            collider2D.size = new Vector2(100f,100f);

            var back = new GameObject("Background").AddComponent<SpriteRenderer>();
            back.sprite = PseudoBackgroundSprite.GetSprite();
            back.color = new Color(0, 0, 0, 0.5f);
            back.transform.SetParent(renderer.transform);
            back.transform.localScale = new Vector2(100f, 100f);
            back.transform.localPosition = new Vector3(0f, 0f, 1f);

            return screen.screen.SetUpButton(null);
        }

        static public PassiveButton SetUpButton(GameObject obj, Vector2 size, string display, Color? color = null)
        {
            Color normalColor = (color == null) ? Color.white : color.Value;

            obj.layer = Nebula.LayerExpansion.GetUILayer();
            obj.transform.localScale = new Vector3(1f, 1f, 1f);
            var renderer = obj.AddComponent<SpriteRenderer>();
            var collider = obj.AddComponent<BoxCollider2D>();

            var text = GameObject.Instantiate(RuntimePrefabs.TextPrefab/*HudManager.Instance.Dialogue.target*/);
            //var text = GameObject.Instantiate(HudManager.Instance.Dialogue.target);
            text.transform.SetParent(obj.transform);
            text.transform.localScale = new Vector3(1f, 1f, 1f);
            text.transform.localPosition = new Vector3(0f, 0f, -1f);

            renderer.sprite = MetaScreen.GetButtonBackSprite();
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.color = normalColor;

            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.text = display;
            text.fontSizeMax = 2f;
            text.fontSizeMin = 1f;
            text.m_fontSizeBase = 3f;
            text.fontSize = 2f;
            text.enableAutoSizing = true;

            text.rectTransform.sizeDelta = new Vector2(size.x - 0.15f, size.y - 0.12f);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            collider.size = size;

            PassiveButton button = obj.AddComponent<PassiveButton>();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            button.OnMouseOut = new UnityEngine.Events.UnityEvent();
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                renderer.color = new Color(0f, 1f, 42f / 255f);
                SoundManager.Instance.PlaySound(MetaDialog.getHoverClip(), false, 0.8f);
            }));
            button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                renderer.color = normalColor;
            }));
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                SoundManager.Instance.PlaySound(MetaDialog.getSelectClip(), false, 0.8f);
            }));

            return button;
        }

        static public PassiveButton AddSubButton(GameObject parent, Vector2 size, string name, string display, Color? normalColor = null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            obj.transform.localPosition = new Vector3(0, 0, -5f);
            var result = SetUpButton(obj, size, display, normalColor);

            return result;
        }

        static public PassiveButton AddSubButton(PassiveButton button, Vector2 size, string name, string display)
        {
            return AddSubButton(button.gameObject, size, name, display);
        }

        static public TMPro.TextMeshPro AddSubText(GameObject obj, float width, float fontsize, string display, TMPro.FontStyles style, TMPro.TextAlignmentOptions alignment)
        {
            TMPro.TextMeshPro text = GameObject.Instantiate(RuntimePrefabs.TextPrefab/*HudManager.Instance.Dialogue.target*/);
            text.transform.SetParent(obj.transform);
            text.transform.localPosition = new Vector3(0, 0, -5f);
            text.text = display;
            text.alignment = alignment;
            text.fontStyle = style;
            text.rectTransform.sizeDelta = new Vector2(width, 0.4f);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.text = display;
            text.fontSize = text.fontSizeMax = fontsize;
            text.fontSizeMin = 2f;
            text.m_fontSizeBase = 3f;

            return text;
        }

        static public TMPro.TextMeshPro AddSubText(PassiveButton button, float width, float fontsize, string display)
        {
            return AddSubText(button.gameObject, width, fontsize, display, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.Center);
        }

        public PassiveButton AddButton(float width, string name, string display)
        {
            return AddButton(new Vector2(width, 0.4f), name, display);
        }

        public PassiveButton AddButton(Vector2 size, string name, string display)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(screen.screen.transform);
            var button = SetUpButton(obj, size, display);

            obj.transform.localPosition = new Vector3(center.x, CurrentOrigin.y - 0.06f - size.y * 0.5f, -10f);
            used += 0.12f + size.y;

            return button;
        }

        public PassiveButton AddPlayerButton(float width, PlayerControl player, bool modifyTextPosition)
        {
            return AddPlayerButton(new Vector2(width, 0.4f), player, modifyTextPosition);
        }

        public PassiveButton AddPlayerButton(Vector2 size, PlayerControl player, bool modifyTextPosition)
        {
            var button = AddButton(size, player.name, player.name);

            if (modifyTextPosition) button.transform.GetChild(0).localPosition += new Vector3(0.2f, 0f, 0f);

            GameObject obj = new GameObject("Icon");
            obj.transform.SetParent(button.transform);
            obj.transform.localPosition = new Vector3(-size.x / 2f + 0.3f, 0f, 0f);
            obj.transform.localScale = new Vector3(1.3f, 0.6f, 1f);
            obj.layer = LayerExpansion.GetUILayer();
            var mask = obj.AddComponent<SpriteMask>();
            mask.sprite = MetaDialog.playerMask.GetSprite();

            var poolable = GameObject.Instantiate(Patches.IntroCutsceneOnDestroyPatch.PlayerPrefab, button.transform);
            poolable.SetPlayerDefaultOutfit(player);
            poolable.cosmetics.SetMaskType(PlayerMaterial.MaskType.SimpleUI);

            poolable.gameObject.layer = LayerExpansion.GetUILayer();
            poolable.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            poolable.transform.localPosition = new Vector3(-size.x / 2f + 0.3f, -0.1f, 0f);

            return button;
        }

        public void AddTopic(params MetaScreenContent[] contents)
        {
            float width = 0f;
            float maxHeight = 0f;
            foreach (var c in contents)
            {
                var vec = c.GetSize();
                width += vec.x;
                float h = vec.y;
                if (h > maxHeight) maxHeight = h;
            }

            float w = -width * 0.5f;
            foreach (var c in contents)
            {
                var vec = c.GetSize();
                GameObject obj = new GameObject("Content");
                obj.layer = LayerExpansion.GetUILayer();
                obj.transform.SetParent(screen.screen.transform);

                obj.transform.localPosition = new Vector3(center.x + w + vec.x * 0.5f, CurrentOrigin.y - maxHeight * 0.5f, -10f);
                w += vec.x;

                c.Generate(obj);
                c.PostBuilder?.Invoke(obj);
            }

            used += maxHeight;
        }

        public delegate string NumericToString(int n);
        public void AddNumericDataTopic(string display, int currentValue, NumericToString converter, int min, int max, Action<int> applyFunc)
        {
            var valTxt = new MSString(0.8f, converter(currentValue), TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
            int val = currentValue;
            AddTopic(
                new MSString(2f, display + ":", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                new MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Normal, () =>
                {
                    val = Mathf.Clamp(--val, min, max);
                    valTxt.text.text = converter(val);
                }),
                valTxt,
                new MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Normal, () =>
                {
                    val = Mathf.Clamp(++val, min, max);
                    valTxt.text.text = converter(val);
                }),
                new MSMargin(0.4f),
                new MSButton(0.8f, 0.5f, "Apply", TMPro.FontStyles.Bold, () =>
                {
                    applyFunc(val);
                })
                );
        }

        public void AddNumericDataTopic(string display, int currentValue, string suffix, int min, int max, Action<int> applyFunc)
        {
            AddNumericDataTopic(display, currentValue, (n) => n.ToString() + suffix, min, max, applyFunc);
        }

        public void AddNumericDataTopic(string display, int currentValue, string[] replace, int min, int max, Action<int> applyFunc)
        {
            AddNumericDataTopic(display, currentValue, (n) => replace[n], min, max, applyFunc);
        }

        public void AddPageTopic(int currentPage, bool hasPrev, bool hasNext, Action<int> changePageFunc)
        {
            Module.MetaScreenContent prev;
            if (hasPrev) prev = new Module.MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () => changePageFunc(-1));
            else prev = new Module.MSMargin(0.5f);

            Module.MetaScreenContent next;
            if (hasNext) next = new Module.MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () => changePageFunc(1));
            else next = new Module.MSMargin(0.5f);

            AddTopic(prev, new Module.MSString(0.5f, (currentPage + 1).ToString(), TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold), next);
        }

        public void AddPageListTopic(int currentPage, int pages, Action<int> changePageFunc)
        {
            Module.MetaScreenContent[] contents = new MetaScreenContent[pages];

            for (int i = 0; i < pages; i++)
            {
                int index = i;
                contents[i] = new MSButton(0.3f, 0.3f, (i + 1).ToString(), TMPro.FontStyles.Normal, () => { changePageFunc(index); }, (i == currentPage) ? new Color(0.5f, 0.5f, 0.5f) : Color.white);
            }
            AddTopic(contents.ToArray());

            foreach (var content in contents)
            {
                ((MSButton)content).text.rectTransform.sizeDelta *= 2f;
            }

        }

        public void AddEnumerableTopic(int contentsPerRow, int rowsPerPage, int page, IEnumerator<MetaScreenContent> enumerator, Action<MetaScreenContent> onGenerated, Action<int>? changePageFunc = null)
        {
            List<MetaScreenContent> contents = new List<MetaScreenContent>();

            void generate()
            {
                AddTopic(contents.ToArray());
                foreach (var c in contents) onGenerated(c);
                contents.Clear();
            }

            int left = rowsPerPage * contentsPerRow;
            int skip = page * rowsPerPage * contentsPerRow;

            bool hasPrev = skip > 0;
            bool hasNext = false;

            while (enumerator.MoveNext())
            {
                if (skip > 0)
                {
                    skip--;
                    continue;
                }

                if (left <= 0)
                {
                    hasNext = true;
                    break;
                }

                contents.Add(enumerator.Current);
                if (contents.Count == 5) generate();

                left--;
            }

            if (contents.Count > 0) generate();

            if (changePageFunc != null)
            {
                AddPageTopic(page, hasPrev, hasNext, changePageFunc);
            }
        }

        public void AddGhostRoleTopic(Predicate<Roles.GhostRole> predicate, Action<Roles.GhostRole> onClicked, int contentsPerRow = 5, int maxRows = 100, int page = 0, Action<int>? changePageFunc = null)
        {
            IEnumerator<MetaScreenContent> enumerator()
            {
                foreach (var r in Roles.Roles.AllGhostRoles)
                {
                    if (!predicate(r)) continue;
                    Roles.GhostRole ghostRole = r;
                    yield return new MSButton(1.65f, 0.36f,
                    Helpers.cs(r.Color, Language.Language.GetString("role." + r.LocalizeName + ".name")),
                    TMPro.FontStyles.Bold,
                    () => onClicked(ghostRole));
                }
            }

            AddEnumerableTopic(contentsPerRow, maxRows, page, enumerator(), (c) =>
            {
                var text = ((MSButton)c).text;
                text.fontSizeMin = 0.5f;
                text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            }, changePageFunc);
        }

        public void AddModifyTopic(Predicate<Roles.ExtraRole> predicate, Action<Roles.ExtraRole> onClicked, int contentsPerRow = 5, int maxRows = 100, int page = 0, Action<int>? changePageFunc = null)
        {
            IEnumerator<MetaScreenContent> enumerator()
            {
                foreach (var r in Roles.Roles.AllExtraRoles)
                {
                    if (!predicate(r)) continue;
                    Roles.ExtraRole extraRole = r;
                    yield return new MSButton(1.65f, 0.36f,
                    Helpers.cs(r.Color, Language.Language.GetString("role." + r.LocalizeName + ".name")),
                    TMPro.FontStyles.Bold,
                    () => onClicked(extraRole));
                }
            }

            AddEnumerableTopic(contentsPerRow, maxRows, page, enumerator(), (c) =>
            {
                var text = ((MSButton)c).text;
                text.fontSizeMin = 0.5f;
                text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            }, changePageFunc);
        }

        public void AddRolesTopic(Predicate<Roles.Role> predicate, Action<Roles.Role> onClicked, int contentsPerRow = 5, int maxRows = 100, int page = 0, Action<int>? changePageFunc = null)
        {
            IEnumerator<MetaScreenContent> enumerator()
            {
                foreach (var r in Roles.Roles.AllRoles)
                {
                    if (!predicate(r)) continue;
                    Roles.Role role = r;
                    yield return new MSButton(1.65f, 0.36f,
                    Helpers.cs(r.Color, Language.Language.GetString("role." + r.LocalizeName + ".name")),
                    TMPro.FontStyles.Bold,
                    () => onClicked(role));
                }
            }

            AddEnumerableTopic(contentsPerRow, maxRows, page, enumerator(), (c) =>
            {
                var text = ((MSButton)c).text;
                text.fontSizeMin = 0.5f;
                text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            }, changePageFunc);
        }


        public MSDesigner[] Split(int division)
        {
            MSDesigner[] result = new MSDesigner[division];

            for (int i = 0; i < division; i++)
            {
                result[i] = new MSDesigner(screen, new Vector2(size.x / (float)division, size.y - used),
                    origin + new Vector2((float)i * size.x / (float)division, -used));
            }

            return result;
        }

        public MSDesigner[] Split(int division, float margin)
        {
            MSDesigner[] result = new MSDesigner[division];

            for (int i = 0; i < division; i++)
            {
                result[i] = new MSDesigner(screen, new Vector2((size.x - (margin * 2f)) / (float)division, size.y - used),
                    origin + new Vector2(margin + (float)i * (size.x - margin * 2f) / (float)division, -used));
            }

            return result;
        }

        public MSDesigner[] SplitVertically(float[] ratios)
        {
            float sum = 0f;
            foreach (var ratio in ratios) sum += ratio;

            MSDesigner[] result = new MSDesigner[ratios.Length];

            float x = 0f;
            for (int i = 0; i < ratios.Length; i++)
            {
                result[i] = new MSDesigner(screen, new Vector2(size.x * ratios[i] / sum, size.y - used),
                    origin + new Vector2(x, -used));
                x += size.x * ratios[i] / sum;
            }

            return result;
        }

        public MSDesigner[] SplitHorizontally(float[] ratios)
        {
            float sum = 0f;
            foreach (var ratio in ratios) sum += ratio;

            MSDesigner[] result = new MSDesigner[ratios.Length];

            float y = 0f;
            for (int i = 0; i < ratios.Length; i++)
            {
                result[i] = new MSDesigner(screen, new Vector2(0, (size.y - used) * ratios[i] / y),
                    origin + new Vector2(0, -used - y));
                y += size.y * ratios[i] / sum;
            }
            return result;
        }
    }

    public GameObject screen { get; }

    public MetaScreen(GameObject screen)
    {
        this.screen = screen;
    }

    public virtual void Close()
    {
        GameObject.Destroy(screen);
    }

    static public MSDesigner OpenScreen(GameObject parent, Vector2 size, Vector2 center)
    {
        GameObject screen = new GameObject("Screen");
        screen.transform.SetParent(parent.transform);

        screen.transform.localScale = new Vector3(1, 1, 1);
        screen.transform.localPosition = new Vector3(center.x, center.y, -50f);
        var metaScreen = new MetaScreen(screen);

        return new MSDesigner(metaScreen, size, 0.2f);
    }
}