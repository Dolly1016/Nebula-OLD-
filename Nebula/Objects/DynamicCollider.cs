namespace Nebula.Objects;

public class DynamicCollider
{
    private Collider2D collider;
    public float duration { get; private set; }
    private bool spreadOverMeeting;
    private bool impostorsCanIgnore;
    private ulong ignoreMask;
    public bool IsValid;
    private Action<DynamicCollider> updateAction;

    public DynamicCollider(Collider2D collider, float duration, bool spreadOverMeeting, Action<DynamicCollider> updateAction, bool impostorsCanIgnore = false, ulong ignoreMask = 0)
    {
        this.collider = collider;
        this.duration = duration;
        this.spreadOverMeeting = spreadOverMeeting;
        this.impostorsCanIgnore = impostorsCanIgnore;
        this.ignoreMask = ignoreMask;
        this.updateAction = updateAction;
        this.IsValid = true;

        collider.gameObject.layer = LayerMask.NameToLayer("Ship");

        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor && impostorsCanIgnore)
            collider.enabled = false;

        Game.GameData.data.ColliderManager.Colliders.Add(this);

        UpdateCollision();
    }

    public void UpdateCollision()
    {
        if (impostorsCanIgnore && PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            collider.enabled = false;
        else if ((ignoreMask & (ulong)(1 << PlayerControl.LocalPlayer.PlayerId)) != 0)
            collider.enabled = false;
        else
            collider.enabled = true;
    }

    public void Update()
    {
        if (!IsValid) return;

        updateAction.Invoke(this);
        duration -= Time.deltaTime;
        if (duration < 0f)
            OnFinalize();
        else
            UpdateCollision();
    }

    public void OnMeetingEnd()
    {
        if (!spreadOverMeeting)
            OnFinalize();
    }

    public void OnFinalize()
    {
        GameObject.Destroy(collider.gameObject);
        IsValid = false;
    }
}

public class ColliderManager
{
    public HashSet<DynamicCollider> Colliders = new HashSet<DynamicCollider>();

    public void Update()
    {
        foreach (var c in Colliders)
        {
            c.Update();
        }
        Colliders.RemoveWhere(c => (!c.IsValid));
    }

    public void OnMeetingEnd()
    {
        foreach (var c in Colliders)
        {
            c.OnMeetingEnd();
        }
    }
}