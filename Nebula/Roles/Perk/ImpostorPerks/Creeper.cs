namespace Nebula.Roles.Perk.ImpostorPerks;

public class Creeper : Perk
{
    public override bool IsAvailable => true;

    public override void EditGlobalIntimidation(PerkHolder.PerkInstance perk, ref float additional, ref float ratio)
    {
        ratio -= IP(0, PerkPropertyType.Percentage);
    }

    public Creeper(int id) : base(id, "creeper", false, 3, 7, new Color(0.4f, 0.09f, 0.15f))
    {
        ImportantProperties = new float[] { 5f };
    }
}

