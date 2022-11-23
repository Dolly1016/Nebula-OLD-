namespace Nebula.Utilities
{
    public class SpriteLoader
    {
        string address;
        float pixelsPerUnit;
        Sprite sprite;

        public SpriteLoader(string address, float pixelsPerUnit)
        {
            this.address = address;
            this.pixelsPerUnit = pixelsPerUnit;
        }

        public SpriteLoader(Sprite sprite)
        {
            this.sprite = sprite;
        }

        public Sprite GetSprite()
        {
            if (!sprite) sprite = Helpers.loadSpriteFromResources(address,pixelsPerUnit);
            return sprite;
        }
    }
}
