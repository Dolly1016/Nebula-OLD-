namespace Nebula.Roles.Perk.CrewmatePerks;

public class Intuition : Perk
{
    public override bool IsAvailable => true;

    public override void EditLocalIntimidation(PerkHolder.PerkInstance perkData, ref float additional, ref float ratio)
    {
        ratio += IP(0,PerkPropertyType.Percentage);
    }

    public Intuition(int id) : base(id, "intuition", true, 21, 2, new Color(0.55f, 0.6f, 0.15f))
    {
        ImportantProperties = new float[] { 15f };
    }
}
