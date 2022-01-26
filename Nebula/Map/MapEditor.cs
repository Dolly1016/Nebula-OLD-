using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

namespace Nebula.Map
{
    public class MapEditor
    {
        //Skeld=0,MIRA=1,Polus=2,AirShip=4
        public int MapId { get; }

        public static Dictionary<int, MapEditor> MapEditors=new Dictionary<int, MapEditor>();

        public static void Load()
        {
            new Editors.SkeldEditor();
            new Editors.MIRAEditor();
            new Editors.PolusEditor();
            new Editors.AirshipEditor();
        }

        public static void AddVents(int mapId)
        {
            if (!CustomOptionHolder.additionalVents.getBool()) return;
            if (!MapEditors.ContainsKey(mapId)) return;

            MapEditors[mapId].AddVents();
        }


        protected static Vent CreateVent(string ventName,Vector3 position)
        {
            var referenceVent = UnityEngine.Object.FindObjectOfType<Vent>();
            Vent vent = UnityEngine.Object.Instantiate<Vent>(referenceVent);
            vent.transform.position = position;
            vent.transform.position += new Vector3(0f, 0f, ShipStatus.Instance.AllVents[0].transform.position.z);
            vent.Left = null;
            vent.Right = null;
            vent.Center = null;
            vent.Id = ShipStatus.Instance.AllVents.Select(x => x.Id).Max() + 1; // Make sure we have a unique id
            var allVentsList = ShipStatus.Instance.AllVents.ToList();
            allVentsList.Add(vent);
            ShipStatus.Instance.AllVents = allVentsList.ToArray();
            vent.gameObject.SetActive(true);
            vent.name = ventName;

            Game.GameData.data.VentMap[ventName] = new Game.VentData(vent);

            return vent;
        }

        /// <summary>
        /// マップにベントを追加します。
        /// </summary>
        public virtual void AddVents() { }

        public MapEditor(int mapId)
        {
            MapId = mapId;
            MapEditors[mapId] = this;
        }
    }
}
