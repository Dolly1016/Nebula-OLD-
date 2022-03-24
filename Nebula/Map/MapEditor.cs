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

        public static void AddWirings(int mapId)
        {
            if (!CustomOptionHolder.additionalWirings.getBool()) return;
            if (!MapEditors.ContainsKey(mapId)) return;

            MapEditors[mapId].AddWirings();
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
            vent.gameObject.name = ventName;

            Game.GameData.data.VentMap[ventName] = new Game.VentData(vent);

            return vent;
        }

        protected static Console ActivateWiring(string consoleName, int consoleId)
        {
            Console console = ActivateConsole(consoleName);

            if (!console.TaskTypes.Contains(TaskTypes.FixWiring))
            {
                var list=console.TaskTypes.ToList();
                list.Add(TaskTypes.FixWiring);
                console.TaskTypes = list.ToArray();
            }
            console.ConsoleId = consoleId;
            return console;
        }

        protected static Console ActivateConsole(string objectName)
        {
            GameObject obj = UnityEngine.GameObject.Find(objectName);
            obj.layer = LayerMask.NameToLayer("ShortObjects");
            Console console = obj.GetComponent<Console>();
            PassiveButton button = obj.GetComponent<PassiveButton>();
            CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
            if (!console)
            {
                console = obj.AddComponent<Console>();
                console.checkWalls = true;
                console.usableDistance = 0.7f;
                console.TaskTypes = new TaskTypes[0];
                console.ValidTasks = new UnhollowerBaseLib.Il2CppReferenceArray<TaskSet>(0);
                var list = ShipStatus.Instance.AllConsoles.ToList();
                list.Add(console);
                ShipStatus.Instance.AllConsoles = new UnhollowerBaseLib.Il2CppReferenceArray<Console>(list.ToArray());
            }
            if (console.Image == null)
            {
                console.Image = obj.GetComponent<SpriteRenderer>();
                console.Image.material = new Material(ShipStatus.Instance.AllConsoles[0].Image.material);
            }
            if (!button)
            {
                button = obj.AddComponent<PassiveButton>();
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
                    console.Use();
                }));
            }
            if (!collider)
            {
                collider = obj.AddComponent<CircleCollider2D>();
                collider.radius = 0.4f;
                collider.isTrigger = true;
            }
            if(!PlayerControl.LocalPlayer.cache.ContainsKey(collider))
                PlayerControl.LocalPlayer.cache.Add(collider, new UnhollowerBaseLib.Il2CppReferenceArray<IUsable>(new IUsable[1] { console.gameObject.GetComponent<IUsable>() }));
            return console;
        }

        /// <summary>
        /// マップにベントを追加します。
        /// </summary>
        public virtual void AddVents() { }

        /// <summary>
        /// マップに新たな配線タスクを追加します
        /// </summary>
        public virtual void AddWirings() { }

        public MapEditor(int mapId)
        {
            MapId = mapId;
            MapEditors[mapId] = this;
        }
    }
}
