using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Game
{
    public class GameModeProperty
    {
        public Module.CustomGameMode RelatedGameMode { private set; get; }
        public bool RequireImpostors { private set; get; }
        public bool RequireGhosts { private set; get; }
        public bool RequireStartCountDown { private set; get; }
        public int MinPlayers { private set; get; }

        public Roles.Role DefaultImpostorRole { private set; get; }
        public Roles.Role DefaultCrewmateRole { private set; get; }

        public System.Action OnCountFinished { private set; get; }

        public GameModeProperty(Module.CustomGameMode RelatedMode,int MinPlayers, bool RequireImpostors, bool RequireGhosts,System.Action? RequireStartCountDown,Roles.Role DefaultCrewmate, Roles.Role DefaultImpostor) {
            this.RelatedGameMode = RelatedMode;
            this.RequireImpostors = RequireImpostors;
            this.RequireGhosts = RequireGhosts;
            this.MinPlayers = MinPlayers;
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
        static public GameModeProperty ParlourMode;
        static public GameModeProperty InvestigatorsMode;
        static public GameModeProperty FreePlayMode;

        static public void Load()
        {
            StandardMode = new GameModeProperty(Module.CustomGameMode.Standard, 4, true, false, null, Roles.Roles.Crewmate, Roles.Roles.Impostor);
            MinigameMode = new GameModeProperty(Module.CustomGameMode.Minigame, 2, false, false, () => { Roles.MinigameRoles.MinigameRoleAssignment.Assign(); }, Roles.Roles.Player, Roles.Roles.Impostor);
            ParlourMode = new GameModeProperty(Module.CustomGameMode.Parlour, 2, false, false, null, Roles.Roles.Gambler, Roles.Roles.Impostor);
            InvestigatorsMode = new GameModeProperty(Module.CustomGameMode.Investigators, 2, false, true, null, Roles.Roles.Investigator, Roles.Roles.Impostor);
            FreePlayMode = new GameModeProperty(Module.CustomGameMode.FreePlay, 0, false, false, null, Roles.Roles.Player, Roles.Roles.Impostor);
        }

        static public GameModeProperty GetProperty(Module.CustomGameMode gameMode)
        {
            return Properties[gameMode];
        }
    }
}
