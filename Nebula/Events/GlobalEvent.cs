namespace Nebula.Events;

public delegate GlobalEvent GlobalEventGenerator(float duration, ulong option);
public class GlobalEvent
{
    static public List<GlobalEvent> Events = new List<GlobalEvent>();
    static private Dictionary<Type, GlobalEventGenerator> Generators = new Dictionary<Type, GlobalEventGenerator>();

    public class Type
    {
        static private byte availableId = 0;
        static public Type Camouflage = new Type();
        static public Type EMI = new Type();
        static public Type BlackOut = new Type();

        public byte Id { get; }

        private Type()
        {
            Id = availableId;
            availableId++;
        }

        public static HashSet<Type> AllTypes = new HashSet<Type>()
            {
                Camouflage,
                EMI,
                BlackOut
            };

        public static Type GetType(byte id)
        {
            foreach (Type type in AllTypes)
            {
                if (type.Id == id)
                {
                    return type;
                }
            }
            return null;
        }
    }

    public Type type { get; }
    public float duration { get; private set; }
    public bool SpreadOverMeeting { get; protected set; }

    public bool CheckTerminal()
    {
        if (duration < 0)
        {
            OnTerminal();
            return true;
        }
        return false;
    }

    public virtual void OnTerminal()
    {

    }

    public virtual void OnActivate()
    {

    }

    public virtual void Update(float left)
    {

    }

    protected GlobalEvent(Type type, float duration, ulong option)
    {
        this.type = type;
        this.duration = duration;
    }

    static public bool IsActive(Type type)
    {
        foreach (GlobalEvent globalEvent in Events)
        {
            if (globalEvent.type != type) continue;
            if (globalEvent.duration > 0) return true;
        }
        return false;
    }

    static public void Update()
    {
        foreach (GlobalEvent globalEvent in Events)
        {
            globalEvent.duration -= Time.deltaTime;
            globalEvent.Update(globalEvent.duration);
        }

        Events.RemoveAll(e => e.CheckTerminal());
    }

    static public bool Activate(GlobalEvent.Type type, float duration, ulong option)
    {
        if (Generators.ContainsKey(type))
        {
            GlobalEvent e = Generators[type](duration, option);
            e.OnActivate();
            Events.Add(e);
            return true;
        }
        return false;
    }

    static public void Register(GlobalEvent.Type type, GlobalEventGenerator generator)
    {
        Generators.Add(type, generator);
    }

    static public void Initialize()
    {
        Events.Clear();
    }

    static public void OnMeeting()
    {
        foreach (GlobalEvent globalEvent in Events)
        {
            if (!globalEvent.SpreadOverMeeting)
            {
                globalEvent.duration = -1;
            }
        }

        Events.RemoveAll(e => e.CheckTerminal());
    }
}
