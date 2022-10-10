using Nebula.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    public class MetaDialogButton : MetaDialogString
    {
        private float height { get; }
        private Action onClick { get; }
        public override Vector2 GetSize() => new Vector2(width + 0.1f, height + 0.12f);
        public PassiveButton? button { get; private set; }
        public MetaDialogButton(float width, float height,string text, TMPro.FontStyles style,Action onClick):
            base(width,text,TMPro.TextAlignmentOptions.Center,style)
        {
            this.height = height;
            this.onClick = onClick;
        }

        public override void Generate(GameObject obj)
        {
            button = MetaDialog.MetaDialogDesigner.SetUpButton(obj, new Vector2(width, height), rawText);
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
            private Vector2 size;
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

            static public PassiveButton SetUpButton(GameObject obj, Vector2 size,string display)
            {
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
                    renderer.color = Palette.White;
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
                    obj.transform.SetParent(dialog.dialog.transform);

                    obj.transform.localPosition = new Vector3(w + vec.x * 0.5f, CurrentOrigin.y - maxHeight * 0.5f, -10f);
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
                    new MetaDialogButton(0.8f, 0.4f, "Apply", TMPro.FontStyles.Bold, () =>
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

            public void AddModifyTopic(Predicate<Roles.ExtraRole> predicate,Action<Roles.ExtraRole> onClicked)
            {
                List<MetaDialogContent> roles = new List<MetaDialogContent>();

                foreach(var r in Roles.Roles.AllExtraRoles)
                {
                    if (!predicate(r)) continue;

                    Roles.ExtraRole extraRole = r;
                    roles.Add(new MetaDialogButton(1.65f, 0.36f,
                        Helpers.cs(r.Color,Language.Language.GetString("role."+r.LocalizeName+".name")),
                        TMPro.FontStyles.Bold,
                        ()=> onClicked(extraRole)));

                    if (roles.Count == 5)
                    {
                        AddTopic(roles.ToArray());
                        foreach(var c in roles)
                        {
                            var text =((MetaDialogButton)c).text;
                            text.fontSizeMin = 0.5f;
                            text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                        }
                        roles.Clear();
                    }
                }

                if (roles.Count > 0)
                {
                    AddTopic(roles.ToArray());
                    foreach (var c in roles)
                    {
                        var text = ((MetaDialogButton)c).text;
                        text.fontSizeMin = 0.5f;
                        text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                    }
                }
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
                    x += ratios[i] / sum;
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
                    y += ratios[i] / sum;
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
            var metaDialog = new MetaDialog(dialogue);
            dialogOrder.Add(metaDialog);

            dialogue.target.rectTransform.sizeDelta = size * 1.66f - new Vector2(0.7f, 0.7f);
            dialogue.Show(title);

            return new MetaDialogDesigner(metaDialog, renderer, size, dialogue.target.GetPreferredHeight() + 0.1f);
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

            Module.MetaDialogContent prev;
            if (page > 0)
            {
                prev = new Module.MetaDialogButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () =>
                {
                    designer.dialog.Close();
                    OpenRolesDialog(roleCondition,page - 1, rolesPerPage,onClick);
                });
            }
            else
            {
                prev = new Module.MetaDialogMargin(0.5f);
            }

            Module.MetaDialogContent next;
            if (hasNext)
            {
                next = new Module.MetaDialogButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () => {
                    designer.dialog.Close();
                    OpenRolesDialog(roleCondition,page + 1, rolesPerPage, onClick);
                });
            }
            else
            {
                next = new Module.MetaDialogMargin(0.5f);
            }

            designer.AddTopic(prev, new Module.MetaDialogString(0.5f, (page + 1).ToString(), TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold), next);

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

        public DialogueBox dialog { get; }
        public Action<MetaDialog>? updateFunc { get; set; }

        public MetaDialog(DialogueBox dialogueBox)
        {
            dialog = dialogueBox;
            updateFunc = null;
        }

    }
}
