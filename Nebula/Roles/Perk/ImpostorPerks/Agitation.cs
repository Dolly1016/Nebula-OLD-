namespace Nebula.Roles.Perk.ImpostorPerks;

public class Agitation : Perk
{
    public override bool IsAvailable => true;

    public override void SetKillCoolDown(PerkHolder.PerkInstance perkData, bool isSuccess, ref float additional, ref float ratio)
    {
        if (isSuccess)
        {
            RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, IP(1, PerkPropertyType.Second), 1f + IP(0, PerkPropertyType.Percentage), false));
            HudManager.Instance.StartCoroutine(CoProceedDisplayTimer(perkData.Display, IP(1, PerkPropertyType.Second)).WrapToIl2Cpp());
        }
    }

    public Agitation(int id) : base(id, "agitation", false, 30, 0, new Color(0.6f, 0.2f, 0.2f))
    {
        ImportantProperties = new float[] { 10f, 5f };
    }
}

