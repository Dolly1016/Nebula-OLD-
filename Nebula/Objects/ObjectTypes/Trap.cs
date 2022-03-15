using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Objects.ObjectTypes
{
    public class Trap : TypeWithImage
    {
        public Trap(byte id,string objectName, string spriteAddress) : base(id,objectName, spriteAddress, true)
        {
        }
    }

    public class VisibleTrap : Trap
    {
        public VisibleTrap(byte id,string objectName, string spriteAddress) : base(id,objectName, spriteAddress)
        {
        }

        public override void Update(CustomObject obj)
        {
            if (obj.PassedMeetings == 0)
            {
                //Trapperを考慮に入れる
                Game.GameData.data.EstimationAI.DetermineMultiply(new Roles.Role[] { Roles.Roles.NiceTrapper, Roles.Roles.EvilTrapper });

                if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId || Game.GameData.data.myData.CanSeeEveryoneInfo)
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

    public class InvisibleTrap : Trap
    {
        public InvisibleTrap(byte id,string objectName, string spriteAddress) : base(id,objectName, spriteAddress)
        {
        }

        public override void Update(CustomObject obj)
        {
            if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId && !Game.GameData.data.myData.CanSeeEveryoneInfo) obj.GameObject.active = false;
            if (obj.PassedMeetings == 0)
            {
                if (obj.Renderer.color.a != 0.5f) obj.Renderer.color = new Color(1f, 1f, 1f, 0.5f);
            }
            else if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
        }
    }
}
