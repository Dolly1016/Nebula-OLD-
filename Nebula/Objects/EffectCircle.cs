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
            allCircles.Clear();
        }

        static private SpriteLoader circleSprite = new SpriteLoader("Nebula.Resources.EffectCircle.png", 250f);

        float timer;
        GameObject gameObject;
        SpriteRenderer renderer;
        float size;
        int phase;
        Color color;

        public EffectCircle(GameObject obj, Color color,float size,float duration = -1f)
        {
            gameObject = new GameObject("Circle");
            gameObject.transform.SetParent(obj.transform);
            gameObject.transform.localPosition=new Vector3(0,0,-50f);
            gameObject.transform.localScale = new Vector3(0,0,1f);
            renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite=circleSprite.GetSprite();
            renderer.color=color.AlphaMultiplied(0f);
            phase = 0;
            this.size = size;
            this.color = color;
            this.timer = duration;
            allCircles.Add(this);

        }

        public static void Update()
        {
            allCircles.RemoveWhere((c) => {
                if (c.timer > 0f)
                {
                    c.timer -= Time.deltaTime;
                    if (!(c.timer > 0f))
                    {
                        c.phase = 1;
                    }
                }

                switch (c.phase)
                {
                    case 0:
                        float s = c.gameObject.transform.localScale.x;
                        s += (c.size - s) * Time.deltaTime * 1.5f;
                        c.gameObject.transform.localScale = new Vector3(s, s, 1f);
                        float p = 1-(s / c.size);
                        p *= p;
                        p = 1f - p;
                        c.renderer.color = c.color.AlphaMultiplied(p);
                        break;
                    case 1:
                        float alpha = c.renderer.color.a;
                        alpha -= Time.deltaTime * 1.5f;
                        c.renderer.color = new Color(c.renderer.color.r, c.renderer.color.g, c.renderer.color.b, alpha);
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
