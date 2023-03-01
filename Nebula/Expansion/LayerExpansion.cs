namespace Nebula;

static public class LayerExpansion
{
    static int? defaultLayer = null;
    static int? shortObjectsLayer = null;
    static int? objectsLayer = null;
    static int? uiLayer = null;
    static int? shipLayer = null;
    static int? shadowLayer = null;

    static public int GetDefaultLayer()
    {
        if (defaultLayer == null) defaultLayer = LayerMask.NameToLayer("Default");
        return defaultLayer.Value;
    }

    static public int GetShortObjectsLayer()
    {
        if (shortObjectsLayer == null) shortObjectsLayer = LayerMask.NameToLayer("ShortObjects");
        return shortObjectsLayer.Value;
    }

    static public int GetObjectsLayer()
    {
        if (objectsLayer == null) objectsLayer = LayerMask.NameToLayer("Objects");
        return objectsLayer.Value;
    }

    static public int GetUILayer()
    {
        if (uiLayer == null) uiLayer = LayerMask.NameToLayer("UI");
        return uiLayer.Value;
    }

    static public int GetShipLayer()
    {
        if (shipLayer == null) shipLayer = LayerMask.NameToLayer("Ship");
        return shipLayer.Value;
    }

    static public int GetShadowLayer()
    {
        if (shadowLayer == null) shadowLayer = LayerMask.NameToLayer("Shadow");
        return shadowLayer.Value;
    }

    static public int GetShadowObjectsLayer()
    {
        return 30;
    }

}
