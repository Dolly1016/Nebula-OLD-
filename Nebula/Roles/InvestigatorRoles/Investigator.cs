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

namespace Nebula.Roles.InvestigatorRoles
{
    public class Investigator : Role
    {
        public static HashSet<Side> investigatorSideSet = new HashSet<Side>() { Side.Crewmate };
        public static HashSet<EndCondition> investigatorEndSet =
            new HashSet<EndCondition>() { EndCondition.InvestigatorRightGuess };

        public Investigator()
                : base("Investigator", "investigator", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.Investigator, Side.Investigator,
                     investigatorSideSet, investigatorSideSet, investigatorEndSet,
                     false, false, false, false, false)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.Investigators;
        }
    }
}
