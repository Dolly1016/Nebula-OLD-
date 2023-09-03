using Nebula.Game;

namespace Nebula.Modules.ScriptComponents;

public class Timer : INebulaScriptComponent
{
    private Func<bool>? predicate = null;
    private bool isActive;
    private float currentTime;
    private float min, max;

    public Timer Pause()
    {
        isActive = false;
        return this;
    }
    public Timer Start(float? time = null)
    {
        isActive = true;
        currentTime = time.HasValue ? time.Value : max;
        return this;
    }
    public Timer Resume()
    {
        isActive = true;
        return this;
    }
    public Timer Reset()
    {
        currentTime = max;
        return this;
    }
    public Timer SetTime(float time)
    {
        currentTime = time;
        return this;
    }
    public Timer SetRange(float min, float max)
    {
        if (min > max)
        {
            this.max = min;
            this.min = max;
        }
        else
        {
            this.max = max;
            this.min = min;
        }
        return this;
    }
    public float CurrentTime { get => currentTime; }
    public float Percentage { get => max > min ? (currentTime - min) / (max - min) : 0f; }
    public bool IsInProcess => CurrentTime > min;

    public override void Update()
    {
        if (isActive && (predicate?.Invoke() ?? true))
            currentTime = Mathf.Clamp(currentTime - Time.deltaTime, min, max);
    }

    public Timer(float max) : this(0f, max) { }

    public Timer(float min, float max)
    {
        NebulaGameManager.Instance?.RegisterComponent(this);
        SetRange(min, max);
        Reset();
        Pause();
    }

    public Timer SetPredicate(Func<bool>? predicate)
    {
        this.predicate = predicate;
        return this;
    }

    public void Abandon()
    {
        NebulaGameManager.Instance?.ReleaseComponent(this);
    }

    public Timer SetAsKillCoolDown()
    {
        return SetPredicate(() => PlayerControl.LocalPlayer.IsKillTimerEnabled);
    }

    public Timer SetAsAbilityCoolDown()
    {
        return SetPredicate(() => PlayerControl.LocalPlayer.CanMove);
    }
}
