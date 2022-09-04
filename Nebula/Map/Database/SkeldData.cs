﻿using System;
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

            RitualRooms.Add(new SystemTypes[] { SystemTypes.UpperEngine });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.LowerEngine });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.UpperEngine, SystemTypes.LowerEngine });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.UpperEngine, SystemTypes.Security });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.LowerEngine, SystemTypes.Security });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.UpperEngine, SystemTypes.MedBay });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Reactor });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Electrical });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Storage });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.MedBay });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Cafeteria });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Weapons });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.LifeSupp });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Admin });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Cafeteria, SystemTypes.Weapons });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Weapons, SystemTypes.LifeSupp });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Nav, SystemTypes.LifeSupp });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Nav });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Shields });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Comms });
            RitualRooms.Add(new SystemTypes[] { SystemTypes.Comms, SystemTypes.Shields });

            RegisterRitualMissionPosition(SystemTypes.Cafeteria, new VectorRange(-3.7f, 1.6f, 5.0f, 5.7f));
            RegisterRitualMissionPosition(SystemTypes.Cafeteria, new VectorRange(-3.5f, 2.0f, -4.0f, -3.2f));
            RegisterRitualMissionPosition(SystemTypes.Cafeteria, new VectorRange(0.9f, 3.1f, -0.3f, 1.9f));
            RegisterRitualMissionPosition(SystemTypes.Cafeteria, new VectorRange(-5.7f, -2.7f, -0.3f, 2.0f));
            RegisterRitualMissionPosition(SystemTypes.Weapons, new VectorRange(9.16f,4.0f));
            RegisterRitualMissionPosition(SystemTypes.Weapons, new VectorRange(9.6f, 3.0f));
            RegisterRitualMissionPosition(SystemTypes.Weapons, new VectorRange(10.4f, 2.0f));
            RegisterRitualMissionPosition(SystemTypes.Weapons, new VectorRange(9.0f, 9.8f, -0.6f, 0.6f));
            RegisterRitualMissionPosition(SystemTypes.LifeSupp, new VectorRange(6.1f,7.4f,-3.8f,-3.3f));
            RegisterRitualMissionPosition(SystemTypes.Nav, new VectorRange(16.55f, -2.5f));
            RegisterRitualMissionPosition(SystemTypes.Nav, new VectorRange(15.5f,17.4f,-5.1f, -4.5f));
            RegisterRitualMissionPosition(SystemTypes.Shields, new VectorRange(8.6f, 10.0f, -13.0f, -11.7f));
            RegisterRitualMissionPosition(SystemTypes.Comms, new VectorRange(2.4f, 5.6f, -16.5f, -15.0f));
            RegisterRitualMissionPosition(SystemTypes.Storage, new VectorRange(0.0f, 0.6f, -16.3f, -9.4f));
            RegisterRitualMissionPosition(SystemTypes.Storage, new VectorRange(-2.9f, -0.9f, -16.5f, -14.8f));
            RegisterRitualMissionPosition(SystemTypes.Storage, new VectorRange(-2.58f, -9.1f));
            RegisterRitualMissionPosition(SystemTypes.Storage, new VectorRange(-4.6f, -4.5f, -15.2f, -11.3f));
            RegisterRitualMissionPosition(SystemTypes.Electrical, new VectorRange(-9.8f, -7.1f, -11.7f, -10.6f));
            RegisterRitualMissionPosition(SystemTypes.Electrical, new VectorRange(-9.1f, -6.4f, -8.8f, -8.2f));
            RegisterRitualMissionPosition(SystemTypes.Electrical, new VectorRange(-9.0f, -10.1f));
            RegisterRitualMissionPosition(SystemTypes.LowerEngine, new VectorRange(-18.4f, -15.8f, -13.5f, -13.2f));
            RegisterRitualMissionPosition(SystemTypes.LowerEngine, new VectorRange(-18.7f, -15.2f, -9.7f, -9.7f));
            RegisterRitualMissionPosition(SystemTypes.Security, new VectorRange(-14.2f, -13.2f, -6.8f, -3.9f));
            RegisterRitualMissionPosition(SystemTypes.Reactor, new VectorRange(-21.1f, -19.5f, -6.5f, -3.7f));
            RegisterRitualMissionPosition(SystemTypes.Reactor, new VectorRange(-22.4f, -21.3f, -8.1f, -6.8f));
            RegisterRitualMissionPosition(SystemTypes.Reactor, new VectorRange(-21.5f, -21.1f, -2.6f, -2.0f));
            RegisterRitualMissionPosition(SystemTypes.UpperEngine, new VectorRange(-17.9f, -15.2f, -1.1f, -0.6f));
            RegisterRitualMissionPosition(SystemTypes.UpperEngine, new VectorRange(-18.1f, -15.9f, 2.5f,2.5f));
            RegisterRitualMissionPosition(SystemTypes.MedBay, new VectorRange(-9.5f, -8.4f, -5.2f, -1.2f));
            RegisterRitualMissionPosition(SystemTypes.MedBay, new VectorRange(-7.7f, -7.1f, -4.4f, -3.9f));
            RegisterRitualMissionPosition(SystemTypes.Admin, new VectorRange(2.4f, 6.5f, -7.6f, -7.1f));
            RegisterRitualMissionPosition(SystemTypes.Admin, new VectorRange(3.2f, 6.0f, -9.8f, -9.5f));

            //Cafe. 左
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-3.8f,1.0f),0.4f,new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(-4.9f,1.9f),0.2f),
                new RitualSpawnCandidate(new Vector2(-4.9f,0.1f),0.2f)
            }));
            //Cafe. 右
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(2.5f, 1.0f), 0.4f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(3.9f,1.9f),0.2f),
                new RitualSpawnCandidate(new Vector2(3.9f,0.1f),0.2f)
            }));
            //Weapons
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(8.5f, 1.0f), 0f));
            //O2
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(6.6f, -3.5f), 0.3f));
            //Nav
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(16.2f, -4.7f), 0.2f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(17.2f,-4.1f),0f),
                new RitualSpawnCandidate(new Vector2(17.2f,-5.2f),0.1f)
            }));
            //コの字通路
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(11.7f, -6.4f), 0.2f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(11.7f, -3.4f),0.2f)
            }));
            //Shield
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(8.8f, -11.8f), 0.2f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(9.5f, -12.6f),0.3f)
            }));
            //Comms.
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(4.7f, -15.3f), 0.4f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(5.2f,-16.1f),0.2f),
                new RitualSpawnCandidate(new Vector2(3f,-15.8f),0.3f)
            }));
            //Storage 下
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-0.8f, -15.4f), 0.5f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(0f,-16.3f),0.2f),
                new RitualSpawnCandidate(new Vector2(-1.9f,-16.2f),0.7f)
            }));
            //Storage 上
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-1.5f, -9.7f), 0.5f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(-0.2f,-10.1f),0.2f),
                new RitualSpawnCandidate(new Vector2(-3.3f,-10.8f),0.1f)
            }));
            //Admin
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(2.8f, -7.4f), 0.2f));
            //Electrical
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-9.0f, -11.7f), 0.3f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(-9.5f,-10.8f),0.1f),
                new RitualSpawnCandidate(new Vector2(-7.5f,-10.9f),0.2f),
                new RitualSpawnCandidate(new Vector2(-6.9f,-9.2f),0.2f)
            }));
            //Electrical前通路
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-12.0f, -14.4f), 0.2f));
            //Lower Engine
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-15.3f, -11.7f), 0.1f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(-16.3f,-13.3f),0.1f),
                new RitualSpawnCandidate(new Vector2(-15.4f,-9.9f),0.1f),
                new RitualSpawnCandidate(new Vector2(-16.9f,-9.3f),0.2f)
            }));
            //Security
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-12.8f, -3.8f), 0.3f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(-13.7f,-5.1f),0.3f),
                new RitualSpawnCandidate(new Vector2(-13.0f,-6.3f),0.2f)
            }));
            //Upper Engine
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-15.5f, -0.9f), 0.2f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(-16.9f,-1.2f),0.3f),
                new RitualSpawnCandidate(new Vector2(-16.0f,2.4f),0.2f)
            }));
            //Med Bay
            RitualSpawnLocations.Add(new RitualSpawnCandidate(new Vector2(-9.0f, -2.0f), 0.4f, new RitualSpawnCandidate[] {
                new RitualSpawnCandidate(new Vector2(-9.4f,-3.9f),0.3f),
                new RitualSpawnCandidate(new Vector2(-8.0f,-4.4f),0.3f)
            }));
        }
    }
}
