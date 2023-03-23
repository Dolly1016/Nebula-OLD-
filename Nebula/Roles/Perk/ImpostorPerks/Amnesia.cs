namespace Nebula.Roles.Perk.ImpostorPerks;

public class Amnesia : Perk
{
    public override bool IsAvailable => true;

    public override void OnKillPlayer(PerkHolder.PerkInstance perkData, byte targetId)
    {
        if (perkData.DataAry[0] > 0f) return;

        Tasks.TimedTask.TimedTaskEvent.Invoke(new Tasks.TimedTask.TimedTaskMessage() { TaskId = 0, LeftTime = IP(0, PerkPropertyType.Second) });
        perkData.DataAry[0] = IP(0, PerkPropertyType.Second) + IP(1, PerkPropertyType.Second);
    }

    public override void MyUpdate(PerkHolder.PerkInstance perkData)
    {
        if (perkData.DataAry[0] > 0f) perkData.DataAry[0] -= Time.deltaTime;

        perkData.Display.SetCool(perkData.DataAry[0]/IP(1,PerkPropertyType.Second));
    }

    public override void Initialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        perkData.DataAry = new float[1] { 0f };
    }

    public Amnesia(int id) : base(id, "amnesia", false, 15, 1, new Color(0.6f, 0.7f, 0.2f))
    {
        ImportantProperties = new float[] { 10f, 20f };
    }
}

