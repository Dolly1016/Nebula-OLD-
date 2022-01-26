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
            Dictionary<string, Game.VentData> ventMap = Game.GameData.data.VentMap;

            CreateVent("CafeUpperVent", new UnityEngine.Vector3(-2.3f, 5.6f));
            CreateVent("StorageVent", new UnityEngine.Vector3(-1f, -17.2f));
        }
    }
}
