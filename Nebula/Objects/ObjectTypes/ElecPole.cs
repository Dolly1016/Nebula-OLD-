using BepInEx.Unity.IL2CPP.Utils;
using Nebula.Utilities;
using UnityEngine;

namespace Nebula.Objects.ObjectTypes;

public class ElecPole : DelayedObject
{
    private DividedSpriteLoader DividedSprite;
    public ElecPole() : base(6, "ElecPole", new DividedSpriteLoader("Nebula.Resources.DisturbBolt.png", 100f,4,1))
    {
        DividedSprite = Sprite as DividedSpriteLoader;
         
    }

    public override bool RequireMonoBehaviour => true;
    public override CustomObject.ObjectOrder GetObjectOrder(CustomObject? obj)
    {
        return CustomObject.ObjectOrder.IsBack;
    }

    public override void Update(CustomObject obj, int command) {
        IEnumerator GetEnumerator(int[] indexAry)
        {
            int i = 0;
            float t = 1f;
            while(i < indexAry.Length)
            {
                t += Time.deltaTime;
                if (t > 0.1f)
                {
                    obj.Renderer.sprite = DividedSprite.GetSprite(indexAry[i]);
                    t = 0f;
                    i++;
                }
                yield return null;
            }
        }

        Sanitize(obj);

        if (command == 0)
        {
            var collider = obj.GameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            obj.Behaviour.StartCoroutine(GetEnumerator(new int[] { 1, 2, 3 }).WrapToIl2Cpp());
        }
        if (command == 1)
        {
            obj.Behaviour.StartCoroutine(GetEnumerator(new int[] { 2, 1, 0 }).WrapToIl2Cpp());
        }
    }

    private void Sanitize(CustomObject obj)
    {
        obj.Renderer.sprite = Sprite.GetSprite();

        var colliders = obj.GameObject.GetComponents<CircleCollider2D>();
        if (colliders.Count == 0) return;
        foreach (var c in colliders) GameObject.Destroy(c);
    }

    public override void OnMeetingEnd(CustomObject obj)
    {
        Sanitize(obj);
    }
}