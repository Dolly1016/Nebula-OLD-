using Il2CppSystem.Threading.Tasks;

namespace Nebula.Roles.Perk.CrewmatePerks;

public class Patience : Perk
{
    public override bool IsAvailable => true;

    public override void GlobalInitialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.IntegerAry = new int[] { 1 };
    }

    public override bool CanPing(PerkHolder.PerkInstance perkData, byte playerId)
    {
        bool result = perkData.IntegerAry[0] == 1;
        perkData.IntegerAry[0] = result ? 0 : 1;
        return !result;
    }

    public override void MyUpdate(PerkHolder.PerkInstance perkData)
    {
        perkData.Display?.SetCool(perkData.IntegerAry[0] == 1 ? 0f : 1f);
    }

    public Patience(int id) : base(id, "patience", true, 34, 4, new Color(0.55f,0.45f,0.1f))
    {
    }
}
