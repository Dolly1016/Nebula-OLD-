using System.Collections.Generic;

namespace Nebula.Roles
{
    static public class Roles
    {

        public static CrewmateRoles.Crewmate Crewmate = new CrewmateRoles.Crewmate();
        public static CrewmateRoles.Agent Agent = new CrewmateRoles.Agent();
        public static CrewmateRoles.Alien Alien = new CrewmateRoles.Alien();
        public static CrewmateRoles.Bait Bait = new CrewmateRoles.Bait();
        public static CrewmateRoles.Comet Booster = new CrewmateRoles.Comet();
        public static CrewmateRoles.DamnedCrew DamnedCrew = new CrewmateRoles.DamnedCrew();
        public static CrewmateRoles.Doctor Doctor = new CrewmateRoles.Doctor();
        public static CrewmateRoles.Mayor Mayor = new CrewmateRoles.Mayor();
        public static CrewmateRoles.Necromancer Necromancer = new CrewmateRoles.Necromancer();
        public static ComplexRoles.Guesser NiceGuesser = new ComplexRoles.Guesser("NiceGuesser", "niceGuesser", false);
        public static ComplexRoles.Trapper NiceTrapper = new ComplexRoles.Trapper("NiceTrapper", "niceTrapper", false);
        public static CrewmateRoles.Oracle Oracle = new CrewmateRoles.Oracle();
        public static CrewmateRoles.Provocateur Provocateur = new CrewmateRoles.Provocateur();
        public static CrewmateRoles.Psychic Psychic = new CrewmateRoles.Psychic();
        public static CrewmateRoles.Navvy Navvy = new CrewmateRoles.Navvy();
        public static CrewmateRoles.Seer Seer = new CrewmateRoles.Seer();
        public static CrewmateRoles.Sheriff Sheriff = new CrewmateRoles.Sheriff();
        public static CrewmateRoles.Spy Spy = new CrewmateRoles.Spy();

        public static CrewmateRoles.Madmate Madmate = new CrewmateRoles.Madmate();

        public static ImpostorRoles.Impostor Impostor = new ImpostorRoles.Impostor();
        public static ImpostorRoles.Camouflager Camouflager = new ImpostorRoles.Camouflager();
        public static ImpostorRoles.Cleaner Cleaner = new ImpostorRoles.Cleaner();
        public static ImpostorRoles.Damned Damned = new ImpostorRoles.Damned();
        public static ImpostorRoles.Disturber Disturber = new ImpostorRoles.Disturber();
        public static ImpostorRoles.Eraser Eraser = new ImpostorRoles.Eraser();
        public static ImpostorRoles.EvilAce EvilAce = new ImpostorRoles.EvilAce();
        public static ComplexRoles.Guesser EvilGuesser = new ComplexRoles.Guesser("EvilGuesser", "evilGuesser", true);
        public static ComplexRoles.Trapper EvilTrapper = new ComplexRoles.Trapper("EvilTrapper", "evilTrapper", true);
        public static ImpostorRoles.Morphing Morphing = new ImpostorRoles.Morphing();
        public static ImpostorRoles.Reaper Reaper = new ImpostorRoles.Reaper();
        public static ImpostorRoles.Sniper Sniper = new ImpostorRoles.Sniper();

        public static NeutralRoles.Arsonist Arsonist = new NeutralRoles.Arsonist();
        public static NeutralRoles.Avenger Avenger = new NeutralRoles.Avenger();
        public static NeutralRoles.Empiric Empiric = new NeutralRoles.Empiric();
        public static NeutralRoles.Jackal Jackal = new NeutralRoles.Jackal();
        public static NeutralRoles.Sidekick Sidekick = new NeutralRoles.Sidekick();
        public static NeutralRoles.Jester Jester = new NeutralRoles.Jester();
        public static NeutralRoles.Opportunist Opportunist = new NeutralRoles.Opportunist();
        public static NeutralRoles.Vulture Vulture = new NeutralRoles.Vulture();

        public static InvestigatorRoles.Investigator Investigator = new InvestigatorRoles.Investigator();

        public static ParlourRoles.Gambler Gambler = new ParlourRoles.Gambler();

        public static MinigameRoles.Player Player = new MinigameRoles.Player();

        public static MinigameRoles.Escapees.Biela Biela = new MinigameRoles.Escapees.Biela();
        public static MinigameRoles.Escapees.Halley Halley = new MinigameRoles.Escapees.Halley();

        public static MinigameRoles.Hunters.Hadar Hadar = new MinigameRoles.Hunters.Hadar();
        public static MinigameRoles.Hunters.Polis Polis = new MinigameRoles.Hunters.Polis();

        
        public static ComplexRoles.FCrewmate F_Crewmate = new ComplexRoles.FCrewmate();
        public static ComplexRoles.FGuesser F_Guesser = new ComplexRoles.FGuesser();
        public static ComplexRoles.FTrapper F_Trapper = new ComplexRoles.FTrapper();

        public static ComplexRoles.SecondaryGuesser SecondaryGuesser = new ComplexRoles.SecondaryGuesser();
        public static NeutralRoles.SecondarySidekick SecondarySidekick = new NeutralRoles.SecondarySidekick();
        public static ExtraRoles.Drunk Drunk = new ExtraRoles.Drunk();
        public static ExtraRoles.Lover Lover = new ExtraRoles.Lover();
        public static ExtraRoles.Trilemma Trilemma = new ExtraRoles.Trilemma();
        public static ExtraRoles.AvengerTarget AvengerTarget = new ExtraRoles.AvengerTarget();
        public static MetaRoles.MetaRole MetaRole = new MetaRoles.MetaRole();


        //全てのロールはこの中に含まれている必要があります
        public static List<Role> AllRoles = new List<Role>()
        {
            Impostor,Camouflager,Cleaner,Damned,Disturber,Eraser,EvilAce,EvilGuesser,EvilTrapper,Morphing,Reaper,Sniper,
            F_Guesser,F_Trapper,
            Arsonist,Avenger,Empiric,Jackal,Jester,Opportunist,Sidekick,Vulture,
            F_Crewmate,
            Crewmate,Agent,Alien,Bait,Booster,DamnedCrew,Doctor,Mayor,Navvy,Necromancer,NiceGuesser,NiceTrapper,Oracle,Provocateur,Psychic,Seer,Sheriff,Spy,
            Madmate,
            Player,
            Halley,Biela,
            Polis,Hadar,
            Gambler,
            Investigator
            
        };

        public static List<ExtraRole> AllExtraRoles = new List<ExtraRole>()
        {
            SecondaryGuesser,SecondarySidekick,
            Drunk,Lover,Trilemma,
            MetaRole,AvengerTarget
        };

        public static void ResetWinTrigger()
        {
            foreach(Role role in AllRoles)
            {
                if(role is Template.HasWinTrigger)
                {
                    ((Template.HasWinTrigger)role).WinTrigger = false;
                }
            }

            foreach (ExtraRole role in AllExtraRoles)
            {
                if (role is Template.HasWinTrigger)
                {
                    ((Template.HasWinTrigger)role).WinTrigger = false;
                }
            }
        }

        public static void StaticInitialize()
        {
            foreach (Role role in AllRoles)
            {
                role.StaticInitialize();
            }

            foreach (ExtraRole role in AllExtraRoles)
            {
                role.StaticInitialize();
            }

            foreach (Role role in AllRoles)
            {
                role.OnRoleRelationSetting();
            }
        }
    }
}
