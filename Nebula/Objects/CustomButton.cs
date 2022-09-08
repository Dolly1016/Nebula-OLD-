using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Nebula.Utilities;

namespace Nebula.Objects
{
    public static class ButtonEffect
    {
        static public void ShowButtonText(this ActionButton button, string text)
        {
            TMPro.TextMeshPro textObj = GameObject.Instantiate(button.cooldownTimerText,button.cooldownTimerText.transform.parent);
            textObj.color = Color.white;
            textObj.transform.localScale = new Vector3(0.7f,0.7f);
            textObj.text = text;
            textObj.transform.localPosition = new Vector3(0.0f,0.55f);
            textObj.gameObject.SetActive(true);
            var vec = textObj.rectTransform.sizeDelta;
            textObj.rectTransform.sizeDelta = new Vector2(vec.x*5.0f,vec.y);

            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(2f, (Il2CppSystem.Action<float>)((p) => {
                textObj.transform.localPosition += new Vector3(0.0f, Time.deltaTime * 0.16f);
                if (p > 0.7f)
                {
                    textObj.color = Color.white.AlphaMultiplied(1f - (p - 0.7f) / 0.3f);
                }
                if (p == 1f)
                {
                    GameObject.Destroy(textObj);
                }
            })));
        }
    }

    public class CustomButton
    {
        public static List<CustomButton> buttons = new List<CustomButton>();
        public ActionButton actionButton;
        public Vector3 PositionOffset;
        public float MaxTimer = float.MaxValue;
        public float Timer = 0f;
        private Action? OnSuspended=null;
        private Action OnClick;
        private Action OnMeetingEnds;
        private Func<bool> HasButton;
        private Func<bool> CouldUse;
        private Action OnEffectEnds;
        public bool HasEffect;
        public bool isEffectActive = false;
        public bool showButtonText = false;
        public float EffectDuration;
        public Sprite Sprite;
        private HudManager hudManager;
        private bool mirror;
        private KeyCode? hotkey;
        private string buttonText;
        private ImageNames textType;
        //ボタンの有効化フラグと、一時的な隠しフラグ
        private bool activeFlag,hideFlag;
        public bool FireOnClicked = false;
        //クールダウンの進みをインポスターキルボタンに合わせる
        private bool isImpostorKillButton=false;

        public bool IsValid { get { return activeFlag; } }
        public bool IsShown { get { return activeFlag && !hideFlag; } }

        private static Texture2D textureKeyBindChara;
        private static Dictionary<KeyCode, Sprite> spriteKeyBindChara = new Dictionary<KeyCode, Sprite>();
        private static Dictionary<KeyCode, float> keyBindReyout = new Dictionary<KeyCode, float>();
        private static Sprite spriteKeyBindBackGround;
        private static Sprite spriteKeyBindOption;

        private TMPro.TextMeshPro? upperText;
        public TMPro.TextMeshPro UpperText { get { 
                if (upperText!=null) return upperText;
                upperText = actionButton.CreateButtonUpperText();
                return upperText;
            } }

        static public void Load()
        {
            keyBindReyout[KeyCode.Q] = 0f;
            keyBindReyout[KeyCode.F] = 1f;
            keyBindReyout[KeyCode.G] = 2f;
            keyBindReyout[KeyCode.H] = 3f;
            keyBindReyout[KeyCode.J] = 4f;
            keyBindReyout[KeyCode.Z] = 5f;
            keyBindReyout[KeyCode.X] = 6f;
            keyBindReyout[KeyCode.C] = 7f;
            keyBindReyout[KeyCode.V] = 8f;
            keyBindReyout[KeyCode.LeftShift] = 9f;
            keyBindReyout[KeyCode.RightShift] = 10f;
        }

        public Sprite? GetKeyBindCharacterSprite(KeyCode? key)
        {
            if (key == null) return null;
            if (!keyBindReyout.ContainsKey(key.Value)) return null;

            if (!textureKeyBindChara) textureKeyBindChara = Helpers.loadTextureFromResources("Nebula.Resources.KeyBindCharacters.png");

            Sprite sprite;
            if (spriteKeyBindChara.TryGetValue(key.Value, out sprite))
            {
                if (sprite) return sprite;
            }
            sprite = Helpers.loadSpriteFromResources(textureKeyBindChara, 100f, new Rect(0f, -19f* keyBindReyout[key.Value], 18f, -19f));
            spriteKeyBindChara[key.Value] = sprite;
            return sprite;
        }

        public Sprite GetKeyBindBackgroundSprite()
        {
            if (spriteKeyBindBackGround) return spriteKeyBindBackGround;
            spriteKeyBindBackGround = Helpers.loadSpriteFromResources("Nebula.Resources.KeyBindBackground.png", 100f);
            return spriteKeyBindBackGround;
        }

        public Sprite GetKeyBindOptionSprite()
        {
            if (spriteKeyBindOption) return spriteKeyBindOption;
            spriteKeyBindOption = Helpers.loadSpriteFromResources("Nebula.Resources.KeyBindOption.png", 100f);
            return spriteKeyBindOption;
        }


        public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite, Vector3 PositionOffset, HudManager hudManager, KeyCode? hotkey, bool HasEffect, float EffectDuration, Action OnEffectEnds, bool mirror = false, string buttonText = "", ImageNames labelType= ImageNames.UseButton)
        {
            this.hudManager = hudManager;
            this.OnClick = OnClick;
            this.HasButton = HasButton;
            this.CouldUse = CouldUse;
            this.PositionOffset = PositionOffset;
            this.OnMeetingEnds = OnMeetingEnds;
            this.HasEffect = HasEffect;
            this.EffectDuration = EffectDuration;
            this.OnEffectEnds = OnEffectEnds;
            this.Sprite = Sprite;
            this.mirror = mirror;
            this.hotkey = hotkey;
            this.activeFlag = false;
            this.textType = labelType;

            Timer = 16.2f;
            buttons.Add(this);
            actionButton = UnityEngine.Object.Instantiate(hudManager.KillButton, hudManager.KillButton.transform.parent);
            PassiveButton button = actionButton.GetComponent<PassiveButton>();

            SetHotKeyGuide();
            
            SetLabel(buttonText);
            
            button.OnClick = new Button.ButtonClickedEvent();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)onClickEvent);


            setActive(true);
        }

        public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite, Vector3 PositionOffset, HudManager hudManager, KeyCode? hotkey, bool mirror = false, string buttonText = "", ImageNames labelType = ImageNames.UseButton)
        : this(OnClick, HasButton, CouldUse, OnMeetingEnds, Sprite, PositionOffset, hudManager, hotkey, false, 0f, () => { }, mirror, buttonText,labelType) { }

        public void SetKeyGuide(KeyCode? key, Vector2 pos,bool requireChangeOption)
        {
            Sprite? numSprite = GetKeyBindCharacterSprite(key);
            if (numSprite == null) return;

            GameObject obj = new GameObject();
            obj.name = "HotKeyGuide";
            obj.transform.SetParent(actionButton.gameObject.transform);
            obj.layer = actionButton.gameObject.layer;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.transform.localPosition = (Vector3)pos + new Vector3(0f, 0f, -1f);
            renderer.sprite = GetKeyBindBackgroundSprite();

            GameObject numObj = new GameObject();
            numObj.name = "HotKeyText";
            numObj.transform.SetParent(obj.transform);
            numObj.layer = actionButton.gameObject.layer;
            renderer = numObj.AddComponent<SpriteRenderer>();
            renderer.transform.localPosition = new Vector3(0,0,-1f);
            renderer.sprite = numSprite;

            if (requireChangeOption)
            {
                numObj = new GameObject();
                numObj.name = "HotKeyOption";
                numObj.transform.SetParent(obj.transform);
                numObj.layer = actionButton.gameObject.layer;
                renderer = numObj.AddComponent<SpriteRenderer>();
                renderer.transform.localPosition = new Vector3(0.12f, 0.07f, -2f);
                renderer.sprite = GetKeyBindOptionSprite();
            }
        }

        private void SetHotKeyGuide()
        {
            SetKeyGuide(hotkey, new Vector2(0.48f, 0.48f),false);
        }

        public void SetSuspendAction(Action OnSuspended)
        {
            this.OnSuspended = OnSuspended;
        }
        public void SetLabel(string label)
        {
            buttonText = label != "" ? Language.Language.GetString(label) : "";
            
            this.showButtonText = (actionButton.graphic.sprite == Sprite || buttonText != "");
        }

        public void SetButtonCoolDownOption(bool isImpostorKillButton)
        {
            this.isImpostorKillButton = isImpostorKillButton;
        }

        public CustomButton SetTimer(float timer)
        {
            this.Timer = timer;
            return this;
        }

        public void onClickEvent()
        {
            if (HasButton() && CouldUse())
            {
                if (this.Timer < 0f)
                {
                    actionButton.graphic.color = new Color(1f, 1f, 1f, 0.3f);

                    if (this.HasEffect && !this.isEffectActive)
                    {
                        this.Timer = this.EffectDuration;
                        actionButton.cooldownTimerText.color = new Color(0F, 0.8F, 0F);
                        this.isEffectActive = true;
                    }

                    this.OnClick();
                }
                else if(OnSuspended!=null && this.HasEffect && this.isEffectActive)
                {
                    this.OnSuspended();
                }
            }
        }
        public void Destroy()
        {
            setActive(false);
            if(actionButton) UnityEngine.Object.Destroy(actionButton.gameObject);
            actionButton = null;
            buttons.Remove(this);
        }

        public static void HudUpdate()
        {
            buttons.RemoveAll(item => item.actionButton == null);

            for (int i = 0; i < buttons.Count; i++)
            {
                try
                {
                    buttons[i].Update();
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine("[WARNING] NullReferenceException from HudUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public static void OnMeetingEnd()
        {
            buttons.RemoveAll(item => item.actionButton == null);
            for (int i = 0; i < buttons.Count; i++)
            {
                try
                {
                    buttons[i].OnMeetingEnds();
                    buttons[i].Update();

                    buttons[i].actionButton.cooldownTimerText.color = Palette.DisabledClear;
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine("[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public static void ResetAllCooldowns()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                try
                {
                    buttons[i].Timer = buttons[i].MaxTimer;
                    buttons[i].Update();
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine("[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public static void ButtonActivate()
        {
            foreach(var b in buttons)
            {
                b.setActive(true);
            }
        }

        public static void ButtonInactivate()
        {
            foreach (var b in buttons)
            {
                b.setActive(false);
            }
        }

        public void setActive(bool isActive)
        {
            if (actionButton)
            {
                if (isActive && !hideFlag)
                {
                    actionButton.gameObject.SetActive(true);
                    actionButton.graphic.enabled = true;
                }
                else
                {
                    actionButton.gameObject.SetActive(false);
                    actionButton.graphic.enabled = false;
                }
            }
            this.activeFlag = isActive;
        }

        public void temporaryHide(bool hideFlag)
        {
            if (hideFlag)
            {
                actionButton.gameObject.SetActive(false);
                actionButton.graphic.enabled = false;
            }
            else if(activeFlag) 
            {
                actionButton.gameObject.SetActive(true);
                actionButton.graphic.enabled = true;
            }
            this.hideFlag = hideFlag;
        }

        private bool MouseClicked()
        {
            if (!Input.GetMouseButtonDown(0)) return false;

            //中心からの距離を求める
            float x = Input.mousePosition.x - (Screen.width)/2;
            float y = Input.mousePosition.y - (Screen.height)/2;

            return Mathf.Sqrt(x * x + y * y) < 280;
        }

        private void Update()
        {
            if (actionButton.cooldownTimerText.color.a != 1f)
            {
                Color c = actionButton.cooldownTimerText.color;
                actionButton.cooldownTimerText.color = new Color(c.r, c.g, c.b, 1f);
            }

            if (Timer >= 0)
            {
                if (HasEffect && isEffectActive)
                    Timer -= Time.deltaTime;
                else if (Helpers.ProceedTimer(isImpostorKillButton))
                    Timer -= Time.deltaTime;
            }

            if (Timer <= 0 && HasEffect && isEffectActive)
            {
                isEffectActive = false;
                actionButton.cooldownTimerText.color = Palette.EnabledColor;
                Timer = MaxTimer;
                OnEffectEnds();
            }

            if (PlayerControl.LocalPlayer.Data == null || !Helpers.ShowButtons || !HasButton())
            {
                temporaryHide(true);
                return;
            }
            temporaryHide(false);


            if (hideFlag) return;

            actionButton.graphic.sprite = Sprite;
            if (showButtonText && buttonText != "")
            {
                actionButton.OverrideText(buttonText);

                actionButton.buttonLabelText.SetSharedMaterial(HudManager.Instance.UseButton.fastUseSettings[textType].FontMaterial);  
            }
            actionButton.buttonLabelText.enabled = showButtonText; // Only show the text if it's a kill button
            if (hudManager.UseButton != null)
            {
                Vector3 pos = hudManager.UseButton.transform.localPosition;
                if (mirror) pos = new Vector3(-pos.x, pos.y, pos.z);
                actionButton.transform.localPosition = pos + PositionOffset;
            }
            if (CouldUse())
            {
                actionButton.graphic.color = actionButton.buttonLabelText.color = Palette.EnabledColor;
                actionButton.graphic.material.SetFloat("_Desat", 0f);
            }
            else
            {
                actionButton.graphic.color = actionButton.buttonLabelText.color = Palette.DisabledClear;
                actionButton.graphic.material.SetFloat("_Desat", 1f);
            }

            actionButton.SetCoolDown(Timer, (HasEffect && isEffectActive) ? EffectDuration : MaxTimer);
            CooldownHelpers.SetCooldownNormalizedUvs(actionButton.graphic);

            // Trigger OnClickEvent if the hotkey is being pressed down
            if ((hotkey.HasValue && Input.GetKeyDown(hotkey.Value)) ||
                (FireOnClicked && MouseClicked())) onClickEvent();
        }
    }
}
