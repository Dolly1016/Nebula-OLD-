using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.CrewmateRoles
{
    public class Spy : Role
    {
        public static HashSet<Side> crewmateSideSet = new HashSet<Side>() { Side.Crewmate };

        public Spy()
                : base("Spy", "spy", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     crewmateSideSet, ImpostorRoles.Impostor.impostorSideSet, Crewmate.crewmateEndSet,
                     false, true, false, false, true)
        {
            deceiveImpostorInNameDisplay = true;
        }
    }
}
