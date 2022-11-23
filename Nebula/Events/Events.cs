namespace Nebula.Events;

class Events
{
    static public void Load()
    {
        GlobalEvent.Register(GlobalEvent.Type.Camouflage, (duration, option) => { return new Variation.Camouflage(duration, option); });
        GlobalEvent.Register(GlobalEvent.Type.BlackOut, (duration, option) => { return new Variation.BlackOut(duration, option); });
        GlobalEvent.Register(GlobalEvent.Type.EMI, (duration, option) => { return new Variation.EMI(duration, option); });
    }

}

