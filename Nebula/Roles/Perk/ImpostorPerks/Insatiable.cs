namespace Nebula.Roles.Perk.ImpostorPerks;

public class Insatiable : Perk
{
    public override bool IsAvailable => true;

    public override void SetKillCoolDown(PerkHolder.PerkInstance perkData, bool isSuccess, ref float additional, ref float ratio)
    {
        if (!isSuccess) return;

        Vector2 myPos = PlayerControl.LocalPlayer.GetTruePosition();
        Vector2 pos = myPos;
        float dis = 0;
        foreach(var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (p.Data.IsDead) continue;

            var temp = p.GetTruePosition();
            var tempDis = myPos.Distance(temp);
            if (tempDis > dis)
            {
                dis = tempDis;
                pos = temp;
            }
        }

        if (dis > 1f)
        {
            Helpers.Ping(pos, false);
        }
    }

    public Insatiable(int id) : base(id, "insatiable", false, 35, 1, new Color(0.8f, 0f, 0f))
    {
    }
}

