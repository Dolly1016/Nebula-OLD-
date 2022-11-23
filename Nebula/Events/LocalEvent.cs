namespace Nebula.Events;

public class LocalEvent
{
    static public List<LocalEvent> Events = new List<LocalEvent>();

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

    public virtual void LocalUpdate()
    {

    }

    protected LocalEvent(float duration)
    {
        this.duration = duration;
    }

    static public void Update()
    {
        foreach (LocalEvent localEvent in Events)
        {
            localEvent.LocalUpdate();
            localEvent.duration -= Time.deltaTime;
        }

        Events.RemoveAll(e => e.CheckTerminal());
    }

    static public void Activate(LocalEvent localEvent)
    {
        localEvent.OnActivate();
        Events.Add(localEvent);
    }

    static public void Inactivate(Predicate<LocalEvent> predicate)
    {
        Events.RemoveAll((e) =>
        {
            if (predicate(e))
            {
                e.OnTerminal();
                return true;
            }
            return false;
        });
    }

    static public void Initialize()
    {
        Events.Clear();
    }

    static public void OnMeeting()
    {
        foreach (LocalEvent localEvent in Events)
        {
            if (!localEvent.SpreadOverMeeting)
            {
                localEvent.duration = -1;
            }
        }

        Events.RemoveAll(e => e.CheckTerminal());
    }
}