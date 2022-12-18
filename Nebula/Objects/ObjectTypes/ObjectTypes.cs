namespace Nebula.Objects.ObjectTypes;

public class TypeWithImage : CustomObject.Type
{

    private Sprite Sprite;
    private string SpriteAddress;
    private float PixelsPerUnit;
    public Sprite GetSprite()
    {
        if (Sprite) return Sprite;
        Sprite = Helpers.loadSpriteFromResources(SpriteAddress, PixelsPerUnit);
        return Sprite;
    }

    public TypeWithImage(byte id, string objectName, string spriteAddress,float pixelsPerUnit =150f) : base(id, objectName)
    {
        SpriteAddress = spriteAddress;
        PixelsPerUnit= pixelsPerUnit;
    }

    public override void Initialize(CustomObject obj)
    {
        obj.Renderer.sprite = GetSprite();
    }
}

public class DelayedObject : TypeWithImage
{
    public DelayedObject(byte id, string objectName, string spriteAddress) : base(id, objectName, spriteAddress)
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
    public DelayedObjectPredicate(byte id, string objectName, string spriteAddress, Predicate<CustomObject> predicate) : base(id, objectName, spriteAddress)
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