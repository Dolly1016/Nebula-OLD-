using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate
{
    public class Spy : Role
    {
        public static HashSet<Side> crewmateSideSet = new HashSet<Side>() { Side.Crewmate };

        public Spy()
                : base("Spy", "spy", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     crewmateSideSet, Impostor.Impostor.impostorSideSet, Crewmate.crewmateEndSet,
                     false, true, false, false, true)
        {

        }
    }
}
