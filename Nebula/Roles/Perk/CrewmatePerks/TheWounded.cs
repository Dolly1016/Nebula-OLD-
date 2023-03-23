using JetBrains.Annotations;

namespace Nebula.Roles.Perk.CrewmatePerks;

public class TheWounded : Perk
{
    public override bool IsAvailable => true;

    public override void SetReviveCost(PerkHolder.PerkInstance perkData, ref float additional, ref float ratio)
    {
        if (perkData.IntegerAry[0] == 1)
        {
            ratio -= IP(1, PerkPropertyType.Percentage);
        }
    }

    public override void OnCompleteHnSTaskLocal(PerkHolder.PerkInstance perkData, ref float additional, ref float ratio)
    {
        if (perkData.IntegerAry[0] == 1) ratio += IP(0, PerkPropertyType.Percentage);
    }

    public override void Initialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.IntegerAry = new int[1] { 0 };
    }

    public override void OnRevived(PerkHolder.PerkInstance perkData)
    {
        perkData.IntegerAry[0] = 1;
    }

    public TheWounded(int id) : base(id, "theWounded", true, 21, 2, new Color(0.4f, 0.35f, 0.7f))
    {
        ImportantProperties = new float[] { 100f,40f };
    }
}
