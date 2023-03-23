namespace Nebula.Roles.Perk.CrewmatePerks;

public class TSPResolver : Perk
{
    public override bool IsAvailable => true;

    public override void OnTaskComplete(PerkHolder.PerkInstance perkData, PlayerTask? task) {
        RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, IP(1, PerkPropertyType.Second), 1 + IP(0, PerkPropertyType.Percentage), false));
        HudManager.Instance.StartCoroutine(CoProceedDisplayTimer(perkData.Display, IP(1, PerkPropertyType.Second)).WrapToIl2Cpp());
    }

    public TSPResolver(int id) : base(id, "tspResolver", true, 31, 3, new Color(0.2f, 0.3f, 0.5f))
    {
        ImportantProperties = new float[] { 100f, 5f };
    }
}
