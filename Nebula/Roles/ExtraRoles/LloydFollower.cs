using AmongUs.GameOptions;
using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.ExtraRoles;

public class LloydFollower : ExtraRole
{
    static public Color RoleColor = NeutralRoles.LordLloyd.RoleColor;

    public override Assignable AssignableOnHelp { get => null; }

    public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
    {
        return !player.Data.IsDead && condition == EndCondition.ClanOfGreyWin && player.GetModData().GetExtraRoleData(this.id) == 1;
    }

    public override void OnMeetingStart()
    {
        RPCEventInvoker.UpdateExtraRoleData(PlayerControl.LocalPlayer.PlayerId, this.id, 1);
    }

    public override void OnThroughCheckingEndAfterExile()
    {
        Helpers.PostponeInterferingProcess(()=> RPCEventInvoker.ImmediatelyUnsetExtraRole(PlayerControl.LocalPlayer,this));
    }

    public LloydFollower() : base("LloydFollower", "lloydFollower", RoleColor, 0)
    {
        IsHideRole= true;
        hasFakeReportButton = true;
        canCallEmergencyMeeting = false;
    }
}
