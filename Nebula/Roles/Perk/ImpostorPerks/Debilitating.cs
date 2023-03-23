namespace Nebula.Roles.Perk.ImpostorPerks;

public class Debilitating : Perk
{
    public override bool IsAvailable => true;

    private Dictionary<byte, Vector3> PosDic = new Dictionary<byte, Vector3>();

    private void ResetPosition()
    {
        PosDic.Clear();
        foreach(var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            PosDic[p.PlayerId] = p.transform.position;
        }

    }

    public override void Initialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.DataAry = new float[1] { IP(0, PerkPropertyType.Second) };
        ResetPosition();
    }

    public override void MyUpdate(PerkHolder.PerkInstance perkData)
    {
        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            PosDic[p.PlayerId] = PosDic[p.PlayerId] * 0.995f + p.transform.position * 0.005f;
        }

        if (perkData.DataAry[0] > 0f)
        {
            perkData.DataAry[0] -= Time.deltaTime;
        }
        else
        {
            float num = float.MaxValue;
            Vector2 pos=Vector2.zero;
            byte id = byte.MaxValue;

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (p.Data.IsDead) continue;

                float d = p.transform.position.Distance(PosDic[p.PlayerId]);
                if (num > d)
                {
                    pos=p.transform.position;
                    num = d;
                    id = p.PlayerId;
                }
            }

            if (num < 6f) Helpers.Ping(pos,false);
            
            ResetPosition();
            perkData.DataAry[0] = IP(0,PerkPropertyType.Second);
        }

        perkData.Display.SetCool(perkData.DataAry[0] / IP(0, PerkPropertyType.Second));
    }

    public Debilitating(int id) : base(id, "debilitating", false, 35, 3, new Color(0.2f, 0.6f, 0.8f))
    {
        ImportantProperties = new float[] { 20f };
    }
}