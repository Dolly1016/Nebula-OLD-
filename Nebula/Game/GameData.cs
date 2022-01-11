using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula.Roles;
using UnityEngine;
using static GameData;

namespace Nebula.Game
{

    class MyPlayerData
    {
        public PlayerControl currentTarget { get; set; }
        private PlayerData globalData { get; set; }

        public PlayerData getGlobalData()
        {
            if (globalData == null)
            {
                if (Game.GameData.data.players.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                {
                    globalData = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId];
                }
            }
            return globalData;
        }

        public MyPlayerData()
        {
            this.currentTarget = null;
            this.globalData = null;
        }
    }

    public class PlayerData
    {
        public class PlayerOutfitData
        {
            public int ColorId { get; }
            public string HatId { get; }
            public string VisorId { get; }
            public string SkinId { get; }
            public string PetId { get; }

            public PlayerOutfitData(PlayerOutfit outfit)
            {
                ColorId = outfit.ColorId;
                HatId = outfit.HatId;
                VisorId = outfit.VisorId;
                SkinId = outfit.SkinId;
                PetId = outfit.PetId;
            }

            public PlayerOutfitData(PlayerOutfitData outfit)
            {
                ColorId = outfit.ColorId;
                HatId = outfit.HatId;
                VisorId = outfit.VisorId;
                SkinId = outfit.SkinId;
                PetId = outfit.PetId;
            }
        }

        public byte id { get; }
        public string name { get; }

        public Role role { get; set; }

        private Dictionary<int, int> roleData { get; set; }

        public bool IsAlive { get; private set; }

        public PlayerOutfitData Outfit { get; }
        public PlayerOutfitData CurrentOutfit { set; get; }
        public string currentName { get; set; }

        public PlayerData(byte playerId, string name,PlayerOutfit outfit,Role role)
        {
            
            this.id = playerId;
            this.name = name;
            this.role = role;
            this.roleData = new Dictionary<int, int>();
            this.IsAlive = true;
            this.Outfit = new PlayerOutfitData(outfit);
            this.CurrentOutfit = new PlayerOutfitData(outfit);
            this.currentName = name;
        }

        public int GetRoleData(int id)
        {
            if (roleData.ContainsKey(id))
            {
                return roleData[id];
            }
            return 0;
        }

        public void SetRoleData(int id,int newValue)
        {
            if (roleData.ContainsKey(id))
            {
                roleData[id] = newValue;
            }
            else
            {
                roleData.Add(id, newValue);
            }
        }
        public void AddRoleData(int id,int addValue)
        {
            SetRoleData(id,GetRoleData(id)+addValue);
        }

        public void Die()
        {
            IsAlive = false;

            if (role.hasFakeTask)
            {
                Helpers.allPlayersById()[id].clearAllTasks();
            }
        }

        public bool IsMyPlayerData()
        {
            return id == GameData.data.myData.getGlobalData().id;
        }
    }

    class GameData
    {
        public static GameData data = null;

        private static Dictionary<string, int> RoleDataIdMap=new Dictionary<string, int>();

        public Dictionary<byte, PlayerData> players;
        
        public MyPlayerData myData;

        public GameData()
        {
            players = new Dictionary<byte, PlayerData>();
            myData = new MyPlayerData();
        }

        //データを消去します。
        private void Clear()
        {
            players.Clear();
        }

        public static void Close()
        {
            if (data != null)
            {
                data.Clear();
                data = null;
            }
        }

        public static void Initialize()
        {
            Close();

            data = new GameData();
        }

        public static int GetRoleDataId(string data)
        {
            if (RoleDataIdMap.ContainsKey(data))
            {
                return RoleDataIdMap[data];
            }
            return -1;
        }

        public static int RegisterRoleDataId(string data)
        {
            if (!RoleDataIdMap.ContainsKey(data))
            {
                RoleDataIdMap.Add(data, RoleDataIdMap.Count);
            }
            return GetRoleDataId(data);
        }

        public void RegisterPlayer(byte playerId,Role role)
        {
            string name = "Unknown";
            PlayerOutfit outfit=null;
            
            foreach(PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player.PlayerId == playerId)
                {
                    name = player.name;
                    outfit = player.CurrentOutfit;
                }
            }

            players.Add(playerId, new PlayerData(playerId,name, outfit,role));
        }
    }
    
}
