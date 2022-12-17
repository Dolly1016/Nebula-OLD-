using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Module.Interpreter
{
    public class CSPlayer
    {
        private PlayerControl? player;

        public CSPlayer(string name)
        {
            foreach(var p in PlayerControl.AllPlayerControls)
            {
                if (p.gameObject.name == name)
                {
                    player = p;
                    return;
                }
            }
            player = null;
        }

        public bool ChangeRole(string roleName)
        {
            if (player == null) return false;
            Roles.Role? role = null;
            role = Roles.Roles.AllRoles.FirstOrDefault(r => r.GetType().Name.Equals(roleName));
            if(role == null) role = Roles.Roles.AllRoles.FirstOrDefault(r => r.Name.Equals(roleName));
            if (role == null) return false;

            RPCEventInvoker.ImmediatelyChangeRole(player,role);
            return true;
        }
    }
}
