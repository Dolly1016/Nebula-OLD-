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

        public Roles.Role DefaultImpostorRole { private set; get; }
        public Roles.Role DefaultCrewmateRole { private set; get; }

        public GameModeProperty(Module.CustomGameMode RelatedMode,bool RequireImpostors, bool RequireGhosts,Roles.Role DefaultCrewmate, Roles.Role DefaultImpostor) {
            this.RelatedGameMode = RelatedMode;
            this.RequireImpostors = RequireImpostors;
            this.RequireGhosts = RequireGhosts;
            this.DefaultCrewmateRole = DefaultCrewmate;
            this.DefaultImpostorRole = DefaultImpostor;

            Properties[RelatedMode] = this;
        }

        static public Dictionary<Module.CustomGameMode, GameModeProperty> Properties = new Dictionary<Module.CustomGameMode, GameModeProperty>();

        static public GameModeProperty StandardMode;
        static public GameModeProperty ParlourMode;
        static public GameModeProperty InvestigatorsMode;

        static public void Load()
        {
            StandardMode = new GameModeProperty(Module.CustomGameMode.Standard, true, false, Roles.Roles.Crewmate, Roles.Roles.Impostor);
            ParlourMode = new GameModeProperty(Module.CustomGameMode.Parlour, false, false, Roles.Roles.Gambler, Roles.Roles.Impostor);
            InvestigatorsMode = new GameModeProperty(Module.CustomGameMode.Investigators, false, true, Roles.Roles.Investigator, Roles.Roles.Impostor);
        }

        static public GameModeProperty GetProperty(Module.CustomGameMode gameMode)
        {
            return Properties[gameMode];
        }
    }
}
