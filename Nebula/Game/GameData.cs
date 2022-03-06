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
        public bool CanSeeEveryoneInfo { get; set; }
        public float VentDurationTimer { get; set; }
        public float VentCoolDownTimer { get; set; }

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
            this.CanSeeEveryoneInfo = false;
            this.VentDurationTimer = this.VentCoolDownTimer = 10f;
        }
    }

    public class DeadPlayerData
    {
        public PlayerData Data { get; }
        public byte MurderId { get; }
        public bool existDeadBody { get; private set; }
        //死亡場所
        public Vector3 deathLocation { get; }
        //死後経過時間
        public float Elapsed { get; set; }
        //復活希望場所(同じプレイヤーの復活希望場所も人によって違う)
        public SystemTypes RespawnRoom { get; }

        public DeadPlayerData(PlayerData playerData,byte murderId)
        {
            this.Data = playerData;
            this.MurderId = murderId;
            this.existDeadBody = true;
            this.deathLocation = Helpers.allPlayersById()[playerData.id].transform.position;
            this.Elapsed = 0f;

            List<SystemTypes> candidate = new List<SystemTypes>();
            float dis = Roles.Roles.Necromancer.maxReviveRoomDistanceOption.getFloat(),nearestDis=100f;
            SystemTypes? nearest = null;
            foreach (var room in Game.GameData.data.Rooms)
            {
                float d = ShipStatus.Instance.FastRooms[room].roomArea.Distance(Helpers.allPlayersById()[playerData.id].Collider).distance;
                if (d > 0.2f)
                {
                    if (dis > d)
                    {
                        candidate.Add(room);
                    }
                    if (nearest == null || nearestDis > d)
                    {
                        nearestDis = d;
                        nearest = room;
                    }
                }
            }
            if (candidate.Count > 0)
                this.RespawnRoom = candidate[NebulaPlugin.rnd.Next(candidate.Count)];
            else
                this.RespawnRoom = nearest.Value;
        }

        public void EraseBody()
        {
            existDeadBody = false;
        }
    }

    public class SpeedFactor
    {
        public bool IsPermanent { get; private set; }
        public float Duration;
        public float SpeedRate { get; private set; }
        public bool CanCrossOverMeeting { get; private set; }
        //DupId=0の場合、重複を許可する。それ以外の場合、既に存在するSpeedFactorは消去される。
        public byte DupId { get; set; }


        public SpeedFactor(bool IsPermanent, byte dupId, float duration, float speed,bool canCrossOverMeeting)
        {
            IsPermanent = IsPermanent;
            DupId = dupId;
            Duration = duration;
            SpeedRate = speed;
            CanCrossOverMeeting = canCrossOverMeeting;
        }

        public SpeedFactor(float speed)
            :this(true,0,1f,speed,true)
        {}

        public SpeedFactor(byte dupId,float speed)
           : this(true, dupId,1f, speed, true)
        { }

        public SpeedFactor(byte dupId, float duration,float speed, bool canCrossOverMeeting)
         : this(false, dupId,duration, speed, canCrossOverMeeting)
        { }
    }

    public class SpeedFactorManager
    {
        HashSet<SpeedFactor> Factors;
        byte PlayerId;

        public SpeedFactorManager(byte playerId)
        {
            Factors = new HashSet<SpeedFactor>();
            PlayerId = playerId;
        }

        public void Register(SpeedFactor speed)
        {
            if (speed.DupId != 0) Factors.RemoveWhere((factor) => { return factor.DupId == speed.DupId; });
            Factors.Add(speed);
            Reflect();
        }

        public void Update()
        {
            int num = Factors.Count;
            Factors.RemoveWhere((speed) =>
            {
                speed.Duration -= Time.deltaTime;
                return !(speed.Duration > 0);
            });
            if (Factors.Count != num)
            {
                Reflect();
            }
        }

        public void OnMeeting()
        {
            Factors.RemoveWhere((speed) =>
            {
                return !speed.CanCrossOverMeeting;
            });
            Reflect();
        }

        public void Reflect()
        {
            PlayerControl player=Helpers.playerById(PlayerId);
            player.MyPhysics.Speed = Game.GameData.data.OriginalSpeed;
            foreach(SpeedFactor speed in Factors)
            {
                player.MyPhysics.Speed *= speed.SpeedRate;
            }
            if(!player.CanMove && player.MyPhysics.Speed < 0)
            {
                player.MyPhysics.Speed *= -1f;
            }
        }
    }

    public class PlayerAttribute
    {
        public static Dictionary<byte, PlayerAttribute> AllAttributes = new Dictionary<byte, PlayerAttribute>();

        public static PlayerAttribute Invisible = new PlayerAttribute(0);

        public byte Id { get; private set; }
        
        private PlayerAttribute(byte Id)
        {
            this.Id = Id;

            AllAttributes[Id] = this;
        }
    }
    public class PlayerAttributeFactor
    {
        public PlayerAttribute Attribute;
        public bool IsPermanent { get; private set; }
        public float Duration;
        public byte DupId { get; private set; }
        public bool CanCrossOverMeeting { get; private set; }

        public PlayerAttributeFactor(PlayerAttribute Attribute, bool IsPermanent,float Duration,byte DupId, bool CanCrossOverMeeting)
        {
            this.Attribute = Attribute;
            this.IsPermanent = IsPermanent;
            this.Duration = Duration;
            this.DupId = DupId;
            this.CanCrossOverMeeting = CanCrossOverMeeting;
        }

        public PlayerAttributeFactor(PlayerAttribute Attribute, byte DupId,bool CanCrossOverMeeting):
            this(Attribute,true,1f,DupId,CanCrossOverMeeting)
        {

        }

        public PlayerAttributeFactor(PlayerAttribute Attribute, float Duration, byte DupId, bool CanCrossOverMeeting):
            this(Attribute, false, Duration, DupId, CanCrossOverMeeting)
        {

        }
    }

    public class PlayerAttributeFactorManager
    {
        HashSet<PlayerAttributeFactor> Factors;
        byte PlayerId;

        public PlayerAttributeFactorManager(byte playerId)
        {
            Factors = new HashSet<PlayerAttributeFactor>();
            PlayerId = playerId;
        }

        public bool HasAttribute(PlayerAttribute Attribute)
        {
            return Factors.Any(factor => factor.Attribute == Attribute);
        }

        public void Register(PlayerAttributeFactor attributeFactor)
        {
            if (attributeFactor.DupId != 0) Factors.RemoveWhere((factor) => { return factor.DupId == attributeFactor.DupId; });
            Factors.Add(attributeFactor);
        }

        public void Update()
        {
            int num = Factors.Count;
            Factors.RemoveWhere((factor) =>
            {
                if(!factor.IsPermanent)
                    factor.Duration -= Time.deltaTime;
                return !(factor.Duration > 0);
            });
        }

        public void OnMeeting()
        {
            Factors.RemoveWhere((factor) =>
            {
                return !factor.CanCrossOverMeeting;
            });
        }
    }

    public class TaskData
    {
        //表示タスク総量
        public int DisplayTasks { get; set; }
        //タスク総量
        public int AllTasks { get; set; }
        //ノルマタスク数
        public int Quota { get; set; }
        //完了タスク数
        public int Completed { get; set; }
        //クルーメイトのタスク勝利に反映させるかどうか
        public bool IsInfluencedCrewmatesTasks;

        public TaskData(bool hasFakeTask,bool fakeTaskIsExecutable){
            int tasks= PlayerControl.GameOptions.NumCommonTasks + PlayerControl.GameOptions.NumShortTasks + PlayerControl.GameOptions.NumLongTasks; ;

            if (hasFakeTask && !fakeTaskIsExecutable) Quota = 0;
            else Quota = tasks;

            AllTasks = Quota;
            DisplayTasks = Quota;
            Completed = 0;
        }
    }

    public class PlayerData
    {
        public class PlayerStatus
        {
            static private byte AvailableId = 0;
            static Dictionary<byte, PlayerStatus> StatusMap = new System.Collections.Generic.Dictionary<byte, PlayerStatus>();

            public static PlayerStatus Alive = new PlayerStatus("alive");
            public static PlayerStatus Dead = new PlayerStatus("dead");
            public static PlayerStatus Exiled = new PlayerStatus("exiled");
            public static PlayerStatus Disconnected = new PlayerStatus("disconnected");
            public static PlayerStatus Suicide = new PlayerStatus("suicide");
            public static PlayerStatus Revived = new PlayerStatus("revived");
            public static PlayerStatus Burned=new PlayerStatus("burned");
            public static PlayerStatus Embroiled = new PlayerStatus("embroiled");
            public static PlayerStatus Guessed = new PlayerStatus("guessed");
            public static PlayerStatus Misguessed = new PlayerStatus("misguessed");
            public static PlayerStatus Trapped = new PlayerStatus("trapped");
            public static PlayerStatus Sniped = new PlayerStatus("sniped");
            public static PlayerStatus Arrested = new PlayerStatus("arrested");
            public static PlayerStatus Punished = new PlayerStatus("punished");
            public static PlayerStatus Misfire = new PlayerStatus("misfire");
            

            public string Status { get; private set; }
            public byte Id;

            private PlayerStatus(string Status)
            {
                this.Status = Status;
                this.Id = AvailableId;
                AvailableId++;
                StatusMap[Id] = this;
            }

            static public PlayerStatus GetStatusById(byte Id)
            {
                return StatusMap[Id];
            }
        }
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
        public HashSet<ExtraRole> extraRole { get; }

        //自身のロールがもつデータ
        private Dictionary<int, int> roleData { get; set; }
        //Extraロール1つにつき1つのlong変数を割り当てる
        private Dictionary<byte, ulong> extraRoleData { get; set; }

        public bool IsAlive { get; private set; }

        public PlayerOutfitData Outfit { get; }
        public PlayerOutfitData CurrentOutfit { set; get; }
        public string currentName { get; set; }
        public byte dragPlayerId { get; set; }

        public SpeedFactorManager Speed { get; }
        public PlayerAttributeFactorManager Attribute { get; }
        public Color TransColor { get; set; }

        public float MouseAngle { get; set; }

        public TaskData Tasks { get; }

        public PlayerStatus Status { get; set; }

        public string RoleInfo { get; set; }

        public PlayerData(byte playerId, string name,PlayerOutfit outfit,Role role)
        {
            
            this.id = playerId;
            this.name = name;
            this.role = role;
            this.extraRole = new HashSet<ExtraRole>();
            this.roleData = new Dictionary<int, int>();
            this.extraRoleData = new Dictionary<byte, ulong>();
            this.IsAlive = true;
            this.Outfit = new PlayerOutfitData(outfit);
            this.CurrentOutfit = new PlayerOutfitData(outfit);
            this.currentName = name;
            this.dragPlayerId = Byte.MaxValue;
            this.Speed = new SpeedFactorManager(playerId) ;
            this.Attribute = new PlayerAttributeFactorManager(playerId);
            this.Tasks = new TaskData(role.side == Roles.Side.Impostor || role.HasFakeTask, role.FakeTaskIsExecutable);
            this.MouseAngle = 0f;
            this.Status = PlayerStatus.Alive;
            this.RoleInfo = "";
            this.TransColor = Color.white;
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

        public bool HasExtraRole(Roles.ExtraRole role)
        {
            return extraRole.Contains(role);
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

        public void Revive()
        {
            IsAlive = true;
            Status = PlayerStatus.Revived;
        }

        public void Die(PlayerStatus status, byte murderId)
        {
            IsAlive = false;

            /*
            if (role.hasFakeTask)
            {
                Helpers.allPlayersById()[id].clearAllTasks();
            }
            */

            Game.GameData.data.deadPlayers.Add(id, new DeadPlayerData(this, murderId));
            Status = status;
        }

        public void Die(PlayerStatus status)
        {
            Die(status,Byte.MaxValue);
        }

        public void Die()
        {
            Die(PlayerStatus.Dead,Byte.MaxValue);
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

    public class VentData
    {
        public Vent Vent { get; }
        //
        public int Id { get; }
        //
        public bool PreSealed,Sealed;

        public VentData(Vent vent)
        {
            this.Vent = vent;
            this.Id = vent.Id;
            PreSealed = false;
            Sealed = false;
        }

        public static implicit operator Vent(VentData vent)
        {
            return vent.Vent;
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

        public Dictionary<string,VentData> VentMap;
        public List<SystemTypes> Rooms;

        public float OriginalSpeed;

        public float Timer;

        public GameRule GameRule;

        public Module.TimeLimit LimitRenderer;

        public Module.CustomGameMode GameMode;

        public Ghost.Ghost Ghost;

        public Objects.CustomMessage CountDownMessage;

        public Roles.RoleAI.EstimationAI EstimationAI;

        public GameData()
        {
            players = new Dictionary<byte, PlayerData>();
            deadPlayers = new Dictionary<byte, DeadPlayerData>();
            myData = new MyPlayerData();
            TotalTasks = 0;
            CompleteTasks = 0;

            VentMap = new Dictionary<string, VentData>();

            OriginalSpeed =PlayerControl.LocalPlayer.MyPhysics.Speed;

            GameRule = new GameRule();
            GameMode = Module.CustomGameModes.GetGameMode(CustomOptionHolder.gameMode.getSelection());

            LimitRenderer = null;

            Ghost = null;
            CountDownMessage = null;

            EstimationAI = new Roles.RoleAI.EstimationAI();

            Timer = 300f;
        }

        //データを消去します。
        private void Clear()
        {
            if (LimitRenderer != null) LimitRenderer.Destroy();

            players.Clear();
            deadPlayers.Clear();

            foreach(PlayerControl player in PlayerControl.AllPlayerControls)
            {
                player.MyPhysics.Speed = OriginalSpeed;
            }
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

        public void LoadMapData()
        {
            foreach (Vent vent in ShipStatus.Instance.AllVents)
            {
                VentMap.Add(vent.gameObject.name, new VentData(vent));
            }

            Map.MapEditor.AddVents(PlayerControl.GameOptions.MapId);


            Rooms = new List<SystemTypes>();
            foreach(SystemTypes type in ShipStatus.Instance.FastRooms.Keys)
            {
                Rooms.Add(type);
            }
        }

        public VentData GetVentData(string name)
        {
            if (VentMap.ContainsKey(name))
            {
                return VentMap[name];
            }
            return null;
        }

        public VentData GetVentData(int Id)
        {
            foreach(VentData vent in VentMap.Values)
            {
                if (vent.Id == Id)
                {
                    return vent;
                }
            }
            return null;
        }

        public void TimerUpdate()
        {
            if (Timer > 0f) Timer -= Time.deltaTime;

            if (LimitRenderer != null) LimitRenderer.Update();
        }
    }
    
}
