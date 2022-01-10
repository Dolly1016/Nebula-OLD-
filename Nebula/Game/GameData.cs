﻿using System;
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
                globalData = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId];
            }
            return globalData;
        }

        public MyPlayerData()
        {
            this.currentTarget = null;
            this.globalData = null;
        }
    }

    class PlayerData
    {
        public byte id { get; }
        public string name { get; }

        public Role role { get; set; }

        private Dictionary<int, int> roleData { get; set; }

        public bool IsAlive { get; private set; }

        public PlayerOutfit outfit { get; }

        public PlayerData(byte playerId, string name,PlayerOutfit outfit,Role role)
        {
            
            this.id = playerId;
            this.name = name;
            this.role = role;
            this.roleData = new Dictionary<int, int>();
            this.IsAlive = true;
            this.outfit = outfit;
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

        public static void RegisterRoleDataId(string data)
        {
            if (!RoleDataIdMap.ContainsKey(data))
            {
                RoleDataIdMap.Add(data, RoleDataIdMap.Count);
            }
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
