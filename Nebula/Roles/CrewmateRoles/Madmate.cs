using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Madmate : Role
    {

        public Madmate()
                : base("Madmate", "madmate", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, ImpostorRoles.Impostor.impostorEndSet,
                     true, true, true, false, false)
        {
            
        }
    }
}
