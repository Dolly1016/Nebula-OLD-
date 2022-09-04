using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
        }
    }
}
