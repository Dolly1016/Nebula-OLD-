using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Map.Editors
{
    class SkeldEditor : MapEditor
    {
        public SkeldEditor() : base(0)
        {
        }

        public override void AddVents()
        {
            CreateVent(SystemTypes.Cafeteria,"CafeUpperVent", new UnityEngine.Vector2(-2.1f, 3.8f));
            CreateVent(SystemTypes.Storage,"StorageVent", new UnityEngine.Vector2(0.45f, -3.6f));
        }
    }
}
