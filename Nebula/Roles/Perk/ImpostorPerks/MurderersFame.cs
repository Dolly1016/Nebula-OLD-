namespace Nebula.Roles.Perk.ImpostorPerks;

public class MurderersFame : Perk
{
    public override bool IsAvailable => true;

    public override void OnKillPlayer(PerkHolder.PerkInstance perkData, byte targetId)
    {
        if (perkData.DataAry[0] > 0f) PerkHolder.ShareFloatPerkData.Invoke(new PerkHolder.PerkDataMessage<float>()
        {
            playerId=PlayerControl.LocalPlayer.PlayerId,
            perkIndex=perkData.Index,
            dataIndex=1,
            value= IP(0, PerkPropertyType.Second)
        });
        perkData.DataAry[0] = IP(1, PerkPropertyType.Second);
    }

    public override void GlobalUpdate(PerkHolder.PerkInstance perkData)
    {
        for (int i = 0; i < 2; i++) if (perkData.DataAry[i] > 0f) perkData.DataAry[i] -= Time.deltaTime;
    }

    public override void GlobalInitialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.DataAry = new float[2] { 0f, 0f };
    }

    public override bool UnconditionalIntimidationGlobal(PerkHolder.PerkInstance perkData)
    {
        return perkData.DataAry[1] > 0f;
    }

    public override void MyUpdate(PerkHolder.PerkInstance perkData)
    {
        perkData.Display.SetCool(perkData.DataAry[0] / IP(1, PerkPropertyType.Second));
    }

    public MurderersFame(int id) : base(id, "murderersFame", false, 3, 1, new Color(0.3f, 0.05f, 0.05f))
    {
        ImportantProperties = new float[2] { 7f, 10f };
    }
}
