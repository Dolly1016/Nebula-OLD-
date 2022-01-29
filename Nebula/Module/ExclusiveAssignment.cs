using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Module
{
    public class ExclusiveAssignment
    {
        public List<Roles.Role> exclusiveRoles;

        public ExclusiveAssignment(params Roles.Role?[] roles)
        {
            exclusiveRoles = new List<Roles.Role>();
            foreach(var role in roles)
            {
                if (role == null) continue;

                exclusiveRoles.Add(role);
            }
        }
        public void ExclusiveAssign(Patches.AssignRoles roles)
        {
            if (exclusiveRoles.Count == 0) return;

            List<int> validRoleList = new List<int>();
            for (int i = 0; i < exclusiveRoles.Count; i++) {
                if (roles.FuzzyContains(exclusiveRoles[i])) validRoleList.Add(i);
            }

            if (validRoleList.Count == 0) return;

            Roles.Role validRole= exclusiveRoles[validRoleList[NebulaPlugin.rnd.Next(validRoleList.Count)]];

            foreach (Roles.Role role in exclusiveRoles)
            {
                if(role!=validRole)
                    roles.FuzzyRemoveRole(role);
            }
        }
    }
}
