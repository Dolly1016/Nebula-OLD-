using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Database
{
    public class MIRAData : MapData
    {
        public MIRAData() : base(1)
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
        }
    }
}
