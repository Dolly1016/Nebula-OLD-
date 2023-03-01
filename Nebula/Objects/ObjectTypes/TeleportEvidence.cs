using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Objects.ObjectTypes;

public class TeleportEvidence : TypeWithImage
{
    public TeleportEvidence() : base(12, "TeleportEvidence", new SpriteLoader("Nebula.Resources.TeleportEvidence.png",100f))
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
            else if (t > 10f)
            {
                obj.Renderer.color = new Color(1f, 1f, 1f, obj.Renderer.color.a - Time.deltaTime);
            }
            yield return null;
        }

        RPCEvents.ObjectDestroy(obj.Id);
    }

    public override void Initialize(CustomObject obj)
    {
        base.Initialize(obj);

        obj.Behaviour.StartCoroutine(GetEnumerator(obj).WrapToIl2Cpp());
    }
}
