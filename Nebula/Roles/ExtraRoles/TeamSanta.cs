using Nebula.Patches;

namespace Nebula.Roles.ExtraRoles;

/*
public class TeamSanta : ExtraRole
{
    static public Color RoleColor = new Color(255f / 255f, 120f / 255f, 120f / 255f);


    public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
    {
        bool showFlag = false;

        if (playerId == PlayerControl.LocalPlayer.PlayerId || Game.GameData.data.myData.CanSeeEveryoneInfo) showFlag = true;
        var data = Game.GameData.data.myData.getGlobalData();
        if (data.role.side == Side.SantaClaus) showFlag = true;

        if (showFlag) EditDisplayNameForcely(playerId, ref displayName);
    }


    public override void EditDisplayNameForcely(byte playerId, ref string displayName)
    {
        displayName += Helpers.cs(
                RoleColor, "✽");
    }

    public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
    {
        return condition == EndCondition.SantaWin;
    }


    public TeamSanta() : base("TeamSanta", "teamSanta", RoleColor, 0)
    {
        IsHideRole = true;
    }
}
*/