using AmongUs.GameOptions;

namespace Nebula.Modules.ScriptComponents;

public class ModAbilityButton : INebulaScriptComponent
{
    public enum LabelType
    {
        Standard,
        Impostor,
        Utility,
        Crewmate,
    }

    public ActionButton VanillaButton { get; private set; }

    public Timer? CoolDownTimer;
    public Timer? EffectTimer;
    public Timer? CurrentTimer => (EffectActive && (EffectTimer?.IsInProcess ?? false)) ? EffectTimer : CoolDownTimer;
    public bool EffectActive = false;

    public float CoolDownOnGameStart = 10f;

    public Action<ModAbilityButton>? OnEffectStart { get; set; } = null;
    public Action<ModAbilityButton>? OnEffectEnd { get; set; } = null;
    public Action<ModAbilityButton>? OnUpdate { get; set; } = null;
    public Action<ModAbilityButton>? OnClick { get; set; } = null;
    public Action<ModAbilityButton>? OnSubAction { get; set; } = null;
    public Action<ModAbilityButton>? OnMeeting { get; set; } = null;
    public Predicate<ModAbilityButton>? Availability { get; set; } = null;
    public Predicate<ModAbilityButton>? Visibility { get; set; } = null;
    private VirtualInput? keyCode { get; set; } = null;
    private VirtualInput? subKeyCode { get; set; } = null;

    internal ModAbilityButton(bool isLeftSideButton = false, bool isArrangedAsKillButton = false,int priority = 0)
    {

        VanillaButton = UnityEngine.Object.Instantiate(HudManager.Instance.KillButton, HudManager.Instance.KillButton.transform.parent);
        VanillaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));
        VanillaButton.cooldownTimerText.gameObject.SetActive(true);

        VanillaButton.buttonLabelText.GetComponent<TextTranslatorTMP>().enabled = false;
        var passiveButton = VanillaButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener(() => DoClick());

        var gridContent = VanillaButton.gameObject.GetComponent<HudContent>();
        gridContent.MarkAsKillButtonContent(isArrangedAsKillButton);
        gridContent.SetPriority(priority);
        NebulaGameManager.Instance?.HudGrid.RegisterContent(gridContent, isLeftSideButton);

    }

    public override void OnReleased()
    {
        if (VanillaButton) UnityEngine.Object.Destroy(VanillaButton.gameObject);
    }

    public override void Update()
    {
        //表示・非表示切替
        VanillaButton.gameObject.SetActive(Visibility?.Invoke(this) ?? true);
        //使用可能性切替
        if (Availability?.Invoke(this) ?? true)
            VanillaButton.SetEnabled();
        else
            VanillaButton.SetDisabled();

        OnUpdate?.Invoke(this);

        if (EffectActive && (EffectTimer == null || !EffectTimer.IsInProcess)) InactivateEffect();

        VanillaButton.SetCooldownFill(CurrentTimer?.Percentage ?? 0f);

        string timerText = "";
        if (CurrentTimer?.IsInProcess ?? false) timerText = Mathf.CeilToInt(CurrentTimer.CurrentTime).ToString();
        VanillaButton.cooldownTimerText.text = timerText;
        VanillaButton.cooldownTimerText.color = EffectActive ? Color.green : Color.white;

        if (keyCode?.KeyDownInGame ?? false) DoClick();
        if (subKeyCode?.KeyDownInGame ?? false) DoSubClick();
    }

    public override void OnMeetingStart()
    {
        OnMeeting?.Invoke(this);
    }


    public ModAbilityButton InactivateEffect()
    {
        if (!EffectActive) return this;
        EffectActive = false;
        OnEffectEnd?.Invoke(this);
        return this;
    }

    public ModAbilityButton ToggleEffect()
    {
        if (EffectActive)
            InactivateEffect();
        else
            ActivateEffect();

        return this;
    }

    public ModAbilityButton ActivateEffect()
    {
        if (EffectActive) return this;
        EffectActive = true;
        EffectTimer?.Start();
        OnEffectStart?.Invoke(this);
        return this;
    }

    public ModAbilityButton StartCoolDown()
    {
        CoolDownTimer?.Start();
        return this;
    }

    public bool UseCoolDownSupport { get; set; } = true;
    public override void OnGameReenabled() {
        if (UseCoolDownSupport) StartCoolDown();
    }

    public override void OnGameStart() {
        if (UseCoolDownSupport && CoolDownTimer != null) CoolDownTimer!.Start(Mathf.Min(CoolDownTimer!.Max, CoolDownOnGameStart));
    }

    public ModAbilityButton DoClick()
    {
        //効果中でなく、クールダウン中ならばなにもしない
        if (!EffectActive && (CoolDownTimer?.IsInProcess ?? false)) return this;
        //使用可能でないかを判定 (ボタン発火のタイミングと可視性更新のタイミングにずれが生じうるためここで再計算)
        if (!(Visibility?.Invoke(this) ?? true) || !(Availability?.Invoke(this) ?? true)) return this;

        OnClick?.Invoke(this);
        return this;
    }

    public ModAbilityButton DoSubClick()
    {
        //見えないボタンは使用させない
        if (!(Visibility?.Invoke(this) ?? true)) return this;

        OnSubAction?.Invoke(this);
        return this;
    }

    public ModAbilityButton SetSprite(Sprite? sprite)
    {
        VanillaButton.graphic.sprite = sprite;
        if (sprite != null) VanillaButton.graphic.SetCooldownNormalizedUvs();
        return this;
    }

    public ModAbilityButton SetLabel(string translationKey)
    {
        VanillaButton.buttonLabelText.text = Language.Translate("button.label." + translationKey);
        return this;
    }

    public ModAbilityButton SetLabelType(LabelType labelType)
    {
        Material? material = null;
        switch (labelType)
        {
            case LabelType.Standard:
                material = HudManager.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial;
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
        if (material != null) VanillaButton.buttonLabelText.SetSharedMaterial(material);
        return this;
    }

    public TMPro.TextMeshPro ShowUsesIcon(int iconVariation)
    {
        ButtonEffect.ShowUsesIcon(VanillaButton,iconVariation,out var text);
        return text;
    }

    public ModAbilityButton ResetKeyBind()
    {
        VanillaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));
        keyCode = null;
        subKeyCode = null;
        return this;
    }

    public ModAbilityButton KeyBind(KeyAssignmentType keyCode) => KeyBind(NebulaInput.GetInput(keyCode));
    public ModAbilityButton KeyBind(VirtualInput keyCode)
    {
        VanillaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));

        this.keyCode= keyCode;
        ButtonEffect.SetKeyGuide(VanillaButton.gameObject, keyCode.TypicalKey);
        return this;
    }

    private static SpriteLoader aidActionSprite = SpriteLoader.FromResource("Nebula.Resources.KeyBindOption.png", 100f);
    public ModAbilityButton SubKeyBind(KeyAssignmentType keyCode) => SubKeyBind(NebulaInput.GetInput(keyCode));
    public ModAbilityButton SubKeyBind(VirtualInput keyCode)
    {
        this.subKeyCode = keyCode;
        var guideObj = ButtonEffect.SetSubKeyGuide(VanillaButton.gameObject, keyCode.TypicalKey, false);

        if (guideObj != null)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("HotKeyOption", guideObj.transform, new Vector3(0.12f, 0.07f, -2f));
            renderer.sprite = aidActionSprite.GetSprite();
        }

        return this;
    }
}

public static class ButtonEffect
{
    [NebulaPreLoad]
    public class KeyCodeInfo
    {
        public static string? GetKeyDisplayName(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Return)
                return "Return";
            if (AllKeyInfo.TryGetValue(keyCode, out var val)) return val.TranslationKey;
            return null;
        }

        static public Dictionary<KeyCode, KeyCodeInfo> AllKeyInfo = new();
        public KeyCode keyCode { get; private set; }
        public DividedSpriteLoader textureHolder { get; private set; }
        public int num { get; private set; }
        public string TranslationKey { get; private set; }
        public KeyCodeInfo(KeyCode keyCode, string translationKey, DividedSpriteLoader spriteLoader, int num)
        {
            this.keyCode = keyCode;
            this.TranslationKey = translationKey;
            this.textureHolder = spriteLoader;
            this.num = num;

            AllKeyInfo.Add(keyCode, this);
        }

        public Sprite Sprite => textureHolder.GetSprite(num);
        public static void Load()
        {
            DividedSpriteLoader spriteLoader;
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters0.png", 100f, 18, 19, true);
            new KeyCodeInfo(KeyCode.Tab, "Tab", spriteLoader, 0);
            new KeyCodeInfo(KeyCode.Space, "Space", spriteLoader, 1);
            new KeyCodeInfo(KeyCode.Comma, "<", spriteLoader, 2);
            new KeyCodeInfo(KeyCode.Period, ">", spriteLoader, 3);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters1.png", 100f, 18, 19, true);
            for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
                new KeyCodeInfo(key, ((char)('A' + key - KeyCode.A)).ToString(), spriteLoader, key - KeyCode.A);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters2.png", 100f, 18, 19, true);
            for (int i = 0; i < 15; i++)
                new KeyCodeInfo(KeyCode.F1 + i, "F" + (i + 1), spriteLoader, i);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters3.png", 100f, 18, 19, true);
            new KeyCodeInfo(KeyCode.RightShift, "RShift", spriteLoader, 0);
            new KeyCodeInfo(KeyCode.LeftShift, "LShift", spriteLoader, 1);
            new KeyCodeInfo(KeyCode.RightControl, "RControl", spriteLoader, 2);
            new KeyCodeInfo(KeyCode.LeftControl, "LControl", spriteLoader, 3);
            new KeyCodeInfo(KeyCode.RightAlt, "RAlt", spriteLoader, 4);
            new KeyCodeInfo(KeyCode.LeftAlt, "LAlt", spriteLoader, 5);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters4.png", 100f, 18, 19, true);
            for (int i = 0; i < 6; i++)
                new KeyCodeInfo(KeyCode.Mouse1 + i, "Mouse " + (i == 0 ? "Right" : i == 1 ? "Middle" : (i + 1).ToString()), spriteLoader, i);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters5.png", 100f, 18, 19, true);
            for (int i = 0; i < 10; i++)
                new KeyCodeInfo(KeyCode.Alpha0 + i, "0" + (i + 1), spriteLoader, i);
        }
    }

    private static IDividedSpriteLoader textureUsesIconsSprite = XOnlyDividedSpriteLoader.FromResource("Nebula.Resources.UsesIcon.png", 100f, 10);
    static public GameObject ShowUsesIcon(this ActionButton button)
    {
        Transform template = HudManager.Instance.AbilityButton.transform.GetChild(2);
        var usesObject = GameObject.Instantiate(template.gameObject);
        usesObject.transform.SetParent(button.gameObject.transform);
        usesObject.transform.localScale = template.localScale;
        usesObject.transform.localPosition = template.localPosition * 1.2f;
        return usesObject;
    }

    static public GameObject ShowUsesIcon(this ActionButton button, int iconVariation, out TMPro.TextMeshPro text)
    {
        GameObject result = ShowUsesIcon(button);
        var renderer = result.GetComponent<SpriteRenderer>();
        renderer.sprite = textureUsesIconsSprite.GetSprite(iconVariation);
        text = result.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
        return result;
    }

    static public SpriteRenderer AddOverlay(this ActionButton button, Sprite sprite, float order)
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

    private static SpriteLoader lockedButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.LockedButton.png", 100f);
    static public SpriteRenderer AddLockedOverlay(this ActionButton button) => AddOverlay(button, lockedButtonSprite.GetSprite(), 0f);
    

    static ISpriteLoader keyBindBackgroundSprite = SpriteLoader.FromResource("Nebula.Resources.KeyBindBackground.png", 100f);
    static public GameObject? AddKeyGuide(GameObject button, KeyCode key, Vector2 pos,bool removeExistingGuide)
    {
        if(removeExistingGuide)button.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)(obj => { if (obj.name == "HotKeyGuide") GameObject.Destroy(obj); }));

        Sprite? numSprite = null;
        if (KeyCodeInfo.AllKeyInfo.ContainsKey(key)) numSprite = KeyCodeInfo.AllKeyInfo[key].Sprite;
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
    static public GameObject? SetKeyGuide(GameObject button, KeyCode key, bool removeExistingGuide = true)
    {
        return AddKeyGuide(button, key, new Vector2(0.48f, 0.48f),removeExistingGuide);
    }

    static public GameObject? SetSubKeyGuide(GameObject button, KeyCode key, bool removeExistingGuide)
    {
        return AddKeyGuide(button, key, new Vector2(0.48f, 0.13f),removeExistingGuide);
    }

    static public GameObject? SetKeyGuideOnSmallButton(GameObject button, KeyCode key)
    {
        return AddKeyGuide(button, key, new Vector2(0.28f, 0.28f), true);
    }
}