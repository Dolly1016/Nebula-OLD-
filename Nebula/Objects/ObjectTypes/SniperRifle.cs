namespace Nebula.Objects.ObjectTypes;

public class SniperRifle : TypeWithImage
{
    public SniperRifle() : base(4, "SniperRifle", new SpriteLoader("Nebula.Resources.SniperRifle.png",150f))
    {

    }

    public override CustomObject.ObjectOrder GetObjectOrder(CustomObject? obj)
    {
        return CustomObject.ObjectOrder.IsFront;
    }

    public override void Update(CustomObject obj)
    {
        var player = Game.GameData.data.playersArray[obj.OwnerId];
        var targetPosition = Helpers.playerById(obj.OwnerId).transform.position + new Vector3(0.8f * (float)Math.Cos(player.MouseAngle), 0.8f * (float)Math.Sin(player.MouseAngle));
        obj.GameObject.transform.position += (targetPosition - obj.GameObject.transform.position) * 0.4f;
        FixZPosition(obj);
        obj.Renderer.transform.eulerAngles = new Vector3(0f, 0f, (float)(player.MouseAngle * 360f / Math.PI / 2f));
        if (Math.Cos(player.MouseAngle) < 0.0)
        {
            if (obj.Renderer.transform.localScale.y > 0)
                obj.Renderer.transform.localScale = new Vector3(1f, -1f);
        }
        else
        {
            if (obj.Renderer.transform.localScale.y < 0)
                obj.Renderer.transform.localScale = new Vector3(1f, 1f);
        }

        if (Helpers.playerById(obj.OwnerId).inVent)
            obj.GameObject.active = false;
        else
            obj.GameObject.active = true;
    }
}