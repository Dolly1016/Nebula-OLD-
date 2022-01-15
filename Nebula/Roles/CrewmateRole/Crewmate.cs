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

namespace Nebula.Roles.CrewmateRoles
{
    public class Crewmate : Role
    {
        public static HashSet<Side> crewmateSideSet = new HashSet<Side>() { Side.Crewmate };
        public static HashSet<EndCondition> crewmateEndSet =
            new HashSet<EndCondition>() { EndCondition.CrewmateWinByTask, EndCondition.CrewmateWinByVote, EndCondition.CrewmateWinDisconnect };

        public Crewmate()
                : base("Crewmate", "crewmate", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     crewmateSideSet, crewmateSideSet, crewmateEndSet,
                     false,false, false, false, false)
        {
            IsHideRole = true;
        }
    }
}
