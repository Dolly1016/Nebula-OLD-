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


            MapScale = 36f;
        }
    }
}
