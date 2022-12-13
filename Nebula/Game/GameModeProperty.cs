namespace Nebula.Game;

public class GameModeProperty
{
    public Module.CustomGameMode RelatedGameMode { private set; get; }
    public bool RequireImpostors { private set; get; }
    public bool RequireGhosts { private set; get; }
    public bool RequireStartCountDown { private set; get; }
    public int MinPlayers { private set; get; }
    public bool CountTasks { private set; get; }
    public Module.CustomOptionTab Tabs { private set; get; }

    public Roles.Role DefaultImpostorRole { private set; get; }
    public Roles.Role DefaultCrewmateRole { private set; get; }

    public System.Action OnCountFinished { private set; get; }

    public GameModeProperty(Module.CustomGameMode RelatedMode, int MinPlayers, bool RequireImpostors, bool RequireGhosts, bool CountTasks, System.Action? RequireStartCountDown, Roles.Role DefaultCrewmate, Roles.Role DefaultImpostor, Module.CustomOptionTab Tabs)
    {
        this.RelatedGameMode = RelatedMode;
        this.RequireImpostors = RequireImpostors;
        this.RequireGhosts = RequireGhosts;
        this.CountTasks = CountTasks;
        this.MinPlayers = MinPlayers;
        this.Tabs = Tabs;
        if (RequireStartCountDown == null)
        {
            this.OnCountFinished = () => { };
            this.RequireStartCountDown = false;
        }
        else
        {
            this.OnCountFinished = RequireStartCountDown;
            this.RequireStartCountDown = true;
        }
        this.DefaultCrewmateRole = DefaultCrewmate;
        this.DefaultImpostorRole = DefaultImpostor;

        Properties[RelatedMode] = this;
    }

    static public Dictionary<Module.CustomGameMode, GameModeProperty> Properties = new Dictionary<Module.CustomGameMode, GameModeProperty>();

    static public GameModeProperty StandardMode;
    static public GameModeProperty MinigameMode;
    static public GameModeProperty RitualMode;
    static public GameModeProperty InvestigatorsMode;
    static public GameModeProperty FreePlayMode;
    static public GameModeProperty StandardHnSMode;

    static public void Load()
    {
        StandardMode = new GameModeProperty(Module.CustomGameMode.Standard, 4, true, false, true, null, Roles.Roles.Crewmate, Roles.Roles.Impostor,
            Module.CustomOptionTab.Settings | Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.ImpostorRoles | Module.CustomOptionTab.NeutralRoles | Module.CustomOptionTab.GhostRoles | Module.CustomOptionTab.Modifiers | Module.CustomOptionTab.AdvancedSettings);
        MinigameMode = new GameModeProperty(Module.CustomGameMode.Minigame, 2, false, false, true, () => { Roles.MinigameRoles.MinigameRoleAssignment.Assign(); }, Roles.Roles.Player, Roles.Roles.Impostor,
            Module.CustomOptionTab.Settings | Module.CustomOptionTab.EscapeRoles);
        RitualMode = new GameModeProperty(Module.CustomGameMode.Ritual, 3, true, false, false, null, Roles.Roles.RitualCrewmate, Roles.Roles.RitualKiller, Module.CustomOptionTab.Settings);
        InvestigatorsMode = new GameModeProperty(Module.CustomGameMode.Investigators, 2, false, true, false, null, Roles.Roles.Crewmate, Roles.Roles.Impostor, Module.CustomOptionTab.None);
        FreePlayMode = new GameModeProperty(Module.CustomGameMode.FreePlay, 0, false, false, false, null, Roles.Roles.Player, Roles.Roles.Impostor,
            Module.CustomOptionTab.Settings | Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.ImpostorRoles | Module.CustomOptionTab.NeutralRoles | Module.CustomOptionTab.GhostRoles | Module.CustomOptionTab.Modifiers | Module.CustomOptionTab.AdvancedSettings | Module.CustomOptionTab.EscapeRoles);
        StandardHnSMode = new GameModeProperty(Module.CustomGameMode.StandardHnS, 4, true, false, false, null, Roles.Roles.Crewmate, Roles.Roles.Impostor,
            Module.CustomOptionTab.Settings | Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.ImpostorRoles | Module.CustomOptionTab.AdvancedSettings);
    }

    static public GameModeProperty GetProperty(Module.CustomGameMode gameMode)
    {
        return Properties[gameMode];
    }
}