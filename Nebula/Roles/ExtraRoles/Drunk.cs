namespace Nebula.Roles.ExtraRoles;

public class Drunk : Template.StandardExtraRole
{
    static public Color RoleColor = new Color(133f / 255f, 161f / 255f, 190f / 255f);

    public override void GlobalInitialize(PlayerControl __instance)
    {
        base.GlobalInitialize(__instance);
        RPCEvents.EmitSpeedFactor(__instance.PlayerId, new Game.SpeedFactor(0, 99999f, -1f, true));
    }

    public override void EditDisplayNameForcely(byte playerId, ref string displayName)
    {
        displayName += Helpers.cs(
                RoleColor, "〻");
    }

    public override void EditSpawnableRoleShower(ref string suffix, Role role)
    {
        if (IsSpawnable() && role.CanHaveExtraAssignable(this)) suffix += Helpers.cs(Color, "〻");
    }

    public override Module.CustomOption? RegisterAssignableOption(Role role)
    {
        Module.CustomOption option = role.CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeDrunk", role.DefaultExtraAssignableFlag(this), true).HiddenOnDisplay(true).SetIdentifier("role." + role.LocalizeName + ".canBeDrunk");
        option.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
        option.AddCustomPrerequisite(() => { return Roles.Drunk.IsSpawnable(); });
        return option;
    }

    public Drunk() : base("Drunk", "drunk", RoleColor, 1)
    {
    }
}