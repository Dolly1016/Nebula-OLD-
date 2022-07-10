using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Database
{
    public class SkeldData : MapData
    {
        public SkeldData() :base(0)
        {
            SabotageMap[SystemTypes.Reactor] = new SabotageData(SystemTypes.Reactor, new Vector3(-21f, -5.4f), true, true);
            SabotageMap[SystemTypes.LifeSupp] = new SabotageData(SystemTypes.LifeSupp, new Vector3(6.5f, -4.7f), true, true);
            SabotageMap[SystemTypes.Electrical] = new SabotageData(SystemTypes.Electrical, new Vector3(-9.3f, -10.9f), true, false);
            SabotageMap[SystemTypes.Comms] = new SabotageData(SystemTypes.Comms, new Vector3(4.6f, -16.1f), true, false);

            //カフェ
            MapPositions.Add(new Vector2(0f, 5.3f));
            MapPositions.Add(new Vector2(-5.2f, 1.2f));
            MapPositions.Add(new Vector2(-0.9f, -3.1f));
            MapPositions.Add(new Vector2(4.6f, 1.2f));
            //ウェポン
            MapPositions.Add(new Vector2(10.1f, 3f));
            //コの字通路/O2
            MapPositions.Add(new Vector2(9.6f, -3.4f));
            MapPositions.Add(new Vector2(11.8f, -6.5f));
            //ナビ
            MapPositions.Add(new Vector2(16.7f, -4.8f));
            //シールド
            MapPositions.Add(new Vector2(9.3f, -10.3f));
            MapPositions.Add(new Vector2(9.5f, -14.1f));
            //コミュ上
            MapPositions.Add(new Vector2(5.2f, -12.2f));
            //コミュ
            MapPositions.Add(new Vector2(3.8f, -15.4f));
            //ストレージ
            MapPositions.Add(new Vector2(-0.3f, -9.8f));
            MapPositions.Add(new Vector2(-0.28f, -16.4f));
            MapPositions.Add(new Vector2(-4.5f, -14.3f));
            //エレク
            MapPositions.Add(new Vector2(-9.6f, -11.3f));
            MapPositions.Add(new Vector2(-7.5f, -8.4f));
            //ロアエンジン右
            MapPositions.Add(new Vector2(-12.1f, -11.4f));
            //ロアエンジン
            MapPositions.Add(new Vector2(-15.4f, -13.1f));
            MapPositions.Add(new Vector2(-16.8f, -9.8f));
            //アッパーエンジン
            MapPositions.Add(new Vector2(-16.8f, -1f));
            MapPositions.Add(new Vector2(-15.2f, 2.4f));
            //セキュ
            MapPositions.Add(new Vector2(-13.8f, -4.5f));
            //リアクター
            MapPositions.Add(new Vector2(-20.9f, -5.4f));
            //メッドベイ
            MapPositions.Add(new Vector2(-7.3f, -4.6f));
            MapPositions.Add(new Vector2(-9.2f, -2.1f));
            //アドミン
            MapPositions.Add(new Vector2(2.6f, -7.1f));
            MapPositions.Add(new Vector2(6.3f, -9.5f));

            DoorHackingCanBlockSabotage = true;

            MapScale = 32f;
        }
    }
}
