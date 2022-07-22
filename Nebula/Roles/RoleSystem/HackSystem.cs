using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.RoleSystem
{
    static public class HackSystem
    {
        static public void showAdminMap(bool ignoreCommSabotage)
        {
            Patches.AdminPatch.isAffectedByCommAdmin = !ignoreCommSabotage;
            Patches.AdminPatch.isStandardAdmin = false;

            PlayerControl.LocalPlayer.NetTransform.Halt();
            Action<MapBehaviour> tmpAction = (MapBehaviour m) => { m.ShowCountOverlay(); };
            DestroyableSingleton<HudManager>.Instance.ShowMap(tmpAction);
        }
    }
}
