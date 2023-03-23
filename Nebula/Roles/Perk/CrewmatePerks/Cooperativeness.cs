namespace Nebula.Roles.Perk.CrewmatePerks;

public class Cooperativeness : Perk
{
    public override bool IsAvailable => true;

    public override void OnCompleteHnSTaskLocal(PerkHolder.PerkInstance perkData, ref float additional, ref float ratio)
    {
        var myPos = PlayerControl.LocalPlayer.transform.position;
        bool flag = false;
        foreach(var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (p.AmOwner) continue;
            if (p.transform.position.Distance(myPos) < IP(0, PerkPropertyType.Meter))
            {
                flag = true; break;
            }
        }
        if (flag)
        {
            ratio += IP(1, PerkPropertyType.Percentage);
        }
    }

    public Cooperativeness(int id) : base(id, "cooperativeness", true, 11, 3, new Color(0.3f, 0.8f, 0.6f))
    {
        ImportantProperties = new float[] { 5f, 50f };
    }
}
