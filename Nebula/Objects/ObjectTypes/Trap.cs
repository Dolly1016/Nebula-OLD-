using UnityEngine;

namespace Nebula.Objects.ObjectTypes;

public class VisibleTrap : DelayedObject
{
    public VisibleTrap(byte id, string objectName, ISpriteLoader sprite) : base(id, objectName, sprite)
    {
    }

    public override void Initialize(CustomObject obj)
    {
        base.Initialize(obj);
        canSeeInShadow = true;
        Events.Schedule.RegisterPreMeetingAction(() =>
        {
                //Trapperを考慮に入れる
                Game.GameData.data.EstimationAI.DetermineMultiply(new Roles.Role[] { Roles.Roles.NiceTrapper, Roles.Roles.EvilTrapper });
        }, 1);
    }
}

public class InvisibleTrap : DelayedObjectPredicate
{
    public InvisibleTrap(byte id, string objectName, ISpriteLoader sprite) : base(id, objectName, sprite, (obj) =>
    {
        if (obj.OwnerId == PlayerControl.LocalPlayer.PlayerId) return true;
        var r = Game.GameData.data.myData.getGlobalData().role;
        return r == Roles.Roles.NiceTrapper || r == Roles.Roles.EvilTrapper;
    })
    {
        canSeeInShadow = true;
    }

}

public class KillTrap : InvisibleTrap
{
    private Sprite BrokenSprite;

    private Sprite GetBrokenSprite()
    {
        if (BrokenSprite) return BrokenSprite;
        BrokenSprite = Helpers.loadSpriteFromResources("Nebula.Resources.KillTrapBroken.png", 150f);
        return BrokenSprite;
    }

    public KillTrap(byte id, string objectName, ISpriteLoader sprite) : base(id, objectName, sprite)
    {
    }

    public override void Initialize(CustomObject obj)
    {
        base.Initialize(obj);

        obj.Data = new int[1];
        obj.Data[0] = 0;
    }

    public override void Update(CustomObject obj, int command)
    {
        obj.Renderer.sprite = GetBrokenSprite();
        obj.Data[0] = 1;
    }

    public override void Update(CustomObject obj)
    {
        if (obj.Data[0] == 1)
        {
            obj.GameObject.active = true;
            if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            base.Update(obj);
        }
    }
}