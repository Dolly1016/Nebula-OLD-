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

    public class MyPlayerData
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

    public class DeadPlayerData
    {
        public enum DeathReason
        {
            Killed,
            Exiled,
            Suicide,
        }

        public PlayerData Data { get; }
        public byte MurderId { get; }
        public DeathReason Reason { get; }
        public bool existDeadBody { get; private set; }
        //死亡場所
        public Vector3 deathLocation { get; }
        //死後経過時間
        public float Elapsed { get; set; }

        public DeadPlayerData(PlayerData playerData,DeathReason deathReason,byte murderId)
        {
            this.Data = playerData;
            this.Reason = deathReason;
            this.MurderId = murderId;
            this.existDeadBody = true;
            this.deathLocation = Helpers.allPlayersById()[playerData.id].transform.position;
            this.Elapsed = 0f;
        }

        public void EraseBody()
        {
            existDeadBody = false;
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
        public List<ExtraRole> extraRole { get; }

        //自身のロールがもつデータ
        private Dictionary<int, int> roleData { get; set; }
        //Extraロール1つにつき1つのlong変数を割り当てる
        private Dictionary<byte, ulong> extraRoleData { get; set; }

        public bool IsAlive { get; private set; }

        public PlayerOutfitData Outfit { get; }
        public PlayerOutfitData CurrentOutfit { set; get; }
        public string currentName { get; set; }
        public byte dragPlayerId { get; set; }

        public PlayerData(byte playerId, string name,PlayerOutfit outfit,Role role)
        {
            
            this.id = playerId;
            this.name = name;
            this.role = role;
            this.extraRole = new List<ExtraRole>();
            this.roleData = new Dictionary<int, int>();
            this.extraRoleData = new Dictionary<byte, ulong>();
            this.IsAlive = true;
            this.Outfit = new PlayerOutfitData(outfit);
            this.CurrentOutfit = new PlayerOutfitData(outfit);
            this.currentName = name;
            this.dragPlayerId = Byte.MaxValue;
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

        public void AddRoleData(int id, int addValue)
        {
            SetRoleData(id, GetRoleData(id) + addValue);
        }

        public ulong GetExtraRoleData(byte id)
        {
            if (extraRoleData.ContainsKey(id))
            {
                return extraRoleData[id];
            }
            return 0;
        }

        public ulong GetExtraRoleData(ExtraRole role)
        {
            return GetExtraRoleData(role.id);
        }

        public void SetExtraRoleData(byte id, ulong newValue)
        {
            if (extraRoleData.ContainsKey(id))
            {
                extraRoleData[id] = newValue;
            }
            else
            {
                extraRoleData.Add(id, newValue);
            }

        }

        public void SetExtraRoleData(Roles.ExtraRole role, ulong newValue)
        {
            SetExtraRoleData(role.id, newValue);
        }

        /// <summary>
        /// ロールデータを差し替えます。ゲーム中に実行できます。
        /// </summary>
        /// <param name="newData"></param>
        public void CleanRoleDataInGame(Dictionary<int,int>? newData=null)
        {
            if (newData != null)
            {
                roleData = newData;
            }
            else
            {
                roleData = new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// ロールデータを他人と交換します。 ロール自体は交換されないことに注意してください。
        /// </summary>
        /// <param name="target">ロールデータの交換相手</param>
        public void SwapRoleData(PlayerData target)
        {
            Dictionary<int, int> temp= target.roleData;
            target.roleData = roleData;
            roleData = temp;
        }

        /// <summary>
        /// ロールデータ全体を取得します。
        /// </summary>
        /// <returns></returns>
        public Dictionary<int,int> ExtractRoleData()
        {
            return roleData;
        }

        private void Die(DeadPlayerData.DeathReason deathReason,byte murderId)
        {
            IsAlive = false;

            if (role.hasFakeTask)
            {
                Helpers.allPlayersById()[id].clearAllTasks();
            }

            Game.GameData.data.deadPlayers.Add(id, new DeadPlayerData(this, deathReason, murderId));
        }

        public void Die(DeadPlayerData.DeathReason deathReason)
        {
            Die(deathReason,Byte.MaxValue);
        }

        public void Die(byte murderId)
        {
            Die(DeadPlayerData.DeathReason.Killed, murderId);
        }

        public bool IsMyPlayerData()
        {
            return id == GameData.data.myData.getGlobalData().id;
        }

        public bool DragPlayer(byte playerId)
        {
            if (playerId == Byte.MaxValue)
            {
                return false;
            }

            dragPlayerId =playerId;

            return true;
        }

        public bool DropPlayer()
        {
            if (dragPlayerId == Byte.MaxValue)
            {
                return false;
            }

            dragPlayerId = Byte.MaxValue;

            return true;
        }
    }

    public class GameData
    {
        public static GameData data = null;

        private static Dictionary<string, int> RoleDataIdMap=new Dictionary<string, int>();

        public Dictionary<byte, PlayerData> players;
        public Dictionary<byte, DeadPlayerData> deadPlayers;

        public MyPlayerData myData;

        public int TotalTasks, CompleteTasks;

        public GameData()
        {
            players = new Dictionary<byte, PlayerData>();
            deadPlayers = new Dictionary<byte, DeadPlayerData>();
            myData = new MyPlayerData();
            TotalTasks = 0;
            CompleteTasks = 0;
        }

        //データを消去します。
        private void Clear()
        {
            players.Clear();
            deadPlayers.Clear();
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
