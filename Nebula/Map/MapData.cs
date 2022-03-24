using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnhollowerBaseLib.Attributes;

namespace Nebula.Map
{
    public class SabotageData
    {
        public SystemTypes Room { get; private set; }
        public Vector3 Position { get; private set; }
        public bool IsLeadingSabotage { get; private set; }
        public bool IsUrgent { get; private set; }

        public SabotageData(SystemTypes Room,Vector3 Position,bool IsLeadingSabotage,bool IsUrgent)
        {
            this.Room = Room;
            this.Position = Position;
            this.IsLeadingSabotage = IsLeadingSabotage;
            this.IsUrgent = IsUrgent;
        }
    }

    public class WiringData
    {
        HashSet<int>[] WiringCandidate;

        public WiringData()
        {
            WiringCandidate = new HashSet<int>[3] { new HashSet<int>(), new HashSet<int>(), new HashSet<int>() };            
        }
    }

    public class MapData
    {
        //Skeld=0,MIRA=1,Polus=2,AirShip=4

        public ShipStatus Assets;
        public int MapId { get; }

        public static Dictionary<int, MapData> MapDatabase = new Dictionary<int, MapData>();

        public List<byte> CommonTaskIdList { get; protected set; }
        public List<byte> ShortTaskIdList { get; protected set; }
        public List<byte> LongTaskIdList { get; protected set; }

        public Dictionary<SystemTypes, SabotageData> SabotageMap;

        //部屋の関連性
        public Dictionary<SystemTypes, HashSet<SystemTypes>> RoomsRelation;
        //ドアを持つ部屋
        public HashSet<SystemTypes> DoorRooms;

        //ドアサボタージュがサボタージュの発生を阻止するかどうか
        public bool DoorHackingCanBlockSabotage;
        //ドアサボタージュの有効時間
        public float DoorHackingDuration;

        //マップの端から端までの距離
        public float MapScale;

        //スポーン位置候補
        public List<SpawnCandidate> SpawnCandidates;
        public bool SpawnOriginalPositionAtFirst;

        //スポーン位置選択がもとから発生するかどうか
        public bool HasDefaultPrespawnMinigame;

        

        public static void Load()
        {
            new Database.SkeldData();
            new Database.MIRAData();
            new Database.PolusData();
            new Database.AirshipData();
        }

        public static Map.MapData GetCurrentMapData()
        {
            return MapDatabase[PlayerControl.GameOptions.MapId];
        }

        public static byte GetRandomCommonTaskId(byte mapId)
        {
            if (!MapDatabase.ContainsKey(mapId)) return 0;

            return MapDatabase[mapId].GetRandomCommonTaskId();
        }
        public static byte GetRandomShortTaskId(byte mapId) {
            if (!MapDatabase.ContainsKey(mapId)) return 0;

            return MapDatabase[mapId].GetRandomShortTaskId(); 
        }
    
        public static byte GetRandomLongTaskId(byte mapId) {
            if (!MapDatabase.ContainsKey(mapId)) return 0;

            return MapDatabase[mapId].GetRandomLongTaskId();
        }

        public MapData(int mapId)
        {
            MapId = mapId;
            MapDatabase[mapId] = this;

            SabotageMap = new Dictionary<SystemTypes, SabotageData>();
            RoomsRelation = new Dictionary<SystemTypes, HashSet<SystemTypes>>();
            DoorRooms = new HashSet<SystemTypes>();

            SpawnCandidates = new List<SpawnCandidate>();
            SpawnOriginalPositionAtFirst = false;

            DoorHackingCanBlockSabotage = false;

            HasDefaultPrespawnMinigame = false;

            MapScale = 1f;
            DoorHackingDuration = 10f;
        }

        public void LoadAssets(AmongUsClient __instance)
        {
            AssetReference assetReference = __instance.ShipPrefabs.ToArray()[MapId];
            AsyncOperationHandle<GameObject> asset = assetReference.LoadAsset<GameObject>();
            asset.WaitForCompletion();
            Assets = asset.Result.GetComponent<ShipStatus>();
        }

        public byte GetRandomCommonTaskId() { return CommonTaskIdList[NebulaPlugin.rnd.Next(CommonTaskIdList.Count)]; }
        public byte GetRandomShortTaskId() { return ShortTaskIdList[NebulaPlugin.rnd.Next(ShortTaskIdList.Count)]; }
        public byte GetRandomLongTaskId() { return LongTaskIdList[NebulaPlugin.rnd.Next(LongTaskIdList.Count)]; }

        public bool PlayInitialPrespawnMinigame
        {
            get
            {
                if (HasDefaultPrespawnMinigame) return true;

                return (SpawnCandidates.Count >= 3 && !SpawnOriginalPositionAtFirst && CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.multipleSpawnPoints.getBool());
            }
        }
    }
}
