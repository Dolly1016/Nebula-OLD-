namespace Nebula.Objects.ObjectTypes
{
    public class Decoy : TypeWithImage
    {
        public Decoy() : base(8, "Decoy", "Nebula.Resources.Decoy.png")
        {

        }

        public override void Initialize(CustomObject obj)
        {
            base.Initialize(obj);

            obj.Renderer.flipX = Helpers.playerById(obj.OwnerId).cosmetics.FlipX;
        }

        public override CustomObject.ObjectOrder GetObjectOrder(CustomObject? obj)
        {
            return CustomObject.ObjectOrder.IsOnSameRow;
        }

        public override void Update(CustomObject obj)
        {
            base.Update(obj);
            FixZPosition(obj);
        }

        public override bool RequireMonoBehaviour => true;
    }
}
