using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;
using Nebula.Patches;

namespace Nebula.Roles.ParlourRoles
{
    public class Gambler : Role
    {
        static public Color Color = new Color(255f / 255f, 213f / 255f, 0f / 255f);

        public static HashSet<Side> gamblerSideSet = new HashSet<Side>() { Side.Gambler };
        public static HashSet<EndCondition> gamblerEndSet =
            new HashSet<EndCondition>() { EndCondition.ShowDownWin };

        public Gambler()
                : base("Gambler", "gambler", Color, RoleCategory.Crewmate, Side.Gambler, Side.Gambler,
                     gamblerSideSet, gamblerSideSet, gamblerEndSet,
                     false, false, false, false, false)
        {
            ValidGamemode = Module.CustomGameMode.Parlour;
        }
    }
}
