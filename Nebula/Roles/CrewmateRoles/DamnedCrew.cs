namespace Nebula.Roles.CrewmateRoles;

public class DamnedCrew : Crewmate
{
    int guardLeftId;
    bool changeTrigger;
    public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(guardLeftId, "Damned Guard", 0, 20) }; }

    public override bool ShowInHelpWindow => false;

    public override bool IsGuessableRole { get => false; }
    public override void GlobalInitialize(PlayerControl __instance)
    {
        Game.GameData.data.playersArray[__instance.PlayerId].SetRoleData(guardLeftId, 1);
    }

    //変身トリガは個人で持つ(役職を交換されても引き継がない)
    public override void Initialize(PlayerControl __instance)
    {
        changeTrigger = false;
    }

    public override Helpers.MurderAttemptResult OnMurdered(byte murderId, byte playerId)
    {
        if (Game.GameData.data.playersArray[playerId].GetRoleData(guardLeftId) > 0)
        {
            RPCEventInvoker.AddAndUpdateRoleData(playerId, guardLeftId, -1);
            return Helpers.MurderAttemptResult.SuppressKill;
        }
        return Helpers.MurderAttemptResult.PerformKill;
    }

    public override void OnUpdateRoleData(int dataId, int newData)
    {
        if (dataId == guardLeftId && newData >= 0)
        {
            Helpers.PlayQuickFlash(Palette.ImpostorRed);
            Objects.CustomMessage.Create(new Vector3(0, 0, 0), false, Language.Language.GetString("role.damned.message.killed"), 0.2f, 4f, 1f, 1.5f, Color.yellow, Color.red);
            //変身トリガ
            changeTrigger = true;
        }
    }

    public override void EditDisplayRoleName(byte playerId, ref string roleName, bool isIntro)
    {
        if (Game.GameData.data.myData.CanSeeEveryoneInfo) EditDisplayRoleNameForcely(playerId, ref roleName);
    }

    public override void EditDisplayRoleNameForcely(byte playerId, ref string roleName)
    {
        string shortText = Helpers.cs(Palette.ImpostorRed, Language.Language.GetString("role.damned.short"));
        roleName += Helpers.cs(new Color(0.6f, 0.6f, 0.6f), $"({shortText})");
    }


    public override void OnMeetingStart()
    {
        if (!changeTrigger) return;
        RPCEventInvoker.ChangeRole(PlayerControl.LocalPlayer, Roles.Damned);
    }

    public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
    {

    }

    public override bool CanHaveExtraAssignable(ExtraAssignable extraRole)
    {
        return Roles.F_Crewmate.CanHaveExtraAssignable(extraRole);
    }

    public DamnedCrew() : base()
    {
        guardLeftId = Game.GameData.RegisterRoleDataId("damnedCrew.guardLeft");
        HideInExclusiveAssignmentOption = true;
    }
}