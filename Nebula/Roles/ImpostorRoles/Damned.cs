using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;

namespace Nebula.Roles.ImpostorRoles
{
    public class Damned : Role
    {
        public static HashSet<Side> impostorSideSet = new HashSet<Side>() { Side.Impostor };
        public static HashSet<EndCondition> impostorEndSet =
           new HashSet<EndCondition>() { EndCondition.ImpostorWinByKill, EndCondition.ImpostorWinBySabotage, EndCondition.ImpostorWinByVote, EndCondition.ImpostorWinDisconnect };

        public override List<Role> GetImplicateRoles() { return new List<Role>() { Roles.DamnedCrew }; }

        public Damned()
                : base("Damned", "damned", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     impostorSideSet, impostorSideSet, impostorEndSet,
                     true, true, true, true, true)
        {
            IsHideRole = true;
        }
    }
}
