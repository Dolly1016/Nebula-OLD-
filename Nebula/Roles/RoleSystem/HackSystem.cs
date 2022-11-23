namespace Nebula.Roles.RoleSystem
{
    static public class HackSystem
    {
        static public void showAdminMap(bool ignoreCommSabotage, Patches.AdminPatch.AdminMode adminMode)
        {
            Patches.AdminPatch.isAffectedByCommAdmin = !ignoreCommSabotage;
            Patches.AdminPatch.isStandardAdmin = false;
            Patches.AdminPatch.adminMode = adminMode;
            Patches.AdminPatch.shouldChangeColor = true;

            PlayerControl.LocalPlayer.NetTransform.Halt();
            Action<MapBehaviour> tmpAction = (MapBehaviour m) => { m.ShowCountOverlay(); };
            FastDestroyableSingleton<HudManager>.Instance.ShowMap(tmpAction);
        }
    }
}
