using BepInEx.Configuration;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public enum KeyAssignmentType
{
    Kill,
    Vent,
    Use,
    Ability,
    SecondaryAbility,
    Command,
    ScreenShot,
    Mute
}

public class KeyAssignment
{
    static private DataSaver KeyAssignmentSaver = new("KeyMapping");
    static private List<KeyAssignment> allKeyAssignments = new();
    static public IEnumerable<KeyAssignment> AllKeyAssignments => allKeyAssignments;

    private DataEntry<int> dataEntry;
    public KeyCode KeyInput { get => (KeyCode)dataEntry.Value; set => dataEntry.Value = (int)value; }
    public KeyCode DefaultKey { get; private set; }
    public string TranslationKey { get; private set; }
    public KeyAssignment(string translationKey,KeyCode defaultKey)
    {
        TranslationKey = translationKey;
        DefaultKey = defaultKey;
        dataEntry = new IntegerDataEntry(translationKey,KeyAssignmentSaver,(int)defaultKey);

        allKeyAssignments.Add(this);
    }

    public string DisplayName => Language.Translate("input." + TranslationKey);
}

[NebulaPreLoad]
public class NebulaInput
{
    private static bool SomeUiIsActive => (ControllerManager.Instance && ControllerManager.Instance.CurrentUiState?.BackButton  != null) || NebulaManager.Instance.HasSomeUI;

    public static bool GetKeyDown(KeyCode keyCode)
    {
        if (SomeUiIsActive) return false;
        return Input.GetKeyDown(keyCode);
    }

    public static bool GetKeyUp(KeyCode keyCode)
    {
        if (SomeUiIsActive) return true;
        return Input.GetKeyUp(keyCode);
    }

    public static bool GetKey(KeyCode keyCode)
    {
        if (SomeUiIsActive) return false;
        return Input.GetKey(keyCode);
    }

    private static Dictionary<KeyAssignmentType, Func<KeyCode>> modInput = new();

    static public KeyCode GetKeyCode(KeyAssignmentType type) => modInput[type].Invoke();

    static public void Load()
    {
        KeyCode GetVanillaKeyCode(int actionId) => Rewired.ReInput.mapping.GetKeyboardMapInstance(0, 0).GetButtonMapsWithAction(actionId)[0].keyCode;
        Func<KeyCode> GetModKeyCodeGetter(string translationKey, KeyCode defaultKey)
        {
            var assignment = new KeyAssignment(translationKey, defaultKey);
            return () => assignment.KeyInput;
        }

        modInput[KeyAssignmentType.Kill] = () => GetVanillaKeyCode(8);
        modInput[KeyAssignmentType.Vent] = () => GetVanillaKeyCode(50);
        modInput[KeyAssignmentType.Ability] = GetModKeyCodeGetter("ability", KeyCode.F);
        modInput[KeyAssignmentType.SecondaryAbility] = GetModKeyCodeGetter("secondaryAbility", KeyCode.G);
        modInput[KeyAssignmentType.Command] = GetModKeyCodeGetter("command", KeyCode.LeftControl);
        modInput[KeyAssignmentType.ScreenShot] = GetModKeyCodeGetter("screenShot", KeyCode.P);
        modInput[KeyAssignmentType.Mute] = GetModKeyCodeGetter("screenShot", KeyCode.V);
    }
}
