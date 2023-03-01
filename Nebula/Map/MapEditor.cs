using Nebula.Expansion;

namespace Nebula.Map;
public class MapEditor
{
    //Skeld=0,MIRA=1,Polus=2,AirShip=4
    public int MapId { get; }

    public static Dictionary<int, MapEditor> MapEditors = new Dictionary<int, MapEditor>();

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

    public static void FixTasks(int mapId)
    {
        if (!MapEditors.ContainsKey(mapId)) return;

        MapEditors[mapId].FixTasks();
    }

    public static void OptimizeMap(int mapId)
    {
        if (!CustomOptionHolder.mapOptions.getBool() || !CustomOptionHolder.optimizedMaps.getBool()) return;
        if (!MapEditors.ContainsKey(mapId)) return;

        MapEditors[mapId].OptimizeMap();
    }

    public static void MapCustomize(int mapId)
    {
        if (CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.quietVentsInTheShadow.getBool())
        {
            //ベントを見えなくする
            foreach (var vent in ShipStatus.Instance.AllVents)
            {
                GameObject shadowObj = new GameObject("ShadowVent");
                shadowObj.transform.SetParent(vent.transform);
                shadowObj.transform.localPosition = new Vector3(0f, 0f, 0f);
                shadowObj.transform.localScale = new Vector3(1f, 1f, 1f);
                shadowObj.AddComponent<SpriteRenderer>().sprite = vent.GetComponent<SpriteRenderer>().sprite;
                shadowObj.layer = LayerExpansion.GetShadowLayer();

                vent.gameObject.layer = LayerExpansion.GetDefaultLayer();
            }
        }

        if (!MapEditors.ContainsKey(mapId)) return;

        MapEditors[mapId].MapCustomize();
    }

    public static void ModifySabotage(int mapId)
    {
        if (!MapEditors.ContainsKey(mapId)) return;

        MapEditors[mapId].ModifySabotage();
    }

    public static void ModifyMap(int mapId)
    {
        /*
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            door.gameObject.layer = LayerExpansion.GetObjectsLayer();
            if (door.transform.childCount > 0 && door.transform.GetChild(0).name == "Shadow")
                door.transform.GetChild(0).gameObject.layer = LayerExpansion.GetShadowLayer();
        }
        */

        if (!MapEditors.ContainsKey(mapId)) return;

        MapEditors[mapId].ModifyMap();
    }

    protected static Vent CreateVent(SystemTypes room, string ventName, Vector2 position)
    {
        var referenceVent = UnityEngine.Object.FindObjectOfType<Vent>();
        Vent vent = UnityEngine.Object.Instantiate<Vent>(referenceVent, ShipStatus.Instance.FastRooms[room].transform);
        vent.transform.localPosition = new Vector3(position.x, position.y, -1);
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
        var console = vent.GetComponent<VentCleaningConsole>();
        console.Room = room;
        console.ConsoleId = ShipStatus.Instance.AllVents.Length;

        var allConsolesList = ShipStatus.Instance.AllConsoles.ToList();
        allConsolesList.Add(console);
        ShipStatus.Instance.AllConsoles = allConsolesList.ToArray();

        Game.GameData.data.VentMap[ventName] = new Game.VentData(vent);

        return vent;
    }

    protected static Console ActivateWiring(string consoleName, int consoleId)
    {
        Console console = ActivateConsole(consoleName);

        if (!console.TaskTypes.Contains(TaskTypes.FixWiring))
        {
            var list = console.TaskTypes.ToList();
            list.Add(TaskTypes.FixWiring);
            console.TaskTypes = list.ToArray();
        }
        console.ConsoleId = consoleId;
        return console;
    }

    public static Console CreateConsole(SystemTypes room, string objectName, Sprite sprite, Vector2 pos, float z)
    {
        if (!ShipStatus.Instance.FastRooms.ContainsKey(room)) return null;
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(ShipStatus.Instance.FastRooms[room].transform);
        obj.transform.localPosition = (Vector3)pos - new Vector3(0, 0, z);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        Console c = ActivateConsole(obj);
        c.Room = room;
        return c;
    }

    public static Console CreateConsoleG(SystemTypes room, string objectName, Sprite sprite, Vector2 globalPos)
    {
        if (!ShipStatus.Instance.FastRooms.ContainsKey(room)) return null;
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(ShipStatus.Instance.FastRooms[room].transform);
        obj.transform.position = (Vector3)globalPos + new Vector3(0, 0, ShipStatus.Instance.FastRooms[room].transform.position.z - 1f);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        Console c = ActivateConsole(obj);
        c.Room = room;
        return c;
    }


    protected static Console ActivateConsole(string objectName)
    {
        GameObject obj = UnityEngine.GameObject.Find(objectName);
        return ActivateConsole(obj);
    }

    protected static Console ActivateConsole(GameObject obj)
    {
        return ConsoleExpansion.Consolize<Console>(obj);
    }

    protected static void EditConsole(SystemTypes room, string objectName, Action<Console> action)
    {
        if (!ShipStatus.Instance.FastRooms.ContainsKey(room)) return;
        PlainShipRoom shipRoom = ShipStatus.Instance.FastRooms[room];
        Transform transform = shipRoom.transform.FindChild(objectName);
        if (!transform) return;
        GameObject obj = transform.gameObject;
        if (!obj) return;

        Console c = obj.GetComponent<Console>();
        if (c) action.Invoke(c);
    }

    /// <summary>
    /// マップにベントを追加します。
    /// </summary>
    public virtual void AddVents() { }

    /// <summary>
    /// マップに新たな配線タスクを追加します
    /// </summary>
    public virtual void AddWirings() { }

    public virtual void FixTasks() { }

    public virtual void ModifySabotage() { }

    /// <summary>
    /// マップを最適化します。
    /// </summary>
    public virtual void OptimizeMap() { }

    /// <summary>
    /// 個別の設定をこの中で行います。
    /// </summary>
    public virtual void MapCustomize() { }

    public virtual void ModifyMap() { }

    public virtual void MinimapOptimizeForJailer(Transform romeNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay) { }
    public MapEditor(int mapId)
    {
        MapId = mapId;
        MapEditors[mapId] = this;
    }
}