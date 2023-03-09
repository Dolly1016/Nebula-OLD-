using Nebula.Game;

namespace Nebula.Roles.Perk.ImpostorPerks;

public class DerelictCorpse : Perk
{
    public override bool IsAvailable => true;

    public override void SetKillRange(PerkHolder.PerkInstance perkData, ref float additional, ref float ratio)
    {
        bool activeFlag = false;
        var pos = PlayerControl.LocalPlayer.transform.position;
        foreach(var d in Helpers.AllDeadBodies())
        {
            if (pos.Distance(d.transform.position) > IP(0,PerkPropertyType.Meter))
            {
                activeFlag= true;
                break;
            }
        }
        if (activeFlag)
        {
            ratio += IP(1,PerkPropertyType.Percentage);
        }

        perkData.Display?.SetCool(activeFlag ? 0f : 1f);
    }

    public DerelictCorpse(int id) : base(id, "derelictCorpse", false, 24, 4, new Color(0, 0.35f, 0.3f))
    {
        ImportantProperties = new float[] { 45f, 20f };
    }
}

