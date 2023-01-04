using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Objects
{
    public class EffectCircle
    {
        static private HashSet<EffectCircle> allCircles=new HashSet<EffectCircle>();

        static public void Initialize()
        {
            foreach(var c in allCircles)if(c.gameObject)GameObject.Destroy(c.gameObject);
            allCircles.Clear();
        }

        static private SpriteLoader circleSprite = new SpriteLoader("Nebula.Resources.EffectCircle.png", 250f);
        static private SpriteLoader circleInnerSprite = new SpriteLoader("Nebula.Resources.EffectCircleInner.png", 100f);

        float timer;
        GameObject gameObject;
        SpriteRenderer renderer;
        float size;
        int phase;
        Color color;
        Color? hitColor;

        public EffectCircle(Vector2 pos, Color color, float size, float duration = -1f, bool isInner = false, Color? hitColor = null) => new EffectCircle(pos, null, color, size, duration,isInner,hitColor);
        public EffectCircle(GameObject obj, Color color, float size, float duration = -1f, bool isInner = false, Color? hitColor = null) => new EffectCircle(new Vector2(0f, 0f), obj, color, size, duration,isInner,hitColor);

        public EffectCircle(Vector2 pos,GameObject? obj, Color color,float size,float duration = -1f, bool isInner = false, Color? hitColor = null)
        {
            gameObject = new GameObject("Circle");
            if (obj != null) gameObject.transform.SetParent(obj.transform);
            gameObject.transform.localPosition=new Vector3(pos.x, pos.y, -50f);
            gameObject.transform.localScale = new Vector3(0,0,1f);
            renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = isInner ? circleInnerSprite.GetSprite() : circleSprite.GetSprite();
            renderer.color=color.AlphaMultiplied(0f);
            phase = 0;
            this.size = size;
            this.color = color;
            this.hitColor = hitColor;
            this.timer = duration;
            allCircles.Add(this);

        }

        public static void Update()
        {
            allCircles.RemoveWhere((c) =>
            {
                if (c.timer > 0f)
                {
                    c.timer -= Time.deltaTime;
                    if (!(c.timer > 0f))
                    {
                        c.phase = 1;
                    }
                }

                bool hitFlag = false;
                if (c.hitColor.HasValue) hitFlag = c.gameObject.transform.position.Distance(PlayerControl.LocalPlayer.transform.position) < c.size;


                switch (c.phase)
                {
                    case 0:
                        float s = c.gameObject.transform.localScale.x;
                        s += (c.size - s) * Time.deltaTime * 1.5f;
                        c.gameObject.transform.localScale = new Vector3(s, s, 1f);
                        float p = 1 - (s / c.size);
                        p *= p;
                        p = 1f - p;
                        c.renderer.color = (hitFlag ? c.hitColor!.Value : c.color).AlphaMultiplied(p);
                        break;
                    case 1:
                        float alpha = c.renderer.color.a;
                        alpha -= Time.deltaTime * 1.5f;
                        c.renderer.color = (hitFlag ? c.hitColor!.Value : c.color).AlphaMultiplied(alpha);
                        if (alpha < 0f)
                        {
                            GameObject.Destroy(c.gameObject);
                            return true;
                        }
                        break;
                }
                return false;
            });
        }
    }
}
