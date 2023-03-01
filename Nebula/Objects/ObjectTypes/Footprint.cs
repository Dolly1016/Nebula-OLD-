namespace Nebula.Objects.ObjectTypes;

public class Footprint : TypeWithImage
{
    public Footprint() : base(11, "Footprint", new SpriteLoader("Nebula.Resources.BloodyFootprint.png",140f))
    {
    }

    public override bool RequireMonoBehaviour => true;

    public IEnumerator GetEnumerator(CustomObject obj)
    {
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime;

            if (obj.Renderer.color.a < Time.deltaTime) break;
            else if (t > 4f)
            {
                obj.Renderer.color = new Color(1f, 1f, 1f, obj.Renderer.color.a - Time.deltaTime);
                obj.Renderer.transform.localScale += new Vector3(Time.deltaTime * 0.4f, Time.deltaTime * 0.4f, 0f);
            }
            else
            {
                obj.Renderer.transform.localScale += new Vector3(Time.deltaTime * 0.1f, Time.deltaTime * 0.1f, 0f);
            }
            yield return null;
        }

        RPCEvents.ObjectDestroy(obj.Id);
    }

    public override void Initialize(CustomObject obj)
    {
        base.Initialize(obj);

        obj.Renderer.transform.eulerAngles = new Vector3(0f, 0f, (float)NebulaPlugin.rnd.NextDouble() * 360f);
        obj.Behaviour.StartCoroutine(GetEnumerator(obj).WrapToIl2Cpp());
    }
}