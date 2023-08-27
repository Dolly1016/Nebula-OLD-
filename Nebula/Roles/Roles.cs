using Nebula.Roles.NeutralRoles;
using Nebula.Roles.Perk;

namespace Nebula.Roles;

static public class Roles
{
    public class SideCommonRoles
    {
        public Side side;
        public Role templateRole;

        public AllSideRoles.Secret? Secret;

        public SideCommonRoles(Side side, Role templateRole)
        {
            if (side == Side.Crewmate || side == Side.Impostor) Secret = new AllSideRoles.Secret(templateRole);

            SideRoles.Add(side, this);
        }

        public class SideCommonRolesLoader
        {
            public SideCommonRolesLoader()
            {
                new SideCommonRoles(Side.Crewmate, Roles.Crewmate);
                new SideCommonRoles(Side.Impostor, Roles.Impostor);
            }
        }

    }

    public static Dictionary<Side, SideCommonRoles> SideRoles = new Dictionary<Side, SideCommonRoles>();

    public static List<ExtraAssignable> AllExtraAssignable = new List<ExtraAssignable>();

    public static MetaRoles.VOID VOID = new MetaRoles.VOID();

    public static Crewmate.Crewmate Crewmate = new Crewmate.Crewmate();
    public static Crewmate.CrewmateWithoutTasks CrewmateWithoutTasks = new Crewmate.CrewmateWithoutTasks();
    public static Crewmate.Agent Agent = new Crewmate.Agent();
    public static Crewmate.Alien Alien = new Crewmate.Alien();
    public static Crewmate.Bait Bait = new Crewmate.Bait();
    public static Crewmate.Busker Busker = new Crewmate.Busker();
    public static Crewmate.Comet Comet = new Crewmate.Comet();
    public static Crewmate.DamnedCrew DamnedCrew = new Crewmate.DamnedCrew();
    public static Crewmate.Doctor Doctor = new Crewmate.Doctor();
    public static Crewmate.Guardian Guardian = new Crewmate.Guardian();
    public static Crewmate.Mayor Mayor = new Crewmate.Mayor();
    public static Crewmate.Necromancer Necromancer = new Crewmate.Necromancer();
    public static ComplexRoles.Guesser NiceGuesser = new ComplexRoles.Guesser("NiceGuesser", "niceGuesser", false);
    public static ComplexRoles.Tracker NiceTracker = new ComplexRoles.Tracker("NiceTracker", "niceTracker", false);
    public static ComplexRoles.Trapper NiceTrapper = new ComplexRoles.Trapper("NiceTrapper", "niceTrapper", false);
    public static Crewmate.Oracle Oracle = new Crewmate.Oracle();
    public static Crewmate.Provocateur Provocateur = new Crewmate.Provocateur();
    public static Crewmate.Psychic Psychic = new Crewmate.Psychic();
    public static Crewmate.Navvy Navvy = new Crewmate.Navvy();
    public static Crewmate.Seer Seer = new Crewmate.Seer();
    public static Crewmate.Sheriff Sheriff = new Crewmate.Sheriff();
    public static Crewmate.Splicer Splicer = new Crewmate.Splicer();
    public static Crewmate.Spy Spy = new Crewmate.Spy();

    public static Crewmate.Madmate Madmate = new Crewmate.Madmate();

    public static Impostor.Impostor Impostor = new Impostor.Impostor();
    public static Impostor.Banshee Banshee = new Impostor.Banshee();
    public static Impostor.BountyHunter BountyHunter = new Impostor.BountyHunter();
    public static Impostor.Camouflager Camouflager = new Impostor.Camouflager();
    public static Impostor.Cleaner Cleaner = new Impostor.Cleaner();
    public static Impostor.Covert Covert = new Impostor.Covert();
    public static Impostor.Damned Damned = new Impostor.Damned();
    public static Impostor.Disturber Disturber = new Impostor.Disturber();
    public static Impostor.Eraser Eraser = new Impostor.Eraser();
    public static Impostor.EvilAce EvilAce = new Impostor.EvilAce();
    public static ComplexRoles.Guesser EvilGuesser = new ComplexRoles.Guesser("EvilGuesser", "evilGuesser", true);
    public static ComplexRoles.Tracker EvilTracker = new ComplexRoles.Tracker("EvilTracker", "evilTracker", true);
    public static ComplexRoles.Trapper EvilTrapper = new ComplexRoles.Trapper("EvilTrapper", "evilTrapper", true);
    public static Impostor.Executioner Executioner = new Impostor.Executioner();
    public static Impostor.Jailer Jailer = new Impostor.Jailer();
    public static Impostor.Marionette Marionette = new Impostor.Marionette();
    public static Impostor.Morphing Morphing = new Impostor.Morphing();
    public static Impostor.Ninja Ninja = new Impostor.Ninja();
    public static Impostor.Painter Painter = new Impostor.Painter();
    public static Impostor.Raider Raider = new Impostor.Raider();
    public static Impostor.Reaper Reaper = new Impostor.Reaper();
    public static Impostor.Sniper Sniper = new Impostor.Sniper();

    public static NeutralRoles.Arsonist Arsonist = new NeutralRoles.Arsonist();
    public static NeutralRoles.Avenger Avenger = new NeutralRoles.Avenger();
    public static NeutralRoles.ChainShifter ChainShifter = new NeutralRoles.ChainShifter();
    public static NeutralRoles.Empiric Empiric = new NeutralRoles.Empiric();
    public static NeutralRoles.Immoralist Immoralist = new NeutralRoles.Immoralist();
    public static NeutralRoles.Jackal Jackal = new NeutralRoles.Jackal();
    public static NeutralRoles.LordLloyd LordLloyd = new NeutralRoles.LordLloyd();
    public static NeutralRoles.Sidekick Sidekick = new NeutralRoles.Sidekick();
    public static NeutralRoles.Jester Jester = new NeutralRoles.Jester();
    public static NeutralRoles.Paparazzo Paparazzo = new NeutralRoles.Paparazzo();
    public static NeutralRoles.Opportunist Opportunist = new NeutralRoles.Opportunist();
    public static NeutralRoles.Spectre Spectre = new NeutralRoles.Spectre();
    //public static NeutralRoles.SantaClaus SantaClaus = new NeutralRoles.SantaClaus();
    //public static NeutralRoles.BlackSanta BlackSanta = new NeutralRoles.BlackSanta();
    public static NeutralRoles.Vulture Vulture = new NeutralRoles.Vulture();

    public static MinigameRoles.Player Player = new MinigameRoles.Player();


    public static HnSCrewmateRoles.Crewmate HnSCrewmate = new HnSCrewmateRoles.Crewmate();

    public static HnSImpostorRoles.HnSCleaner HnSCleaner = new HnSImpostorRoles.HnSCleaner();
    public static HnSImpostorRoles.HnSHadar HnSHadar = new HnSImpostorRoles.HnSHadar();
    public static HnSImpostorRoles.HnSRaider HnSRaider = new HnSImpostorRoles.HnSRaider();
    public static HnSImpostorRoles.HnSReaper HnSReaper = new HnSImpostorRoles.HnSReaper();
    public static HnSImpostorRoles.HnSViper HnSViper = new HnSImpostorRoles.HnSViper();

    public static ComplexRoles.FCrewmate F_Crewmate = new ComplexRoles.FCrewmate();
    public static ComplexRoles.FGuesser F_Guesser = new ComplexRoles.FGuesser();
    public static ComplexRoles.FTracker F_Tracker = new ComplexRoles.FTracker();
    public static ComplexRoles.FTrapper F_Trapper = new ComplexRoles.FTrapper();

    public static ComplexRoles.SecondaryGuesser SecondaryGuesser = new ComplexRoles.SecondaryGuesser();
    public static NeutralRoles.SecondarySidekick SecondarySidekick = new NeutralRoles.SecondarySidekick();
    public static Crewmate.SecondaryMadmate SecondaryMadmate = new Crewmate.SecondaryMadmate();
    public static Impostor.Jailer.InheritedJailer InheritedJailer = new Impostor.Jailer.InheritedJailer();
    public static ExtraRoles.DiamondPossessor DiamondPossessor = new ExtraRoles.DiamondPossessor();
    public static ExtraRoles.Bloody Bloody = new ExtraRoles.Bloody();
    public static ExtraRoles.Confused Confused = new ExtraRoles.Confused();
    public static ExtraRoles.Drunk Drunk = new ExtraRoles.Drunk();
    public static ExtraRoles.LastImpostor LastImpostor = new ExtraRoles.LastImpostor();
    public static ExtraRoles.LloydFollower LloydFollower = new ExtraRoles.LloydFollower();
    public static ExtraRoles.Lover Lover = new ExtraRoles.Lover();
    public static ExtraRoles.Trilemma Trilemma = new ExtraRoles.Trilemma();
    public static ExtraRoles.AvengerTarget AvengerTarget = new ExtraRoles.AvengerTarget();
   // public static ExtraRoles.TeamSanta TeamSanta = new ExtraRoles.TeamSanta();
    public static MetaRoles.MetaRole MetaRole = new MetaRoles.MetaRole();
    public static PerkHolder PerkHolder = new Perk.PerkHolder();

    public static GhostRoles.Poltergeist Poltergeist = new GhostRoles.Poltergeist();

    //全てのロールはこの中に含まれている必要があります
    public static List<Role> AllRoles = new List<Role>()
        {
            VOID,
            Impostor,Banshee,BountyHunter,Camouflager,Cleaner,Covert,Damned,Disturber,Eraser,EvilAce,EvilGuesser,EvilTracker,EvilTrapper,Executioner,Jailer,Marionette,Morphing,Ninja,Painter,Raider,Reaper,Sniper,
            /*SantaClaus,BlackSanta,*/Arsonist,Avenger,ChainShifter,Empiric,Immoralist,Jackal,Jester,LordLloyd,Opportunist,Paparazzo,Sidekick,Spectre,Vulture,
            F_Crewmate,
            F_Guesser,F_Tracker,F_Trapper,
            Crewmate,CrewmateWithoutTasks,Agent,Alien,Bait,Busker,Comet,DamnedCrew,Doctor,Guardian,Mayor,Navvy,Necromancer,NiceGuesser,NiceTracker,NiceTrapper,Oracle,Provocateur,Psychic,Seer,Sheriff,Splicer,Spy,
            Madmate,
            Player,
            HnSCrewmate,
            HnSCleaner,HnSHadar,HnSRaider,HnSReaper,HnSViper
        };

    public static List<ExtraRole> AllExtraRoles = new List<ExtraRole>()
        {
            SecondaryGuesser,SecondarySidekick,SecondaryMadmate,InheritedJailer,
            DiamondPossessor,LastImpostor,/*TeamSanta,*/
            Bloody,Confused,Drunk,LloydFollower,Lover,Trilemma,
            MetaRole,AvengerTarget,
            PerkHolder
        };

    public static List<GhostRole> AllGhostRoles = new List<GhostRole>()
        {
            Poltergeist
        };

    private static SideCommonRoles.SideCommonRolesLoader loader = new SideCommonRoles.SideCommonRolesLoader();

    public static void ResetWinTrigger()
    {
        foreach (Role role in AllRoles)
        {
            if (role is Template.HasWinTrigger)
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
