using AmongUs.Data.Player;
using Nebula.Patches;
using Sentry.Unity.NativeUtils;
using Steamworks;
using System.Reflection.Metadata.Ecma335;
using UnityEngine.TextCore;
using UnityEngine.UI;
using static Il2CppSystem.Xml.Schema.FacetsChecker.FacetsCompiler;

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

    static SpriteLoader keyBindBackgroundSprite = new("Nebula.Resources.KeyBindBackground.png", 100f);
    static public GameObject? AddKeyGuide(GameObject button, KeyCode key, Vector2 pos)
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
        renderer.sprite = keyBindBackgroundSprite.GetSprite();

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
        return AddKeyGuide(button, key, new Vector2(0.48f, 0.48f));
    }

    static public GameObject? SetKeyGuideOnSmallButton(GameObject button, KeyCode key)
    {
        return AddKeyGuide(button, key, new Vector2(0.28f, 0.28f));
    }
}

public static class AbilityButtonDecorator
{
    private static SpriteLoader lockedButtonSprite = new SpriteLoader("Nebula.Resources.LockedButton.png", 100f);

    public static SpriteRenderer AddLockedOverlay(ModAbilityButton button)
    {
        return button.AddOverlay(lockedButtonSprite.GetSprite(), 0f);
    }
}

public static class ModAbilityAssets
{
    public static Sprite OriginalVentButtonSprite;
}

public class ModAbilityButton
{
    public enum LabelType
    {
        Standard,
        Impostor,
        Utility,
        Crewmate,
    }

    public interface IButtonEvent
    {
        KeyCode GetKey();
        bool CanBeTriggeredByCenterClickToo();
        bool IsMainEvent();
        void OnTriggered(ModAbilityButton button);
    }

    public interface IButtonAttribute
    {
        void OnActivated(ModAbilityButton button);
        void Update(ModAbilityButton button);
        void OutlineUpdate(ModAbilityButton button);
        void OnEndMeeting(ModAbilityButton button);
        void OnDestroy(ModAbilityButton button);
        //使用可能かどうかを返します。
        bool IsEnabled();
        //ボタンが表示されるかどうか返します。
        bool IsShown();
        bool IsCoolingDown();
        //クールダウンを開始します。
        void StartCoolingDown();
        void StartCoolingDown(float coolDown);
        IEnumerable<IButtonEvent> GetEvents();
    }

    public ModAbilityButton(Sprite sprite, Expansion.GridArrangeExpansion.GridArrangeParameter gridParam = Expansion.GridArrangeExpansion.GridArrangeParameter.None)
    {
        allButtons.Add(this);
        button = UnityEngine.Object.Instantiate(HudManager.Instance.KillButton, HudManager.Instance.KillButton.transform.parent);
        if (button.transform.childCount == 4) GameObject.Destroy(button.transform.GetChild(3).gameObject);
        Component.Destroy(button.buttonLabelText.gameObject.GetComponent<TextTranslatorTMP>());

        PassiveButton passiveButton = button.GetComponent<PassiveButton>();
        passiveButton.OnClick = new Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            if (currentAttribute == null) return;
            foreach(var ev in currentAttribute.GetEvents())
            {
                if (!ev.IsMainEvent()) continue;
                ev.OnTriggered(this);
                break;
            }
        }));

        SetSprite(sprite);

        Expansion.GridArrangeExpansion.AddGridArrangeContent(button.gameObject, gridParam);
        SetLabelType(LabelType.Standard);
    }

    private ActionButton button;
    private IButtonAttribute? currentAttribute;


    public IButtonAttribute? MyAttribute { get => currentAttribute; set {
            if (currentAttribute == value) return;
            currentAttribute?.OnDestroy(this);
            currentAttribute = value;
            currentAttribute?.OnActivated(this);
        } }

    //ボタン下部のテキスト
    public TMPro.TextMeshPro LabelText { get { return button.buttonLabelText; } }

    public ModAbilityButton SetLabelType(LabelType labelType)
    {
        Material material = HudManager.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial;
        switch (labelType)
        {
            case LabelType.Standard:
                break;
            case LabelType.Utility:
                material = HudManager.Instance.UseButton.fastUseSettings[ImageNames.PolusAdminButton].FontMaterial;
                break;
            case LabelType.Impostor:
                material = RoleManager.Instance.GetRole(RoleTypes.Shapeshifter).Ability.FontMaterial;
                break;
            case LabelType.Crewmate:
                material = RoleManager.Instance.GetRole(RoleTypes.Engineer).Ability.FontMaterial;
                break;

        }
        LabelText.SetSharedMaterial(material);
        return this;
    }

    public ModAbilityButton SetLabelLocalized(string localeKey) {
        LabelText.text = Language.Language.GetString(localeKey);
        return this;
    }

    //ボタン上部のテキスト
    private TMPro.TextMeshPro? upperText;
    public TMPro.TextMeshPro UpperText
    {
        get
        {
            if (upperText != null) return upperText;
            upperText = button.CreateButtonUpperText();
            return upperText;
        }
    }

    //ボタンの使用回数テキスト
    private GameObject? usesObject = null;
    private TMPro.TextMeshPro? usesText = null;
    public TMPro.TextMeshPro UsesText
    {
        get
        {
            if (usesObject != null) return usesText!;
            usesObject = button.ShowUsesIcon();
            usesText = usesObject.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
            return usesText;
        }
    }

    public ModAbilityButton SetUsesIcon(int variation)
    {
        if (usesObject == null) { var text = UsesText; }
        var renderer = usesObject.GetComponent<SpriteRenderer>();
        renderer.sprite = GetUsesIconSprite(variation);
        CooldownHelpers.SetCooldownNormalizedUvs(renderer);
        return this;
    }

    public void ShowUsesText(bool showFlag = true)
    {
        if (usesObject == null && showFlag) { var text = UsesText; }
        if (!usesObject) SetUsesIcon(0);
        usesObject.SetActive(showFlag);
    }

    public ModAbilityButton SetCoolDownTextColor(Color color)
    {
        button.cooldownTimerText.color = color;
        return this;
    }

    private static DividedSpriteLoader textureUsesIconsSprite = new("Nebula.Resources.UsesIcon.png", 100f, 10, 1);
    private static Sprite GetUsesIconSprite(int variation)
    {
        if (variation == 0)
            return HudManager.Instance.AbilityButton.transform.GetChild(2).GetComponent<SpriteRenderer>().sprite;
        return textureUsesIconsSprite.GetSprite(variation - 1);
    }

    public SpriteRenderer AddOverlay(Sprite sprite, float order)
    {
        return button.AddOverlay(sprite, order);
    }

    public void SetCoolDown(float timer,float max)
    {
        if (!(max > 0f)) max = 1f;
        button.SetCoolDown(timer, max);
        CooldownHelpers.SetCooldownNormalizedUvs(button.graphic);
    }

    private List<GameObject> allKeyGuide = new();

    SpriteLoader keyBindOptionSprite = new("Nebula.Resources.KeyBindOption.png", 100f);

    public void ClearAllKeyGuide()
    {
        foreach (var keyGuide in allKeyGuide) GameObject.Destroy(keyGuide);
        allKeyGuide.Clear();
    }

    public void AddKeyGuide(KeyCode? key, bool requireChangeOption)
    {
        if (!key.HasValue) return;

        var guide = ButtonEffect.AddKeyGuide(button.gameObject, key.Value, new Vector2(0.48f, 0.48f - 0.35f * (float)allKeyGuide.Count));
        allKeyGuide.Add(guide);

        if (requireChangeOption)
        {
            GameObject obj = new GameObject();
            obj.name = "HotKeyOption";
            obj.transform.SetParent(guide.transform);
            obj.layer = button.gameObject.layer;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.transform.localPosition = new Vector3(0.12f, 0.07f, -2f);
            renderer.sprite = keyBindOptionSprite.GetSprite();
        }
    }

    public void SetSprite(Sprite buttonSprite)
    {
        button.graphic.sprite = buttonSprite;
    }

    public void Destroy()
    {
        if (button) GameObject.Destroy(button.gameObject);
    }

    public bool IsShown => button && button.gameObject.active;

    private static List<ModAbilityButton> allButtons = new();
    public static void HudUpdate()
    {
        allButtons.RemoveAll((b)=>
        {
            bool result = !b.button;
            if (!result) b.MyUpdate();
            return result;
        });
    }
    public static void OutlineUpdate()
    {
        foreach (var button in allButtons) if (button.button) button.MyAttribute?.OutlineUpdate(button);
    }

    public static void OnMeetingEnd()
    {
        foreach (var button in allButtons) if (button.button) button.MyAttribute?.OnEndMeeting(button);
    }

    private void MyUpdate()
    {
        bool checkMouseClick()
        {
            if (!Input.GetMouseButtonDown(0)) return false;

            //中心からの距離を求める
            float x = Input.mousePosition.x - (Screen.width) / 2;
            float y = Input.mousePosition.y - (Screen.height) / 2;

            return Mathf.Sqrt(x * x + y * y) < 280;
        }

        if (currentAttribute != null)
        {
            button.gameObject.SetActive(currentAttribute.IsShown());

            currentAttribute.Update(this);

            if (button.gameObject.active)
            {
                var enabled = currentAttribute.IsEnabled();

                button.graphic.color = button.buttonLabelText.color = enabled ? Palette.EnabledColor : Palette.DisabledClear;
                button.graphic.material.SetFloat("_Desat", enabled ? 0f : 1f);

                foreach(var ev in currentAttribute.GetEvents()) {
                    if ((ev.CanBeTriggeredByCenterClickToo() && checkMouseClick()) || Input.GetKeyDown(ev.GetKey()))
                        ev.OnTriggered(this);
                }
            }
        }
        else
        {
            button.gameObject.SetActive(true);
            button.graphic.color = button.buttonLabelText.color = Palette.DisabledClear;
            button.graphic.material.SetFloat("_Desat", 1f);
            SetCoolDown(0f, 1f);
        }
    }

    public Tuple<EffectInactivatedAttribute,EffectActivatedAttribute> SetUpEffectAttribute(KeyCode keyCode,float initialCoolDown,float coolDown,float duration,Action? onTriggeredAction = null, Action? onInactivatedAction = null)
    {
        var activatedAttribute = new EffectActivatedAttribute(duration, onInactivatedAction);
        var inactivatedAttribute = new EffectInactivatedAttribute(coolDown, initialCoolDown, keyCode, activatedAttribute, onTriggeredAction);
        activatedAttribute.SetInactivatedAttribute(inactivatedAttribute);
        MyAttribute = inactivatedAttribute;
        return new(inactivatedAttribute,activatedAttribute);
    }
}

public class StaticAttribute : ModAbilityButton.IButtonAttribute
{
    public virtual void OnActivated(ModAbilityButton button) {
        button.SetCoolDown(0f, 1f);
        button.ClearAllKeyGuide();
        button.AddKeyGuide(events[0].GetKey(), false);
    }

    public virtual void Update(ModAbilityButton button) { }
    public virtual void OutlineUpdate(ModAbilityButton button) { }

    public virtual void OnEndMeeting(ModAbilityButton button) { }
    public virtual void OnDestroy(ModAbilityButton button) { }
    public virtual bool IsEnabled() => canUsePredicate?.Invoke() ?? true;
    public virtual bool IsShown() => canUseIgnoredSafety || !PlayerControl.LocalPlayer.Data.IsDead;
    public virtual bool IsCoolingDown() => false;
    public virtual void StartCoolingDown() { }
    public virtual void StartCoolingDown(float coolDown) { }
    private bool canUseIgnoredSafety;

    public virtual IEnumerable<ModAbilityButton.IButtonEvent> GetEvents() => events;
    private ModAbilityButton.IButtonEvent[] events;
    private Func<bool>? canUsePredicate;
    public StaticAttribute(bool canUseIgnoredSafety,Action buttonAction,KeyCode keyCode, Func<bool>? canUsePredicate=null)
    {
        this.canUseIgnoredSafety = canUseIgnoredSafety;
        this.canUsePredicate = canUsePredicate;
        events = new ModAbilityButton.IButtonEvent[1] { 
            new SimpleButtonEvent((button)=>buttonAction.Invoke(),keyCode)
        };
    }
}

public class HasCoolDownAttribute : ModAbilityButton.IButtonAttribute
{
    public virtual void OnActivated(ModAbilityButton button){}

    public virtual void Update(ModAbilityButton button)
    {
        if (Helpers.ProceedTimer(IsKillButton))
        {
            timer = Mathf.Max(0f, timer -= Time.deltaTime);
            button.SetCoolDown(timer, coolDown);

        }
    }
    public virtual void OutlineUpdate(ModAbilityButton button) { }

    public virtual void OnEndMeeting(ModAbilityButton button) => StartCoolingDown();
    public virtual void OnDestroy(ModAbilityButton button) { }
    public virtual bool IsEnabled() => PlayerControl.LocalPlayer.CanMove;
    public virtual bool IsShown() => !PlayerControl.LocalPlayer.Data.IsDead;
    public virtual bool IsCoolingDown() => timer > 0f;
    public virtual void StartCoolingDown() => timer = coolDown;
    public virtual void StartCoolingDown(float coolDown) => timer = coolDown;
    protected virtual bool IsKillButton => false;

    public virtual IEnumerable<ModAbilityButton.IButtonEvent> GetEvents() => new ModAbilityButton.IButtonEvent[0];
    protected float timer, coolDown;

    public HasCoolDownAttribute(float coolDown, float initialCoolDown)
    {
        timer = initialCoolDown;
        this.coolDown = coolDown;
    }
}

public class SimpleAbilityAttribute : HasCoolDownAttribute
{
    public override void OnActivated(ModAbilityButton button) {
        button.ClearAllKeyGuide();
        button.AddKeyGuide(events[0].GetKey(),false);
    }

    public override IEnumerable<ModAbilityButton.IButtonEvent> GetEvents() => events;
    private ModAbilityButton.IButtonEvent[] events;

    public SimpleAbilityAttribute(float coolDown, float initialCoolDown, ModAbilityButton.IButtonEvent buttonEvent):
        base(coolDown,initialCoolDown)
    {
        this.events = new ModAbilityButton.IButtonEvent[1] { buttonEvent };
    }
}

public class InterpersonalAbilityAttribute : SimpleAbilityAttribute
{
    public override void OutlineUpdate(ModAbilityButton button) {
        if (!button.IsShown) return;

        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(distance, targetablePredicate);
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, outlineColor);
    }

    public override bool IsEnabled() => base.IsEnabled() && Game.GameData.data.myData.currentTarget != null;

    private Predicate<GameData.PlayerInfo> targetablePredicate;
    private Color outlineColor;
    private float distance;

    public InterpersonalAbilityAttribute(float coolDown, float initialCoolDown, Predicate<GameData.PlayerInfo> targetablePredicate, Color outlineColor,float distance,ModAbilityButton.IButtonEvent buttonEvent)
    :base(coolDown,initialCoolDown, buttonEvent)
    {
        this.targetablePredicate = targetablePredicate;
        this.distance = distance;
        this.outlineColor= outlineColor;
    }
}

public class KillAbilityAttribute : InterpersonalAbilityAttribute
{
    protected override bool IsKillButton => true;

    public KillAbilityAttribute(float coolDown, float initialCoolDown, Predicate<GameData.PlayerInfo> targetablePredicate, Color outlineColor, float distance, Action<PlayerControl> buttonEvent)
    : base(coolDown, initialCoolDown, targetablePredicate, outlineColor,distance,new SimpleButtonEvent((button) => {
        if (Game.GameData.data.myData.currentTarget != null) buttonEvent.Invoke(Game.GameData.data.myData.currentTarget);
        button.MyAttribute!.StartCoolingDown();
    }, Module.NebulaInputManager.modKillInput.keyCode)){}

    public KillAbilityAttribute(float coolDown, float initialCoolDown, Predicate<GameData.PlayerInfo> targetablePredicate, Color outlineColor, float distance, Game.PlayerData.PlayerStatus deathReason ,Action<PlayerControl>? extraAction)
     : base(coolDown, initialCoolDown, targetablePredicate, outlineColor, distance, new SimpleButtonEvent((button) => {
         if (Game.GameData.data.myData.currentTarget != null)
         {
             var r = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget, Game.PlayerData.PlayerStatus.Dead, true);
             if (r == Helpers.MurderAttemptResult.PerformKill) extraAction?.Invoke(Game.GameData.data.myData.currentTarget);
             Game.GameData.data.myData.currentTarget = null;
             button.MyAttribute!.StartCoolingDown();
         }
     }, Module.NebulaInputManager.modKillInput.keyCode)){}
}

public class EffectInactivatedAttribute : SimpleAbilityAttribute
{
    public override void OnActivated(ModAbilityButton button)
    {
        base.OnActivated(button);
        StartCoolingDown();
        button.SetCoolDownTextColor(Color.white);
    }
    public EffectInactivatedAttribute(float coolDown, float initialCoolDown,KeyCode keyCode,EffectActivatedAttribute activatedAttribute,Action? activateEvent = null) 
        : base(coolDown,initialCoolDown,new SimpleButtonEvent((button) => {
            activateEvent?.Invoke();
            button.MyAttribute = activatedAttribute;
        },keyCode))
    {}
}

public class EffectActivatedAttribute : HasCoolDownAttribute
{
    EffectInactivatedAttribute? inactivatedAttribute;
    Action? inactivateEvent;

    public override void OnActivated(ModAbilityButton button)
    {
        base.OnActivated(button);
        button.ClearAllKeyGuide();
        StartCoolingDown();
        button.SetCoolDownTextColor(new Color(0f, 0.8f, 0f));
    }
    public override bool IsEnabled() => true;
    
    public override void Update(ModAbilityButton button)
    {
        timer = Mathf.Max(0f, timer -= Time.deltaTime);
        button.SetCoolDown(timer, coolDown);

        if (!IsCoolingDown())
        {
            inactivateEvent?.Invoke();
            button.MyAttribute = inactivatedAttribute;
        }
    }

    public override void OnEndMeeting(ModAbilityButton button) {
        inactivateEvent?.Invoke();
        button.MyAttribute = inactivatedAttribute;
        button.MyAttribute.OnEndMeeting(button);
    }

    public EffectActivatedAttribute(float duration, Action? inactivateEvent = null) :base(duration,duration)
    {
        inactivatedAttribute = null;
        this.inactivateEvent = inactivateEvent;
    }

    public void SetInactivatedAttribute(EffectInactivatedAttribute inactivatedAttribute)
    {
        this.inactivatedAttribute = inactivatedAttribute;
    }
}

public class SimpleButtonEvent : ModAbilityButton.IButtonEvent
{
    public KeyCode GetKey() => keyCode;
    public bool CanBeTriggeredByCenterClickToo() => false;
    public bool IsMainEvent() => isMainEvent;
    public void OnTriggered(ModAbilityButton button)
    {
        if (!button.MyAttribute!.IsShown() || !button.MyAttribute!.IsEnabled() || button.MyAttribute.IsCoolingDown()) return;
        button.MyAttribute!.StartCoolingDown();
        buttonEvent.Invoke(button);
    }

    KeyCode keyCode;
    bool isMainEvent;
    Action<ModAbilityButton> buttonEvent;

    public SimpleButtonEvent(Action<ModAbilityButton> buttonEvent,KeyCode keyCode,bool isMainEvent = true)
    {
        this.keyCode= keyCode;
        this.isMainEvent= isMainEvent;
        this.buttonEvent= buttonEvent;
    }
}

public class CustomButton
{
    public static List<CustomButton> buttons = new List<CustomButton>();
    public ActionButton actionButton;
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

    public void SetHotKey(KeyCode key)
    {
        hotkey = key;
        SetHotKeyGuide();
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