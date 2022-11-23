namespace Nebula.Map.Database
{
    public class AirshipData : MapData 
    {
        public AirshipData() : base(4)
        {
            SabotageMap[SystemTypes.GapRoom] = new SabotageData(SystemTypes.GapRoom, new Vector3(8f, 8.3f), true, true);
            SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(0f, 0f), false, false);
            SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(-13.6f, 2f), true, false);

            //金庫
            MapPositions.Add(new Vector2(-9f, 12.8f));
            MapPositions.Add(new Vector2(-8.7f, 4.9f));
            MapPositions.Add(new Vector2(-12.8f, 8.7f));
            MapPositions.Add(new Vector2(-4.8f, 8.7f));
            MapPositions.Add(new Vector2(-7.1f, 6.8f));
            MapPositions.Add(new Vector2(-10.4f, 6.9f));
            MapPositions.Add(new Vector2(-7f, 10.2f));
            //宿舎前
            MapPositions.Add(new Vector2(-0.5f, 8.5f));
            //エンジン上
            MapPositions.Add(new Vector2(-0.4f, 5f));
            //エンジン
            MapPositions.Add(new Vector2(0f, -1.4f));
            MapPositions.Add(new Vector2(3.6f, 0.1f));
            MapPositions.Add(new Vector2(0.4f, -2.5f));
            MapPositions.Add(new Vector2(-6.9f, 1.1f));
            //コミュ前
            MapPositions.Add(new Vector2(-11f, -1f));
            //コミュ
            MapPositions.Add(new Vector2(-12.3f, 0.9f));
            //コックピット
            MapPositions.Add(new Vector2(-19.9f, -2.6f));
            MapPositions.Add(new Vector2(-19.9f, 0.5f));
            //武器庫
            MapPositions.Add(new Vector2(-14.5f, -3.6f));
            MapPositions.Add(new Vector2(-9.9f, -6f));
            MapPositions.Add(new Vector2(-15f, -9.4f));
            //キッチン
            MapPositions.Add(new Vector2(-7.5f, -7.5f));
            MapPositions.Add(new Vector2(-7f, -12.8f));
            MapPositions.Add(new Vector2(-2.5f, -11.2f));
            MapPositions.Add(new Vector2(-3.9f, -9.3f));
            //左展望
            MapPositions.Add(new Vector2(-13.8f, -11.8f));
            //セキュ
            MapPositions.Add(new Vector2(7.3f, -12.3f));
            MapPositions.Add(new Vector2(5.8f, -10.6f));
            //右展望
            MapPositions.Add(new Vector2(10.3f, -15f));
            //エレク
            MapPositions.Add(new Vector2(10.5f, -8.5f));
            //エレクの9部屋
            MapPositions.Add(new Vector2(10.5f, -6.3f));
            MapPositions.Add(new Vector2(13.5f, -6.3f));
            MapPositions.Add(new Vector2(16.5f, -6.3f));
            MapPositions.Add(new Vector2(19.4f, -6.3f));
            MapPositions.Add(new Vector2(13.5f, -8.8f));
            MapPositions.Add(new Vector2(16.5f, -8.8f));
            MapPositions.Add(new Vector2(19.4f, -8.8f));
            MapPositions.Add(new Vector2(16.5f, -11f));
            MapPositions.Add(new Vector2(19.4f, -11f));
            //エレク右上
            MapPositions.Add(new Vector2(19.4f, -4.2f));
            //メディカル
            MapPositions.Add(new Vector2(25.2f, -9.8f));
            MapPositions.Add(new Vector2(22.9f, -6f));
            MapPositions.Add(new Vector2(25.2f, -9.8f));
            MapPositions.Add(new Vector2(29.5f, -6.3f));
            //貨物
            MapPositions.Add(new Vector2(31.8f, -3.3f));
            MapPositions.Add(new Vector2(34f, 1.4f));
            MapPositions.Add(new Vector2(39f, -0.9f));
            MapPositions.Add(new Vector2(37.6f, -3.4f));
            MapPositions.Add(new Vector2(32.8f, 3.6f));
            MapPositions.Add(new Vector2(35.3f, 3.6f));
            //ロミジュリ右
            MapPositions.Add(new Vector2(29.8f, -1.5f));
            //ラウンジ
            MapPositions.Add(new Vector2(33.7f, 7.1f));
            MapPositions.Add(new Vector2(32.4f, 7.1f));
            MapPositions.Add(new Vector2(30.9f, 7.1f));
            MapPositions.Add(new Vector2(29.2f, 7.1f));
            MapPositions.Add(new Vector2(30.8f, 5.3f));
            MapPositions.Add(new Vector2(24.9f, 4.9f));
            MapPositions.Add(new Vector2(27.1f, 7.3f));
            //レコード
            MapPositions.Add(new Vector2(22.3f, 9.1f));
            MapPositions.Add(new Vector2(20f, 11.5f));
            MapPositions.Add(new Vector2(17.6f, 9.4f));
            MapPositions.Add(new Vector2(20.1f, 6.6f));
            //ギャップ右
            MapPositions.Add(new Vector2(15.4f, 9.2f));
            MapPositions.Add(new Vector2(11.2f, 8.5f));
            MapPositions.Add(new Vector2(12.6f, 6.2f));
            //シャワー/ロミジュリ左
            MapPositions.Add(new Vector2(18.9f, 4.5f));
            MapPositions.Add(new Vector2(17.2f, 5.2f));
            MapPositions.Add(new Vector2(18.5f, 0f));
            MapPositions.Add(new Vector2(21.2f, -2f));
            MapPositions.Add(new Vector2(24f, 0.7f));
            MapPositions.Add(new Vector2(22.3f, 2.5f));
            //メインホール
            MapPositions.Add(new Vector2(10.8f, 0f));
            MapPositions.Add(new Vector2(14.8f, 1.9f));
            MapPositions.Add(new Vector2(11.8f, 1.8f));
            MapPositions.Add(new Vector2(9.7f, 2.5f));
            MapPositions.Add(new Vector2(6.2f, 2.4f));
            MapPositions.Add(new Vector2(6.6f, -3f));
            MapPositions.Add(new Vector2(12.7f, -2.9f));
            //ギャップ左
            MapPositions.Add(new Vector2(3.8f, 8.8f));
            //ミーティング
            MapPositions.Add(new Vector2(6.5f, 15.3f));
            MapPositions.Add(new Vector2(11.8f, 14.1f));
            MapPositions.Add(new Vector2(11.8f, 16f));
            MapPositions.Add(new Vector2(16.3f, 15.2f));

            MapScale = 30f;
            HasDefaultPrespawnMinigame = true;

            RegisterObjectPosition("ruby", new Vector2(-8.8515f, 9.0895f), SystemTypes.VaultRoom, 40f);
            RegisterObjectPosition("crane", new Vector2(1.3844f,0.379f), SystemTypes.Engine, 40f, 1.2f);
            RegisterObjectPosition("steering", new Vector2(-19.677f,-0.791f), SystemTypes.Cockpit, 30f, 1f);
            RegisterObjectPosition("kitchen", new Vector2(-5.1793f, -8.5239f), SystemTypes.Kitchen, 35f);
            RegisterObjectPosition("innermost", new Vector2(18.315f, -3.8655f), SystemTypes.Electrical, 30f,0.9f);
            RegisterObjectPosition("lounge", new Vector2(29.0523f, -7.461f), SystemTypes.Medical, 30f, 1.4f);
            RegisterObjectPosition("plasticContainer", new Vector2(39.039f, 1.4811f), SystemTypes.CargoBay, 35f);

            RitualRooms.Add(new SystemTypes[] { SystemTypes.Cockpit });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Cockpit,SystemTypes.Comms });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Armory, SystemTypes.Comms });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Armory});
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Comms });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Engine });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Kitchen });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.ViewingDeck });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Kitchen,SystemTypes.ViewingDeck });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Security });

            SpawnPoints.Add(new SpawnPointData("cargoBay", new Vector2(33.5778f, -1.5979f)));
            SpawnPoints.Add(new SpawnPointData("toilet", new Vector2(32.3184f, 7.0118f)));
            SpawnPoints.Add(new SpawnPointData("lounge", new Vector2(24.8702f, 6.459f)));
            SpawnPoints.Add(new SpawnPointData("record", new Vector2(19.8717f, 10.1845f)));
            SpawnPoints.Add(new SpawnPointData("gapRoom", new Vector2(11.9727f, 8.6011f)));
            SpawnPoints.Add(new SpawnPointData("shower", new Vector2(19.8887f, -0.0723f)));
            SpawnPoints.Add(new SpawnPointData("mainHall", new Vector2(10.6875f, -0.1902f)));
            SpawnPoints.Add(new SpawnPointData("electrical", new Vector2(16.3201f, -8.808f)));
            SpawnPoints.Add(new SpawnPointData("medical", new Vector2(28.4471f, -5.8789f)));
            SpawnPoints.Add(new SpawnPointData("security", new Vector2(7.0693f, -11.6312f)));
            SpawnPoints.Add(new SpawnPointData("kitchen", new Vector2(-4.0987f, -11.3393f)));
            SpawnPoints.Add(new SpawnPointData("viewingDeck", new Vector2(-13.5882f, -12.5294f)));
            SpawnPoints.Add(new SpawnPointData("armory", new Vector2(-10.141f, -6.3739f)));
            SpawnPoints.Add(new SpawnPointData("comms", new Vector2(-12.9433f, 1.4259f)));
            SpawnPoints.Add(new SpawnPointData("cockpit", new Vector2(-23.5643f, -1.4405f)));
            SpawnPoints.Add(new SpawnPointData("engineRoom", new Vector2(0.0174f, -1.1194f)));
            SpawnPoints.Add(new SpawnPointData("brig", new Vector2(-0.4439f,8.5204f)));
            SpawnPoints.Add(new SpawnPointData("vault", new Vector2(-8.789f,8.049f)));
            SpawnPoints.Add(new SpawnPointData("meetingRoom", new Vector2(11.1469f, 16.0138f)));
        }
    }
}
