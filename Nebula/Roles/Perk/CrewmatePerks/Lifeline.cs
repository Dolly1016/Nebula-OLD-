using Nebula.Game;

namespace Nebula.Roles.Perk.CrewmatePerks;

public class Lifeline : Perk
{
    public override bool IsAvailable => true;

    public override void SetReviveCharge(PerkHolder.PerkInstance perkData, ref int charge)
    {
        charge += (int)IP(0);
    }

    public override void SetReviveCost(PerkHolder.PerkInstance perkData, ref float additional, ref float ratio)
    {
        ratio -= IP(1, PerkPropertyType.Percentage);
    }

    public Lifeline(int id) : base(id, "lifeline", true, 39, 4, new Color(0.3f, 0.7f, 0.75f))
    {
        ImportantProperties = new float[] { 1f, 20f };
    }
}
