using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Map
{
    public class MapData
    {
        //Skeld=0,MIRA=1,Polus=2,AirShip=4
        public int MapId { get; }

        public static Dictionary<int, MapData> MapDatabase = new Dictionary<int, MapData>();

        protected List<byte> CommonTaskIdList { get; set; }
        protected List<byte> ShortTaskIdList { get; set; }
        protected List<byte> LongTaskIdList { get; set; }

        public static void Load()
        {
            new Database.SkeldData();
            new Database.MIRAData();
            new Database.PolusData();
            new Database.AirshipData();
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
        }

        public byte GetRandomCommonTaskId() { return CommonTaskIdList[NebulaPlugin.rnd.Next(CommonTaskIdList.Count)]; }
        public byte GetRandomShortTaskId() { return ShortTaskIdList[NebulaPlugin.rnd.Next(ShortTaskIdList.Count)]; }
        public byte GetRandomLongTaskId() { return LongTaskIdList[NebulaPlugin.rnd.Next(LongTaskIdList.Count)]; }
    }
}
