﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Map;

public class SkeldData : MapData
{
    static private Vector2[] MapPositions = new Vector2[]
    {
        //カフェ
        new(0f, 5.3f), new(-5.2f, 1.2f), new(-0.9f, -3.1f), new(4.6f, 1.2f),
        //ウェポン
        new(10.1f, 3f),
        //コの字通路/O2
        new(9.6f, -3.4f), new(11.8f, -6.5f),
        //ナビ
        new(16.7f, -4.8f),
        //シールド
        new(9.3f, -10.3f), new(9.5f, -14.1f),
        //コミュ上
        new(5.2f, -12.2f),
        //コミュ
        new(3.8f, -15.4f),
        //ストレージ
        new(-0.3f, -9.8f), new(-0.28f, -16.4f), new(-4.5f, -14.3f),
        //エレク
        new(-9.6f, -11.3f), new(-7.5f, -8.4f),
        //ロアエンジン右
        new(-12.1f, -11.4f),
        //ロアエンジン
        new(-15.4f, -13.1f), new(-16.8f, -9.8f),
        //アッパーエンジン
        new(-16.8f, -1f), new(-15.2f, 2.4f),
        //セキュ
        new(-13.8f, -4.5f),
        //リアクター
        new(-20.9f, -5.4f),
        //メッドベイ
        new(-7.3f, -4.6f), new(-9.2f, -2.1f),
        //アドミン
        new(2.6f, -7.1f), new(6.3f, -9.5f)
    };
    protected override Vector2[] MapArea => MapPositions;
}
