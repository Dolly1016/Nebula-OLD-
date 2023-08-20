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

    static public CustomGameMode CurrentGameMode => CustomGameMode.AllGameMode[GameModeOption.CurrentValue];
}
