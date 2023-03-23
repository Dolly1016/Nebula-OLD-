using Nebula.Game;

namespace Nebula.Roles.Perk.ImpostorPerks;

public class LimitlessAlarm : Perk
{
    public override bool IsAvailable => true;

    public override void EditPingInterval(byte playerId, ref float additional, ref float ratio)
    {
        additional = -IP(0, PerkPropertyType.Second);
    }

    public override void MyUpdate(PerkHolder.PerkInstance perkData)
    {
        perkData.Display?.SetCool(HnSModificator.HideAndSeekManager.LogicFlowHnS.IsFinalCountdown ? 0f : 1f);
    }

    public LimitlessAlarm(int id) : base(id, "limitlessAlarm", false, 34, 6, new Color(0.3f, 0.1f, 0.1f))
    {
        ImportantProperties = new float[] { 2.5f };
    }
}

