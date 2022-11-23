namespace Nebula.Objects.ObjectTypes
{
    public class ElecPole : DelayedObject
    {
        public ElecPole() : base(6, "ElecPole", "Nebula.Resources.ElecPole.png")
        {
            canSeeInShadow = true;
        }

        public override CustomObject.ObjectOrder GetObjectOrder(CustomObject? obj)
        {
            return CustomObject.ObjectOrder.IsOnSameRow;
        }
    }

    public class ElecPoleGuide : TypeWithImage
    {
        public ElecPoleGuide() : base(7, "ElecPoleGuide", "Nebula.Resources.ElecPole.png")
        {
            canSeeInShadow = true;
        }
        public override CustomObject.ObjectOrder GetObjectOrder(CustomObject? obj)
        {
            return CustomObject.ObjectOrder.IsFront;
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
