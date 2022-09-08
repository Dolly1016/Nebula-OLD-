using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula
{
    static public class LayerExpansion
    {
        static int? defaultLayer=null;
        static int? shortObjectsLayer = null;
        static int? objectsLayer = null;

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
    }
}
