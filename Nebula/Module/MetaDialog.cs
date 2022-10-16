using Nebula.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace Nebula.Module
{
    public class MetaDialogContent
    {
        public virtual Vector2 GetSize() { return new Vector3(0f, 0f); }

        public MetaDialogContent()
        {
        }

        public virtual void Generate(GameObject obj) { }
    }

    public class MetaDialogMargin : MetaDialogContent
    {
        protected float width { get; }

        public override Vector2 GetSize() => new Vector2(width, 0f);

        public MetaDialogMargin(float width)
        {
            this.width = width;
        }
    }

    public class MetaDialogString : MetaDialogContent
    {
        protected float width { get; }
        protected string rawText { get; }
        protected TMPro.TextAlignmentOptions alignment { get; }
        protected TMPro.FontStyles style { get; }
        public TMPro.TextMeshPro? text { get; protected set; }
        public override Vector2 GetSize() => new Vector2(width + 0.06f, 0.5f);
        
        public MetaDialogString(float width,string text,TMPro.TextAlignmentOptions alignment,TMPro.FontStyles style)
        {
            this.width = width;
            this.rawText = text;
            this.alignment = alignment;
            this.style = style;
        }

        public override void Generate(GameObject obj)
        {
            this.text = GameObject.Instantiate(HudManager.Instance.Dialogue.target);
            text.transform.SetParent(obj.transform);
            text.transform.localScale = new Vector3(1f, 1f, 1f);
            text.transform.localPosition = new Vector3(0f, 0f, -1f);

            text.alignment = alignment;
            text.fontStyle = style;
            text.rectTransform.sizeDelta = new Vector2(width-0.2f, 0.36f);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.text = rawText;
            text.fontSize = text.fontSizeMax = text.fontSizeMax = 3f;
        }
    }

    public class MetaDialogMultiString : MetaDialogContent
    {
        protected float width { get; }
        protected string rawText { get; }
        protected TMPro.TextAlignmentOptions alignment { get; }
        protected TMPro.FontStyles style { get; }
        public TMPro.TextMeshPro? text { get; protected set; }
        public float fontSize { get; protected set; }
        public override Vector2 GetSize() => new Vector2(width + 0.06f, 0.1f + 0.72f * fontSize / 6f * (float)(1 + rawText.Count((c) => c == '\n')));

        public MetaDialogMultiString(float width, float size,string text, TMPro.TextAlignmentOptions alignment, TMPro.FontStyles style)
        {
            this.width = width;
            this.fontSize = size;
            this.rawText = text;
            this.alignment = alignment;
            this.style = style;
        }

        public override void Generate(GameObject obj)
        {
            this.text = GameObject.Instantiate(HudManager.Instance.Dialogue.target);
            text.transform.SetParent(obj.transform);
            text.transform.localScale = new Vector3(1f, 1f, 1f);
            text.transform.localPosition = new Vector3(0f, 0f, -1f);

            text.alignment = alignment;
            text.fontStyle = style;
            text.rectTransform.sizeDelta = GetSize() - new Vector2(0.206f, 0.1f);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.text = rawText;
            text.fontSize = text.fontSizeMax = text.fontSizeMax = fontSize;
        }
    }

    public class MetaDialogSprite : MetaDialogContent
    {
        protected Utilities.SpriteLoader sprite;
        protected float margin;
        protected float scale;
        public override Vector2 GetSize() => (Vector2)sprite.GetSprite().bounds.size * scale + new Vector2(margin, margin);
        public SpriteRenderer renderer;

        public MetaDialogSprite(Utilities.SpriteLoader sprite, float margin,float scale)
        {
            this.sprite = sprite;
            this.margin = margin;
            this.scale = scale;
        }

        public override void Generate(GameObject obj)
        {
            this.renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite.GetSprite();
            renderer.transform.localScale = new Vector3(scale,scale,1f);
        }
    }


    public class MetaDialogButton : MetaDialogString
    {
        private Color? color { get; }
        private float height { get; }
        private Action onClick { get; }
        public override Vector2 GetSize() => new Vector2(width + 0.1f, height + 0.12f);
        public PassiveButton? button { get; private set; }
        public MetaDialogButton(float width, float height,string text, TMPro.FontStyles style,Action onClick,Color? color=null):
            base(width,text,TMPro.TextAlignmentOptions.Center,style)
        {
            this.color = color;
            this.height = height;
            this.onClick = onClick;
        }

        public override void Generate(GameObject obj)
        {
            button = MetaDialog.MetaDialogDesigner.SetUpButton(obj, new Vector2(width, height), rawText,color);
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)onClick);
            var text = button.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.fontStyle = style;
            this.text = text;
        }
    }

    public class MetaDialog
    {
        static private Sprite? buttonSprite = null;
        static private AudioClip? audioHover = null;
        static private AudioClip? audioSelect = null;
        static private SpriteLoader playerMask = new SpriteLoader("Nebula.Resources.PlayerMask.png", 100f);
        static private Sprite? getButtonBackSprite()
        {
            if (buttonSprite == null) buttonSprite = Helpers.getSpriteFromAssets("buttonClick");
            return buttonSprite;
        }

        static private AudioClip? getHoverClip()
        {
            if (audioHover == null) audioHover = Helpers.FindSound("UI_Hover");
            return audioHover;
        }

        static private AudioClip? getSelectClip()
        {
            if (audioSelect == null) audioSelect = Helpers.FindSound("UI_Select");
            return audioSelect;
        }

        static public List<MetaDialog> dialogOrder = new List<MetaDialog>();
        static public MetaDialog? activeDialogue { get => dialogOrder.Count == 0 ? null : dialogOrder[dialogOrder.Count - 1]; }
        
        static public void Update()
        {
            for (int i = 0; i < dialogOrder.Count; i++)
            {
                if (dialogOrder[i].updateFunc != null) dialogOrder[i].updateFunc(dialogOrder[i]);
                dialogOrder[i].dialog.BackButton.gameObject.SetActive(i == dialogOrder.Count - 1);

                if (dialogOrder[i].dialog.gameObject.activeSelf) continue;

                EraseDialog(dialogOrder[i]);

                break;
            }
        }

        static public void Initialize()
        {
            foreach (var dialog in dialogOrder.AsEnumerable().Reverse())
            {
                if (dialog.dialog)
                {
                    dialog.dialog.Hide();
                    GameObject.Destroy(dialog.dialog.gameObject);
                }
            }
            dialogOrder.Clear();
        }

        static public void EraseDialogAll()
        {
            Initialize();
        }

        /// <summary>
        /// 最前面から指定の数だけダイアログを閉じます。
        /// </summary>
        /// <param name="num"></param>
        static public void EraseDialog(int num)
        {
            for (int i = 0; i <num; i++)
            {
                dialogOrder[dialogOrder.Count-1-i].dialog.Hide();
                GameObject.Destroy(dialogOrder[dialogOrder.Count - 1 - i].dialog.gameObject);
            }
            dialogOrder.RemoveRange(dialogOrder.Count - num, num);
        }

        static public void EraseDialog(MetaDialog dialog)
        {
            if (!dialogOrder.Contains(dialog)) return;
            int index = dialogOrder.IndexOf(dialog);

            for (int i = dialogOrder.Count-1; i >=index; i--)
            {
                dialogOrder[i].dialog.Hide();
                GameObject.Destroy(dialogOrder[i].dialog.gameObject);
            }
            dialogOrder.RemoveRange(index,dialogOrder.Count-index);
        }

        public void Close()
        {
            EraseDialog(this);
        }

        

        public class MetaDialogDesigner
        {
            public MetaDialog dialog { get; private set; }
            private SpriteRenderer background;
            public Vector2 size { get; private set; }
            private Vector2 origin;
            private float used;
            private Vector2 center;
            public Vector2 CurrentOrigin { get { return origin - new Vector2(0f, used); } }
            public float Used { get => used; }

            public void CustomUse(float used) { this.used += used; }

            private MetaDialogDesigner(MetaDialog dialog, SpriteRenderer background, Vector2 size, Vector2 origin)
            {
                this.dialog = dialog;
                this.background = background;
                this.size = size;
                this.origin = origin;
                this.used = 0f;
                center = new Vector2(origin.x + size.x * 0.5f, origin.y - size.y * 0.5f);
            }

            public MetaDialogDesigner(MetaDialog dialog, SpriteRenderer background, Vector2 size, float titleHeight)
            {
                this.dialog = dialog;
                this.background = background;
                this.size = size;
                origin = new Vector2(-size.x / 2f, size.y / 2f);
                used += titleHeight;
                center = new Vector2(origin.x + size.x * 0.5f, origin.y - size.y * 0.5f);
            }

            static public PassiveButton SetUpButton(GameObject obj, Vector2 size,string display,Color? color=null)
            {
                Color normalColor = (color == null) ? Color.white : color.Value;

                obj.layer = Nebula.LayerExpansion.GetUILayer();
                obj.transform.localScale = new Vector3(1f, 1f, 1f);
                var renderer = obj.AddComponent<SpriteRenderer>();
                var collider = obj.AddComponent<BoxCollider2D>();
                var text = GameObject.Instantiate(HudManager.Instance.Dialogue.target);
                text.transform.SetParent(obj.transform);
                text.transform.localScale = new Vector3(1f, 1f, 1f);
                text.transform.localPosition = new Vector3(0f, 0f, -1f);

                renderer.sprite = MetaDialog.getButtonBackSprite();
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = size;
                renderer.color = normalColor;

                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(size.x - 0.15f, 0.2f);
                text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                text.text = display;
                text.fontSize = text.fontSizeMax = text.fontSizeMax = 2f;

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
            
            static public PassiveButton AddSubButton(PassiveButton button,Vector2 size,string name,string display)
            {
                GameObject obj = new GameObject(name);
                obj.transform.SetParent(button.transform);
                obj.transform.localPosition = new Vector3(0, 0, -5f);
                var result = SetUpButton(obj, size, display);

                return result;
            }

            static public TMPro.TextMeshPro AddSubText(PassiveButton button, float width, float fontsize,string display)
            {
                TMPro.TextMeshPro text = GameObject.Instantiate(HudManager.Instance.Dialogue.target);
                text.transform.SetParent(button.transform);
                text.transform.localPosition = new Vector3(0, 0, -5f);
                text.text = display;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(width, 0.4f);
                text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                text.text = display;
                text.fontSize = text.fontSizeMax = text.fontSizeMax = fontsize;

                return text;
            }

            public PassiveButton AddButton(float width, string name, string display)
            {
                return AddButton(new Vector2(width,0.4f),name,display);
            }

            public PassiveButton AddButton(Vector2 size, string name, string display)
            {
                GameObject obj = new GameObject(name);
                obj.transform.SetParent(dialog.dialog.transform);
                var button = SetUpButton(obj,size,display);

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

            public void AddTopic(params MetaDialogContent[] contents)
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
                foreach(var c in contents)
                {
                    var vec = c.GetSize();
                    GameObject obj = new GameObject("Content");
                    obj.layer = LayerExpansion.GetUILayer();
                    obj.transform.SetParent(dialog.dialog.transform);

                    obj.transform.localPosition = new Vector3(center.x + w + vec.x * 0.5f, CurrentOrigin.y - maxHeight * 0.5f, -10f);
                    w += vec.x;

                    c.Generate(obj);
                }

                used += maxHeight;
            }

            public delegate string NumericToString(int n);
            public void AddNumericDataTopic(string display, int currentValue, NumericToString converter, int min, int max, Action<int> applyFunc)
            {
                var valTxt = new MetaDialogString(0.8f, converter(currentValue), TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
                int val = currentValue;
                AddTopic(
                    new MetaDialogString(2f, display + ":", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                    new MetaDialogButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Normal, () =>
                    {
                        val = Mathf.Clamp(--val, min, max);
                        valTxt.text.text = converter(val);
                    }),
                    valTxt,
                    new MetaDialogButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Normal, () =>
                    {
                        val = Mathf.Clamp(++val, min, max);
                        valTxt.text.text = converter(val);
                    }),
                    new MetaDialogMargin(0.4f),
                    new MetaDialogButton(0.8f, 0.5f, "Apply", TMPro.FontStyles.Bold, () =>
                    {
                        applyFunc(val);
                    })
                    );
            }

            public void AddNumericDataTopic(string display,int currentValue,string suffix,int min,int max,Action<int> applyFunc)
            {
                AddNumericDataTopic(display, currentValue, (n) => n.ToString() + suffix, min, max, applyFunc);
            }

            public void AddNumericDataTopic(string display, int currentValue, string[] replace, int min, int max, Action<int> applyFunc)
            {
                AddNumericDataTopic(display, currentValue, (n) => replace[n], min, max, applyFunc);
            }

            public void AddPageTopic(int currentPage,bool hasPrev,bool hasNext,Action<int> changePageFunc)
            {
                Module.MetaDialogContent prev;
                if (hasPrev)prev = new Module.MetaDialogButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () => changePageFunc(-1));
                else prev = new Module.MetaDialogMargin(0.5f);
                
                Module.MetaDialogContent next;
                if (hasNext) next = new Module.MetaDialogButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () => changePageFunc(1));
                else next = new Module.MetaDialogMargin(0.5f);

                AddTopic(prev, new Module.MetaDialogString(0.5f, (currentPage + 1).ToString(), TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold), next);
            }

            public void AddPageListTopic(int currentPage, int pages, Action<int> changePageFunc)
            {
                Module.MetaDialogContent[] contents=new MetaDialogContent[pages];

                for (int i = 0; i < pages; i++)
                {
                    int index = i;
                    contents[i] = new MetaDialogButton(0.3f, 0.3f, i.ToString(), TMPro.FontStyles.Normal, () => { changePageFunc(index); }, (i == currentPage) ? new Color(0.5f, 0.5f, 0.5f) : Color.white);
                }
                AddTopic(contents.ToArray());
            }

            public void AddEnumerableTopic(int contentsPerRow, int rowsPerPage, int page, IEnumerator<MetaDialogContent> enumerator,Action<MetaDialogContent> onGenerated,Action<int>? changePageFunc=null)
            {
                List<MetaDialogContent> contents = new List<MetaDialogContent>();

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
                IEnumerator<MetaDialogContent> enumerator()
                {
                    foreach (var r in Roles.Roles.AllGhostRoles)
                    {
                        if (!predicate(r)) continue;
                        Roles.GhostRole ghostRole = r;
                        yield return new MetaDialogButton(1.65f, 0.36f,
                        Helpers.cs(r.Color, Language.Language.GetString("role." + r.LocalizeName + ".name")),
                        TMPro.FontStyles.Bold,
                        () => onClicked(ghostRole));
                    }
                }

                AddEnumerableTopic(contentsPerRow, maxRows, page, enumerator(), (c) => {
                    var text = ((MetaDialogButton)c).text;
                    text.fontSizeMin = 0.5f;
                    text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                }, changePageFunc);
            }

            public void AddModifyTopic(Predicate<Roles.ExtraRole> predicate,Action<Roles.ExtraRole> onClicked, int contentsPerRow=5, int maxRows = 100, int page = 0,Action<int>? changePageFunc=null)
            {
                IEnumerator<MetaDialogContent> enumerator()
                {
                    foreach (var r in Roles.Roles.AllExtraRoles)
                    {
                        if (!predicate(r)) continue;
                        Roles.ExtraRole extraRole = r;
                        yield return new MetaDialogButton(1.65f, 0.36f,
                        Helpers.cs(r.Color, Language.Language.GetString("role." + r.LocalizeName + ".name")),
                        TMPro.FontStyles.Bold,
                        () => onClicked(extraRole));
                    }
                }

                AddEnumerableTopic(contentsPerRow, maxRows, page, enumerator(),(c)=> {
                    var text = ((MetaDialogButton)c).text;
                    text.fontSizeMin = 0.5f;
                    text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                },changePageFunc);
            }

            public void AddRolesTopic(Predicate<Roles.Role> predicate, Action<Roles.Role> onClicked, int contentsPerRow = 5,int maxRows=100,int page=0, Action<int>? changePageFunc = null)
            {
                IEnumerator<MetaDialogContent> enumerator()
                {
                    foreach (var r in Roles.Roles.AllRoles)
                    {
                        if (!predicate(r)) continue;
                        Roles.Role role = r;
                        yield return new MetaDialogButton(1.65f, 0.36f,
                        Helpers.cs(r.Color, Language.Language.GetString("role." + r.LocalizeName + ".name")),
                        TMPro.FontStyles.Bold,
                        () => onClicked(role));
                    }
                }

                AddEnumerableTopic(contentsPerRow, maxRows, page, enumerator(), (c) => {
                    var text = ((MetaDialogButton)c).text;
                    text.fontSizeMin = 0.5f;
                    text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                },changePageFunc);
            }


            public MetaDialogDesigner[] Split(int division)
            {
                MetaDialogDesigner[] result = new MetaDialogDesigner[division];

                for (int i = 0; i < division; i++)
                {
                    result[i] = new MetaDialogDesigner(dialog, background, new Vector2(size.x / (float)division, size.y - used),
                        origin + new Vector2((float)i * size.x/ (float)division, -used));
                }

                return result;
            }

            public MetaDialogDesigner[] Split(int division,float margin)
            {
                MetaDialogDesigner[] result = new MetaDialogDesigner[division];

                for (int i = 0; i < division; i++)
                {
                    result[i] = new MetaDialogDesigner(dialog, background, new Vector2((size.x - (margin * 2f)) / (float)division, size.y - used),
                        origin + new Vector2(margin + (float)i * (size.x - margin * 2f) / (float)division, -used));
                }

                return result;
            }

            public MetaDialogDesigner[] SplitVertically(float[] ratios)
            {
                float sum = 0f;
                foreach (var ratio in ratios) sum += ratio;

                MetaDialogDesigner[] result = new MetaDialogDesigner[ratios.Length];

                float x = 0f;
                for (int i = 0; i < ratios.Length; i++)
                {
                    result[i] = new MetaDialogDesigner(dialog, background, new Vector2(size.x * ratios[i] / sum, size.y - used),
                        origin + new Vector2(x, -used));
                    x += size.x * ratios[i] / sum;
                }

                return result;
            }

            public MetaDialogDesigner[] SplitHorizontally(float[] ratios)
            {
                float sum = 0f;
                foreach (var ratio in ratios) sum += ratio;

                MetaDialogDesigner[] result = new MetaDialogDesigner[ratios.Length];

                float y = 0f;
                for (int i = 0; i < ratios.Length; i++)
                {
                    result[i] = new MetaDialogDesigner(dialog, background, new Vector2(0, (size.y - used) * ratios[i] / y),
                        origin + new Vector2(0, -used - y));
                    y += size.y * ratios[i] / sum;
                }
                return result;
            }
        }

        static public MetaDialogDesigner OpenDialog(Vector2 size, string title)
        {
            DialogueBox dialogue = GameObject.Instantiate(HudManager.Instance.Dialogue);
            dialogue.name = "Dialogue" + dialogOrder.Count;
            dialogue.transform.SetParent(activeDialogue?.dialog.transform ?? HudManager.Instance.transform);
            SpriteRenderer renderer = dialogue.gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
            SpriteRenderer closeButton = dialogue.gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
            GameObject fullScreen = renderer.transform.GetChild(0).gameObject;
            fullScreen.GetComponent<PassiveButton>().OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            fullScreen.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.35f);
            renderer.gameObject.AddComponent<BoxCollider2D>().size = size;
            renderer.color = new Color(1f, 1f, 1f, 0.8f);
            renderer.size = size;

            closeButton.transform.localPosition = new Vector3(-size.x / 2f - 0.3f, size.y / 2f - 0.3f, -10f);
            dialogue.transform.localScale = new Vector3(1, 1, 1);
            dialogue.transform.localPosition = new Vector3(0f, 0f, -50f);
            if (dialogue.transform.parent == HudManager.Instance.transform) dialogue.transform.localPosition += new Vector3(0, 0, -50f);
            var metaDialog = new MetaDialog(dialogue);
            dialogOrder.Add(metaDialog);

            dialogue.target.rectTransform.sizeDelta = size * 1.66f - new Vector2(0.7f, 0.7f);
            dialogue.Show(title);

            return new MetaDialogDesigner(metaDialog, renderer, size, title.Length > 0 ? dialogue.target.GetPreferredHeight() + 0.1f : 0.2f);
        }

        static public MetaDialogDesigner OpenPlayerDialog(Vector2 size,PlayerControl player)
        {
            var dialog = OpenDialog(size, player.name);

            var poolable = GameObject.Instantiate(Patches.IntroCutsceneOnDestroyPatch.PlayerPrefab);
            poolable.transform.SetParent(dialog.dialog.dialog.transform);
            poolable.gameObject.layer = LayerExpansion.GetUILayer();

            poolable.SetPlayerDefaultOutfit(player);
            poolable.cosmetics.SetMaskType(PlayerMaterial.MaskType.None);

            poolable.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            poolable.transform.localPosition = new Vector3(-size.x / 2f + 0.35f, size.y / 2f - 0.37f, 0f);


            dialog.dialog.dialog.target.transform.localPosition += new Vector3(0.4f, 0f, 0f);

            return dialog;
        }

        static public MetaDialogDesigner OpenRolesDialog(Predicate<Roles.Role> roleCondition,int page,int rolesPerPage,Action<Roles.Role> onClick)
        {
            var designer = Module.MetaDialog.OpenDialog(new Vector2(10.5f, 5.4f), "Roles");
            var designers = designer.Split(6, 0.14f);

            int skip = page * rolesPerPage;
            int index = 0;
            bool hasNext = false;
            foreach (var role in Roles.Roles.AllRoles)
            {
                if (!roleCondition(role)) continue;

                if (skip > 0)
                {
                    skip--;
                    continue;
                }
                if (index >= rolesPerPage)
                {
                    hasNext = true;
                    break;
                }

                var r = role;
                var button = designers[index % 6].AddButton(new Vector2(1.65f, 0.36f), role.Name, Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => onClick(r)));
                button.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshPro>().fontStyle = TMPro.FontStyles.Bold;
                designers[index % 6].CustomUse(-0.07f);
                index++;
            }

            designer.CustomUse(designers[0].Used);

            designer.AddPageTopic(page, page > 0, hasNext, (p) =>
            {
                designer.dialog.Close();
                OpenRolesDialog(roleCondition, page + p, rolesPerPage, onClick);
            });

            return designer;
        }

        static public MetaDialogDesigner OpenPlayersDialog(string display, Action<PlayerControl, PassiveButton> setUpFunc,Action<PlayerControl> onClicked)=>OpenPlayersDialog(display,0.4f,0f,setUpFunc,onClicked);
        static public MetaDialogDesigner OpenPlayersDialog(string display,float height,float margin,Action<PlayerControl,PassiveButton> setUpFunc,Action<PlayerControl> onClicked)
        {
            var designer = MetaDialog.OpenDialog(new Vector2(9f, (height+0.12f) * 5f + 1f+ margin), display);
            var designers = designer.Split(3, 0.2f);
            int i = 0;

            foreach (var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                PlayerControl p = player;

                var button = designers[i].AddPlayerButton(new Vector2(2.7f, height), p, true);
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    onClicked(p);
                }));
                setUpFunc(p, button);

                i = (i + 1) % 3;
            }
            designer.CustomUse(designers[0].Used);
            return designer;
        }

        static private void AddRoleInfo(MetaDialogDesigner designer,Roles.Assignable assignable) 
        {
            var designers = designer.SplitVertically(new float[] { 0.01f, 0.55f, 0.45f, 0.01f });
            designers[1].AddTopic(new MetaDialogString(designers[1].size.x, Helpers.cs(assignable.Color, Language.Language.GetString("role." + assignable.LocalizeName + ".name")), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Bold));
            designers[1].AddTopic(new MetaDialogMultiString(designers[1].size.x, 1.2f, Language.Language.GetString("role." + assignable.LocalizeName + ".info"), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Normal));
            foreach(var hs in assignable.helpSprite)
                designers[1].AddTopic(new MetaDialogSprite(hs.sprite,0.1f,hs.ratio),new MetaDialogMultiString(designers[1].size.x-0.8f,1.2f,Language.Language.GetString(hs.localizedName),TMPro.TextAlignmentOptions.Left,TMPro.FontStyles.Normal));
            
            if(assignable.AssignableOnHelp.TopOption != null) designers[2].AddTopic(new MetaDialogMultiString(designers[2].size.x, 1.4f, Module.GameOptionStringGenerator.optionsToString(assignable.AssignableOnHelp.TopOption), TMPro.TextAlignmentOptions.TopLeft, TMPro.FontStyles.Normal));
        }

        static public MetaDialogDesigner OpenAssignableHelpDialog(Roles.Assignable assignable)
        {
            var designer = MetaDialog.OpenDialog(new Vector2(8f, 4f),"");
            AddRoleInfo(designer,assignable);
            return designer;
        }

        static public MetaDialogDesigner OpenHelpDialog(int tab,int arg,List<string>? options=null)
        {
            var designer = MetaDialog.OpenDialog(new Vector2(9f, 5.5f),"");

            var rolesTab = new MetaDialogButton(1.2f, 0.4f,"Roles",TMPro.FontStyles.Bold,()=> {
                if (tab != 1)
                {
                    EraseDialog(designer.dialog);
                    OpenHelpDialog(1,0,options);
                }
            });

            var ghostRolesTab = new MetaDialogButton(1.2f, 0.4f, "Ghost", TMPro.FontStyles.Bold, () => {
                if (tab != 2)
                {
                    EraseDialog(designer.dialog);
                    OpenHelpDialog(2, 0, options);
                }
            });

            var modifiesTab = new MetaDialogButton(1.2f, 0.4f, "Modifies", TMPro.FontStyles.Bold, () => {
                if (tab != 3)
                {
                    EraseDialog(designer.dialog);
                    OpenHelpDialog(3, 0, options);
                }
            });

            var optionsTab = new MetaDialogButton(1.2f, 0.4f, "Options", TMPro.FontStyles.Bold, () => {
                if (tab != 4)
                {
                    EraseDialog(designer.dialog);
                    OpenHelpDialog(4, 0, options);
                }
            });

            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
            {
                var myTab = new MetaDialogButton(1.2f, 0.4f, "My Role", TMPro.FontStyles.Bold, () => {
                    if (tab != 0)
                    {
                        EraseDialog(designer.dialog);
                        OpenHelpDialog(0, 0, options);
                    }
                });

                designer.AddTopic(myTab,rolesTab, ghostRolesTab,modifiesTab, optionsTab);
            }
            else
            {
                if (tab == 0) tab = 1;
                designer.AddTopic(rolesTab, ghostRolesTab,modifiesTab, optionsTab);
            }

            //見出し
            designer.AddTopic(new MetaDialogString(4f,new string[] { "My Roles","Roles","Ghost Roles","Modifies","All Options"}[tab],TMPro.TextAlignmentOptions.Center,TMPro.FontStyles.Bold));

            switch (tab)
            {
                case 0:
                    Roles.Assignable? assignable = null;

                    IEnumerator<MetaDialogContent> myRoleEnumerator()
                    {
                        if (Game.GameData.data == null) yield break;
                        var data = Game.GameData.data.myData.getGlobalData();
                        if (data == null) yield break;

                        if (arg == 0) assignable = data.role;
                        yield return new MetaDialogButton(1.3f, 0.36f,
                       Helpers.cs(data.role.Color, Language.Language.GetString("role." + data.role.LocalizeName + ".name")),
                       TMPro.FontStyles.Bold,
                       () => { MetaDialog.EraseDialog(1); OpenHelpDialog(0, 0, options); });

                        if (arg == 1) assignable = (!data.IsAlive && data.role.CanHaveGhostRole && data.ghostRole != null) ? (Roles.Assignable)data.ghostRole : data.role;
                        if (!data.IsAlive && data.role.CanHaveGhostRole && data.ghostRole != null)
                        {
                            yield return new MetaDialogButton(1.3f, 0.36f,
                           Helpers.cs(data.ghostRole.Color, Language.Language.GetString("role." + data.ghostRole.LocalizeName + ".name")),
                           TMPro.FontStyles.Bold,
                           () => { MetaDialog.EraseDialog(1); OpenHelpDialog(0, 1, options); });
                        }

                        int index = 2;
                        foreach (var r in data.extraRole)
                        {
                            var extraRole = r;
                            int currentIndex = index;
                            if (arg == index) assignable = extraRole;
                            yield return new MetaDialogButton(1.3f, 0.36f,
                      Helpers.cs(extraRole.Color, Language.Language.GetString("role." + extraRole.LocalizeName + ".name")),
                      TMPro.FontStyles.Bold,
                      () => { MetaDialog.EraseDialog(1); OpenHelpDialog(0, currentIndex, options); });

                            index++;
                        }
                    }

                    designer.AddEnumerableTopic(6,1,0, myRoleEnumerator(), (c) => {
                        var text = ((MetaDialogButton)c).text;
                        text.fontSizeMin = 0.5f;
                        text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                    });

                    if(assignable!=null)AddRoleInfo(designer,assignable);

                    break;
                case 1:
                    designer.AddRolesTopic((r) => r.category != Roles.RoleCategory.Complex, (r) => OpenAssignableHelpDialog(r), 5, 6, arg, (p) => {
                        MetaDialog.EraseDialog(1);
                        OpenHelpDialog(tab, arg + p, options);
                    });
                    break;
                case 2:
                    designer.AddGhostRoleTopic((r) => true, (r) => OpenAssignableHelpDialog(r), 5, 6, arg, (p) => {
                        MetaDialog.EraseDialog(1);
                        OpenHelpDialog(tab, arg + p, options);
                    });
                    break;
                case 3:
                    designer.AddModifyTopic((r) => true, (r) => OpenAssignableHelpDialog(r), 5, 6, arg,(p)=> {
                        MetaDialog.EraseDialog(1);
                        OpenHelpDialog(tab, arg + p, options);
                        });
                    break;
                case 4:
                    if (options == null) options = GameOptionStringGenerator.GenerateString(20);

                    var designers = designer.SplitVertically(new float[] { 0.05f, 0.5f, 0.5f, 0.05f });

                    for (int i = 0; i < 2; i++) if (options.Count > i + arg * 2) designers[1+i].AddTopic(new MetaDialogMultiString(designers[i+1].size.x,1f,options[i+arg*2],TMPro.TextAlignmentOptions.TopLeft,TMPro.FontStyles.Normal));

                    designer.CustomUse(3.7f);
                    designer.AddPageListTopic(arg,(options.Count+1)/2,(p)=> {
                        MetaDialog.EraseDialog(1);
                        OpenHelpDialog(tab, p, options);
                    });
                    break;
            }

            return designer;
        }

        public DialogueBox dialog { get; }
        public Action<MetaDialog>? updateFunc { get; set; }

        public MetaDialog(DialogueBox dialogueBox)
        {
            dialog = dialogueBox;
            updateFunc = null;
        }

    }
}
