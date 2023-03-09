namespace Nebula.Objects.ObjectTypes;

public class TypeWithImage : CustomObject.Type
{
    protected ISpriteLoader Sprite;

    public Sprite GetSprite()
    {
        return Sprite.GetSprite();
    }

    public TypeWithImage(byte id, string objectName, ISpriteLoader sprite) : base(id, objectName)
    {
        Sprite = sprite;
    }

    public override void Initialize(CustomObject obj)
    {
        obj.Renderer.sprite = GetSprite();
    }
}

public class DelayedObject : TypeWithImage
{
    public DelayedObject(byte id, string objectName,ISpriteLoader sprite) : base(id, objectName,sprite)
    {
    }

    protected virtual bool canSeeOnlyMe { get { return false; } }

    public override void Update(CustomObject obj)
    {
        if (canSeeOnlyMe && obj.OwnerId != PlayerControl.LocalPlayer.PlayerId && !Game.GameData.data.myData.CanSeeEveryoneInfo) { obj.GameObject.SetActive(false); return; }
        if (!obj.GameObject.activeSelf) obj.GameObject.SetActive(true);

        if (obj.PassedMeetings == 0)
        {
            if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId && !Game.GameData.data.myData.CanSeeEveryoneInfo)
            {
                if (obj.Renderer.color.a != 0f) obj.Renderer.color = new Color(1f, 1f, 1f, 0f);
            }
            else
            {
                if (obj.Renderer.color.a != 0.5f) obj.Renderer.color = new Color(1f, 1f, 1f, 0.5f);
            }
        }
        else if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
    }
}

public class DelayedObjectPredicate : TypeWithImage
{
    protected Predicate<CustomObject> MyPredicate { get; private set; }
    public DelayedObjectPredicate(byte id, string objectName, ISpriteLoader sprite, Predicate<CustomObject> predicate) : base(id, objectName, sprite)
    {
        MyPredicate = predicate;
    }

    public override void Update(CustomObject obj)
    {
        if (obj.PassedMeetings == 0)
        {
            if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId && !Game.GameData.data.myData.CanSeeEveryoneInfo)
            {
                if (obj.Renderer.color.a != 0f) obj.Renderer.color = new Color(1f, 1f, 1f, 0f);
            }
            else
            {
                if (obj.Renderer.color.a != 0.5f) obj.Renderer.color = new Color(1f, 1f, 1f, 0.5f);
            }
        }
        else
        {
            if (MyPredicate(obj))
            {
                if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                if (obj.Renderer.color.a > 0f) obj.Renderer.color = new Color(1f, 1f, 1f, 0f);
            }

        }
    }
}