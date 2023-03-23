
namespace Nebula;

public static class NebulaEffects
{
    private static SpriteLoader smokeSprite = new("Nebula.Resources.Smoke.png", 100f);
    private static SpriteLoader[] leafSprites = new SpriteLoader[]{
        new("Nebula.Resources.Leaf0.png", 100f),
        new("Nebula.Resources.Leaf1.png", 100f),
        new("Nebula.Resources.Leaf2.png", 100f)
    };
    
    private static IEnumerator CoPlayEffect(int layer, string name,SpriteLoader sprite,Transform? parent, Vector3 pos, Vector3 velocity, float angVel, float scale,Color color, float maxTime,float fadeInTime,float fadeOutTime)
    {
        var obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(scale, scale, 1f);
        obj.transform.localEulerAngles = new Vector3(0, 0, (float)NebulaPlugin.rnd.NextDouble() * 360f);
        obj.layer = layer;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite.GetSprite();
        
        float p = 0f;
        while (p < maxTime)
        {
            obj.transform.localPosition += velocity * Time.deltaTime;
            obj.transform.eulerAngles += new Vector3(0, 0, angVel * Time.deltaTime);
            
            float c = 1f;
            if (fadeInTime > 0f && p < fadeInTime) c = Math.Clamp(p / fadeInTime, 0f, 1f);
            else if (fadeOutTime > 0f) c = Math.Clamp((maxTime - p) / fadeOutTime, 0f, 1f);

            renderer.color = new Color(color.r, color.g, color.b, c);
            p += Time.deltaTime;
            yield return null;
        }
        GameObject.Destroy(obj);
    }
    public static IEnumerator CoLeafEffect(int layer, Transform? parent, Vector3 pos, Vector3 velocity, float angVel, float scale)
    {
        return CoPlayEffect(layer, "Leaf", leafSprites[NebulaPlugin.rnd.Next(leafSprites.Length)], parent, pos, velocity, angVel, scale, Color.white, 0.3f + (float)NebulaPlugin.rnd.NextDouble() * 0.7f, 0.2f, 0.2f);
    }

    public static IEnumerator CoSmokeEffect(int layer, Transform? parent, Vector3 pos, Vector3 velocity, float angVel, float scale)
    {
        return CoPlayEffect(layer, "Smoke", smokeSprite, parent, pos, velocity, angVel, scale, Color.white, 0.4f, 0f, 0.35f);
    }

    public static IEnumerator CoGroupOfLeavesEffect(MonoBehaviour coroutineHolder, int layer, Transform? parent, Vector3 pos, float scale = 1f)
    {
        var obj = new GameObject("LeavesGroup");
        if (parent != null) obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(scale, scale, 1f);

        for (int i = 0; i < 7; i++)
        {
            coroutineHolder.StartCoroutine(CoLeafEffect(layer, obj.transform,
                 new Vector3((float)NebulaPlugin.rnd.NextDouble() * 0.8f - 0.4f, (float)NebulaPlugin.rnd.NextDouble() * 1.8f - 0.5f),
                new Vector3((float)NebulaPlugin.rnd.NextDouble() * 0.6f-0.3f, 0f) + Vector3.down*(0.05f+ (float)NebulaPlugin.rnd.NextDouble() * 0.25f),
                (float)NebulaPlugin.rnd.NextDouble() * 10, 0.8f + (float)NebulaPlugin.rnd.NextDouble() * 0.2f).WrapToIl2Cpp());
        }

        yield return Effects.Wait(1f);
        GameObject.Destroy(obj);
    }

    public static IEnumerator CoDisappearEffect(MonoBehaviour coroutineHolder,int layer,Transform? parent, Vector3 pos,float scale=1f)
    {
        var obj = new GameObject("DisappearEffect");
        if (parent != null) obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(scale, scale, 1f);


        //円を描くように7つの煙を配置
        for (int i = 0; i < 7; i++)
        {
            coroutineHolder.StartCoroutine(CoSmokeEffect(layer, obj.transform, new Vector3(0.4f, 0f).RotateZ(360f / 7f * (float)i),
                new Vector3((float)NebulaPlugin.rnd.NextDouble() * 0.4f + 0.1f, 0f).RotateZ((float)NebulaPlugin.rnd.NextDouble() * 360f),
                (float)NebulaPlugin.rnd.NextDouble() * 40, 0.35f + (float)NebulaPlugin.rnd.NextDouble() * 0.1f).WrapToIl2Cpp());
        }
        //ランダムに配置
        for (int i = 0; i < 4; i++)
        {
            coroutineHolder.StartCoroutine(CoSmokeEffect(layer, obj.transform,
                 new Vector3((float)NebulaPlugin.rnd.NextDouble() * 0.3f, 0f).RotateZ((float)NebulaPlugin.rnd.NextDouble() * 360f),
                new Vector3((float)NebulaPlugin.rnd.NextDouble() * 0.4f + 0.1f, 0f).RotateZ((float)NebulaPlugin.rnd.NextDouble() * 360f),
                (float)NebulaPlugin.rnd.NextDouble() * 40, 0.35f + (float)NebulaPlugin.rnd.NextDouble() * 0.1f).WrapToIl2Cpp());
        }

        yield return Effects.Wait(0.5f);
        GameObject.Destroy(obj);
    }

    public static IEnumerator CoWait(IEnumerator coroutine, Action waitAction)
    {
        while (coroutine.MoveNext())
        {
            waitAction.Invoke();
            yield return null;
        }
    }
}
