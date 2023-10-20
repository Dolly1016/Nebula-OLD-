using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Extensions;

public static class ShipExtension
{
    public static void PatchModification(byte mapId)
    {
        switch (mapId)
        {
            case 0:
                ModifySkeld();
                break;
            case 1:
                ModifyMira();
                break;
            case 2:
                ModifyPolus();
                break;
            case 4:
                ModifyAirship();
                break;
        }
    }

    private static void ModifySkeld()
    {
        if (GeneralConfigurations.SkeldCafeVentOption) CreateVent(SystemTypes.Cafeteria, "CafeUpperVent", new UnityEngine.Vector2(-2.1f, 3.8f));
        if (GeneralConfigurations.SkeldStorageVentOption) CreateVent(SystemTypes.Storage, "StorageVent", new UnityEngine.Vector2(0.45f, -3.6f));

        if (!GeneralConfigurations.SkeldAdminOption)
        {
            var obj = ShipStatus.Instance.FastRooms[SystemTypes.Admin].gameObject.transform.parent.GetChild(0).GetChild(3).gameObject;
            GameObject.Destroy(obj.transform.GetChild(1).GetComponent<CircleCollider2D>());
        }
    }

    private static void ModifyMira()
    {
        if (!GeneralConfigurations.MiraAdminOption)
        {
            var obj = ShipStatus.Instance.FastRooms[SystemTypes.Admin].gameObject.transform.FindChild("MapTable").gameObject;
            GameObject.Destroy(obj.transform.GetChild(0).gameObject);
        }
    }

    private static void ModifyPolus()
    {
        if (GeneralConfigurations.PolusSpecimenVentOption) CreateVent(SystemTypes.Specimens, "SpecimenVent", new UnityEngine.Vector2(-1f, -1.35f));

        if (!GeneralConfigurations.PolusAdminOption)
        {
            var obj = ShipStatus.Instance.FastRooms[SystemTypes.Admin].gameObject.transform.FindChild("mapTable").gameObject;
            GameObject.Destroy(obj.transform.GetChild(0).GetComponent<BoxCollider2D>());
            GameObject.Destroy(obj.transform.GetChild(1).GetComponent<BoxCollider2D>());
            GameObject.Destroy(obj.transform.GetChild(2).gameObject);
        }
    }

    private static SpriteLoader medicalWiringSprite = SpriteLoader.FromResource("Nebula.Resources.AirshipWiringM.png",100f);
    private static void ModifyAirship()
    {
        //宿舎下ダウンロード
        EditConsole(SystemTypes.Engine, "panel_data", (c) =>
        {
            c.checkWalls = true;
            c.usableDistance = 0.9f;
        });

        //写真現像タスク
        EditConsole(SystemTypes.MainHall, "task_developphotos", (c) => c.checkWalls = true);

        //シャワータスク
        EditConsole(SystemTypes.Showers, "task_shower", (c) => c.checkWalls = true);

        //ラウンジゴミ箱タスク
        EditConsole(SystemTypes.Lounge, "task_garbage5", (c) => c.checkWalls = true);

        if (GeneralConfigurations.AirshipMeetingVentOption) CreateVent(SystemTypes.MeetingRoom, "MeetingVent", new Vector2(-3.1f, -1.6f)).transform.localPosition += new Vector3(0, 0, 2);
        if (GeneralConfigurations.AirshipElectricalVentOption) CreateVent(SystemTypes.Electrical, "ElectricalVent", new Vector2(-0.275f, -1.7f)).transform.localPosition += new Vector3(0, 0, 1);

        ActivateWiring("task_wiresHallway2", 2);
        if (GeneralConfigurations.AirshipArmoryWireOption) ActivateWiring("task_electricalside2", 3).Room = SystemTypes.Armory;
        ActivateWiring("task_wireShower", 4);
        ActivateWiring("taks_wiresLounge", 5);
        if (GeneralConfigurations.AirshipMedicalWireOption)
        {
            CreateConsole(SystemTypes.Medical, "task_wireMedical", medicalWiringSprite.GetSprite(), new Vector2(-0.84f, 5.63f), 0f);
            ActivateWiring("task_wireMedical", 6).Room = SystemTypes.Medical;
        }
        if(GeneralConfigurations.AirshipHallwayWireOption)ActivateWiring("panel_wireHallwayL", 7);
        ActivateWiring("task_wiresStorage", 8);
        if (GeneralConfigurations.AirshipVaultWireOption) ActivateWiring("task_electricalSide", 9).Room = SystemTypes.VaultRoom;
        ActivateWiring("task_wiresMeeting", 10);

        if (GeneralConfigurations.AirshipOneWayMeetingRoomOption) ModifyMeetingRoom();

        if (!GeneralConfigurations.AirshipCockpitAdminOption)
        {
            var obj = ShipStatus.Instance.FastRooms[SystemTypes.Cockpit].gameObject;
            GameObject.Destroy(obj.transform.FindChild("cockpit_mapfloating").gameObject);
            GameObject.Destroy(obj.transform.FindChild("panel_cockpit_map").GetComponent<BoxCollider2D>());
        }
        if (!GeneralConfigurations.AirshipRecordAdminOption)
        {
            var obj = ShipStatus.Instance.FastRooms[SystemTypes.Records].gameObject;
            GameObject.Destroy(obj.transform.FindChild("records_admin_map").gameObject);
        }
    }









   
    private static Vent CreateVent(SystemTypes room, string ventName, Vector2 position)
    {
        var referenceVent = ShipStatus.Instance.AllVents[0];
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

        return vent;
    }

    private static void EditConsole(SystemTypes room, string objectName, Action<Console> action)
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

    private static Console ActivateWiring(string consoleName, int consoleId)
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

    private static Console CreateConsole(SystemTypes room, string objectName, Sprite sprite, Vector2 pos, float z)
    {
        if (!ShipStatus.Instance.FastRooms.ContainsKey(room)) return null!;
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(ShipStatus.Instance.FastRooms[room].transform);
        obj.transform.localPosition = (Vector3)pos - new Vector3(0, 0, z);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        Console c = Consolize<Console>(obj);
        c.Room = room;
        return c;
    }

    private static Console ActivateConsole(string objectName)
    {
        GameObject obj = UnityEngine.GameObject.Find(objectName);
        return Consolize<Console>(obj);
    }

    private static Material? highlightMaterial = null;
    private static Material GetHighlightMaterial()
    {
        if (highlightMaterial != null) return new Material(highlightMaterial);
        foreach (var mat in UnityEngine.Resources.FindObjectsOfTypeAll(Il2CppType.Of<Material>()))
        {
            if (mat.name == "HighlightMat")
            {
                highlightMaterial = mat.TryCast<Material>();
                break;
            }
        }
        return new Material(highlightMaterial);
    }

    private static Console Consolize<C>(GameObject obj, SpriteRenderer? renderer = null) where C : Console
    {
        obj.layer = LayerMask.NameToLayer("ShortObjects");
        Console console = obj.GetComponent<Console>();
        PassiveButton button = obj.GetComponent<PassiveButton>();
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (!console)
        {
            console = obj.AddComponent<C>();
            console.checkWalls = true;
            console.usableDistance = 0.7f;
            console.TaskTypes = new TaskTypes[0];
            console.ValidTasks = new Il2CppReferenceArray<TaskSet>(0);
            var list = ShipStatus.Instance.AllConsoles.ToList();
            list.Add(console);
            ShipStatus.Instance.AllConsoles = new Il2CppReferenceArray<Console>(list.ToArray());
        }
        if (console.Image == null)
        {
            if (renderer != null)
            {
                console.Image = renderer;
            }
            else
            {
                console.Image = obj.GetComponent<SpriteRenderer>();
                console.Image.material = GetHighlightMaterial();
            }
        }
        if (!button)
        {
            button = obj.AddComponent<PassiveButton>();
            button.OnMouseOut = new UnityEngine.Events.UnityEvent();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            button._CachedZ_k__BackingField = 0.1f;
            button.CachedZ = 0.1f;
        }

        if (!collider)
        {
            var cCollider = obj.AddComponent<CircleCollider2D>();
            cCollider.radius = 0.4f;
            cCollider.isTrigger = true;
        }

        return console;
    }

    static private SpriteLoader customMeetingSideSprite = SpriteLoader.FromResource("Nebula.Resources.AirshipCustomMeeting.png",100f);
    static private SpriteLoader customMeetingLadderSprite = SpriteLoader.FromResource("Nebula.Resources.AirshipCustomMeetingLadder.png", 100f);
    static private void ModifyMeetingRoom()
    {
        Transform meetingRoom = ShipStatus.Instance.FastRooms[SystemTypes.MeetingRoom].transform;
        Transform gapRoom = ShipStatus.Instance.FastRooms[SystemTypes.GapRoom].transform;

        float diffX = (meetingRoom.position.x - gapRoom.transform.position.x) / 0.7f;
        float[] shadowX = new float[2] { 0f, 0f };

        //画像を更新する
        GameObject customRendererObj = new GameObject("meeting_custom");
        customRendererObj.transform.SetParent(meetingRoom);
        customRendererObj.transform.localPosition = new Vector3(9.58f, -2.86f, 4.8f);
        customRendererObj.transform.localScale = new Vector3(1f, 1f, 1f);
        customRendererObj.AddComponent<SpriteRenderer>().sprite = customMeetingSideSprite.GetSprite(); ;
        customRendererObj.layer = LayerExpansion.GetShipLayer();

        //はしごを生成
        GameObject originalLadderObj = meetingRoom.FindChild("ladder_meeting").gameObject;
        GameObject ladderObj = GameObject.Instantiate(meetingRoom.FindChild("ladder_meeting").gameObject, meetingRoom);
        ladderObj.name = "ladder_meeting_custom";
        ladderObj.transform.position += new Vector3(10.9f, 0);
        ladderObj.GetComponent<SpriteRenderer>().sprite = customMeetingLadderSprite.GetSprite();

        //MeetingRoomの当たり判定に手を加える
        var collider = meetingRoom.FindChild("Walls").GetComponents<EdgeCollider2D>().Where((c) => c.pointCount == 43).FirstOrDefault();
        Il2CppSystem.Collections.Generic.List<Vector2> colliderPosList = new Il2CppSystem.Collections.Generic.List<Vector2>();
        int index = 0;
        float tempX = 0f;
        float tempY = 0f;

        foreach (var p in collider!.points)
        {
            if (index != 30) colliderPosList.Add(p);
            if (index == 29) tempX = p.x;
            if (index == 30)
            {
                tempX = (tempX + p.x) / 2f;
                colliderPosList.Add(new Vector2(tempX, p.y));
                colliderPosList.Add(new Vector2(tempX, -1.8067f));
                colliderPosList.Add(new Vector2(p.x, -1.8067f));
            }
            index++;
        }
        collider.SetPoints(colliderPosList);

        //MeetingRoomの影に手を加える
        collider = meetingRoom.FindChild("Shadows").GetComponents<EdgeCollider2D>().Where((c) => c.pointCount == 46).FirstOrDefault();

        colliderPosList = new Il2CppSystem.Collections.Generic.List<Vector2>();
        index = 0;
        while (index <= 40)
        {
            colliderPosList.Add(collider!.points[index]);
            index++;
        }

        shadowX[0] = collider!.points[41].x;
        shadowX[1] = tempX = (collider.points[40].x + collider.points[41].x) / 2f;
        tempY = (collider.points[40].y + collider.points[41].y) / 2f;
        colliderPosList.Add(new Vector2(tempX, tempY));
        colliderPosList.Add(new Vector2(tempX, tempY - 2.56f));
        var newCollider = meetingRoom.FindChild("Shadows").gameObject.AddComponent<EdgeCollider2D>();
        newCollider.SetPoints(colliderPosList);

        colliderPosList = new Il2CppSystem.Collections.Generic.List<Vector2>();
        index = 41;
        while (index <= 45)
        {
            if (index == 41) colliderPosList.Add(collider.points[41] - new Vector2(0, 2.56f));
            colliderPosList.Add(collider.points[index]);
            index++;
        }
        tempX = collider.points[41].x;
        collider.SetPoints(colliderPosList);

        //GapRoomの影に手を加える
        collider = gapRoom.FindChild("Shadow").GetComponents<EdgeCollider2D>().Where(x => Math.Abs(x.points[0].x + 6.2984f) < 0.1).FirstOrDefault();
        colliderPosList = new Il2CppSystem.Collections.Generic.List<Vector2>();
        index = 0;
        while (index <= 1)
        {
            colliderPosList.Add(collider!.points[index]);
            index++;
        }
        colliderPosList.Add(new Vector2(shadowX[0] + diffX, collider!.points[1].y));
        newCollider = gapRoom.FindChild("Shadow").gameObject.AddComponent<EdgeCollider2D>();
        newCollider.SetPoints(colliderPosList);
        colliderPosList = new Il2CppSystem.Collections.Generic.List<Vector2>();
        index = 2;
        colliderPosList.Add(new Vector2(shadowX[1] + diffX, collider.points[1].y));
        while (index <= 4)
        {
            colliderPosList.Add(collider.points[index]);
            index++;
        }
        collider.SetPoints(colliderPosList);

        AirshipStatus airship = ShipStatus.Instance.Cast<AirshipStatus>();
        airship.Ladders = new Il2CppReferenceArray<Ladder>(airship.GetComponentsInChildren<Ladder>());

        originalLadderObj.transform.GetChild(0).gameObject.SetActive(false);
        originalLadderObj.transform.GetChild(1).gameObject.SetActive(false);
        ladderObj.transform.GetChild(2).gameObject.SetActive(false);
        ladderObj.transform.GetChild(3).gameObject.SetActive(false);


        //MovingPlatformを無効化する
        airship.GapPlatform.SetSide(true);
        airship.outOfOrderPlat.SetActive(true);
        airship.GapPlatform.transform.localPosition = airship.GapPlatform.DisabledPosition;
    }
}
