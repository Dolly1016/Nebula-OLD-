using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Objects.ObjectTypes
{
    public class Trap : DelayedObject
    {
        public Trap(byte id,string objectName, string spriteAddress) : base(id,objectName, spriteAddress)
        {
            canSeeInShadow = true;
        }
    }

    public class VisibleTrap : Trap
    {
        public VisibleTrap(byte id,string objectName, string spriteAddress) : base(id,objectName, spriteAddress)
        {
        }

        public override void Initialize(CustomObject obj)
        {
            base.Initialize(obj);
            Events.Schedule.RegisterPreMeetingAction(() =>
            {
                //Trapperを考慮に入れる
                Game.GameData.data.EstimationAI.DetermineMultiply(new Roles.Role[] { Roles.Roles.NiceTrapper, Roles.Roles.EvilTrapper });
            },1);
        }
    }

    public class InvisibleTrap : Trap
    {
        public InvisibleTrap(byte id,string objectName, string spriteAddress) : base(id,objectName, spriteAddress)
        {
        }

        protected override bool canSeeOnlyMe { get { return true; } }

    }

    public class KillTrap : InvisibleTrap
    {
        private Sprite BrokenSprite;

        private Sprite GetBrokenSprite()
        {
            if (BrokenSprite) return BrokenSprite;
            BrokenSprite = Helpers.loadSpriteFromResources("Nebula.Resources.KillTrapBroken.png", 150f);
            return BrokenSprite;
        }

        public KillTrap(byte id, string objectName, string spriteAddress) : base(id, objectName, spriteAddress)
        {
        }

        public override void Initialize(CustomObject obj)
        {
            base.Initialize(obj);

            obj.Data = new int[1];
            obj.Data[0] = 0;
        }

        public override void Update(CustomObject obj, int command) {
            obj.Renderer.sprite = GetBrokenSprite();
            obj.Data[0] = 1;
        }

        public override void Update(CustomObject obj)
        {
            if (obj.Data[0] == 1)
            {
                obj.GameObject.active = true;
                if (obj.Renderer.color.a < 1f) obj.Renderer.color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                base.Update(obj);
            }
        }
    }
}
