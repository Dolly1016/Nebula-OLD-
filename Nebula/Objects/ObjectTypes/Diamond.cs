namespace Nebula.Objects.ObjectTypes;

public class Diamond : TypeWithImage
{
    public Diamond() : base(10, "Diamond", new SpriteLoader("Nebula.Resources.Diamond.png",100f))
    {

    }

    public override bool CanSeeInShadow(CustomObject? obj)
    {
        return true;
    }

    public override bool IsUsable => true;
    public override Color UsableColor => Color.yellow;
    public override bool CanUse(CustomObject obj, PlayerControl player)
    {
        if (player.GetModData().HasExtraRole(Roles.Roles.DiamondPossessor)) return false;
        return true;
    }
    public override void Use(CustomObject obj)
    {
        RPCEventInvoker.AddExtraRole(PlayerControl.LocalPlayer, Roles.Roles.DiamondPossessor, 0);
        RPCEventInvoker.ObjectDestroy(obj);
    }

    public override void Initialize(CustomObject obj)
    {
        base.Initialize(obj);
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