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
    public enum MapOptionType
    {
        Vent = 0,
        Console = 1,
        Blueprint = 2,
        Wiring = 3
    }

    static public NebulaConfiguration GameModeOption = new(null, "options.gamemode", null, CustomGameMode.AllGameMode.Count - 1, 0, 0);

    static public List<(NebulaConfiguration.NebulaByteConfiguration configuration, MapOptionType type, Vector2 position)>[] MapCustomizations = new List<(NebulaConfiguration.NebulaByteConfiguration, MapOptionType,Vector2)>[]{
        new(),
        new(),
        new(),
        null!,
        new(),
    };

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
    static public NebulaConfiguration MapEditorOption = new NebulaConfiguration(MapOptions, ()=> new MetaContext.Button(() => OpenMapEditor(MetaScreen.GenerateWindow(new(7.5f, 4.5f), HudManager.Instance.transform, Vector3.zero, true, false, true)), TextAttribute.BoldAttr) { Alignment =IMetaContext.AlignmentOption.Center, TranslationKey = "options.map.customization" });
    static public NebulaConfiguration[] MapCustomizationOptions = new NebulaConfiguration[]
    {
        new NebulaConfiguration(null,"options.map.customization.skeld",null,int.MaxValue,int.MaxValue,int.MaxValue),
        new NebulaConfiguration(null,"options.map.customization.mira",null,int.MaxValue,int.MaxValue,int.MaxValue),
        new NebulaConfiguration(null,"options.map.customization.polus",null,int.MaxValue,int.MaxValue,int.MaxValue),
        null!,
        new NebulaConfiguration(null,"options.map.customization.airship",null,int.MaxValue,int.MaxValue,int.MaxValue),
    };

    static private NebulaConfiguration.NebulaByteConfiguration GenerateMapCustomization(byte mapId, MapOptionType type,string id,bool defaultValue,Vector2 pos) {
        id = "options.map.customization." + AmongUsUtil.ToMapName(mapId) + "." + id;
        NebulaConfiguration.NebulaByteConfiguration option = new(MapCustomizationOptions[mapId], id, MapCustomizations[mapId].Count, defaultValue);
        MapCustomizations[mapId].Add((option, type, pos));
        return option;
    }
    static public NebulaConfiguration.NebulaByteConfiguration SkeldAdminOption = GenerateMapCustomization(0, MapOptionType.Console, "useAdmin",true,new(4.7f,-8.6f));
    static public NebulaConfiguration.NebulaByteConfiguration SkeldCafeVentOption = GenerateMapCustomization(0, MapOptionType.Vent, "cafeteriaVent", false, new(-2.4f, 5f));
    static public NebulaConfiguration.NebulaByteConfiguration SkeldStorageVentOption = GenerateMapCustomization(0, MapOptionType.Vent, "storageVent", false, new(-1f, -16.7f));
    static public NebulaConfiguration.NebulaByteConfiguration MiraAdminOption = GenerateMapCustomization(1, MapOptionType.Console, "useAdmin", true, new(20f, 19f));
    static public NebulaConfiguration.NebulaByteConfiguration PolusAdminOption = GenerateMapCustomization(2, MapOptionType.Console, "useAdmin", true, new(24f, -21.5f));
    static public NebulaConfiguration.NebulaByteConfiguration PolusSpecimenVentOption = GenerateMapCustomization(2, MapOptionType.Vent, "specimenVent", false, new(37f, -22f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipCockpitAdminOption = GenerateMapCustomization(4, MapOptionType.Console, "useCockpitAdmin", true, new(-22f, 1f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipRecordAdminOption = GenerateMapCustomization(4, MapOptionType.Console, "useRecordsAdmin", true, new(19.9f, 12f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipMeetingVentOption = GenerateMapCustomization(4, MapOptionType.Vent, "meetingVent", false, new(6.6f, 14f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipElectricalVentOption = GenerateMapCustomization(4, MapOptionType.Vent, "electricalVent", false, new(16.3f, -8.8f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipOneWayMeetingRoomOption = GenerateMapCustomization(4, MapOptionType.Blueprint, "oneWayMeetingRoom", false, new(13.5f, 10f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipArmoryWireOption = GenerateMapCustomization(4, MapOptionType.Wiring, "armoryWiring", false, new(-11.3f, -7.4f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipVaultWireOption = GenerateMapCustomization(4, MapOptionType.Wiring, "vaultWiring", false, new(-11.5f, 12.5f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipHallwayWireOption = GenerateMapCustomization(4, MapOptionType.Wiring, "hallwayWiring", false, new(-10.3f, -0.25f));
    static public NebulaConfiguration.NebulaByteConfiguration AirshipMedicalWireOption = GenerateMapCustomization(4, MapOptionType.Wiring, "medicalWiring", false, new(27f, -5f));

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

    static private XOnlyDividedSpriteLoader mapCustomizationSprite = XOnlyDividedSpriteLoader.FromResource("Nebula.Resources.MapCustomizations.png", 100f, 50, true);
    static void OpenMapEditor(MetaScreen screen, byte? mapId = null)
    {
        mapId ??= AmongUsUtil.CurrentMapId;

        MetaContext context = new();

        byte Lessen()
        {
            byte id = mapId.Value;
            while (true)
            {
                if (id == 0)
                    id = (byte)(MapCustomizations.Length - 1);
                else
                    id--;
                if (MapCustomizations[id] != null) return id;
            }
        }

        byte Increase()
        {
            byte id = mapId.Value;
            while (true)
            {
                if (id == (byte)(MapCustomizations.Length - 1))
                    id = 0;
                else
                    id++;
                if (MapCustomizations[id] != null) return id;
            }
        }

        context.Append(new CombinedContext(
            new MetaContext.Button(() => OpenMapEditor(screen, Lessen()), new(TextAttribute.BoldAttr) { Size = new(0.2f, 0.2f) }) { RawText = "<<"},
            new MetaContext.Text(TextAttribute.BoldAttr) { RawText = Constants.MapNames[mapId.Value] },
            new MetaContext.Button(() => OpenMapEditor(screen, Increase()), new(TextAttribute.BoldAttr) { Size = new(0.2f, 0.2f) }) { RawText = ">>" }
            ));
        if (mapId.Value is 0 or 4) context.Append(new MetaContext.VerticalMargin(0.35f));
        context.Append(MetaContext.Image.AsMapImage(mapId.Value, 5.6f, MapCustomizations[mapId.Value],
            (c) => (new MetaContext.Image(mapCustomizationSprite.GetSprite((int)c.type))
            {
                Width = 0.5f,
                PostBuilder = (renderer) =>
                {
                    renderer.color = c.configuration.CurrentValue ? Color.white : Color.red.RGBMultiplied(0.65f);
                    var button = renderer.gameObject.SetUpButton(true);
                    button.OnMouseOver.AddListener(() => {
                        MetaContext context = new();
                        context.Append(new MetaContext.VariableText(TextAttribute.BoldAttr) { Alignment = IMetaContext.AlignmentOption.Left, TranslationKey = c.configuration.Id }).Append(NebulaConfiguration.GetDetailContext(c.configuration.Id + ".detail"));
                        NebulaManager.Instance.SetHelpContext(button,context);
                    });
                    button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpContext());
                    button.OnClick.AddListener(() => { c.configuration.ToggleValue(); renderer.color = c.configuration.CurrentValue ? Color.white : Color.red.RGBMultiplied(0.65f); });
                    var collider = button.gameObject.AddComponent<BoxCollider2D>();
                    collider.isTrigger = true;
                    collider.size = new(0.5f, 0.5f);
                }
            }, c.position)));

        screen.SetContext(context);
    }

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
