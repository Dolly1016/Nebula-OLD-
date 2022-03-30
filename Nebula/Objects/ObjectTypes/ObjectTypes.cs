using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Objects.ObjectTypes
{
    public class TypeWithImage : CustomObject.Type
    {

        private Sprite Sprite;
        private string SpriteAddress;
        public Sprite GetSprite()
        {
            if (Sprite) return Sprite;
            Sprite = Helpers.loadSpriteFromResources(SpriteAddress, 150f);
            return Sprite;
        }

        public TypeWithImage(byte id,string objectName, string spriteAddress, bool isBack) : base(id,objectName, isBack)
        {
            SpriteAddress = spriteAddress;
        }

        public override void Initialize(CustomObject obj)
        {
            obj.Renderer.sprite = GetSprite();
        }
    }
}
