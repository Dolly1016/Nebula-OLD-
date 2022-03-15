using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Hazel;
using BepInEx.Configuration;
using Nebula.Language;

namespace Nebula.Module
{
    [Flags]
    public enum CustomGameMode
    {
        Standard=0x01,
        Minigame = 0x02,
        Parlour = 0x04,
        Investigators = 0x08,
        FreePlay = 0x10,
        All =int.MaxValue
    }

    public static class CustomGameModes
    {
        static public List<CustomGameMode> AllGameModes = new List<CustomGameMode>()
        {
            CustomGameMode.Standard,CustomGameMode.Minigame,CustomGameMode.Parlour,CustomGameMode.Investigators,
            CustomGameMode.FreePlay
        };

        static public CustomGameMode GetGameMode(int GameModeIndex)
        {
            if (AllGameModes.Count > GameModeIndex && GameModeIndex>=0)
            {
                return AllGameModes[GameModeIndex];
            }
            return AllGameModes[0];
        }
    }


    public delegate string CustomOptionDecorator(string original,CustomOption option);

    public class CustomOption
    {
        public static List<CustomOption> options = new List<CustomOption>();
        public static int preset = 0;

        public int id;
        public Color color;
        public string name;
        public string format;
        public string prefix, suffix;
        public System.Object[] selections;

        public int defaultSelection;
        public ConfigEntry<int> entry;
        public int selection;
        public OptionBehaviour optionBehaviour;
        public CustomOption parent;
        public List<CustomOption> children;
        public bool isHeader;
        public bool isHidden;
        public bool isHiddenOnDisplay;
        public CustomGameMode GameMode;

        static public CustomGameMode CurrentGameMode;

        public List<CustomOption> prerequisiteOptions;
        public List<CustomOption> prerequisiteOptionsInv;
        public List<Func<bool>> prerequisiteOptionsCustom;

        public CustomOptionDecorator? Decorator { get; set; }

        public virtual bool enabled
        {
            get
            {
                return this.getBool();
            }
        }

        public CustomOption HiddenOnDisplay(bool Hidden)
        {
            isHiddenOnDisplay = Hidden;
            return this;
        }

        public CustomOption SetGameMode(CustomGameMode gameMode)
        {
            GameMode = gameMode;
            return this;
        }

        public bool IsHidden(CustomGameMode gameMode)
        {
            return isHidden || (parent!=null && (parent.IsHidden(gameMode) || !parent.enabled)) || (0 == (int)(gameMode & GameMode))
                || prerequisiteOptions.Count > 0 && prerequisiteOptions.Any((option) => { return !option.enabled || option.IsHidden(gameMode); })
                || prerequisiteOptionsInv.Count > 0 && prerequisiteOptionsInv.Any((option) => { return option.enabled || option.IsHidden(gameMode); })
                || prerequisiteOptionsCustom.Count > 0 && prerequisiteOptionsCustom.Any((func) => { return !func.Invoke(); });
        }

        public bool IsHiddenOnDisplay(CustomGameMode gameMode)
        {
            return isHiddenOnDisplay || IsHidden(gameMode) || (parent != null && parent.IsHiddenOnDisplay(gameMode));
        }

        // Option creation
        public CustomOption()
        {

        }

        public CustomOption(int id, Color color,string name, System.Object[] selections, System.Object defaultValue, CustomOption parent, bool isHeader, bool isHidden, string format)
        {
            this.id = id;
            this.color = color;
            this.name = name;
            this.format = format;
            this.selections = selections;
            int index = Array.IndexOf(selections, defaultValue);
            this.defaultSelection = index >= 0 ? index : 0;
            this.parent = parent;
            this.isHeader = isHeader;
            this.isHidden = isHidden;

            this.prefix = null;
            this.suffix = null;

            this.isHiddenOnDisplay = false;

            this.children = new List<CustomOption>();
            if (parent != null)
            {
                parent.children.Add(this);
            }

            selection = 0;
            if (id > 0)
            {
                entry = NebulaPlugin.Instance.Config.Bind($"Preset{preset}", id.ToString(), defaultSelection);
                selection = Mathf.Clamp(entry.Value, 0, selections.Length - 1);
            }
            options.Add(this);

            this.prerequisiteOptions = new List<CustomOption>();
            this.prerequisiteOptionsInv = new List<CustomOption>();
            this.prerequisiteOptionsCustom = new List<Func<bool>>();
            this.GameMode = CustomGameMode.Standard;

            this.Decorator = null;
        }

        public static CustomOption Create(int id, Color color,string name, string[] selections,string defaultValue, CustomOption parent = null, bool isHeader = false, bool isHidden = false, string format = "")
        {
            return new CustomOption(id, color,name, selections, defaultValue, parent, isHeader, isHidden, format);
        }

        public static CustomOption Create(int id, Color color, string name, float defaultValue, float min, float max, float step, CustomOption parent = null, bool isHeader = false, bool isHidden = false, string format = "")
        {
            List<float> selections = new List<float>();
            for (float s = min; s <= max; s += step)
                selections.Add(s);
            return new CustomOption(id, color,name, selections.Cast<object>().ToArray(), defaultValue, parent, isHeader, isHidden, format);
        }

        public static CustomOption Create(int id, Color color, string name, bool defaultValue, CustomOption parent = null, bool isHeader = false, bool isHidden = false, string format = "")
        {
            return new CustomOption(id, color,name, new string[] { "option.switch.off", "option.switch.on" }, defaultValue ? "option.switch.on" : "option.switch.off", parent, isHeader, isHidden, format);
        }

        // Static behaviour

        public static void switchPreset(int newPreset)
        {
            CustomOption.preset = newPreset;
            foreach (CustomOption option in CustomOption.options)
            {
                if (option.id <= 1) continue;

                option.entry = NebulaPlugin.Instance.Config.Bind($"Preset{preset}", option.id.ToString(), option.defaultSelection);
                option.selection = Mathf.Clamp(option.entry.Value, 0, option.selections.Length - 1);
                if (option.optionBehaviour != null && option.optionBehaviour is StringOption stringOption)
                {
                    stringOption.oldValue = stringOption.Value = option.selection;
                    stringOption.ValueText.text = option.getString();
                }
            }
        }

        public static void ShareOptionSelections()
        {
            if (PlayerControl.AllPlayerControls.Count <= 1 || AmongUsClient.Instance?.AmHost == false && PlayerControl.LocalPlayer == null) return;

            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShareOptions, Hazel.SendOption.Reliable);
            messageWriter.WritePacked((uint)CustomOption.options.Count);

            messageWriter.WritePacked((uint)0);
            messageWriter.WritePacked((uint)PlayerControl.GameOptions.NumImpostors);

            foreach (CustomOption option in CustomOption.options)
            {
                messageWriter.WritePacked((uint)option.id);
                messageWriter.WritePacked((uint)Convert.ToUInt32(option.selection));
            }
            messageWriter.EndMessage();
        }

        public void AddPrerequisite(CustomOption option)
        {
            prerequisiteOptions.Add(option);
        }

        public void AddInvPrerequisite(CustomOption option)
        {
            prerequisiteOptionsInv.Add(option);
        }

        public void AddCustomPrerequisite(Func<bool> func)
        {
            prerequisiteOptionsCustom.Add(func);
        }

        // Getter

        public virtual int getSelection()
        {
            return selection;
        }

        public virtual bool getBool()
        {
            return selection > 0 || selections.Length == 1;
        }

        public virtual float getFloat()
        {
            return (float)selections[selection];
        }

        protected virtual string getStringSelection()
        {
            string sel = selections[selection].ToString();
            if (format != "")
            {
                return string.Format(Language.Language.GetString(format), sel);
            }
            float temp;
            if (float.TryParse(sel,out temp))
            {
                return sel;
            }
            return Language.Language.GetString(sel);
        }

        public string getString()
        {
            string text = getStringSelection();
            
            if (prefix != null)
            {
                text = Language.Language.GetString("option.prefix." + prefix) + text;
            }

            if (suffix != null)
            {
                text += Language.Language.GetString("option.suffix." + suffix);
            }

            return text;
        }

        public virtual string getName(bool display=false)
        {
            string original = Helpers.cs(color, Language.Language.GetString(name));
            if (Decorator != null && display)
            {
                return Decorator.Invoke(original, this);
            }
            else
            {
                return original;
            }
        }

        // Option changes

        public virtual void updateSelection(int newSelection)
        {
            if (newSelection < 0)
            {
                selection = selections.Length - 1;
            }
            else
            {
                selection = newSelection % selections.Length;
            }
            

            if (optionBehaviour != null && optionBehaviour is StringOption stringOption)
            {
                stringOption.oldValue = stringOption.Value = selection;
                stringOption.ValueText.text = getString();

                if (AmongUsClient.Instance?.AmHost == true && PlayerControl.LocalPlayer)
                {
                    if (id == 1) switchPreset(selection); // Switch presets
                    else if (entry != null) entry.Value = selection; // Save selection to config

                    ShareOptionSelections();// Share all selections
                }
            }
        }

        public void SetParent(CustomOption newParent)
        {
            if (parent != null)
            {
                parent.children.Remove(this);
            }

            parent = newParent;
            if (parent != null)
            {
                parent.children.Add(this);
            }
        }
    }

    public class CustomRoleOption : CustomOption
    {
        public CustomOption countOption = null;

        public int rate
        {
            get
            {
                return getSelection();
            }
        }

        public int count
        {
            get
            {
                if (countOption != null)
                    return Mathf.RoundToInt(countOption.getFloat());

                return 1;
            }
        }

        public (int, int) data
        {
            get
            {
                return (rate, count);
            }
        }

        public CustomRoleOption(int id, string name, Color color, int max = 15) :
            base(id, color, name, CustomOptionHolder.rates, "", null, true, false, "")
        {
            if (max > 1)
                countOption = CustomOption.Create(id + 10000,Color.white,"option.roleNumAssigned", 1f, 1f, 15f, 1f, this, format: "unitPlayers");
        }
    }

    public class CustomOptionBlank : CustomOption
    {
        public CustomOptionBlank(CustomOption parent)
        {
            this.parent = parent;
            this.id = -1;
            this.name = "";
            this.isHeader = false;
            this.isHidden = true;
            this.children = new List<CustomOption>();
            this.selections = new string[] { "" };
            options.Add(this);
        }

        public override int getSelection()
        {
            return 0;
        }

        public override bool getBool()
        {
            return true;
        }

        public override float getFloat()
        {
            return 0f;
        }

        protected override string getStringSelection()
        {
            return "";
        }

        public override void updateSelection(int newSelection)
        {
            return;
        }

    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
    class GameOptionsMenuStartPatch
    {
        public static void Postfix(GameOptionsMenu __instance)
        {
            if (GameObject.Find("NebulaSettings") != null)
            { // Settings setup has already been performed, fixing the title of the tab and returning
                GameObject.Find("NebulaSettings").transform.FindChild("GameGroup").FindChild("Text").GetComponent<TMPro.TextMeshPro>().SetText("The Nebula Settings");
                return;
            }

            // Setup TOR tab
            var template = UnityEngine.Object.FindObjectsOfType<StringOption>().FirstOrDefault();
            if (template == null) return;
            var gameSettings = GameObject.Find("Game Settings");
            var gameSettingMenu = UnityEngine.Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();
            var nebulaSettings = UnityEngine.Object.Instantiate(gameSettings, gameSettings.transform.parent);
            var nebulaMenu = nebulaSettings.transform.FindChild("GameGroup").FindChild("SliderInner").GetComponent<GameOptionsMenu>();
            nebulaSettings.name = "NebulaSettings";

            var roleTab = GameObject.Find("RoleTab");
            var gameTab = GameObject.Find("GameTab");

            var torTab = UnityEngine.Object.Instantiate(roleTab, roleTab.transform.parent);
            var nebulaTabHighlight = torTab.transform.FindChild("Hat Button").FindChild("Tab Background").GetComponent<SpriteRenderer>();
            torTab.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = Helpers.loadSpriteFromResources("Nebula.Resources.TabIcon.png", 100f);

            gameTab.transform.position += Vector3.left * 0.5f;
            torTab.transform.position += Vector3.right * 0.5f;
            roleTab.transform.position += Vector3.left * 0.5f;

            var tabs = new GameObject[] { gameTab, roleTab, torTab };
            for (int i = 0; i < tabs.Length; i++)
            {
                var button = tabs[i].GetComponentInChildren<PassiveButton>();
                if (button == null) continue;
                int copiedIndex = i;
                button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
                    gameSettingMenu.RegularGameSettings.SetActive(false);
                    gameSettingMenu.RolesSettings.gameObject.SetActive(false);
                    nebulaSettings.gameObject.SetActive(false);
                    gameSettingMenu.GameSettingsHightlight.enabled = false;
                    gameSettingMenu.RolesSettingsHightlight.enabled = false;
                    nebulaTabHighlight.enabled = false;
                    if (copiedIndex == 0)
                    {
                        gameSettingMenu.RegularGameSettings.SetActive(true);
                        gameSettingMenu.GameSettingsHightlight.enabled = true;
                    }
                    else if (copiedIndex == 1)
                    {
                        gameSettingMenu.RolesSettings.gameObject.SetActive(true);
                        gameSettingMenu.RolesSettingsHightlight.enabled = true;
                    }
                    else if (copiedIndex == 2)
                    {
                        nebulaSettings.gameObject.SetActive(true);
                        nebulaTabHighlight.enabled = true;
                    }
                }));
            }

            foreach (OptionBehaviour option in nebulaMenu.GetComponentsInChildren<OptionBehaviour>())
                UnityEngine.Object.Destroy(option.gameObject);
            List<OptionBehaviour> nebulaOptions = new List<OptionBehaviour>();

            for (int i = 0; i < CustomOption.options.Count; i++)
            {
                CustomOption option = CustomOption.options[i];
                if (option.optionBehaviour == null)
                {
                    StringOption stringOption = UnityEngine.Object.Instantiate(template, nebulaMenu.transform);
                    nebulaOptions.Add(stringOption);
                    stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => { });
                    stringOption.TitleText.text = option.name;
                    stringOption.Value = stringOption.oldValue = option.selection;
                    stringOption.ValueText.text = option.selections[option.selection].ToString();

                    if (option.selections.Length == 1)
                    {
                        stringOption.gameObject.transform.FindChild("Plus_TMP").gameObject.SetActive(false);
                        stringOption.gameObject.transform.FindChild("Minus_TMP").gameObject.SetActive(false);
                    }

                    if (option.prefix != null)
                    {
                        stringOption.ValueText.text = Language.Language.GetString("option.prefix." + option.prefix) + stringOption.ValueText.text;
                    }

                    if (option.suffix != null)
                    {
                        stringOption.ValueText.text += Language.Language.GetString("option.suffix."+option.suffix);
                    }

                    option.optionBehaviour = stringOption;
                }
                option.optionBehaviour.gameObject.SetActive(true);
            }

            nebulaMenu.Children = nebulaOptions.ToArray();
            nebulaSettings.gameObject.SetActive(false);

            var killCoolOption = __instance.Children.FirstOrDefault(x => x.name == "KillCooldown").TryCast<NumberOption>();
            if (killCoolOption != null) killCoolOption.ValidRange = new FloatRange(2.5f, 60f);

            
            var commonTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumCommonTasks").TryCast<NumberOption>();
            if (commonTasksOption != null) commonTasksOption.ValidRange = new FloatRange(0f, 4f);

            var shortTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumShortTasks").TryCast<NumberOption>();
            if (shortTasksOption != null) shortTasksOption.ValidRange = new FloatRange(0f, 23f);

            var longTasksOption = __instance.Children.FirstOrDefault(x => x.name == "NumLongTasks").TryCast<NumberOption>();
            if (longTasksOption != null) longTasksOption.ValidRange = new FloatRange(0f, 15f);

            var impostorsOption = __instance.Children.FirstOrDefault(x => x.name == "NumImpostors").TryCast<NumberOption>();
            if (impostorsOption != null) impostorsOption.ValidRange = new FloatRange(0f, 5f);
        }
    }

    [HarmonyPatch(typeof(KeyValueOption), nameof(KeyValueOption.OnEnable))]
    public class KeyValueOptionEnablePatch
    {
        public static void Postfix(KeyValueOption __instance)
        {
            GameOptionsData gameOptions = PlayerControl.GameOptions;
            if (__instance.Title == StringNames.GameMapName)
            {
                __instance.Selected = gameOptions.MapId;
            }
            try
            {
                __instance.ValueText.text = __instance.Values[Mathf.Clamp(__instance.Selected, 0, __instance.Values.Count - 1)].Key;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
    public class StringOptionEnablePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            CustomOption option = CustomOption.options.FirstOrDefault(opt => opt.optionBehaviour == __instance);
            if (option == null) return true;

            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = option.getName();
            __instance.Value = __instance.oldValue = option.selection;
            __instance.ValueText.text = option.getString();

            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            CustomOption option = CustomOption.options.FirstOrDefault(opt => opt.optionBehaviour == __instance);
            if (option == null) return true;
            option.updateSelection(option.selection + 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            CustomOption option = CustomOption.options.FirstOrDefault(opt => opt.optionBehaviour == __instance);
            if (option == null) return true;
            option.updateSelection(option.selection - 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            CustomOption.ShareOptionSelections();
        }
    }


    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    class GameOptionsMenuUpdatePatch
    {
        private static float timer = 1f;
        public static void Postfix(GameOptionsMenu __instance)
        {
            if (__instance.Children.Length < 20) return; // TODO: Introduce a cleaner way to seperate the TOR settings from the game settings

            timer += Time.deltaTime;
            if (timer < 0.1f) return;
            timer = 0f;

            float numItems = __instance.Children.Length;

            float offset = 2.75f;
            foreach (CustomOption option in CustomOption.options)
            {
                if (option?.optionBehaviour != null && option.optionBehaviour.gameObject != null)
                {
                    bool enabled = true;

                    if (AmongUsClient.Instance?.AmHost == false)
                    {
                        enabled = false;
                    }

                    if (option.IsHidden(CustomOption.CurrentGameMode))
                    {
                        enabled = false;
                    }

                    option.optionBehaviour.gameObject.SetActive(enabled);
                    if (enabled)
                    {
                        offset -= option.isHeader ? 0.75f : 0.5f;
                        option.optionBehaviour.transform.localPosition = new Vector3(option.optionBehaviour.transform.localPosition.x, offset, option.optionBehaviour.transform.localPosition.z);

                        if (option.isHeader)
                        {
                            numItems += 0.5f;
                        }
                    }
                    else
                    {
                        numItems--;
                    }
                }
            }
            __instance.GetComponentInParent<Scroller>().ContentYBounds.max = (-offset) - 1.5f;
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class GameSettingMenuStartPatch
    {
        public static void Prefix(GameSettingMenu __instance)
        {
            __instance.HideForOnline = new Transform[] { };
        }

        public static void Postfix(GameSettingMenu __instance)
        {
            // Setup mapNameTransform
            var mapNameTransform = __instance.AllItems.FirstOrDefault(x => x.gameObject.activeSelf && x.name.Equals("MapName", StringComparison.OrdinalIgnoreCase));
            if (mapNameTransform == null) return;

            var options = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<string, int>>();
            for (int i = 0; i < Constants.MapNames.Length; i++)
            {
                var kvp = new Il2CppSystem.Collections.Generic.KeyValuePair<string, int>();
                kvp.key = Constants.MapNames[i];
                kvp.value = i;
                options.Add(kvp);
            }

            mapNameTransform.GetComponent<KeyValueOption>().Values = options;
        }
    }

    [HarmonyPatch(typeof(Constants), nameof(Constants.ShouldFlipSkeld))]
    class ConstantsShouldFlipSkeldPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (PlayerControl.GameOptions == null) return true;
            __result = PlayerControl.GameOptions.MapId == 3;
            return false;
        }

        public static bool aprilFools
        {
            get
            {
                try
                {
                    DateTime utcNow = DateTime.UtcNow;
                    DateTime t = new DateTime(utcNow.Year, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    DateTime t2 = t.AddDays(1.0);
                    if (utcNow >= t && utcNow <= t2)
                    {
                        return true;
                    }
                }
                catch
                {
                }
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(FreeWeekendShower), nameof(FreeWeekendShower.Start))]
    class FreeWeekendShowerPatch
    {
        public static bool Prefix()
        {
            return ConstantsShouldFlipSkeldPatch.aprilFools;
        }
    }

    [HarmonyPatch]
    class GameOptionsDataPatch
    {
        public static string tl(string key)
        {
            return Language.Language.GetString(key);
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(GameOptionsData).GetMethods().Where(x => x.ReturnType == typeof(string) && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(int));
        }

        public static string optionToString(CustomOption option)
        {
            if (option == null) return "";
            return $"{option.getName(true)}: {option.getString()}";
        }

        public static string optionsToString(CustomOption option, bool skipFirst = false)
        {
            if (option == null) return "";

            List<string> options = new List<string>();
            if (!option.IsHiddenOnDisplay(CustomOption.CurrentGameMode) && !skipFirst) options.Add(optionToString(option));
            if (option.enabled)
            {
                foreach (CustomOption op in option.children)
                {
                    string str = optionsToString(op);
                    if (str != "") options.Add(str);
                }
            }
            return string.Join("\n", options);
        }

        private static void Postfix(ref string __result)
        {
            CustomOption.CurrentGameMode = CustomGameModes.GetGameMode(CustomOptionHolder.gameMode.getSelection());

            List<string> pages = new List<string>();
            pages.Add(__result);

            StringBuilder entry = new StringBuilder();
            List<string> entries = new List<string>();

            // First add the presets and the role counts
            entries.Add(optionToString(CustomOptionHolder.presetSelection));

            var optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), tl("option.crewmateRoles"));
            var min = CustomOptionHolder.crewmateRolesCountMin.getSelection();
            var max = CustomOptionHolder.crewmateRolesCountMax.getSelection();
            if (min > max) min = max;
            var optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");

            optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), tl("option.neutralRoles"));
            min = CustomOptionHolder.neutralRolesCountMin.getSelection();
            max = CustomOptionHolder.neutralRolesCountMax.getSelection();
            if (min > max) min = max;
            optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");

            optionName = CustomOptionHolder.cs(new Color(204f / 255f, 204f / 255f, 0, 1f), tl("option.impostorRoles"));
            min = CustomOptionHolder.impostorRolesCountMin.getSelection();
            max = CustomOptionHolder.impostorRolesCountMax.getSelection();
            if (min > max) min = max;
            optionValue = (min == max) ? $"{max}" : $"{min} - {max}";
            entry.AppendLine($"{optionName}: {optionValue}");

            entries.Add(entry.ToString().Trim('\r', '\n'));

            void addChildren(CustomOption option, ref StringBuilder builder, bool indent = true,string inheritIndent = "")
            {
                if (!option.enabled || option.IsHiddenOnDisplay(CustomOption.CurrentGameMode)) return;

                foreach (var child in option.children)
                {
                    if (!(child.IsHiddenOnDisplay(CustomOption.CurrentGameMode)))
                        builder.AppendLine((indent ? "    " : "") + inheritIndent+ optionToString(child));
                    addChildren(child, ref builder, indent, inheritIndent + (indent ? "    " : ""));
                }
            }

            foreach (CustomOption option in CustomOption.options)
            {
                if (option.isHiddenOnDisplay)
                {
                    continue;
                }

                if (option.parent == null)
                {
                    if (!option.enabled || option.IsHiddenOnDisplay(CustomOption.CurrentGameMode))
                    {
                        continue;
                    }

                    entry = new StringBuilder();
                    entry.AppendLine(optionToString(option));
                    addChildren(option, ref entry, !option.isHidden);
                    entries.Add(entry.ToString().Trim('\r', '\n'));
                }
            }

            int maxLines = 28;
            int lineCount = 0;
            string page = "";
            foreach (var e in entries)
            {
                int lines = e.Count(c => c == '\n') + 1;

                if (lineCount + lines > maxLines)
                {
                    pages.Add(page);
                    page = "";
                    lineCount = 0;
                }

                page = page + e + "\n\n";
                lineCount += lines + 1;
            }

            page = page.Trim('\r', '\n');
            if (page != "")
            {
                pages.Add(page);
            }

            int numPages = pages.Count;
            int counter = CustomOptionHolder.optionsPage = CustomOptionHolder.optionsPage % numPages;

            __result = pages[counter].Trim('\r', '\n') + "\n\n" + tl("option.display.pressTabForMore") + $" ({counter + 1}/{numPages})";
        }
    }

    [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Deserialize),typeof(Il2CppSystem.IO.BinaryReader))]
    public static class GameOptionsDeserializePatch
    {
        static private int NumImpostors = PlayerControl.GameOptions.NumImpostors;
        public static bool Prefix(GameOptionsData __instance)
        {
            NumImpostors = PlayerControl.GameOptions.NumImpostors;
            return true;
        }

        public static void Postfix(GameOptionsData __instance)
        {
            PlayerControl.GameOptions.NumImpostors = NumImpostors;
        }
    }

    [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Serialize))]
    public static class GameOptionsSerializePatch
    {
        static private int NumImpostors= PlayerControl.GameOptions.NumImpostors;
        public static bool Prefix(GameOptionsData __instance)
        {
            NumImpostors = PlayerControl.GameOptions.NumImpostors;
            if (NumImpostors == 0)
            {
                PlayerControl.GameOptions.NumImpostors = 1;
            }else if (NumImpostors > 3)
            {
                PlayerControl.GameOptions.NumImpostors = 3;
            }
            return true;
        }

        public static void Postfix(GameOptionsData __instance)
        {
            PlayerControl.GameOptions.NumImpostors = NumImpostors;
        }
    }

    [HarmonyPatch(typeof(SaveManager), "GameHostOptions", MethodType.Getter)]
    public static class SaveManagerGameHostOptionsPatch
    {
        private static int numImpostors;
        public static void Prefix()
        {
            if (SaveManager.hostOptionsData == null)
            {
                SaveManager.hostOptionsData = SaveManager.LoadGameOptions("gameHostOptions");
            }

            numImpostors = SaveManager.hostOptionsData.NumImpostors;
        }

        public static void Postfix(ref GameOptionsData __result)
        {
            __result.NumImpostors = numImpostors;
        }
    }

    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    public static class GameOptionsNextPagePatch
    {
        public static void Postfix(KeyboardJoystick __instance)
        {
            if (Input.GetKeyDown(KeyCode.Tab) && AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
            {
                CustomOptionHolder.optionsPage = CustomOptionHolder.optionsPage + 1;
            }
        }
    }


    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class GameSettingsScalePatch
    {
        public static void Prefix(HudManager __instance)
        {
            if (__instance.GameSettings != null) __instance.GameSettings.fontSize = 1.2f;
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.Start))]
    public class CreateOptionsPickerPatch
    {
        public static void Postfix(CreateOptionsPicker __instance)
        {
            int numImpostors = __instance.GetTargetOptions().NumImpostors;
            if (numImpostors > 3)
            {
                numImpostors = 3;
            }else if (numImpostors < 1)
            {
                numImpostors = 1;
            }
            __instance.SetImpostorButtons(numImpostors);
        }
    }

    [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.ToggleMapFilter))]
    public static class GameOptionsData_ToggleMapFilter_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(GameOptionsData __instance, [HarmonyArgument(0)] byte mapId)
        {
            __instance.MapId ^= (byte)(1 << (int)mapId);
            if (__instance.MapId == 0)
            {
                __instance.MapId ^= (byte)(1 << (int)mapId);
            }
            return false;
        }
    }
}

