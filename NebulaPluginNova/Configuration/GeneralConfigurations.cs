using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Il2CppMono.Security.X509.X520;

namespace Nebula.Configuration;

[NebulaPreLoad(typeof(Roles.Roles))]
[NebulaOptionHolder]
public static class GeneralConfigurations
{
    static public NebulaConfiguration GameModeOption = new(null, "options.gamemode", null, CustomGameMode.AllGameMode.Count - 1, 0, 0);

    /*
    static public ConfigurationHolder MeetingOptions = new("options.meeting", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public NebulaConfiguration MeetingOption1 = new(MeetingOptions, "very_very_long_option_name", null, 4, 2, 0);

    static public ConfigurationHolder MapOptions = new("options.map", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public ConfigurationHolder DeviceOptions = new("options.device", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public ConfigurationHolder SabotageOptions = new("options.sabotage", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public ConfigurationHolder TimeLimitOptions = new("options.timeLimit", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public ConfigurationHolder FreePlayOptions = new("options.freePlayDummies", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    */

    static public ConfigurationHolder AssignmentOptions = new("options.assignment", null, ConfigurationTab.Settings, CustomGameMode.Standard);

    static public ConfigurationHolder SoloFreePlayOptions = new("options.soloFreePlay", null, ConfigurationTab.Settings, CustomGameMode.FreePlay);
    static public NebulaConfiguration NumOfDummiesOption = new NebulaConfiguration(SoloFreePlayOptions, "numOfDummies", null, 0, 14, 0, 0);


    static private Func<object?, string> AssignmentDecorator = (obj) => (int)obj! == -1 ? Language.Translate("options.assignment.unlimited") : obj.ToString()!;
    static public NebulaConfiguration AssignmentCrewmateOption = new NebulaConfiguration(AssignmentOptions, "crewmate", null, -1, 15, -1, -1) { Decorator = AssignmentDecorator };
    static public NebulaConfiguration AssignmentImpostorOption = new NebulaConfiguration(AssignmentOptions, "impostor", null, -1, 3, -1, -1) { Decorator = AssignmentDecorator };
    static public NebulaConfiguration AssignmentNeutralOption = new NebulaConfiguration(AssignmentOptions, "neutral", null, -1, 15, 0, 0) { Decorator = AssignmentDecorator };

    static public ConfigurationHolder MapOptions = new("options.map", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public NebulaConfiguration SpawnMethodOption = new(MapOptions, "spawnMethod", null,
        new string[] { "options.map.spawnMethod.default", "options.map.spawnMethod.selective", "options.map.spawnMethod.random" }, 0, 0);
    static public NebulaConfiguration SpawnCandidatesOption = new NebulaConfiguration(MapOptions, "spawnCandidates", null, 1, 8, 1, 1) { Predicate = () => (SpawnMethodOption.GetString()?.Equals("options.map.spawnMethod.selective")) ?? false };
    static public NebulaConfiguration SilentVentOption = new NebulaConfiguration(MapOptions, "silentVents", null, false, false);

    static public ConfigurationHolder MeetingOptions = new("options.meeting", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public NebulaConfiguration NoticeExtraVictimsOption = new NebulaConfiguration(MeetingOptions, "noticeExtraVictims", null, false, false);

    static public ConfigurationHolder ExclusiveAssignmentOptions = new("options.exclusiveAssignment", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public ExclusiveAssignmentConfiguration ExclusiveOptionBody = new(ExclusiveAssignmentOptions, 10);

    static public ConfigurationHolder VoiceChatOptions = new("options.voiceChat", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public NebulaConfiguration UseVoiceChatOption = new NebulaConfiguration(VoiceChatOptions, "useVoiceChat", null, false, false);
    static public NebulaConfiguration WallsBlockAudioOption = new NebulaConfiguration(VoiceChatOptions, "wallsBlockAudio", null, false, false) { Predicate = () => UseVoiceChatOption };
    static public NebulaConfiguration KillersHearDeadOption = new(VoiceChatOptions, "killersHearDead", null,
    new string[] { "options.switch.off", "options.voiceChat.killersHearDead.onlyImpostors", "options.switch.on" }, 0, 0)
    { Predicate = () => UseVoiceChatOption };
    static public NebulaConfiguration ImpostorsRadioOption = new NebulaConfiguration(VoiceChatOptions, "impostorsRadio", null, false, false) { Predicate = () => UseVoiceChatOption };
    static public NebulaConfiguration JackalRadioOption = new NebulaConfiguration(VoiceChatOptions, "jackalRadio", null, false, false) { Predicate = () => UseVoiceChatOption };
    static public NebulaConfiguration AffectedByCommsSabOption = new NebulaConfiguration(VoiceChatOptions, "affectedByCommsSab", null, false, false) { Predicate = () => UseVoiceChatOption };

    static IEnumerable<object?> RestrictionSelections()
    {
        yield return null;
        float time = 0f;
        while (time < 10f)
        {
            time += 1f;
            yield return time;
        }
        while (time < 120f)
        {
            time += 5f;
            yield return time;
        }
    }
    static string RestrictionDecorator(object? val)
    {
        if (val is null) return Language.Translate("options.consoleRestriction.unlimited");
        return NebulaConfiguration.SecDecorator(val);
    }

    static public ConfigurationHolder ConsoleRestrictionOptions = new("options.consoleRestriction", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public NebulaConfiguration ResetRestrictionsOption = new NebulaConfiguration(ConsoleRestrictionOptions, "resetRestrictions", null, true, true);
    static public NebulaConfiguration AdminRestrictionOption = new NebulaConfiguration(ConsoleRestrictionOptions, "adminRestriction", null, RestrictionSelections().ToArray(), null, null, RestrictionDecorator);
    static public NebulaConfiguration VitalsRestrictionOption = new NebulaConfiguration(ConsoleRestrictionOptions, "vitalsRestriction", null, RestrictionSelections().ToArray(), null, null, RestrictionDecorator);
    static public NebulaConfiguration CameraRestrictionOption = new NebulaConfiguration(ConsoleRestrictionOptions, "cameraRestriction", null, RestrictionSelections().ToArray(), null, null, RestrictionDecorator);


    static public CustomGameMode CurrentGameMode => CustomGameMode.AllGameMode[GameModeOption.CurrentValue];

    public class ExclusiveAssignmentConfiguration
    {
        private static Func<string, int> RoleMapper = (name) =>
        {
            if (name == "none") return short.MaxValue;
            return Roles.Roles.AllRoles.FirstOrDefault((role) => role.LocalizedName == name)?.Id ?? short.MaxValue;
        };

        private static Func<int,string> RoleSerializer = (id) =>
        {
            if (id == short.MaxValue) return "none";
            return Roles.Roles.AllRoles.FirstOrDefault((role) => role.Id == id)?.LocalizedName ?? "none";
        };

        public class ExclusiveAssignment
        {
            NebulaConfiguration toggleOption = null!;
            NebulaStringConfigEntry[] roles = null!;
            public ExclusiveAssignment(ConfigurationHolder holder,int index)
            {
                toggleOption = new(holder, "category." + index, null, false, false);
                toggleOption.Editor = () =>
                {
                    MetaContext context = new();

                    List<IMetaParallelPlacable> contents = new();
                    contents.Add(NebulaConfiguration.OptionButtonContext(() => toggleOption.ChangeValue(true), toggleOption.Title.Text, 0.85f));
                    contents.Add(NebulaConfiguration.OptionTextColon);


                    if (!toggleOption)
                    {
                        string innerText = "";
                        bool isFirst = true;
                        foreach (var assignment in roles)
                        {
                            var role = assignment.CurrentValue == short.MaxValue ? null : Roles.Roles.AllRoles[assignment.CurrentValue];
                            if (role == null) continue;
                            if (!isFirst) innerText += ", ";
                            innerText += role?.DisplayName.Color(role.RoleColor) ?? "None";
                            isFirst = false;
                        }
                        if (innerText.Length > 0) innerText = "(" + innerText + ")";
                        contents.Add(new MetaContext.Text(NebulaConfiguration.GetOptionBoldAttr(4.8f,TMPro.TextAlignmentOptions.Left)) { RawText = Language.Translate("options.inactivated") + " " + innerText.Color(Color.gray) });
                    }
                    else
                    {
                        foreach (var assignment in roles)
                        {
                            var role = assignment.CurrentValue == short.MaxValue ? null : Roles.Roles.AllRoles[assignment.CurrentValue];

                            var copiedAssignment = assignment;
                            contents.Add(new MetaContext.Button(() =>
                            {
                                MetaScreen screen = MetaScreen.GenerateWindow(new(6.5f, 3f), HudManager.Instance.transform, Vector3.zero, true, true);
                                MetaContext inner = new();
                                inner.Append(Roles.Roles.AllRoles.Prepend(null), (role) => NebulaConfiguration.OptionButtonContext(
                                    () =>
                                    {
                                        copiedAssignment.UpdateValue(role?.Id ?? short.MaxValue, true).Share();
                                        screen.CloseScreen();
                                    },
                                    role?.DisplayName.Color(role.RoleColor) ?? "None",
                                    1.1f
                                    ), 4, -1, 0, 0.45f);
                                screen.SetContext(new MetaContext.ScrollView(new Vector2(6.5f, 3f), inner));
                            }, new(NebulaConfiguration.OptionValueAttr) { Size = new(1.3f, 0.3f) })
                            { RawText = role?.DisplayName.Color(role.RoleColor) ?? "None", PostBuilder = (_, renderer, _) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask });
                        }
                    }

                    context.Append(new CombinedContext(0.65f, contents.ToArray()));
                    return context;
                };
                toggleOption.Shower = () =>
                {
                    if (!toggleOption) return null!;

                    string innerText = "";
                    bool isFirst = true;
                    foreach (var assignment in roles)
                    {
                        var role = assignment.CurrentValue == short.MaxValue ? null : Roles.Roles.AllRoles[assignment.CurrentValue];
                        if(role == null) continue;
                        if (!isFirst) innerText += ", ";
                        innerText += role?.DisplayName.Color(role.RoleColor) ?? "None";
                        isFirst = false;
                    }
                    return toggleOption.Title.Text + " : " + innerText;
                };

                roles = new NebulaStringConfigEntry[3];
                
                for (int i = 0; i < 3; i++) roles[i] = new NebulaStringConfigEntry(toggleOption.Id + ".role" + i, "none", RoleMapper, RoleSerializer);
                
            }

            public IEnumerable<AbstractRole> OnAsigned(AbstractRole role) {
                if (!toggleOption) yield break;
                if (!roles.Any(entry => entry.CurrentValue == role.Id)) yield break;

                foreach(var assignment in roles)
                {
                    if (assignment.CurrentValue == role.Id) continue;
                    if (assignment.CurrentValue == short.MaxValue) continue;

                    var r = Roles.Roles.AllRoles.FirstOrDefault((role) => role.Id == assignment.CurrentValue);
                    if(r != null) yield return r;
                }
            }
        }

        ExclusiveAssignment[] allAsignment;
        public ExclusiveAssignmentConfiguration(ConfigurationHolder holder,int num)
        {
            allAsignment = new ExclusiveAssignment[num];
            for (int i = 0; i < num; i++) allAsignment[i] = new ExclusiveAssignment(holder,i);
        }

        public IEnumerable<AbstractRole> OnAssigned(AbstractRole role) {
            foreach (var assignment in allAsignment) foreach (var r in assignment.OnAsigned(role)) yield return r;
        }
    }
}
