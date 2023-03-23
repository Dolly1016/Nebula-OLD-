namespace Nebula.Roles.Perk.CrewmatePerks;

public class Tenacity : Perk
{
    public override bool IsAvailable => true;

    public override void EditLocalIntimidation(PerkHolder.PerkInstance perkData, ref float additional, ref float ratio)
    {

        ratio += Mathf.Min((float)perkData.IntegerAry[0], 8) * IP(0, PerkPropertyType.Percentage);
    }

    public override void OneAnyoneDied(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.IntegerAry[0]++;
    }

    public override void Initialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.IntegerAry = new int[] { 0 };
    }

    public Tenacity(int id) : base(id, "tenacity", true, 33, 8, new Color(0.75f, 0.5f, 0.25f))
    {
        ImportantProperties = new float[] { 5f };
    }
}
