namespace Nebula.Objects.ObjectTypes;

public class Decoy : TypeWithImage
{
    public Decoy() : base(8, "Decoy", new SpriteLoader("Nebula.Resources.Decoy.png",150f))
    {

    }

    public override void Initialize(CustomObject obj)
    {
        base.Initialize(obj);

        obj.Renderer.flipX = Helpers.playerById(obj.OwnerId).cosmetics.FlipX;
    }

    public override void OnDestroy(CustomObject obj)
    {
        HudManager.Instance.StartCoroutine(NebulaEffects.CoDisappearEffect(HudManager.Instance, LayerExpansion.GetDefaultLayer(), null, obj.GameObject.transform.position - new Vector3(0, 0, 1f), 1f).WrapToIl2Cpp());
    }

    public override CustomObject.ObjectOrder GetObjectOrder(CustomObject? obj)
    {
        return CustomObject.ObjectOrder.IsOnSameRow;
    }

    public override void Update(CustomObject obj)
    {
        base.Update(obj);
        FixZPosition(obj);
    }

    public override bool RequireMonoBehaviour => true;
}