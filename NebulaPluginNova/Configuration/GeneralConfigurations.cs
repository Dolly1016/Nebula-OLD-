using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Configuration;

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


    static private Func<object?, string> AssignmentDecorator = (obj) => (int)obj == -1 ? Language.Translate("options.assignment.unlimited") : obj.ToString();
    static public NebulaConfiguration AssignmentCrewmateOption = new NebulaConfiguration(AssignmentOptions, "crewmate", null, -1, 15, -1, -1) { Decorator = AssignmentDecorator };
    static public NebulaConfiguration AssignmentImpostorOption = new NebulaConfiguration(AssignmentOptions, "impostor", null, -1, 3, -1, -1) { Decorator = AssignmentDecorator };
    static public NebulaConfiguration AssignmentNeutralOption = new NebulaConfiguration(AssignmentOptions, "neutral", null, -1, 15, 0, 0) { Decorator = AssignmentDecorator };

    static public ConfigurationHolder MapOptions = new("options.map", null, ConfigurationTab.Settings, CustomGameMode.Standard | CustomGameMode.FreePlay);
    static public NebulaConfiguration SpawnMethodOption = new(MapOptions, "spawnMethod", null,
        new string[] { "options.map.spawnMethod.default", "options.map.spawnMethod.selective", "options.map.spawnMethod.random" }, 0, 0);
    static public NebulaConfiguration SpawnCandidatesOption = new NebulaConfiguration(MapOptions, "spawnCandidates", null, 1, 8, 1, 1) { Predicate = () => (!SpawnMethodOption.GetString()?.Equals("options.map.spawnMethod.default")) ?? false };

    static public CustomGameMode CurrentGameMode => CustomGameMode.AllGameMode[GameModeOption.CurrentValue];
}
