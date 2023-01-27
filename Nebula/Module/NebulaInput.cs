using BepInEx.Configuration;

namespace Nebula.Module;

public class NebulaInputManager
{
    public class KeyInputTexture
    {
        private string address;
        private Texture2D? texture = null;
        public Texture2D GetTexture()
        {
            if (texture == null || !texture) texture = Helpers.loadTextureFromResources(address);
            return texture;
        }

        public KeyInputTexture(string address)
        {
            this.address = address;
        }
    }

    public class KeyCodeData
    {
        public KeyCode keyCode { get; private set; }
        public KeyInputTexture texture { get; private set; }
        public int textureNum { get; private set; }
        public string displayKey { get; private set; }
        private Sprite? sprite = null;
        public KeyCodeData(KeyCode keyCode, string displayKey, KeyInputTexture texture, int num)
        {
            this.keyCode = keyCode;
            this.displayKey = displayKey;
            this.texture = texture;
            this.textureNum = num;

            allKeyCodes.Add(keyCode, this);
        }

        public Sprite GetSprite()
        {
            if (sprite == null || !sprite) sprite = Helpers.loadSpriteFromResources(texture.GetTexture(), 100f, new Rect(0f, -19f * (float)textureNum, 18f, -19f));

            return sprite;
        }
    }
    public class NebulaInput
    {
        public string identifier { get; private set; }
        private ConfigEntry<int> config;
        public KeyCode keyCode { get; private set; }
        private KeyCode defaultKeyCode;

        public NebulaInput(string identifier, KeyCode defaultKeyCode)
        {
            this.identifier = identifier;
            this.defaultKeyCode = defaultKeyCode;
            config = NebulaPlugin.Instance.Config.Bind($"KeyBinding", identifier, (int)defaultKeyCode);
            keyCode = (KeyCode)config.Value;
            NebulaInputManager.allInputs.Add(this);
        }

        public void changeKeyCode(KeyCode keyCode)
        {
            if (this.keyCode == keyCode) return;

            this.keyCode = keyCode;
            config.Value = (int)keyCode;
        }

        public void resetToDefault()
        {
            changeKeyCode(defaultKeyCode);
        }
    }

    public static List<NebulaInput> allInputs = new List<NebulaInput>();
    public static Dictionary<KeyCode, KeyCodeData> allKeyCodes = new Dictionary<KeyCode, KeyCodeData>();

    public static NebulaInput abilityInput;
    public static NebulaInput secondaryAbilityInput;
    public static NebulaInput changeAbilityInput;
    public static NebulaInput modifierAbilityInput;
    public static NebulaInput modKillInput;
    public static NebulaInput helpInput;
    public static NebulaInput observerInput;
    public static NebulaInput changeEyesightLeftInput;
    public static NebulaInput changeEyesightRightInput;
    public static NebulaInput observerShortcutInput;
    public static NebulaInput metaControlInput;
    public static NebulaInput noGameInput;

    public static void Load()
    {
        KeyInputTexture kit;
        kit = new KeyInputTexture("Nebula.Resources.KeyBindCharacters0.png");
        new KeyCodeData(KeyCode.Tab, "Tab", kit, 0);
        new KeyCodeData(KeyCode.Space, "Space", kit, 1);
        new KeyCodeData(KeyCode.Comma, "<", kit, 2);
        new KeyCodeData(KeyCode.Period, ">", kit, 3);
        kit = new KeyInputTexture("Nebula.Resources.KeyBindCharacters1.png");
        for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
            new KeyCodeData(key, ((char)('A' + key - KeyCode.A)).ToString(), kit, key - KeyCode.A);
        kit = new KeyInputTexture("Nebula.Resources.KeyBindCharacters2.png");
        for (int i = 0; i < 15; i++)
            new KeyCodeData(KeyCode.F1 + i, "F" + (i + 1), kit, i);
        kit = new KeyInputTexture("Nebula.Resources.KeyBindCharacters3.png");
        new KeyCodeData(KeyCode.RightShift, "RShift", kit, 0);
        new KeyCodeData(KeyCode.LeftShift, "LShift", kit, 1);
        new KeyCodeData(KeyCode.RightControl, "RControl", kit, 2);
        new KeyCodeData(KeyCode.LeftControl, "LControl", kit, 3);
        new KeyCodeData(KeyCode.RightAlt, "RAlt", kit, 4);
        new KeyCodeData(KeyCode.LeftAlt, "LAlt", kit, 5);
        kit = new KeyInputTexture("Nebula.Resources.KeyBindCharacters4.png");
        for (int i = 0; i < 6; i++)
            new KeyCodeData(KeyCode.Mouse1 + i, "Mouse " + (i == 0 ? "Right" : i == 1 ? "Middle" : (i + 1).ToString()), kit, i);
        kit = new KeyInputTexture("Nebula.Resources.KeyBindCharacters5.png");
        for (int i = 0; i < 10; i++)
            new KeyCodeData(KeyCode.Alpha0 + i, "0" + (i + 1), kit, i);

        abilityInput = new NebulaInput("ability", KeyCode.F);
        secondaryAbilityInput = new NebulaInput("secondaryAbility", KeyCode.G);
        changeAbilityInput = new NebulaInput("changeAbility", KeyCode.LeftShift);
        modifierAbilityInput = new NebulaInput("modifierAbility", KeyCode.Z);
        modKillInput = new NebulaInput("kill", KeyCode.Q);
        helpInput = new NebulaInput("help", KeyCode.H);
        observerInput = new NebulaInput("observer", KeyCode.M);
        changeEyesightLeftInput = new NebulaInput("changeEyesightLeft", KeyCode.Comma);
        changeEyesightRightInput = new NebulaInput("changeEyesightRight", KeyCode.Period);
        observerShortcutInput = new NebulaInput("observerShortcut", KeyCode.Mouse2);
        metaControlInput = new NebulaInput("metaControl", KeyCode.LeftControl);
        noGameInput = new NebulaInput("noGame", KeyCode.F5);
    }
}