namespace Nebula.Roles.Perk.ImpostorPerks;

public class WillNotMissYou : Perk
{
    public override bool IsAvailable => true;

    public override void SetFailedKillPenalty(PerkHolder.PerkInstance perkData, ref float speedAdditional, ref float speedRatio, ref float timeAdditional, ref float timeRatio)
    {
        speedRatio -= IP(0, PerkPropertyType.Percentage);
    }

    public WillNotMissYou(int id) : base(id, "willNotMissYou", false, 20, 4, new Color(0.2f, 0.1f, 0.6f))
    {
        ImportantProperties = new float[] { 70f };
    }
}

