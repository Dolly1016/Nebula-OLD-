using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Player;

public class AttributeShower
{
    class AttributeIcon
    {
        private static ISpriteLoader guageSprite = SpriteLoader.FromResource("Nebula.Resources.AttributeGuage.png", 100f);
        private static IDividedSpriteLoader imageSprites = XOnlyDividedSpriteLoader.FromResource("Nebula.Resources.AttributeIcon.png", 100f, 33, true);

        public AttributeModulator.PlayerAttribute Attribute { get; private set; }
        public Transform MyTransform { get; private set; }

        private SpriteRenderer guageRenderer;
        public void Update(float ratio) {
            guageRenderer.material.SetFloat("_Guage", Mathf.Clamp01(ratio));
        }

        public AttributeIcon(Transform parent,AttributeModulator.PlayerAttribute attribute)
        {
            MyTransform = UnityHelper.CreateObject("AttrIcon", parent, Vector3.zero).transform;
            var renderer = MyTransform.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = imageSprites.GetSprite(0);

            var iconRenderer = UnityHelper.CreateObject<SpriteRenderer>("AttrIconTop", MyTransform, new Vector3(0f, 0f, -0.1f));
            iconRenderer.sprite = imageSprites.GetSprite((int)attribute + 2);

            guageRenderer = UnityHelper.CreateObject<SpriteRenderer>("Guage", MyTransform, new Vector3(0f, 0f, -0.2f));
            guageRenderer.sprite = guageSprite.GetSprite();
            guageRenderer.material.shader = NebulaAsset.GuageShader;
            guageRenderer.material.SetFloat("_Guage", 1f);

            Attribute = attribute;
        }

        public void Destroy() => GameObject.Destroy(MyTransform.gameObject);
    }

    private List<AttributeIcon> allIcons = new();
    private Transform myTransform;

    public void Update(PlayerModInfo player)
    {
        var attributes = player.GetValidAttributes().ToArray();
        allIcons.RemoveAll(icon =>
        {
            if (!attributes.Any(a => a.attribute == icon.Attribute))
            {
                icon.Destroy();
                return true;
            }
            return false;
        });

        foreach(var attr in attributes)
        {
            var icon = allIcons.FirstOrDefault(icon => icon.Attribute == attr.attribute);
            if (icon == null)
            {
                icon = new AttributeIcon(myTransform, attr.attribute);
                allIcons.Add(icon);
            }
            icon.Update(attr.percentage);
        }

        float y = 0.35f + (float)(allIcons.Count - 1) * 0.2f;
        for(int i = 0; i < allIcons.Count; i++)
        {
            allIcons[i].MyTransform.localPosition = new Vector3(0f, y - (float)i * 0.4f, 0f);
        }
    }

    public AttributeShower()
    {
        myTransform = UnityHelper.CreateObject("AttributeShower", HudManager.Instance.transform, new Vector3(5.05f, 0f, -400f)).transform;
    }
}
