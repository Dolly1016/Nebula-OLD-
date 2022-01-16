using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Roles
{
    static public class Roles
    {

        public static CrewmateRoles.Crewmate Crewmate=new CrewmateRoles.Crewmate();
        public static CrewmateRoles.Bait Bait = new CrewmateRoles.Bait();
        public static CrewmateRoles.DamnedCrew DamnedCrew = new CrewmateRoles.DamnedCrew();
        public static ComplexRoles.Guesser NiceGuesser = new ComplexRoles.Guesser("NiceGuesser", "niceGuesser", false);
        public static CrewmateRoles.Provocateur Provocateur = new CrewmateRoles.Provocateur();
        public static CrewmateRoles.Psychic Psychic = new CrewmateRoles.Psychic();
        public static CrewmateRoles.SecurityGuard SecurityGuard = new CrewmateRoles.SecurityGuard();
        public static CrewmateRoles.Sheriff Sheriff = new CrewmateRoles.Sheriff();
        public static CrewmateRoles.Spy Spy = new CrewmateRoles.Spy();

        public static CrewmateRoles.Madmate Madmate = new CrewmateRoles.Madmate();

        public static ImpostorRoles.Impostor Impostor = new ImpostorRoles.Impostor();
        public static ImpostorRoles.Camouflager Camouflager = new ImpostorRoles.Camouflager();
        public static ImpostorRoles.Damned Damned = new ImpostorRoles.Damned();
        public static ImpostorRoles.Eraser Eraser = new ImpostorRoles.Eraser();
        public static ImpostorRoles.EvilAce EvilAce = new ImpostorRoles.EvilAce();
        public static ComplexRoles.Guesser EvilGuesser = new ComplexRoles.Guesser("EvilGuesser", "evilGuesser", true);
        public static ImpostorRoles.Reaper Reaper = new ImpostorRoles.Reaper();

        public static NeutralRoles.Jackal Jackal = new NeutralRoles.Jackal();
        public static NeutralRoles.Jester Jester = new NeutralRoles.Jester();
        public static NeutralRoles.Vulture Vulture = new NeutralRoles.Vulture();

        public static ComplexRoles.FCrewmate F_Crewmate = new ComplexRoles.FCrewmate();
        public static ComplexRoles.FGuesser F_Guesser = new ComplexRoles.FGuesser();

        public static ExtraRoles.Lover Lover = new ExtraRoles.Lover();


        //全てのロールはこの中に含まれている必要があります
        public static List<Role> AllRoles = new List<Role>()
        {
            F_Crewmate,F_Guesser,
            Crewmate,Bait,DamnedCrew,NiceGuesser,Provocateur,Psychic,SecurityGuard,Sheriff,Spy,
            Madmate,
            Impostor,Camouflager,Damned,Eraser,EvilAce,EvilGuesser,Reaper,
            Jackal,Jester,Vulture,
        };

        public static List<ExtraRole> AllExtraRoles = new List<ExtraRole>()
        {
            Lover
        };

        public static void RegisterAddonRoles(Role role)
        {

        }
    }
}
