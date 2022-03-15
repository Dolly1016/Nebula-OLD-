using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Module
{
    public class TimeLimit
    {
        SpriteRenderer[]? Renderers;
        static Sprite[] Sprites=null;
        static Texture2D Texture = null;
        int Timer;
        float Rate;
        Color Color;

        private void SetPos(SpriteRenderer[] renderers,float y)
        {
            renderers[0].transform.localPosition = new Vector3(-0.28f, y, -400f);
            renderers[1].transform.localPosition = new Vector3(-0.06f, y, -400f);
            renderers[2].transform.localPosition = new Vector3(0.0f, y, -400f);
            renderers[3].transform.localPosition = new Vector3(0.25f, y, -400f);
            renderers[4].transform.localPosition = new Vector3(0.47f, y, -400f);
            renderers[5].transform.localPosition = new Vector3(0.59f, y, -400f);
        }

        public TimeLimit(HudManager __instance)
        {
            Texture = Helpers.loadTextureFromResources("Nebula.Resources.Timer.png");

            Sprites = new Sprite[12];
            for (int i = 0; i < 12; i++)
            {
                Sprites[i] = Helpers.loadSpriteFromResources(Texture, 100f, new Rect(0, -i * 40, 27, -40));
            }


            Timer = 0;

            Renderers = new SpriteRenderer[6];
            for (int i = 0; i < Renderers.Length; i++)
            {
                Renderers[i] = new UnityEngine.GameObject("Timer" + i).AddComponent<SpriteRenderer>();
                Renderers[i].transform.SetParent(__instance.transform);
                Renderers[i].gameObject.layer = UnityEngine.LayerMask.NameToLayer("UI");

                Renderers[i].sortingOrder = 0;
            }

            SetPos(Renderers,2.7f);

            Renderers[2].sprite = Sprites[10];
            Renderers[5].sprite = Sprites[11];

            Rate = 1f;
            Color = Color.white;
        }

        public void Destroy()
        {
            foreach(var Renderer in Renderers)
            {
                UnityEngine.Object.Destroy(Renderer);
            }
        }

        private void RenderersUpdate(SpriteRenderer[] renderers,float y)
        {
            renderers[4].sprite = Sprites[Timer % 10];
            renderers[3].sprite = Sprites[(Timer % 60) / 10];
            renderers[1].sprite = Sprites[(Timer / 60) % 10];
            renderers[0].sprite = Sprites[(Timer / 600) % 10];

            foreach (var renderer in renderers)
            {
                renderer.transform.localScale = new Vector3(Rate, Rate);
                renderer.color = Color;

                SetPos(renderers,y);
            }

            //一番上の桁の表示・非表示
            if ((Timer / 600) % 10 == 0)
            {
                renderers[0].enabled = false;
            }
            else
            {
                renderers[0].enabled = true;
            }
        }

        public void Update()
        {
            bool ChangeFlag = false;
            if (Timer != (int)Game.GameData.data.Timer)
            {
                ChangeFlag = true;
            }
            Timer = (int)Game.GameData.data.Timer;

            if (ChangeFlag)
            {
                if ((Timer % 60 == 0 && Timer <= 600) || (Timer % 10 == 0 && Timer <= 60) || Timer < 10)
                    Rate = 1.3f;

                if (Timer % 60 == 0)
                {
                    RPCEventInvoker.SynchronizeTimer();
                }
            }

            if (Timer < 60)
            {
                Color = new Color(0.7f, 0f, 0f);
            }
            else if (Timer < 180)
            {
                Color = new Color(0.7f + 0.3f * (float)(Timer - 60) / 120f,
                    (float)(Timer - 60) / 120f, (float)(Timer - 60) / 120f);
            }
            else
            {
                Color = Color.white;
            }

            Rate -= (Rate - 1f) * 0.1f;

            RenderersUpdate(Renderers, 2.7f);
        }
    }
}
