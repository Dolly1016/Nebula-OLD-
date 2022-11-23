namespace Nebula.Roles.ExtraRoles;

public class DiamondPossessor : ExtraRole
{
    static public Color RoleColor = new Color(145f / 255f, 159f / 255f, 232f / 255f);

    public override void Assignment(Patches.AssignMap assignMap)
    {

    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        base.GlobalInitialize(__instance);
    }

    public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
    {
        bool showFlag = false;
        if (playerId == PlayerControl.LocalPlayer.PlayerId || Game.GameData.data.myData.CanSeeEveryoneInfo) showFlag = true;

        if (showFlag) EditDisplayNameForcely(playerId, ref displayName);
    }


    public override void EditDisplayNameForcely(byte playerId, ref string displayName)
    {
        displayName += Helpers.cs(
                RoleColor, "◇");
    }

    public override void LoadOptionData()
    {

    }

    public DiamondPossessor() : base("DiamondPossessor", "diamondPossessor", RoleColor, 0)
    {
        IsHideRole = true;
    }
}