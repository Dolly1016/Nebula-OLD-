namespace Nebula.Map.Editors;

public class AirshipEditor : MapEditor
{
    public AirshipEditor() : base(4)
    {
    }

    private Sprite MedicalWiring;
    private Sprite GetMedicalSprite()
    {
        if (MedicalWiring) return MedicalWiring;
        MedicalWiring = Helpers.loadSpriteFromResources("Nebula.Resources.AirshipWiringM.png", 100f);
        return MedicalWiring;
    }

    public override void AddVents()
    {
        CreateVent(SystemTypes.Electrical, "ElectricalVent", new UnityEngine.Vector2(-0.275f, -1.7f)).transform.localPosition += new Vector3(0, 0, 1);
        CreateVent(SystemTypes.MeetingRoom, "MeetingVent", new UnityEngine.Vector2(-3.1f, -1.6f)).transform.localPosition += new Vector3(0, 0, 2);
    }

    public override void AddWirings()
    {
        ActivateWiring("task_wiresHallway2", 2);
        ActivateWiring("task_electricalside2", 3).Room = SystemTypes.Armory;
        ActivateWiring("task_wireShower", 4);
        ActivateWiring("taks_wiresLounge", 5);
        CreateConsole(SystemTypes.Medical, "task_wireMedical", GetMedicalSprite(), new Vector2(-0.84f, 5.63f), 0f);
        ActivateWiring("task_wireMedical", 6).Room = SystemTypes.Medical;
        ActivateWiring("panel_wireHallwayL", 7);
        ActivateWiring("task_wiresStorage", 8);
        ActivateWiring("task_electricalSide", 9).Room = SystemTypes.VaultRoom;
        ActivateWiring("task_wiresMeeting", 10);
    }

    public override void FixTasks()
    {
        //宿舎下ダウンロード
        EditConsole(SystemTypes.Engine, "panel_data", (c) =>
        {
            c.checkWalls = true;
            c.usableDistance = 0.9f;
        });

        //写真現像タスク
        EditConsole(SystemTypes.MainHall, "task_developphotos", (c) =>
        {
            c.checkWalls = true;
        });

        //シャワータスク
        EditConsole(SystemTypes.Showers, "task_shower", (c) =>
        {
            c.checkWalls = true;
        });

        //ラウンジゴミ箱タスク
        EditConsole(SystemTypes.Lounge, "task_garbage5", (c) =>
        {
            c.checkWalls = true;
        });

        if (CustomOptionHolder.TasksOption.getBool() && CustomOptionHolder.DangerousDownloadSpotOption.getBool())
        {
            var obj = ShipStatus.Instance.FastRooms[SystemTypes.GapRoom].gameObject;
            var panel = obj.transform.FindChild("panel_data");
            panel.localPosition = new Vector3(4.52f, -3.95f, 0.1f);
        }
        else if (CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.optimizedMaps.getBool())
        {
            var obj = ShipStatus.Instance.FastRooms[SystemTypes.GapRoom].gameObject;
            var panel = obj.transform.FindChild("panel_data");

            SpriteRenderer renderer;

            GameObject fance = new GameObject("ModFance");
            fance.layer = LayerMask.NameToLayer("Ship");
            fance.transform.SetParent(obj.transform);
            fance.transform.localPosition = new Vector3(4.2f, 0.15f, 0.5f);
            fance.transform.localScale = new Vector3(1f, 1f, 1f);
            fance.SetActive(true);
            var Collider = fance.AddComponent<EdgeCollider2D>();
            Collider.points = new Vector2[] { new Vector2(1.5f, -0.2f), new Vector2(-1.5f, -0.2f), new Vector2(-1.5f, 1.5f) };
            Collider.enabled = true;
            renderer = fance.AddComponent<SpriteRenderer>();
            renderer.sprite = Helpers.loadSpriteFromResources("Nebula.Resources.AirshipFance.png", 100f);

            GameObject pole = new GameObject("DownloadPole");
            pole.layer = LayerMask.NameToLayer("Ship");
            pole.transform.SetParent(obj.transform);
            pole.transform.localPosition = new Vector3(4.1f, 0.75f, 0.8f);
            pole.transform.localScale = new Vector3(1f, 1f, 1f);
            renderer = pole.AddComponent<SpriteRenderer>();
            renderer.sprite = Helpers.loadSpriteFromResources("Nebula.Resources.AirshipDownloadG.png", 100f);

            panel.localPosition = new Vector3(4.1f, 0.72f, 0.1f);
            panel.gameObject.GetComponent<Console>().usableDistance = 0.9f;
        }
    }

    public override void OptimizeMap()
    {
        var obj = ShipStatus.Instance.FastRooms[SystemTypes.GapRoom].gameObject;

        var ledgeShadow = obj.transform.FindChild("Shadow").FindChild("LedgeShadow").GetComponent<OneWayShadows>();
        //インポスターについてのみ影を無効化
        ledgeShadow.IgnoreImpostor = true;
        //上下両方から見えないように
        ledgeShadow.RoomCollider.enabled = false;



    }

    static private SpriteLoader customMeetingSideSprite = new SpriteLoader("Nebula.Resources.AirshipCustomMeeting.png", 100f);
    static private SpriteLoader customMeetingLadderSprite = new SpriteLoader("Nebula.Resources.AirshipCustomMeetingLadder.png", 100f);

    public override void ModifyMap()
    {
        /*
        var elecDoors = ShipStatus.Instance.Systems[SystemTypes.Decontamination].Cast<ElectricalDoors>();
        foreach (var d in elecDoors.Doors)
        {
            //if(d.transform.childCount==0)d.gameObject.layer = LayerExpansion.GetShadowObjectsLayer();
        }
        */
    }

    public override void MapCustomize()
    {
        if (CustomOptionHolder.mapOptions.getBool())
        {
            if (CustomOptionHolder.invalidatePrimaryAdmin.getSelection() > 0)
            {
                var obj = ShipStatus.Instance.FastRooms[SystemTypes.Cockpit].gameObject;
                //第一のアドミンを無効化
                GameObject.Destroy(obj.transform.FindChild("cockpit_mapfloating").gameObject);
                GameObject.Destroy(obj.transform.FindChild("panel_cockpit_map").GetComponent<BoxCollider2D>());
            }
            if (CustomOptionHolder.invalidateSecondaryAdmin.getBool())
            {
                var obj = ShipStatus.Instance.FastRooms[SystemTypes.Records].gameObject;
                //第二のアドミンを無効化
                GameObject.Destroy(obj.transform.FindChild("records_admin_map").gameObject);
            }

            if (CustomOptionHolder.useClassicAdmin.getBool())
            {
                PolygonCollider2D collider;
                List<Vector2> pointsList;


                //Security
                collider = ShipStatus.Instance.FastRooms[SystemTypes.Security].gameObject.GetComponent<PolygonCollider2D>();
                pointsList = new List<Vector2>(collider.points);
                pointsList.RemoveAt(5);
                collider.points = pointsList.ToArray();
            }

            if (CustomOptionHolder.oneWayMeetingRoomOption.getBool())
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

                foreach (var p in collider.points)
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
                    colliderPosList.Add(collider.points[index]);
                    index++;
                }

                shadowX[0] = collider.points[41].x;
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
                    colliderPosList.Add(collider.points[index]);
                    index++;
                }
                colliderPosList.Add(new Vector2(shadowX[0] + diffX, collider.points[1].y));
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


    }

    public override void MinimapOptimizeForJailer(Transform romeNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        for (int i = 0; i < infectedOverlay.transform.childCount; i++)
            infectedOverlay.transform.GetChild(i).transform.localScale *= 0.8f;


        romeNames.GetChild(0).localPosition += new Vector3(0f, 0.2f, 0f);
        romeNames.GetChild(2).localPosition += new Vector3(0f, 0.2f, 0f);
        romeNames.GetChild(3).localPosition += new Vector3(0f, 0.25f, 0f);
        romeNames.GetChild(8).localPosition += new Vector3(0f, 0.3f, 0f);
        romeNames.GetChild(11).localPosition += new Vector3(0f, 0.1f, 0f);
        romeNames.GetChild(15).localPosition += new Vector3(0f, 0.1f, 0f);

        infectedOverlay.transform.GetChild(0).localPosition += new Vector3(0f, -0.15f, 0f);
        infectedOverlay.transform.GetChild(1).localPosition += new Vector3(-0.12f, 0.35f, 0f);
        infectedOverlay.transform.GetChild(2).localPosition += new Vector3(0f, 0.15f, 0f);
        infectedOverlay.transform.GetChild(3).localPosition += new Vector3(0f, 0.15f, 0f);
        infectedOverlay.transform.GetChild(4).localPosition += new Vector3(0.02f, 0.3f, 0f);
        infectedOverlay.transform.GetChild(5).localPosition += new Vector3(0.06f, 0.12f, 0f);
        infectedOverlay.transform.GetChild(6).localPosition += new Vector3(0f, 0.35f, 0f);
        infectedOverlay.transform.GetChild(7).localPosition += new Vector3(0f, 0.25f, 0f);

        countOverlay.transform.GetChild(2).localPosition += new Vector3(-0.2f, -0.4f, 0f);
        countOverlay.transform.GetChild(3).localPosition += new Vector3(0.05f, -0.2f, 0f);
        countOverlay.transform.GetChild(5).localPosition += new Vector3(0.06f, -0.25f, 0f);
        countOverlay.transform.GetChild(6).localPosition += new Vector3(0f, -0.28f, 0f);
        countOverlay.transform.GetChild(16).localPosition += new Vector3(0.15f, -0.3f, 0f);
        countOverlay.transform.GetChild(17).localPosition += new Vector3(-0.1f, -0.5f, 0f);

        foreach (var c in countOverlay.CountAreas) c.YOffset *= -1f;

    }
}

//これだけ定数なのでパッチで対応
[HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.RepairDamage))]
class HeliSabotageSystemPatch
{
    static void Postfix(HeliSabotageSystem __instance, [HarmonyArgument(1)] byte amount)
    {
        if (!CustomOptionHolder.SabotageOption.getBool()) return;

        HeliSabotageSystem.Tags tags = (HeliSabotageSystem.Tags)(amount & 240);
        if (tags == HeliSabotageSystem.Tags.DamageBit)
        {
            __instance.Countdown = CustomOptionHolder.AvertCrashTimeLimitOption.getFloat();
        }
    }
}
