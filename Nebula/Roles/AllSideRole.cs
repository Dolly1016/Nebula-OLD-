namespace Nebula.Roles;
public class AllSideRole : Role
{
    public AllSideRole(Role templateRole, string name, string localizedName, Color color,
        bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
        bool ignoreBlackout, bool useImpostorLightRadius)
        : base(name, templateRole.side.localizeSide + "." + localizedName, color, templateRole.category, templateRole.side, templateRole.introMainDisplaySide,
             templateRole.introDisplaySides, templateRole.introInfluenceSides, templateRole.winReasons,
             hasFakeTask, canUseVents, canMoveInVents, ignoreBlackout, useImpostorLightRadius)
    {
        Roles.AllRoles.Add(this);
    }
}