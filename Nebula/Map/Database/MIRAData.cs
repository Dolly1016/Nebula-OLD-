namespace Nebula.Map.Database;

public class MIRAData : MapData
{
    public override IEnumerable<Tuple<GameObject, float>> AllAdmins(ShipStatus shipStatus)
    {
        yield return new(shipStatus.transform.GetChild(10).GetChild(3).GetChild(0).gameObject,0.5f);
    }

    public override IEnumerable<Tuple<GameObject, float>> AllCameras(ShipStatus shipStatus)
    {
        yield return new(shipStatus.transform.GetChild(13).GetChild(0).GetChild(0).gameObject,0.3f);
    }

    public override void CreateOption()
    {
        LimitedAdmin.Add(0, Module.CustomOption.Create(Color.white, "option.admin." + PointData.mapNames[MapId] + "-0", Int32.MaxValue, CustomOptionHolder.mapOptions, false, true));
        AdminNameMap.Add("AdminMapConsole", 0);
    }

    public MIRAData() : base(1,"MiraShip")
    {
        SabotageMap[SystemTypes.Reactor] = new SabotageData(SystemTypes.Reactor, new Vector3(2.5f, 13f), true, true);
        SabotageMap[SystemTypes.LifeSupp] = new SabotageData(SystemTypes.LifeSupp, new Vector3(3.7f, -1f), false, true);
        SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(15f, 21f), true, false);
        SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(15f, 5f), false, false);

        //ラウンチパッド
        MapPositions.Add(new Vector2(-4.4f, 3.3f));
        //ランチパッド下通路
        MapPositions.Add(new Vector2(3.7f, -1.7f));
        //メッドベイ
        MapPositions.Add(new Vector2(15.2f, 0.4f));
        //コミュ
        MapPositions.Add(new Vector2(14f, 4f));
        //三叉路
        MapPositions.Add(new Vector2(12.3f, 6.7f));
        MapPositions.Add(new Vector2(23.6f, 6.8f));
        //ロッカー
        MapPositions.Add(new Vector2(9f, 5f));
        MapPositions.Add(new Vector2(8.4f, 1.4f));
        //デコン
        MapPositions.Add(new Vector2(6.0f, 5.6f));
        //デコン上通路
        MapPositions.Add(new Vector2(6.0f, 11.6f));
        //リアクター
        MapPositions.Add(new Vector2(2.5f, 10.3f));
        MapPositions.Add(new Vector2(2.5f, 13f));
        //ラボラトリ
        MapPositions.Add(new Vector2(7.6f, 13.9f));
        MapPositions.Add(new Vector2(9.7f, 10.4f));
        MapPositions.Add(new Vector2(10.7f, 12.2f));
        //カフェ
        MapPositions.Add(new Vector2(21.8f, 5f));
        MapPositions.Add(new Vector2(10.7f, 12.2f));
        MapPositions.Add(new Vector2(28.3f, 0.2f));
        MapPositions.Add(new Vector2(25.5f, 2.3f));
        MapPositions.Add(new Vector2(22.1f, 2.6f));
        //ストレージ
        MapPositions.Add(new Vector2(19.2f, 1.7f));
        MapPositions.Add(new Vector2(18.5f, 4.2f));
        //バルコニー
        MapPositions.Add(new Vector2(18.3f, -3.2f));
        MapPositions.Add(new Vector2(23.7f, -1.9f));
        //三叉路上通路
        MapPositions.Add(new Vector2(17.8f, 19f));
        //オフィス
        MapPositions.Add(new Vector2(15.7f, 17.2f));
        MapPositions.Add(new Vector2(13.7f, 20.4f));
        MapPositions.Add(new Vector2(13.6f, 18.7f));
        //アドミン
        MapPositions.Add(new Vector2(20.6f, 20.8f));
        MapPositions.Add(new Vector2(22.3f, 18.6f));
        MapPositions.Add(new Vector2(21.2f, 17.3f));
        MapPositions.Add(new Vector2(19.4f, 17.6f));
        //グリーンハウス
        MapPositions.Add(new Vector2(13.2f, 22.3f));
        MapPositions.Add(new Vector2(22.4f, 23.3f));
        MapPositions.Add(new Vector2(20.2f, 24.3f));
        MapPositions.Add(new Vector2(16.5f, 24.4f));
        MapPositions.Add(new Vector2(20.7f, 22.2f));
        MapPositions.Add(new Vector2(18f, 25.3f));

        RegisterObjectPosition("memorabilia", new Vector2(8.3714f, -0.8347f), SystemTypes.Hallway, 30f, 1f);
        RegisterObjectPosition("receivingDevice", new Vector2(14.933f, 5.649f), SystemTypes.Comms, 50f);
        RegisterObjectPosition("switchboard", new Vector2(14.802f, 21.384f), SystemTypes.Office, 50f);
        RegisterObjectPosition("planters", new Vector2(20.486f, 22.78f), SystemTypes.Greenhouse, 30f);
        RegisterObjectPosition("vendingMachine", new Vector2(27.49f, 5.665f), SystemTypes.Cafeteria, 40f);
        RegisterObjectPosition("rendezvous", new Vector2(17.7506f, 11.4057f), SystemTypes.Hallway, 60f, 1.2f);
        RegisterObjectPosition("lockers", new Vector2(9.6259f, 3.9384f), SystemTypes.LockerRoom, 40f, 1.4f);
        RegisterObjectPosition("reactor", new Vector2(2.549f, 12.407f), SystemTypes.Reactor, 40f);

        MapScale = 36f;

        SpawnCandidates.Add(new SpawnCandidate("admin", new Vector2(19.4462f, 19.0366f), "assets/SpawnCandidates/Mira/Admin.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("balcony", new Vector2(26.7091f, -1.9142f), "assets/SpawnCandidates/Mira/Balcony.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("cafeteria", new Vector2(25.433f, 2.553f), "assets/SpawnCandidates/Mira/Cafeteria.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("comms", new Vector2(14.4909f, 4.0153f), "assets/SpawnCandidates/Mira/Comms.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("decontamination", new Vector2(6.1333f, 6.27f), "assets/SpawnCandidates/Mira/Decontermination.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("greenhouse", new Vector2(17.857f, 23.5425f), "assets/SpawnCandidates/Mira/Greenhouse.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("laboratory", new Vector2(9.0136f, 12.081f), "assets/SpawnCandidates/Mira/Laboratory.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("launchpad", new Vector2(-4.4f, 2.1969f), "assets/SpawnCandidates/Mira/Launchpad.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("locker", new Vector2(9.0862f, 1.3112f), "assets/SpawnCandidates/Mira/LockerRoom.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("medBay", new Vector2(15.3094f, -0.4085f), "assets/SpawnCandidates/Mira/MedBay.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("office", new Vector2(14.7004f, 20.0933f), "assets/SpawnCandidates/Mira/Office.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("reactor", new Vector2(2.4809f, 13.2443f), "assets/SpawnCandidates/Mira/Reactor.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("rendezvous", new Vector2(17.8176f, 11.3095f), "assets/SpawnCandidates/Mira/Rendezvous.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("storage", new Vector2(19.9159f, 4.718f), "assets/SpawnCandidates/Mira/Storage.png", null, 115f));

        SpawnOriginalPositionAtFirst = true;

        SpawnPoints.Add(new SpawnPointData("launchpad", new Vector2(-4.4f, 2.1969f)));
        SpawnPoints.Add(new SpawnPointData("medBay", new Vector2(15.3094f, -0.4085f)));
        SpawnPoints.Add(new SpawnPointData("comms", new Vector2(14.4909f, 4.0153f)));
        SpawnPoints.Add(new SpawnPointData("locker", new Vector2(9.0862f, 1.3112f)));
        SpawnPoints.Add(new SpawnPointData("decontamination", new Vector2(6.1333f, 6.27f)));
        SpawnPoints.Add(new SpawnPointData("reactor", new Vector2(2.4809f, 13.2443f)));
        SpawnPoints.Add(new SpawnPointData("laboratory", new Vector2(9.0136f, 12.081f)));
        SpawnPoints.Add(new SpawnPointData("rendezvous", new Vector2(17.8176f, 11.3095f)));
        SpawnPoints.Add(new SpawnPointData("office", new Vector2(14.7004f, 20.0933f)));
        SpawnPoints.Add(new SpawnPointData("admin", new Vector2(19.4462f, 19.0366f)));
        SpawnPoints.Add(new SpawnPointData("greenhouse", new Vector2(17.857f, 23.5425f)));
        SpawnPoints.Add(new SpawnPointData("cafeteria", new Vector2(25.433f, 2.553f)));
        SpawnPoints.Add(new SpawnPointData("storage", new Vector2(19.9159f, 4.718f)));
        SpawnPoints.Add(new SpawnPointData("balcony", new Vector2(26.7091f, -1.9142f)));

        AdminRooms.Add(new PointData("launchpad", new Vector2(-4.4f, 2.1969f)));
        AdminSystemTypeMap.Add(SystemTypes.Cockpit, 1);
        AdminRooms.Add(new PointData("medBay", new Vector2(15.8f, -0.4085f)));
        AdminSystemTypeMap.Add(SystemTypes.Armory, 2);
        AdminRooms.Add(new PointData("comms", new Vector2(15.8f, 4.0153f)));
        AdminSystemTypeMap.Add(SystemTypes.Comms, 3);
        AdminRooms.Add(new PointData("locker", new Vector2(9.0862f, 1.3112f)));
        AdminSystemTypeMap.Add(SystemTypes.Engine, 4);
        AdminRooms.Add(new PointData("decontamination", new Vector2(6.1333f, 6.27f)));
        AdminSystemTypeMap.Add(SystemTypes.ViewingDeck, 5);
        AdminRooms.Add(new PointData("laboratory", new Vector2(9.9136f, 12.081f)));
        AdminSystemTypeMap.Add(SystemTypes.Kitchen, 6);
        AdminRooms.Add(new PointData("reactor", new Vector2(2.4809f, 13.2443f)));
        AdminSystemTypeMap.Add(SystemTypes.HallOfPortraits, 7);
        AdminRooms.Add(new PointData("admin", new Vector2(20.9462f, 19.0366f)));
        AdminSystemTypeMap.Add(SystemTypes.Security, 8);
        AdminRooms.Add(new PointData("office", new Vector2(15.7004f, 20.0933f)));
        AdminSystemTypeMap.Add(SystemTypes.Electrical, 9);
        AdminRooms.Add(new PointData("greenhouse", new Vector2(17.857f, 23.5425f)));
        AdminSystemTypeMap.Add(SystemTypes.MainHall, 10);
        AdminRooms.Add(new PointData("storage", new Vector2(19.9159f, 4.018f)));
        AdminSystemTypeMap.Add(SystemTypes.Showers, 11);
        AdminRooms.Add(new PointData("cafeteria", new Vector2(25.433f, 2.553f)));
        AdminSystemTypeMap.Add(SystemTypes.Ventilation, 12);
        AdminRooms.Add(new PointData("balcony", new Vector2(24.7091f, -1.9142f)));
        AdminSystemTypeMap.Add(SystemTypes.Medical, 13);
    }
}
