namespace Nebula.Roles.MinigameRoles
{
    public static class MinigameRoleAssignment
    {
        public static void Assign()
        {
            Role hunterRole = Roles.Polis;
            string hunter=CustomOptionHolder.escapeHunterOption.getRawString();
            foreach (var role in Roles.AllRoles)
            {
                if ("role." + role.LocalizeName + ".name" == hunter)
                {
                    hunterRole = role;
                    break;
                }
            }

            int index = NebulaPlugin.rnd.Next(PlayerControl.AllPlayerControls.Count);
            PlayerControl player;
            for (int i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
            {
                player = PlayerControl.AllPlayerControls[i];
                if (i == index)
                {
                    RPCEventInvoker.ImmediatelyChangeRole(player, hunterRole);
                }
                else
                {
                    RPCEventInvoker.ImmediatelyChangeRole(player, Roles.Halley);
                }
            }
        }
    }
}
