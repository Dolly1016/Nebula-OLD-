using JetBrains.Annotations;
using Nebula.Roles.Perk;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Objects;

public class ArrowDisplay : MonoBehaviour
{
    private HashSet<AbstractArrow> AllArrows = new HashSet<AbstractArrow>();

    static ArrowDisplay()
    {
        ClassInjector.RegisterTypeInIl2Cpp<ArrowDisplay>();
    }

    public void Update()
    {
        AllArrows.RemoveWhere((arrow)=>
        {
            bool result = !arrow.IsValid();
            if (result)
                arrow.Destroy();
            else
                arrow.Update();
            return result;
        });
    }

    public void RegisterArrow(AbstractArrow arrow)
    {
        AllArrows.Add(arrow);
    }

    public void RemoveArrow(AbstractArrow arrow)
    {
        arrow.Destroy();
        AllArrows.Remove(arrow);
    }

    public void RemoveArrow(string name)
    {
        AllArrows.RemoveWhere(
            (arrow) =>
            {
                if (arrow.Name == name)
                {
                    arrow.Destroy();
                    return true;
                }
                return false;
            }
            );
    }
}

[HarmonyPatch(typeof(HudManager),nameof(HudManager.Start))]
public static class AddArrowDisplayPatch
{
    static public ArrowDisplay CurrentDisplay;
    public static void Prefix(HudManager __instance)
    {
        var obj = new GameObject("ArrowDisplay");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(__instance.transform);
        CurrentDisplay = obj.AddComponent<ArrowDisplay>();
        obj.transform.localPosition = new Vector3(0, 0, -10f);
    }
}

public abstract class AbstractArrow
{
    protected GameObject arrowObject;
    protected SpriteRenderer renderer;
    private bool isBroken = false;
    public bool IsAffectedByComms { get; set; } = false;

    public Color Color { get { return renderer.color; } set { renderer.color = value; } }
    public string Name { get { return arrowObject.name; } }

    public virtual bool IsValid() => !isBroken;

    public static implicit operator bool(AbstractArrow? arrow)
    {
        if (arrow == null) return false;
        return arrow.IsValid();
    }

    public void Destroy()
    {
        if (isBroken) return;

        GameObject.Destroy(arrowObject);
        isBroken = true;
    }

    public virtual void Update()
    {
        if(isBroken) return;
        if (IsAffectedByComms && PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
        {
            arrowObject.SetActive(false);
            return;
        }
        arrowObject.SetActive(true);
    }

    public AbstractArrow(string name = "Arrow")
    {
        arrowObject = new GameObject(name);
        arrowObject.layer = LayerExpansion.GetUILayer();
        arrowObject.transform.SetParent(AddArrowDisplayPatch.CurrentDisplay.transform);
        renderer = arrowObject.AddComponent<SpriteRenderer>();
        AddArrowDisplayPatch.CurrentDisplay.RegisterArrow(this);
        arrowObject.SetActive(false);
    }

    private static float perc = 0.925f;

    protected void UpdateArrow(Vector2 position,bool smallenNearArrow,bool rotate)
    {
        Camera main = Camera.main;
        Vector2 vector = position - (Vector2)main.transform.position;
        float num = vector.magnitude / (main.orthographicSize * perc);

        //近くの矢印を隠す
        bool flag = !smallenNearArrow || (double)num > 0.3;
        arrowObject.SetActive(flag);
        if (!flag) return;
        
        bool Between(float value, float min, float max) => value > min && value < max;
        Vector2 viewportPoint = main.WorldToViewportPoint(position);
        if (Between(viewportPoint.x, 0f, 1f) && Between(viewportPoint.y, 0f, 1f))
        {
            //画面内のオブジェクト

            arrowObject.transform.localPosition = vector - vector.normalized * 0.6f;
            if (smallenNearArrow)
                arrowObject.transform.localScale = Vector3.one * Mathf.Clamp(num, 0f, 1f);
            else
                arrowObject.transform.localScale = Vector3.one;
            
        }
        else
        {
            Vector2 vector3 = new Vector2(Mathf.Clamp(viewportPoint.x * 2f - 1f, -1f, 1f), Mathf.Clamp(viewportPoint.y * 2f - 1f, -1f, 1f));
            float orthographicSize = main.orthographicSize;
            float num3 = main.orthographicSize * main.aspect;
            Vector3 vector4 = new Vector3(Mathf.LerpUnclamped(0f, num3 * 0.88f, vector3.x), Mathf.LerpUnclamped(0f, orthographicSize * 0.79f, vector3.y), 0f);
            arrowObject.transform.localPosition = vector4;
            arrowObject.transform.localScale = Vector3.one;
        }

        //矢印の向きを調整
        if (rotate)
        {
            vector.Normalize();
            arrowObject.transform.eulerAngles = new Vector3(0f, 0f, Mathf.Atan2(vector.y, vector.x) * 180f / Mathf.PI);
        }
    }

    public void StartCoroutine(IEnumerator coroutine)
    {
        AddArrowDisplayPatch.CurrentDisplay.StartCoroutine(coroutine.WrapToIl2Cpp());
    }

    static public void RemoveArrow(string name)
    {
        AddArrowDisplayPatch.CurrentDisplay.RemoveArrow(name);
    }
}

public abstract class NormalArrow : AbstractArrow
{
    private bool smallenNearArrow;
    
    static private SpriteLoader defaultArrowSprite = new("Nebula.Resources.Arrow.png", 200f);
    public override void Update()
    {
        base.Update();
        UpdateArrow(CurrentGoal,smallenNearArrow,true);
    }

    abstract protected Vector2 CurrentGoal { get; }

    public NormalArrow(string name,bool smallenNearArrow,Color color,Sprite? sprite = null):base(name)
    {
        this.smallenNearArrow= smallenNearArrow;

        renderer.sprite = sprite ?? defaultArrowSprite.GetSprite();
        renderer.color = color;
    }
}

public class FollowerArrow : NormalArrow
{
    GameObject target;

    public override bool IsValid() => base.IsValid() && target;
    protected override Vector2 CurrentGoal => target.transform.position;

    public FollowerArrow(string name, bool smallenNearArrow, GameObject target,Color color, Sprite? sprite = null)
        :base(name,smallenNearArrow,color,sprite)
    {
        this.target = target;
    }
}

public class FixedArrow : NormalArrow
{
    public Vector2 Position { get; set; } = PlayerControl.LocalPlayer.transform.position;

    protected override Vector2 CurrentGoal => Position;


    public FixedArrow(string name, bool smallenNearArrow, Vector2 position,Color color, Sprite? sprite = null)
        : base(name, smallenNearArrow, color, sprite)
    {
        Position = position;
    }
}

public class PlayerArrow
{
    Dictionary<byte, FollowerArrow> arrows = new();
    Predicate<PlayerControl> predicate;
    public Color ArrowColor { get; set; } = Color.white;
    public string ArrowName { get; set; } = "PlayersArrow";
    public Sprite? ArrowSprite { get; set; } = null;
    public bool ArrowAffecetedByComms { get; set; } = false;
    public void Update()
    {
        foreach(var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (predicate.Invoke(p))
            {
                if (!arrows.ContainsKey(p.PlayerId)) arrows[p.PlayerId] = new FollowerArrow(ArrowName, true, p.gameObject, ArrowColor, ArrowSprite) { IsAffectedByComms = ArrowAffecetedByComms };
            }
            else
            {
                if (arrows.ContainsKey(p.PlayerId))
                {
                    arrows[p.PlayerId].Destroy();
                    arrows.Remove(p.PlayerId);
                }
            }
        }
    }

    public void Destroy()
    {
        foreach (var arrow in arrows.Values) arrow.Destroy();
        arrows.Clear();
    }
    public PlayerArrow(Predicate<PlayerControl> predicate) {
        this.predicate = predicate;
    }
}

public class PositionalIcon : AbstractArrow
{
    private bool smallenNearIcon;

    public override void Update()
    {
        base.Update();
        UpdateArrow(position, smallenNearIcon, true);
    }

    private Vector2 position { get; init; }

    public PositionalIcon(string name, Vector2 position,bool smallenNearIcon, Sprite sprite) : base(name)
    {
        this.position = position;
        this.smallenNearIcon = smallenNearIcon;

        renderer = arrowObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
    }
}

public class ModPing : AbstractArrow
{
    public override void Update()
    {
        base.Update();
        UpdateArrow(position, false, false);
    }

    private Vector2 position { get; init; }


    public ModPing(Vector2 position,Color? color = null) : base("Ping")
    {
        this.position = position;

    }
}


/*
public class Arrow
{
    public float perc = 0.925f;
    public SpriteRenderer image;
    public GameObject arrow;
    private Vector3 oldTarget;
    private bool smallenNearArrow;

    
    static private Sprite sprite;
    
    public static Sprite getSprite()
    {

        if (sprite) return sprite;
        sprite = Helpers.loadSpriteFromResources("Nebula.Resources.Arrow.png", 200f);
        return sprite;
    }


    public Arrow(Color color,bool smallenNearArrow=true,Sprite? sprite=null)
    {
        arrow = new GameObject("Arrow");
        arrow.layer = 5;
        image = arrow.AddComponent<SpriteRenderer>();
        image.sprite = sprite != null ? sprite : getSprite();
        image.color = color;
        this.smallenNearArrow = smallenNearArrow;
    }

    public void Update()
    {
        Vector3 target = oldTarget;
        if (target == null) target = Vector3.zero;
        Update(target);
    }

    public void Update(Vector3 target, Color? color = null)
    {
        if (arrow == null) return;
        oldTarget = target;

        if (color.HasValue) image.color = color.Value;

        Camera main = Camera.main;
        Vector2 vector = target - main.transform.position;
        float num = vector.magnitude / (main.orthographicSize * perc);
        image.enabled = !smallenNearArrow || (double)num > 0.3;
        Vector2 vector2 = main.WorldToViewportPoint(target);
        if (Between(vector2.x, 0f, 1f) && Between(vector2.y, 0f, 1f))
        {
            arrow.transform.position = target - (Vector3)vector.normalized * 0.6f;
            if (smallenNearArrow)
            {
                float num2 = Mathf.Clamp(num, 0f, 1f);
                arrow.transform.localScale = new Vector3(num2, num2, num2);
            }
            else
            {
                arrow.transform.localScale = Vector3.one;
            }
        }
        else
        {
            Vector2 vector3 = new Vector2(Mathf.Clamp(vector2.x * 2f - 1f, -1f, 1f), Mathf.Clamp(vector2.y * 2f - 1f, -1f, 1f));
            float orthographicSize = main.orthographicSize;
            float num3 = main.orthographicSize * main.aspect;
            Vector3 vector4 = new Vector3(Mathf.LerpUnclamped(0f, num3 * 0.88f, vector3.x), Mathf.LerpUnclamped(0f, orthographicSize * 0.79f, vector3.y), 0f);
            arrow.transform.position = main.transform.position + vector4;
            arrow.transform.localScale = Vector3.one;
        }

        LookAt2d(arrow.transform, target);
    }

    private void LookAt2d(Transform transform, Vector3 target)
    {
        Vector3 vector = target - transform.position;
        vector.Normalize();
        float num = Mathf.Atan2(vector.y, vector.x);
        if (transform.lossyScale.x < 0f)
            num += 3.1415927f;
        transform.rotation = Quaternion.Euler(0f, 0f, num * 57.29578f);
    }

    private bool Between(float value, float min, float max)
    {
        return value > min && value < max;
    }
}
*/