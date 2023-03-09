namespace Nebula.Roles.Perk.ImpostorPerks;

public class Creeper : Perk
{
    public override bool IsAvailable => true;

    public override void EditGlobalIntimidation(byte playerId, ref float additional, ref float ratio)
    {
        ratio -= IP(0, PerkPropertyType.Percentage);
    }

    public Creeper(int id) : base(id, "creeper", false, 3, 7, new Color(0.5f, 0.04f, 0.04f))
    {
        ImportantProperties = new float[] { 5f };
    }
}

