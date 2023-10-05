using Newtonsoft.Json.Bson;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Modules.ScriptComponents;


public static class ObjectTrackers
{
    static private Func<IEnumerable<PlayerControl>> PlayerSupplier = () => PlayerControl.AllPlayerControls.GetFastEnumerator();
    static private Func<PlayerControl, Vector2> DefaultPlayerPosConverter = (p) => p.GetTruePosition();
    static private Func<PlayerControl, SpriteRenderer> DefaultPlayerRendererConverter = (p) => p.cosmetics.currentBodySprite.BodySprite;

    static private Func<IEnumerable<DeadBody>> DeadBodySupplier = () => Helpers.AllDeadBodies();
    static private Func<DeadBody, Vector2> DefaultDeadBodyPosConverter = (d) => d.TruePosition;
    static private Func<DeadBody, SpriteRenderer> DefaultDeadBodyRendererConverter = (d) => d.bodyRenderers[0];

    public static ObjectTracker<PlayerControl> ForPlayer(float distance, PlayerControl tracker, Predicate<PlayerControl>? candidatePredicate)
    {
        return new ObjectTracker<PlayerControl>(distance, tracker, PlayerSupplier,
            (p) => (candidatePredicate?.Invoke(p) ?? true) && (p.GetModInfo()?.HasAttribute(AttributeModulator.PlayerAttribute.Invisible) ?? true),
            DefaultPlayerPosConverter, DefaultPlayerRendererConverter);
    }

    public static ObjectTracker<DeadBody> ForDeadBody(float distance, PlayerControl tracker, Predicate<DeadBody>? candidatePredicate)
    {
        return new ObjectTracker<DeadBody>(distance, tracker, DeadBodySupplier, candidatePredicate, DefaultDeadBodyPosConverter, DefaultDeadBodyRendererConverter);
    }
}

public class ObjectTracker<T> : INebulaScriptComponent where T : MonoBehaviour 
{
    public T? CurrentTarget { get; private set; }
    private PlayerControl tracker;
    private Func<IEnumerable<T>> enumerableSupplier;
    private Predicate<T>? candidatePredicate;
    private Func<T, Vector2> positionConverter;
    private Func<T, SpriteRenderer> rendererConverter;
    public Color? Color = UnityEngine.Color.yellow;
    private bool UpdateTarget { get; set; } = true;
    private float MaxDistance { get; set; } = 1f;
    public bool IgnoreColliders { get; set; } = false;

    public override bool UpdateWithMyPlayer => true;

    public ObjectTracker(float distance, PlayerControl tracker, Func<IEnumerable<T>> enumerableSupplier, Predicate<T>? candidatePredicate, Func<T, Vector2> positionConverter, Func<T, SpriteRenderer> rendererConverter)
    {
        CurrentTarget = null;
        this.tracker = tracker;
        this.candidatePredicate = candidatePredicate;
        this.positionConverter = positionConverter;
        MaxDistance = distance;
        this.rendererConverter = rendererConverter;
        this.enumerableSupplier = enumerableSupplier;
    }

    private void ShowTarget()
    {
        if (!CurrentTarget) return;

        if (Color.HasValue)
        {
            var renderer = rendererConverter.Invoke(CurrentTarget!);
            renderer.material.SetFloat("_Outline", 1f);
            renderer.material.SetColor("_OutlineColor", Color.Value);
        }
    }

    public override void Update()
    {
        if (!UpdateTarget)
        {
            ShowTarget();
            return;
        }

        if (!tracker)
        {
            CurrentTarget = null;
            return;
        }

        if (!CurrentTarget) CurrentTarget = null;

        Vector2 myPos = tracker.GetTruePosition();

        float distance = float.MaxValue;
        T? candidate = null;

        foreach (var t in enumerableSupplier.Invoke())
        {
            Vector2 pos = positionConverter(t);
            Vector2 dVec = pos - myPos;
            float magnitude = dVec.magnitude;
            if (MaxDistance < magnitude) continue;
            if (candidate != null && distance < magnitude) continue;
            if (!(candidatePredicate?.Invoke(t) ?? true)) continue;
            if (!IgnoreColliders && PhysicsHelpers.AnyNonTriggersBetween(myPos, dVec.normalized, magnitude, Constants.ShipAndObjectsMask)) continue;

            candidate = t;
        }

        CurrentTarget = candidate;
        ShowTarget();
    }
}
