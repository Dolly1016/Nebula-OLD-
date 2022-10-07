using Nebula.Objects;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.MetaRoles
{
    public class VOID : Role
    {
        static public Color RoleColor = new Color(173f / 255f, 173f / 255f, 198f / 255f);

        private Module.CustomOption killerCanKnowBaitKillByFlash;

        public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
        {
            initialTasks.Clear();
            actualTasks = null;
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            __instance.Die(DeathReason.Exile);
        }

        public override void Initialize(PlayerControl __instance)
        {
            Game.GameData.data.myData.CanSeeEveryoneInfo = true;
            dialogOrder.Clear();
        }


        public override void LoadOptionData()
        {

        }

        //今出現しているダイアログ
        public List<DialogueBox> dialogOrder;
        //アクティブなダイアログ
        public DialogueBox? activeDialogue { get => dialogOrder.Count == 0 ? null : dialogOrder[dialogOrder.Count - 1]; }

        private SpriteLoader voidButtonSprite = new SpriteLoader("Nebula.Resources.VOIDButton.png", 115f);
        private CustomButton voidButton;

        private Sprite? buttonSprite = null;
        private Sprite? getButtonBackSprite()
        {
            if (buttonSprite == null) buttonSprite = Helpers.getSpriteFromAssets("buttonClick");
            return buttonSprite;
        }
        public class DialogueDesigner
        {
            private DialogueBox dialogue;
            private SpriteRenderer background;
            private Vector2 size;
            private Vector2 origin;
            private Vector2 center;

            public DialogueDesigner(DialogueBox dialog,SpriteRenderer background,Vector2 size,float titleHeight)
            {
                dialogue = dialog;
                this.background = background;
                this.size = size;
                origin = new Vector2(-size.x / 2f, size.y / 2f);
                origin -= new Vector2(0f, titleHeight);
                center = new Vector2(0.0f, 0.0f); 
            }

            public PassiveButton AddButton(float width,string name,string display)
            {
                GameObject obj = new GameObject(name);
                obj.layer = Nebula.LayerExpansion.GetUILayer();
                obj.transform.SetParent(dialogue.transform);
                obj.transform.localScale=new Vector3(1f,1f,1f);
                var renderer = obj.AddComponent<SpriteRenderer>();
                var collider = obj.AddComponent<BoxCollider2D>();
                var text = GameObject.Instantiate(dialogue.target);
                text.transform.SetParent(obj.transform);
                text.transform.localScale = new Vector3(1f,1f,1f);

                renderer.sprite = Roles.VOID.getButtonBackSprite();
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size=new Vector2(width,0.4f);

                text.transform.localScale = new Vector3(1f,1f,1f);
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(width,0f);
                text.rectTransform.pivot=new Vector2(0.5f,0.5f);
                text.text = display;
                text.fontSize = text.fontSizeMax = text.fontSizeMax = 3.5f;

                collider.size = new Vector2(width, 0.4f);

                obj.transform.localPosition = new Vector3(0f, origin.y - 0.26f , -10f);
                origin -= new Vector2(0, 0.52f);

                return obj.AddComponent<PassiveButton>();
            }
        }
        public DialogueDesigner OpenDialog(Vector2 size,string title)
        {
            DialogueBox dialogue = GameObject.Instantiate(HudManager.Instance.Dialogue);
            dialogue.name = "VOID Dialogue" + dialogOrder.Count;
            dialogue.transform.SetParent(activeDialogue?.transform ?? HudManager.Instance.transform);
            SpriteRenderer renderer = dialogue.gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
            SpriteRenderer closeButton = dialogue.gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
            GameObject fullScreen = renderer.transform.GetChild(0).gameObject;
            fullScreen.GetComponent<PassiveButton>().OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            fullScreen.GetComponent<SpriteRenderer>().color = new Color(0f,0f,0f,0.35f);
            renderer.gameObject.AddComponent<BoxCollider2D>().size = size;
            renderer.color = new Color(1f, 1f, 1f, 0.8f);
            renderer.size = size;

            closeButton.transform.localPosition = new Vector3(-size.x/2f -0.3f, size.y/2f - 0.3f, -10f);
            dialogue.transform.localScale = new Vector3(1, 1, 1);
            dialogue.transform.localPosition = new Vector3(0f, 0f, -50f);
            dialogOrder.Add(dialogue);

            dialogue.target.rectTransform.sizeDelta = size * 1.66f - new Vector2(0.7f,0.7f);
            dialogue.Show(title);

            return new DialogueDesigner(dialogue, renderer, size,dialogue.target.GetPreferredHeight());
        }

        public override void MyUpdate()
        {
            for(int i = 0; i < dialogOrder.Count; i++)
            {
                dialogOrder[i].BackButton.gameObject.SetActive(i == dialogOrder.Count - 1);

                if (dialogOrder[i].gameObject.activeSelf) continue;

                GameObject.Destroy(dialogOrder[i].gameObject);
                dialogOrder.RemoveRange(i, dialogOrder.Count - i);

                break;
            }
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (voidButton != null)
            {
                voidButton.Destroy();
            }
            voidButton = new CustomButton(
                () =>
                {
                    var d = OpenDialog(new Vector2(8.5f, 5.1f), "Dialogue 0");
                    d.AddButton(1f, "button1", "ボタン1");
                    d.AddButton(1f, "button2", "ボタン2");
                    OpenDialog(new Vector2(5.0f, 2.0f), "Dialogue 1").AddButton(2f, "button", "前面のボタン");
                },
                () => true,
                () => dialogOrder.Count == 0,
                () => { },
                voidButtonSprite.GetSprite(),
                new Vector3(-1.8f, 0, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.void"
            ).SetTimer(0f);
        }

        public override void CleanUp()
        {
            if (voidButton != null)
            {
                voidButton.Destroy();
                voidButton = null;
            }
        }

        public VOID()
            : base("VOID", "void", RoleColor, RoleCategory.Neutral, Side.VOID, Side.VOID,
                 new HashSet<Side>(), new HashSet<Side>(), new HashSet<Patches.EndCondition>(),
                 true, VentPermission.CanNotUse, false, false, false)
        {
            DefaultCanBeLovers = false;
            DefaultCanBeDrunk = false;
            DefaultCanBeGuesser = false;
            DefaultCanBeMadmate = false;
            DefaultCanBeSecret = false;

            Allocation = AllocationType.Switch;
            FixedRoleCount = true;

            IsGuessableRole = false;

            dialogOrder = new List<DialogueBox>();
        }
    }
}
