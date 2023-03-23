namespace Nebula.Map.Database;
public class PolusData : MapData
{
    public override IEnumerable<Tuple<GameObject, float>> AllAdmins(ShipStatus shipStatus)
    {
        yield return new(shipStatus.transform.GetChild(10).GetChild(5).GetChild(0).gameObject,0.5f);
        yield return new(shipStatus.transform.GetChild(10).GetChild(5).GetChild(1).gameObject,0.5f);
    }

    public override IEnumerable<Tuple<GameObject, float>> AllVitals(ShipStatus shipStatus)
    {
        yield return new(shipStatus.transform.GetChild(9).GetChild(6).gameObject,0.28f);
    }

    public override IEnumerable<Tuple<GameObject, float>> AllCameras(ShipStatus shipStatus)
    {
        yield return new(shipStatus.transform.GetChild(4).GetChild(9).gameObject,0.1f);
    }

    public override void CreateOption()
    {
        LimitedAdmin.Add(0, Module.CustomOption.Create(Color.white, "option.admin." + PointData.mapNames[MapId] + "-0", Int32.MaxValue, CustomOptionHolder.mapOptions, false, true));
        AdminNameMap.Add("panel_map", 0);
        AdminNameMap.Add("panel_map (1)", 0);
    }
    public PolusData() : base(2,"PolusShip")
    {
        SabotageMap[SystemTypes.Laboratory] = new SabotageData(SystemTypes.Reactor, new Vector3(18f, -6f), true, true);
        SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(10f, -11f), true, false);
        SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(14f, -15.5f), true, false);

        //ドロップシップ
        MapPositions.Add(new Vector2(16.7f, -2.6f));
        //ドロップシップ下
        MapPositions.Add(new Vector2(14.1f, -10f));
        MapPositions.Add(new Vector2(22.0f, -7.1f));
        //エレクトリカル
        MapPositions.Add(new Vector2(7.5f, -9.7f));
        MapPositions.Add(new Vector2(3.1f, -11.7f));
        MapPositions.Add(new Vector2(5.4f, -11.5f));
        MapPositions.Add(new Vector2(9.6f, -12.1f));
        //O2
        MapPositions.Add(new Vector2(4.7f, -19f));
        MapPositions.Add(new Vector2(2.4f, -17f));
        MapPositions.Add(new Vector2(3.1f, -21.7f));
        MapPositions.Add(new Vector2(1.9f, -19.4f));
        MapPositions.Add(new Vector2(2.4f, -23.6f));
        MapPositions.Add(new Vector2(6.3f, -21.3f));
        //Elec,O2,Comm周辺外
        MapPositions.Add(new Vector2(7.9f, -23.6f));
        MapPositions.Add(new Vector2(9.4f, -20.1f));
        MapPositions.Add(new Vector2(8.2f, -16.0f));
        MapPositions.Add(new Vector2(8.0f, -14.3f));
        MapPositions.Add(new Vector2(13.4f, -13f));
        //左上リアクター前通路
        MapPositions.Add(new Vector2(10.3f, -7.4f));
        //左上リアクター
        MapPositions.Add(new Vector2(4.6f, -5f));
        //Comm
        MapPositions.Add(new Vector2(11.4f, -15.9f));
        MapPositions.Add(new Vector2(11.7f, -17.3f));
        //Weapons
        MapPositions.Add(new Vector2(13f, -23.5f));
        //Storage
        MapPositions.Add(new Vector2(19.4f, -11.2f));
        //オフィス左下
        MapPositions.Add(new Vector2(18f, -24.5f));
        //オフィス
        MapPositions.Add(new Vector2(18.6f, -21.5f));
        MapPositions.Add(new Vector2(20.2f, -19.2f));
        MapPositions.Add(new Vector2(19.6f, -17.6f));
        MapPositions.Add(new Vector2(19.6f, -16.4f));
        MapPositions.Add(new Vector2(26.5f, -17.4f));
        //アドミン
        MapPositions.Add(new Vector2(20f, -22.5f));
        MapPositions.Add(new Vector2(21.4f, -25.2f));
        MapPositions.Add(new Vector2(22.4f, -22.6f));
        MapPositions.Add(new Vector2(25f, -20.8f));
        //デコン（左）
        MapPositions.Add(new Vector2(24.1f, -24.7f));
        //スペシメン左通路
        MapPositions.Add(new Vector2(27.7f, -24.7f));
        MapPositions.Add(new Vector2(33f, -20.6f));
        //スペシメン
        MapPositions.Add(new Vector2(36.8f, -21.6f));
        MapPositions.Add(new Vector2(36.5f, -19.3f));
        //スペシメン右通路
        MapPositions.Add(new Vector2(39.2f, -15.2f));
        //デコン(上)
        MapPositions.Add(new Vector2(39.8f, -10f));
        //ラボ
        MapPositions.Add(new Vector2(34.7f, -10.2f));
        MapPositions.Add(new Vector2(36.4f, -8f));
        MapPositions.Add(new Vector2(40.5f, -7.6f));
        MapPositions.Add(new Vector2(34.5f, -6.2f));
        MapPositions.Add(new Vector2(31.2f, -7.6f));
        MapPositions.Add(new Vector2(28.4f, -9.6f));
        MapPositions.Add(new Vector2(26.5f, -7f));
        MapPositions.Add(new Vector2(26.5f, -8.3f));
        //右リアクター
        MapPositions.Add(new Vector2(24.2f, -4.5f));
        //ストレージ・ラボ下・オフィス右
        MapPositions.Add(new Vector2(24f, -14.6f));
        MapPositions.Add(new Vector2(26f, -12.2f));
        MapPositions.Add(new Vector2(29.8f, -15.7f));


        MapScale = 32f;

        //スポーン候補
        SpawnCandidates.Add(new SpawnCandidate("dropship", new Vector2(16.6f, -1.5f), "assets/SpawnCandidates/Polus/Dropship.png", "rollover_brig"));
        SpawnCandidates.Add(new SpawnCandidate("storage", new Vector2(20.6f, -11.7f), "assets/SpawnCandidates/Polus/Storage.png", "panel_O2Drop"));
        SpawnCandidates.Add(new SpawnCandidate("laboratory", new Vector2(34.8f, -6.0f), "assets/SpawnCandidates/Polus/Laboratory.png", null));
        SpawnCandidates.Add(new SpawnCandidate("specimen", new Vector2(36.5f, -21.2f), "assets/SpawnCandidates/Polus/Specimens.png", null));
        SpawnCandidates.Add(new SpawnCandidate("office", new Vector2(19.5f, -17.6f), "assets/SpawnCandidates/Polus/Office.png", null));
        SpawnCandidates.Add(new SpawnCandidate("weapons", new Vector2(12.2f, -23.3f), "assets/SpawnCandidates/Polus/Weapons.png", "panel_weaponfire"));
        SpawnCandidates.Add(new SpawnCandidate("lifeSupport", new Vector2(3.5f, -21.5f), "assets/SpawnCandidates/Polus/LifeSupport.png", null));
        SpawnCandidates.Add(new SpawnCandidate("electrical", new Vector2(7.4f, -9.6f), "assets/SpawnCandidates/Polus/Electrical.png", "AMB_Electricshock1"));
        SpawnCandidates.Add(new SpawnCandidate("abditory", new Vector2(25.7226f, -12.8779f), "assets/SpawnCandidates/Polus/Abditory.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("admin", new Vector2(21.1384f, -22.7731f), "assets/SpawnCandidates/Polus/Admin.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("drill", new Vector2(27.5518f, -7.3609f), "assets/SpawnCandidates/Polus/Drill.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("ejection", new Vector2(32.1547f, -15.7529f), "assets/SpawnCandidates/Polus/Ejection.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("security", new Vector2(3.0694f, -11.9939f), "assets/SpawnCandidates/Polus/Security.png", null, 115f));
        SpawnCandidates.Add(new SpawnCandidate("snowdrift", new Vector2(12.918f, -13.0296f), "assets/SpawnCandidates/Polus/Snowdrift.png", null, 115f));

        SpawnOriginalPositionAtFirst = true;

        RegisterObjectPosition("plasticContainer", new Vector2(21.0664f, -11.2992f), SystemTypes.Storage, 40f);
        RegisterObjectPosition("switchboard", new Vector2(9.718f, -11.0947f), SystemTypes.Electrical, 50f);
        RegisterObjectPosition("weaponsConsole", new Vector2(9.9291f, -22.391f), SystemTypes.Weapons, 45f);
        RegisterObjectPosition("tree", new Vector2(1.654f, -16.012f), SystemTypes.LifeSupp, 35f, 0.8f);
        RegisterObjectPosition("telescope", new Vector2(33.8684f, -5.4713f), SystemTypes.Laboratory, 40f);
        RegisterObjectPosition("drill", new Vector2(27.421f, -6.9823f), SystemTypes.Laboratory, 35f);
        RegisterObjectPosition("reactor", new Vector2(34.7578f, -18.8785f), SystemTypes.Specimens, 50f);
        RegisterObjectPosition("decontamination", new Vector2(39.8686f, -9.8323f), SystemTypes.Decontamination, 35f);

        SpawnPoints.Add(new SpawnPointData("dropship", new Vector2(16.6f, -1.5f)));
        SpawnPoints.Add(new SpawnPointData("storage", new Vector2(20.6f, -11.7f)));
        SpawnPoints.Add(new SpawnPointData("laboratory", new Vector2(34.8f, -6.0f)));
        SpawnPoints.Add(new SpawnPointData("specimenRoom", new Vector2(36.5f, -21.2f)));
        SpawnPoints.Add(new SpawnPointData("office", new Vector2(19.5f, -17.6f)));
        SpawnPoints.Add(new SpawnPointData("weapons", new Vector2(12.2f, -23.3f)));
        SpawnPoints.Add(new SpawnPointData("lifeSupport", new Vector2(3.5f, -21.5f)));
        SpawnPoints.Add(new SpawnPointData("electrical", new Vector2(7.4f, -9.6f)));
        SpawnPoints.Add(new SpawnPointData("comms", new Vector2(10.9f, -17.7f)));
        SpawnPoints.Add(new SpawnPointData("security", new Vector2(3.0694f, -11.9939f)));
        SpawnPoints.Add(new SpawnPointData("snowdrift", new Vector2(12.918f, -13.0296f)));
        SpawnPoints.Add(new SpawnPointData("admin", new Vector2(21.1384f, -22.7731f)));
        SpawnPoints.Add(new SpawnPointData("ejection", new Vector2(32.1547f, -15.7529f)));
        SpawnPoints.Add(new SpawnPointData("abditory", new Vector2(25.7226f, -12.8779f)));
        SpawnPoints.Add(new SpawnPointData("drill", new Vector2(27.5518f, -7.3609f)));

        AdminRooms.Add(new PointData("office", new Vector2(22.5f, -17.6f)));
        AdminSystemTypeMap.Add(SystemTypes.Office, 1);
        AdminRooms.Add(new PointData("admin", new Vector2(21.1384f, -22.7731f)));
        AdminSystemTypeMap.Add(SystemTypes.Admin, 2);
        AdminRooms.Add(new PointData("specimenRoom", new Vector2(36.5f, -21.2f)));
        AdminSystemTypeMap.Add(SystemTypes.Specimens, 3);
        AdminRooms.Add(new PointData("laboratory", new Vector2(34.8f, -8.0f)));
        AdminSystemTypeMap.Add(SystemTypes.Laboratory, 4);
        AdminRooms.Add(new PointData("dropship", new Vector2(16.6f, -1.5f)));
        AdminSystemTypeMap.Add(SystemTypes.Dropship, 5);
        AdminRooms.Add(new PointData("storage", new Vector2(20.6f, -11.7f)));
        AdminSystemTypeMap.Add(SystemTypes.Storage, 6);
        AdminRooms.Add(new PointData("weapons", new Vector2(12.2f, -23.3f)));
        AdminSystemTypeMap.Add(SystemTypes.Weapons, 7);
        AdminRooms.Add(new PointData("comms", new Vector2(11.4f, -16.7f)));
        AdminSystemTypeMap.Add(SystemTypes.Comms, 8);
        AdminRooms.Add(new PointData("boilerRoom", new Vector2(2.4f, -24.2f)));
        AdminSystemTypeMap.Add(SystemTypes.BoilerRoom, 9);
        AdminRooms.Add(new PointData("lifeSupport", new Vector2(2.5f, -17.5f)));
        AdminSystemTypeMap.Add(SystemTypes.LifeSupp, 10);
        AdminRooms.Add(new PointData("security", new Vector2(2.4f, -12.0f)));
        AdminSystemTypeMap.Add(SystemTypes.Security, 11);
        AdminRooms.Add(new PointData("electrical", new Vector2(7.4f, -9.6f)));
        AdminSystemTypeMap.Add(SystemTypes.Electrical, 12);

        ClassicAdminMask = 0b1000000000;
    }
}
