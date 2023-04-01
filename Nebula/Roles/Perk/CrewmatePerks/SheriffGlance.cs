using Il2CppSystem.CodeDom;
using System;
using static Nebula.Roles.Perk.PerkHolder;

namespace Nebula.Roles.Perk.CrewmatePerks;



public class SheriffGlance : Perk
{

    public override bool IsAvailable => true;

    public override void Initialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.DataAry = new float[] { IP(0, PerkPropertyType.Second) };
        perkData.IntegerAry = new int[] { 1 };
    }

    public override void MyUpdate(PerkInstance perkData)
    {
        float threthold = Game.HnSModificator.HideAndSeekManager.LogicDangerLevel.scaryMusicDistance;

        if (PlayerControl.LocalPlayer.Data.IsDead) return;

        float dis = PlayerControl.LocalPlayer.GetTruePosition().Distance(Game.HnSModificator.Seeker.GetTruePosition());
        dis *= dis;

        if (dis < threthold && perkData.IntegerAry[0] == 0 && !(perkData.DataAry[0]>0f))
        {
            Game.HnSModificator.NoticeSeekerEvent.Invoke();
            perkData.DataAry[0] = IP(0, PerkPropertyType.Second);
        }

        perkData.IntegerAry[0] = dis < threthold ? 1 : 0;

        if (perkData.DataAry[0] > 0f) perkData.DataAry[0] -= Time.deltaTime;

        perkData.Display?.SetCool(perkData.DataAry[0] / IP(0, PerkPropertyType.Second));
    }

    public SheriffGlance(int id) : base(id, "sheriffGlance", true, 32, 5, CrewmateRoles.Sheriff.RoleColor)
    {
        ImportantProperties = new float[] { 40f };
    }
}
