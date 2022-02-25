using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.MinigameRoles
{
    public static class MinigameRoleAssignment
    {
        public static void Assign()
        {
            int index = NebulaPlugin.rnd.Next(PlayerControl.AllPlayerControls.Count);
            PlayerControl player;
            for (int i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
            {
                player = PlayerControl.AllPlayerControls[i];
                if (i == index)
                {
                    RPCEventInvoker.ImmediatelyChangeRole(player, Roles.Hunter);
                }
                else
                {
                    RPCEventInvoker.ImmediatelyChangeRole(player, Roles.Escapee);
                }
            }
        }
    }
}
