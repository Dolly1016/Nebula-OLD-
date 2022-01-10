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
        public static Crewmate.Sheriff Sheriff = new Crewmate.Sheriff();
        public static Crewmate.SecurityGuard SecurityGuard = new Crewmate.SecurityGuard();
        public static Crewmate.Spy Spy = new Crewmate.Spy();

        public static Crewmate.Madmate Madmate = new Crewmate.Madmate();

        public static Impostor.Impostor Impostor = new Impostor.Impostor();
        public static Impostor.EvilAce EvilAce = new Impostor.EvilAce();
        public static Impostor.Camouflager Camouflager = new Impostor.Camouflager();

        public static Neutral.Jackal Jackal = new Neutral.Jackal();


        public static List<Role> AllRoles = new List<Role>()
        {
            Crewmate,Sheriff,SecurityGuard,Spy,
            Madmate,
            Impostor,EvilAce,Camouflager,
            Jackal
        };
    }
}
