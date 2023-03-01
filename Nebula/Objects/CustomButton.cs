using Nebula.Patches;
using UnityEngine.UI;

namespace Nebula.Objects;

public static class ButtonEffect
{
    static public void ShowButtonText(this ActionButton button, string text)
    {
        TMPro.TextMeshPro textObj = GameObject.Instantiate(button.cooldownTimerText, button.cooldownTimerText.transform.parent);
        textObj.color = Color.white;
        textObj.transform.localScale = new Vector3(0.7f, 0.7f);
        textObj.text = text;
        textObj.transform.localPosition = new Vector3(0.0f, 0.55f);
        textObj.gameObject.SetActive(true);
        var vec = textObj.rectTransform.sizeDelta;
        textObj.rectTransform.sizeDelta = new Vector2(vec.x * 5.0f, vec.y);

        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(2f, (Il2CppSystem.Action<float>)((p) =>
        {
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

    static public GameObject ShowUsesIcon(this ActionButton button)
    {
        Transform template = HudManager.Instance.AbilityButton.transform.GetChild(2);
        var usesObject = GameObject.Instantiate(template.gameObject);
        usesObject.transform.SetParent(button.gameObject.transform);
        usesObject.transform.localScale = template.localScale;
        usesObject.transform.localPosition = template.localPosition * 1.2f;
        return usesObject;
    }

    static public GameObject ShowUsesIcon(this ActionButton button,int iconVariation, out TMPro.TextMeshPro text)
    {
        GameObject result = ShowUsesIcon(button);
        var renderer = result.GetComponent<SpriteRenderer>();
        renderer.sprite = CustomButton.GetUsesIconSprite(iconVariation);
        text = result.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
        return result;
    }

    static public SpriteRenderer AddOverlay(this ActionButton button,Sprite sprite,float order)
    {
        GameObject obj = new GameObject("Overlay");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(button.gameObject.transform);
        obj.transform.localScale = new Vector3(1, 1, 1);
        obj.transform.localPosition = new Vector3(0, 0, -1f - order);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return renderer;
    }
}

public class CustomButton
{
    public static Sprite OriginalVentButtonSprite;

    public static List<CustomButton> buttons = new List<CustomButton>();
    public ActionButton actionButton;
    public Vector3 PositionOffset;
    public float MaxTimer = float.MaxValue;
    public float Timer = 0f;
    private Action? AidAction = null;
    private Action? OnSuspended = null;
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
    private KeyCode? hotkey;
    private KeyCode? aidHotkey = null;
    private bool canInvokeAidActionWithMouseRightButton = true;
    private string buttonText;
    private ImageNames textType;
    private GameObject? usesObject=null;
    private TMPro.TextMeshPro? usesText = null;
    //ボタンの有効化フラグと、一時的な隠しフラグ
    private bool activeFlag, hideFlag;
    public bool FireOnClicked = false;
    //クールダウンの進みをインポスターキルボタンに合わせる
    private bool isImpostorKillButton = false;

    public bool IsValid { get { return activeFlag; } }
    public bool IsShown { get { return activeFlag && !hideFlag; } }

    private static Sprite spriteKeyBindBackGround;
    private static Sprite spriteKeyBindOption;
    private static Texture2D textureUsesIcon;
    private static Sprite[] spriteCustomUsesIcon = new Sprite[10];

    public static SpriteLoader lockedButtonSprite = new SpriteLoader("Nebula.Resources.LockedButton.png", 100f);

    public SpriteRenderer AddOverlay(Sprite sprite,float order)
    {
        return actionButton.AddOverlay(sprite,order);
    }

    public TMPro.TextMeshPro LabelText { get { return actionButton.buttonLabelText; } }
    private TMPro.TextMeshPro? upperText;
    public TMPro.TextMeshPro UpperText
    {
        get
        {
            if (upperText != null) return upperText;
            upperText = actionButton.CreateButtonUpperText();
            return upperText;
        }
    }

    public TMPro.TextMeshPro UsesText
    {
        get
        {
            if (usesObject != null) return usesText!;
            usesObject = actionButton.ShowUsesIcon();
            usesText = usesObject.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
            return usesText;
        }
    }

    public void SetUsesIcon(int variation)
    {
        if (usesObject == null) { var text = UsesText; }
        var renderer = usesObject.GetComponent<SpriteRenderer>();
        renderer.sprite = GetUsesIconSprite(variation);
        CooldownHelpers.SetCooldownNormalizedUvs(renderer);
    }

    public void ShowUsesText(bool showFlag = true)
    {
        if (usesObject == null && showFlag) { var text = UsesText; }
        if (usesObject) usesObject.SetActive(showFlag);
    }

    public static Sprite GetKeyBindBackgroundSprite()
    {
        if (spriteKeyBindBackGround) return spriteKeyBindBackGround;
        spriteKeyBindBackGround = Helpers.loadSpriteFromResources("Nebula.Resources.KeyBindBackground.png", 100f);
        return spriteKeyBindBackGround;
    }

    public static Sprite GetKeyBindOptionSprite()
    {
        if (spriteKeyBindOption) return spriteKeyBindOption;
        spriteKeyBindOption = Helpers.loadSpriteFromResources("Nebula.Resources.KeyBindOption.png", 100f);
        return spriteKeyBindOption;
    }

    public static Sprite GetUsesIconSprite(int variation)
    {
        if (variation == 0)
        {
            return HudManager.Instance.AbilityButton.transform.GetChild(2).GetComponent<SpriteRenderer>().sprite;
        }
        if (variation < 0 || variation > 10) return null;
        if (!textureUsesIcon)textureUsesIcon = Helpers.loadTextureFromResources("Nebula.Resources.UsesIcon.png");
        if (!spriteCustomUsesIcon[variation])
        {
            spriteCustomUsesIcon[variation] = Helpers.loadSpriteFromResources(textureUsesIcon, 100f, new Rect(57f * (float)(variation - 1),-56f,57f,56f));
        }
        return spriteCustomUsesIcon[variation];
    }


    public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite, Expansion.GridArrangeExpansion.GridArrangeParameter GridParam, HudManager hudManager, KeyCode? hotkey, bool HasEffect, float EffectDuration, Action OnEffectEnds, string buttonText = "", ImageNames labelType = ImageNames.UseButton)
    {
        this.hudManager = hudManager;
        this.OnClick = OnClick;
        this.HasButton = HasButton;
        this.CouldUse = CouldUse;
        this.OnMeetingEnds = OnMeetingEnds;
        this.HasEffect = HasEffect;
        this.EffectDuration = EffectDuration;
        this.OnEffectEnds = OnEffectEnds;
        this.Sprite = Sprite;
        this.hotkey = hotkey;
        this.activeFlag = false;
        this.textType = labelType;

        Timer = 16.2f;
        buttons.Add(this);
        actionButton = UnityEngine.Object.Instantiate(hudManager.KillButton, hudManager.KillButton.transform.parent);
        if (actionButton.transform.childCount == 4) GameObject.Destroy(actionButton.transform.GetChild(3).gameObject);
        PassiveButton button = actionButton.GetComponent<PassiveButton>();

        SetHotKeyGuide();

        SetLabel(buttonText);

        button.OnClick = new Button.ButtonClickedEvent();
        button.OnClick.AddListener((UnityEngine.Events.UnityAction)onClickEvent);

        Expansion.GridArrangeExpansion.AddGridArrangeContent(button.gameObject,GridParam);

        setActive(true);
    }

    public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite, Expansion.GridArrangeExpansion.GridArrangeParameter GridParam, HudManager hudManager, KeyCode? hotkey, string buttonText = "", ImageNames labelType = ImageNames.UseButton)
    : this(OnClick, HasButton, CouldUse, OnMeetingEnds, Sprite, GridParam, hudManager, hotkey, false, 0f, () => { }, buttonText, labelType) { }

    static public GameObject? SetKeyGuide(GameObject button, KeyCode key, Vector2 pos)
    {
        Sprite? numSprite = null;
        if (Module.NebulaInputManager.allKeyCodes.ContainsKey(key)) numSprite = Module.NebulaInputManager.allKeyCodes[key].GetSprite();
        if (numSprite == null) return null;

        GameObject obj = new GameObject();
        obj.name = "HotKeyGuide";
        obj.transform.SetParent(button.transform);
        obj.layer = button.layer;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = (Vector3)pos + new Vector3(0f, 0f, -10f);
        renderer.sprite = GetKeyBindBackgroundSprite();

        GameObject numObj = new GameObject();
        numObj.name = "HotKeyText";
        numObj.transform.SetParent(obj.transform);
        numObj.layer = button.layer;
        renderer = numObj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = new Vector3(0, 0, -1f);
        renderer.sprite = numSprite;

        return obj;
    }

    static public GameObject? SetKeyGuide(GameObject button, KeyCode key)
    {
        return SetKeyGuide(button, key, new Vector2(0.48f, 0.48f));
    }

    static public GameObject? SetKeyGuideOnSmallButton(GameObject button, KeyCode key)
    {
        return SetKeyGuide(button, key, new Vector2(0.28f, 0.28f));
    }

    public void SetKeyGuide(KeyCode? key, Vector2 pos, bool requireChangeOption)
    {
        if (!key.HasValue) return;

        var guideObj = SetKeyGuide(actionButton.gameObject, key.Value, pos);

        if (guideObj == null) return;

        if (requireChangeOption)
        {
            SpriteRenderer renderer;

            GameObject obj = new GameObject();
            obj.name = "HotKeyOption";
            obj.transform.SetParent(guideObj.transform);
            obj.layer = actionButton.gameObject.layer;
            renderer = obj.AddComponent<SpriteRenderer>();
            renderer.transform.localPosition = new Vector3(0.12f, 0.07f, -2f);
            renderer.sprite = GetKeyBindOptionSprite();
        }
    }

    public void SetAidAction(KeyCode key, bool requireChangeOption, Action aidAction)
    {
        SetKeyGuide(key, new Vector2(0.48f, 0.13f), requireChangeOption);
        AidAction = aidAction;
        aidHotkey = key;
    }

    private void SetHotKeyGuide()
    {
        SetKeyGuide(hotkey, new Vector2(0.48f, 0.48f), false);
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
            else if (OnSuspended != null && this.HasEffect && this.isEffectActive)
            {
                this.OnSuspended();
            }
        }
    }
    public void Destroy()
    {
        if (actionButton)
        {
            if (HudManager.InstanceExists)
            {
                GridArrange.currentChildren.Remove(actionButton.transform);
            }
            UnityEngine.Object.Destroy(actionButton.gameObject);
        }
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
            }
            catch (NullReferenceException)
            {}
        }
    }

    public static void ButtonActivate()
    {
        foreach (var b in buttons)
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
        else if (activeFlag)
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
        float x = Input.mousePosition.x - (Screen.width) / 2;
        float y = Input.mousePosition.y - (Screen.height) / 2;

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

        if (PlayerControl.LocalPlayer.Data == null || !HasButton())
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

        float max = (HasEffect && isEffectActive) ? EffectDuration : MaxTimer;
        if (!(max > 0f)) max = 1f;
        actionButton.SetCoolDown(Timer, max);
        CooldownHelpers.SetCooldownNormalizedUvs(actionButton.graphic);

        if (actionButton.gameObject.active)
        {
            // Trigger OnClickEvent if the hotkey is being pressed down
            if ((hotkey.HasValue && Input.GetKeyDown(hotkey.Value)) ||
                (FireOnClicked && MouseClicked())) onClickEvent();

            if (AidAction != null && aidHotkey.HasValue && (Input.GetKeyDown(aidHotkey.Value) || (canInvokeAidActionWithMouseRightButton && Input.GetKeyDown(KeyCode.Mouse1))))
            {
                AidAction();
            }
        }
    }
}