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

        public static Crewmate.Crewmate Crewmate=new Crewmate.Crewmate();
        public static Crewmate.Bait Bait = new Crewmate.Bait();
        public static Crewmate.Provocateur Provocateur = new Crewmate.Provocateur();
        public static Crewmate.Psychic Psychic = new Crewmate.Psychic();
        public static Crewmate.SecurityGuard SecurityGuard = new Crewmate.SecurityGuard();
        public static Crewmate.Sheriff Sheriff = new Crewmate.Sheriff();
        public static Crewmate.Spy Spy = new Crewmate.Spy();

        public static Crewmate.Madmate Madmate = new Crewmate.Madmate();

        public static Impostor.Impostor Impostor = new Impostor.Impostor();
        public static Impostor.Camouflager Camouflager = new Impostor.Camouflager();
        public static Impostor.EvilAce EvilAce = new Impostor.EvilAce();
        public static Impostor.Reaper Reaper = new Impostor.Reaper();

        public static Neutral.Jackal Jackal = new Neutral.Jackal();
        public static Neutral.Jester Jester = new Neutral.Jester();
        public static Neutral.Vulture Vulture = new Neutral.Vulture();

        //全てのロールはこの中に含まれている必要があります
        public static List<Role> AllRoles = new List<Role>()
        {
            Crewmate,Bait,Provocateur,Psychic,SecurityGuard,Sheriff,Spy,
            Madmate,
            Impostor,Camouflager,EvilAce,Reaper,
            Jackal,Jester,Vulture
        };

        public static void RegisterAddonRoles(Role role)
        {

        }
    }
}
