using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Objects.ObjectTypes
{
    class SniperRifle : TypeWithImage
    {
        public static SniperRifle Rifle = new SniperRifle();

        public SniperRifle() : base("SniperRifle", "Nebula.Resources.SniperRifle.png",false)
        {

        }

        public override void Update(CustomObject obj)
        {
            var player = Game.GameData.data.players[obj.OwnerId];
            var targetPosition = Helpers.playerById(obj.OwnerId).transform.position + new Vector3(0.8f * (float)Math.Cos(player.MouseAngle), 0.8f * (float)Math.Sin(player.MouseAngle));
            obj.GameObject.transform.position += (targetPosition - obj.GameObject.transform.position) * 0.4f;
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
}
