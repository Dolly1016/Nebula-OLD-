using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Perk.ImpostorPerks;

public class DitherInDarkness : Perk
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

    public override void GlobalInitialize(PerkHolder.PerkInstance perkData, byte playerId)
    {
        Tasks.TimedTask.TimedTaskEvent.LocalInvoke(new Tasks.TimedTask.TimedTaskMessage() { TaskId = 0, LeftTime = IP(0, PerkPropertyType.Second) });
    }

    public DitherInDarkness(int id) : base(id, "ditherInDarkness", false, 10, 2, new Color(0.1f, 0.05f, 0.7f))
    {
        ImportantProperties = new float[] { 30f };
    }
}

