namespace Nebula.Events.Variation;

public class EMI : GlobalEvent
{
    public EMI(float duration, ulong option) : base(GlobalEvent.Type.EMI, duration, option)
    {
    }
}

