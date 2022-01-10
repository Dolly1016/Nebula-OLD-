using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.Crewmate
{
    public class Madmate : Role
    {

        public Madmate()
                : base("Madmate", "madmate", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Impostor, Side.Crewmate,
                     Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Impostor.Impostor.impostorEndSet,
                     false, true, true, false, false)
        {

        }
    }
}
