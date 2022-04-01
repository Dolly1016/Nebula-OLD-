using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Objects.ObjectTypes
{
    public class ElecPole : TypeWithImage
    {
        public ElecPole() : base(5, "ElecPole", "Nebula.Resources.ElecPole.png", false)
        {
            IsBack = IsFront = false;
            CanSeeInShadow = true;
        }

        public override void Update(CustomObject obj)
        {
            CustomObject.Type.VisibleObjectUpdate(obj);
        }
    }

    public class ElecPoleGuide : TypeWithImage
    {
        public ElecPoleGuide() : base(6, "ElecPoleGuide", "Nebula.Resources.ElecPole.png", false)
        {
            IsBack = false;
            IsFront = true;
            CanSeeInShadow = true;
        }

        public override void Initialize(CustomObject obj)
        {
            base.Initialize(obj);

            obj.Data = new int[1];
        }
        public override void Update(CustomObject obj)
        {
            switch (obj.Data[0])
            {
                case 0:
                    obj.Renderer.color = new UnityEngine.Color(0f, 0.7f, 1f, 0.5f);
                    break;
                case 1:
                    obj.Renderer.color = new UnityEngine.Color(0.6f, 0.6f, 0.6f, 0.5f);
                    break;
                case 2:
                    obj.Renderer.color = new UnityEngine.Color(1f, 0f, 0f, 0.5f);
                    break;
            }
        }
    }
}
