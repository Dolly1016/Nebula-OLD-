using Nebula.Roles;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Nebula.Utilities;
using BepInEx.IL2CPP.Utils.Collections;

using static GameData;
using Hazel;

namespace Nebula.Game
{

    public enum SynchronizeTag
    {
        PreSpawnMinigame,
        RitualInitialize
    }
    public class SynchronizeData
    {
        private Dictionary<SynchronizeTag, ulong> dic;

        public SynchronizeData()
        {
            dic = new Dictionary<SynchronizeTag, ulong>();
        }

        public void Synchronize(SynchronizeTag tag,byte playerId)
        {
            if (!dic.ContainsKey(tag)) dic[tag] = 0;

            dic[tag] |= (ulong)1 << playerId;
        }

        private IEnumerator GetAlignEnumerator(SynchronizeTag tag, bool withGhost, bool withSurvivor = true)
        {
            while (!Align(tag, withGhost, withSurvivor))
            {
                yield return null;
            }
        }

        public Il2CppSystem.Collections.IEnumerator GetAlignEnumeratorIl2Cpp(SynchronizeTag tag, bool withGhost, bool withSurvivor = true)
        {
            return GetAlignEnumerator(tag, withGhost, withSurvivor).WrapToIl2Cpp();
        }

        static private IEnumerator GetStaticAlignEnumerator(SynchronizeTag tag, bool withGhost, bool withSurvivor = true)
        {
            while (Game.GameData.data == null)
            {
                yield return null;
            }
            yield return Game.GameData.data.SynchronizeData.GetAlignEnumerator(tag, withGhost, withSurvivor);
        }

        static public Il2CppSystem.Collections.IEnumerator GetStaticAlignEnumeratorIl2Cpp(SynchronizeTag tag, bool withGhost, bool withSurvivor = true)
        {
            return GetStaticAlignEnumerator(tag, withGhost, withSurvivor).WrapToIl2Cpp();
        }

        public bool Align(SynchronizeTag tag, bool withGhost, bool withSurvivor = true)
        {
            bool result = true;

            ulong value = 0;
            dic.TryGetValue(tag, out value);

            foreach(PlayerControl pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (pc.Data.IsDead ? withGhost : withSurvivor)
                    result &= ((value & ((ulong)1 << pc.PlayerId)) != 0);
            }

            return result;
        }

        public void Reset(SynchronizeTag tag)
        {
            dic[tag] = 0;
        }

        public void Initialize()
        {
            dic.Clear();
        }
    }

    public class MyPlayerData
    {
        public PlayerControl currentTarget { get; set; }
        private PlayerData globalData { get; set; }
        public bool CanSeeEveryoneInfo { get; set; }
        public float VentDurationTimer { get; set; }
        public float VentCoolDownTimer { get; set; }
        public List<TaskInfo> InitialTasks { get; set; }

        public PlayerData getGlobalData()
        {
            if (globalData == null)
            {
                if (Game.GameData.data.AllPlayers.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                {
                    globalData = Game.GameData.data.playersArray[PlayerControl.LocalPlayer.PlayerId];
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
            this.Elapsed = 0f;
            if (Helpers.allPlayersById().ContainsKey(playerData.id))
            {
                this.deathLocation = Helpers.allPlayersById()[playerData.id].transform.position;

                List<SystemTypes> candidate = new List<SystemTypes>();
                float dis = Roles.Roles.Necromancer.maxReviveRoomDistanceOption.getFloat(), nearestDis = 100f;
                SystemTypes? nearest = null;
                foreach (var room in Game.GameData.data.Rooms)
                {
                    if (room == SystemTypes.Ventilation) continue;

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
            else
            {
                this.deathLocation = new Vector3(0, 0);
                this.RespawnRoom = ShipStatus.Instance.AllRooms[0].RoomId;
            }


            
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
            if (num == 0) return;

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
            int tasks;
            if (Game.GameModeProperty.GetProperty(Game.GameData.data.GameMode).CountTasks) {
                tasks = PlayerControl.GameOptions.NumCommonTasks + PlayerControl.GameOptions.NumShortTasks + PlayerControl.GameOptions.NumLongTasks; ;
            }
            else
            {
                tasks = 0;
            }

            if (hasFakeTask && !fakeTaskIsExecutable) Quota = 0;
            else Quota = tasks;

            AllTasks = Quota;
            DisplayTasks = Quota;
            Completed = 0;
        }
    }

    public class PlayerProperty
    {
        private PlayerControl player { get; }
        private bool underTheFloor { get; set; }

        public bool CanTransmitWalls { get; set; }
        public bool UnderTheFloor { 
            get {
                return underTheFloor;
            } 
            set {
                if (underTheFloor != value)
                {
                    if (underTheFloor)
                        Gush();
                    else
                        Dive();
                }
            }
        }

        public void SetUnderTheFloorForcely(bool flag)
        {
            underTheFloor = flag;
        }

        private void Dive()
        {
            List<Il2CppSystem.Collections.IEnumerator> sequence = new List<Il2CppSystem.Collections.IEnumerator>();

            sequence.Add(Effects.Action(new System.Action(() =>
            {
                player.MyPhysics.body.velocity = Vector2.zero;
                if (player.AmOwner)
                    player.MyPhysics.inputHandler.enabled = true;
                player.cosmetics.skin.SetEnterVent(player.cosmetics.FlipX);
                player.MyPhysics.Animator.Play(player.MyPhysics.CurrentAnimationGroup.EnterVentAnim, 1f);
                player.MyPhysics.Animator.Time = 0f;
                player.moveable = false;

                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId && player.GetModData().role == Roles.Roles.Hadar)
                {
                    Objects.SoundPlayer.PlaySound(Module.AudioAsset.HadarDive);
                }
            })));
            sequence.Add(Effects.Wait(player.MyPhysics.CurrentAnimationGroup.EnterVentAnim.length));
            sequence.Add(Effects.Action(new System.Action(() =>
            {
                if (player.AmOwner)
                    player.MyPhysics.inputHandler.enabled = false;
                player.MyPhysics.myPlayer.Visible = false;
                player.cosmetics.skin.SetIdle(player.cosmetics.FlipX);
                player.MyPhysics.Animator.Play(player.MyPhysics.CurrentAnimationGroup.IdleAnim, 1f);
                player.moveable = true;
                underTheFloor = true;
            })));

            var refArray = new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Collections.IEnumerator>(sequence.ToArray());
            player.MyPhysics.StopAllCoroutines();
            player.MyPhysics.StartCoroutine(Effects.Sequence(refArray));
        }

        private void Gush()
        {
            List<Il2CppSystem.Collections.IEnumerator> sequence = new List<Il2CppSystem.Collections.IEnumerator>();

            sequence.Add(Effects.Action(new System.Action(() =>
            {
                player.MyPhysics.body.velocity = Vector2.zero;
                if (player.AmOwner)
                    player.MyPhysics.inputHandler.enabled = true;
                player.cosmetics.skin.SetExitVent(player.cosmetics.FlipX);
                player.MyPhysics.Animator.Play(player.MyPhysics.CurrentAnimationGroup.ExitVentAnim, 1f);
                player.MyPhysics.Animator.Time = 0f;
                player.moveable = false;
                player.MyPhysics.myPlayer.Visible = true;
                underTheFloor = false;

                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId && player.GetModData().role==Roles.Roles.Hadar)
                {
                    Objects.SoundPlayer.PlaySound(Module.AudioAsset.HadarReappear);
                }
            })));
            sequence.Add(Effects.Wait(player.MyPhysics.CurrentAnimationGroup.ExitVentAnim.length));
            sequence.Add(Effects.Action(new System.Action(() =>
            {
                if (player.AmOwner)
                    player.MyPhysics.inputHandler.enabled = false;
                player.cosmetics.skin.SetIdle(player.cosmetics.FlipX);
                player.MyPhysics.Animator.Play(player.MyPhysics.CurrentAnimationGroup.IdleAnim, 1f);
                player.moveable = true;
            })));

            var refArray = new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Collections.IEnumerator>(sequence.ToArray());
            player.MyPhysics.StopAllCoroutines();
            player.MyPhysics.StartCoroutine(Effects.Sequence(refArray));
        }

        public PlayerProperty(PlayerControl player)
        {
            CanTransmitWalls = false;
            UnderTheFloor = false;

            this.player = player;
        }
    }

    public class GuardStatus
    {
        byte myPlayerId;
        HashSet<byte> Guardians;
        int SingleUseGuardsNum;

        public GuardStatus(byte playerId)
        {
            Guardians = new HashSet<byte>();
            SingleUseGuardsNum = 0;
            myPlayerId = playerId;
        }

        public void AddGuardian(byte playerId)
        {
            Guardians.Add(playerId);
        }

        public void RemoveGuardian(byte playerId)
        {
            Guardians.Remove(playerId);
        }

        public void AddSingleUseGuardian(int num)
        {
            SingleUseGuardsNum += num;
        }

        public bool RPCGuard()
        {
            foreach(byte p in Guardians)
            {
                if (!Helpers.playerById(p).Data.IsDead) return true;
            }
            if (SingleUseGuardsNum > 0)
            {
                RPCEventInvoker.ConsumeSingleUseGuard(myPlayerId, 1);
                return true;
            }
            return false;
        }
    }

    public class PlayerData
    {
        public class CosmicPartTimer
        {
            public float Timer { get; set; } = 0f;
            public int Index { get; set; } = 0;
        }

        public class CosmicTimer
        {
            public CosmicPartTimer Visor { get; }
            public CosmicPartTimer Hat { get; }

            public CosmicTimer()
            {
                Visor = new CosmicPartTimer();
                Hat = new CosmicPartTimer();
            }
        }

        static private Dictionary<byte, CosmicTimer> Cosmic = new Dictionary<byte, CosmicTimer>();

        static public CosmicTimer GetCosmicTimer(byte playerId)
        {
            if (!Cosmic.ContainsKey(playerId)) Cosmic[playerId] = new CosmicTimer();
            return Cosmic[playerId];
        }

        public class PlayerStatus
        {
            static private byte AvailableId = 0;
            static Dictionary<byte, PlayerStatus> StatusMap = new System.Collections.Generic.Dictionary<byte, PlayerStatus>();

            public static PlayerStatus Alive = new PlayerStatus("alive");
            public static PlayerStatus Revived = new PlayerStatus("revived");
            public static PlayerStatus Dead = new PlayerStatus("dead");
            public static PlayerStatus Exiled = new PlayerStatus("exiled");
            public static PlayerStatus Disconnected = new PlayerStatus("disconnected");
            public static PlayerStatus Pseudocide = new PlayerStatus("pseudocide");
            public static PlayerStatus Suicide = new PlayerStatus("suicide");
            public static PlayerStatus Burned=new PlayerStatus("burned");
            public static PlayerStatus Embroiled = new PlayerStatus("embroiled");
            public static PlayerStatus Guessed = new PlayerStatus("guessed");
            public static PlayerStatus Misguessed = new PlayerStatus("misguessed");
            public static PlayerStatus Trapped = new PlayerStatus("trapped");
            public static PlayerStatus Sniped = new PlayerStatus("sniped");
            public static PlayerStatus Beaten = new PlayerStatus("beaten");
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
            public int Priority { get; }
            public int ColorId { get; }
            public string HatId { get; }
            public string VisorId { get; }
            public string SkinId { get; }
            public string PetId { get; }
            public string? Name { get; set; }

            public PlayerOutfitData(int priority, string? name,int color,string hat,string visor,string skin,string pet)
            {
                Priority = priority;
                Name = name;
                ColorId = color;
                HatId = hat;
                VisorId = visor;
                SkinId = skin;
                PetId = pet;
            }

            public PlayerOutfitData(int priority,PlayerOutfit outfit)
            {
                Priority = priority;
                Name = outfit.PlayerName;
                ColorId = outfit.ColorId;
                HatId = outfit.HatId;
                VisorId = outfit.VisorId;
                SkinId = outfit.SkinId;
                PetId = outfit.PetId;
            }

            public PlayerOutfitData Clone(int priority)
            {
                return new PlayerOutfitData(priority,Name,ColorId,HatId,VisorId,SkinId,PetId);
            }

            public PlayerOutfitData(int priority, string name, PlayerOutfitData outfit)
            {
                Priority = priority;
                Name = name;
                ColorId = outfit.ColorId;
                HatId = outfit.HatId;
                VisorId = outfit.VisorId;
                SkinId = outfit.SkinId;
                PetId = outfit.PetId;
            }

            public void Serialize(MessageWriter writer)
            {
                writer.Write(Priority);
                writer.Write(Name);
                writer.Write(ColorId);
                writer.Write(HatId);
                writer.Write(VisorId);
                writer.Write(SkinId);
                writer.Write(PetId);
            }

            public PlayerOutfitData(MessageReader reader)
            {
                Priority = reader.ReadInt32();
                Name = reader.ReadString();
                ColorId = reader.ReadInt32();
                HatId = reader.ReadString();
                VisorId = reader.ReadString();
                SkinId = reader.ReadString();
                PetId = reader.ReadString();
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
        public List<PlayerOutfitData> AllOutfits { get; private set; }
        public string currentName { get; set; }
        public byte dragPlayerId { get; set; }

        public SpeedFactorManager Speed { get; }
        public PlayerAttributeFactorManager Attribute { get; }
        public Color TransColor { get; set; }

        public float MouseAngle { get; set; }

        public TaskData Tasks { get; }

        public PlayerStatus Status { get; set; }

        public string RoleInfo { get; set; }

        public PlayerProperty Property { get; set; }

        public float DeathGuage { get; set; }

        //状態として、何らかの理由で見えないプレイヤーであるかどうか
        public bool isInvisiblePlayer { get; set; }

        //役職遍歴
        public List<Tuple<string,string>> roleHistory { get; private set; }

        public GuardStatus guardStatus { get; private set; }

        public Patches.FinalPlayerData.FinalPlayer? FinalData { get {
                return Patches.OnGameEndPatch.FinalData.players.FirstOrDefault((p) => p.id == id);
        }}

        public PlayerData(PlayerControl player, string name,PlayerOutfit outfit,Role role)
        {
            
            this.id = player.PlayerId;
            this.name = name;
            this.role = role;
            this.extraRole = new HashSet<ExtraRole>();
            this.roleData = new Dictionary<int, int>();
            this.extraRoleData = new Dictionary<byte, ulong>();
            this.IsAlive = true;

            this.CurrentOutfit = this.Outfit = new PlayerOutfitData(0, outfit);
            this.AllOutfits = new List<PlayerOutfitData>();
            AddOutfit(this.Outfit);

            this.currentName = name;
            this.dragPlayerId = Byte.MaxValue;
            this.Speed = new SpeedFactorManager(player.PlayerId) ;
            this.Attribute = new PlayerAttributeFactorManager(player.PlayerId);
            this.Tasks = new TaskData(role.side == Roles.Side.Impostor || role.HasFakeTask, role.FakeTaskIsExecutable);
            this.MouseAngle = 0f;
            this.Status = PlayerStatus.Alive;
            this.RoleInfo = "";
            this.TransColor = Color.white;
            this.Property = new PlayerProperty(player);
            this.DeathGuage = 0f;
            this.isInvisiblePlayer = false;
            this.roleHistory=new List<Tuple<string, string>>();
            this.guardStatus = new GuardStatus(id);
            
        }

        public void RemoveOutfit(PlayerOutfitData outfit)
        {
            this.AllOutfits.Remove(outfit);
            UpdateOutfit();
        }

        public void AddOutfit(PlayerOutfitData outfit)
        {
            this.AllOutfits.Add(outfit);
            UpdateOutfit();
        }

        public void UpdateOutfit()
        {
            AllOutfits = new List<PlayerOutfitData>(this.AllOutfits.OrderBy((outfit) => outfit.Priority));
            CurrentOutfit = AllOutfits[AllOutfits.Count-1];
            Helpers.playerById(id).SetLook(CurrentOutfit);
        }

        public PlayerOutfitData GetOutfitData(int maxPriority)
        {
            for(int i = AllOutfits.Count - 1; i >= 0; i--)
            {
                if (AllOutfits[i].Priority <= maxPriority) return AllOutfits[i];
            }
            return AllOutfits[0];
        }

        public void AddRoleHistory()
        {
            string shortRole = Helpers.cs(this.role.Color, Language.Language.GetString("role." + this.role.LocalizeName + ".short"));
            string role = Helpers.cs(this.role.Color, Language.Language.GetString("role." + this.role.LocalizeName + ".name"));
            Helpers.RoleAction(this, (r) =>
             {
                 r.EditDisplayNameForcely(this.id, ref shortRole);
                 r.EditDisplayNameForcely(this.id, ref role);
             });
            Helpers.RoleAction(this, (r) =>
            {
                r.EditDisplayRoleNameForcely(this.id, ref shortRole);
                r.EditDisplayRoleNameForcely(this.id, ref role);
            });
            //同じものは重ねて登録しない
            if (roleHistory.Count > 0 && roleHistory[roleHistory.Count - 1].Item1 == role) return;
            roleHistory.Add(new Tuple<string,string>(role,shortRole));
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

        public void Revive(bool changeStatus = true)
        {
            DeathGuage = 0f;
            IsAlive = true;

            if (changeStatus) Status = PlayerStatus.Revived;
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
            
            Game.GameData.data.deadPlayers[id] = new DeadPlayerData(this, murderId);
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

    public class UtilityTimer
    {
        public float AdminTimer { get; set; }
        public float VitalsTimer { get; set; }
        public float CameraTimer { get; set; }

        public UtilityTimer()
        {
            AdminTimer = CustomOptionHolder.AdminLimitOption.getFloat();
            VitalsTimer = CustomOptionHolder.VitalsLimitOption.getFloat();
            CameraTimer = CustomOptionHolder.CameraAndDoorLogLimitOption.getFloat();
        }

        /// <summary>
        /// ミーティング終了時のリセット
        /// </summary>
        public void OnMeetingEnd()
        {
            if (CustomOptionHolder.DevicesOption.getSelection() != 1) return;

            AdminTimer = CustomOptionHolder.AdminLimitOption.getFloat();
            VitalsTimer = CustomOptionHolder.VitalsLimitOption.getFloat();
            CameraTimer = CustomOptionHolder.CameraAndDoorLogLimitOption.getFloat();
        }
    }

    public class GameData
    {
        public static GameData? data = null;

        private static Dictionary<string, int> RoleDataIdMap=new Dictionary<string, int>();

        public Dictionary<byte, PlayerData> AllPlayers;
        //アクセスまでの時間を短縮する
        public List<PlayerData?> playersArray;

        public Dictionary<byte, DeadPlayerData> deadPlayers;

        public MyPlayerData myData;

        public int TotalTasks, CompleteTasks;

        public Dictionary<string,VentData> VentMap;
        public List<SystemTypes> Rooms;

        public float OriginalSpeed;

        public float Timer;

        public GameRule GameRule;

        //ゲームモード
        public Module.CustomGameMode GameMode;

        //ゴースト
        public Ghost.Ghost? Ghost;

        //ミニゲーム開始時のカウントダウン
        public Objects.CustomMessage CountDownMessage;

        //Oracleの役職絞り込み
        public Roles.RoleAI.EstimationAI EstimationAI;

        //情報端末タイマー
        public UtilityTimer UtilityTimer;

        //同期用のデータ
        public SynchronizeData SynchronizeData;

        //当たり判定
        public Objects.ColliderManager ColliderManager;

        //Ritualモード
        public RitualData RitualData;

        public bool IsCanceled;

        public GameData()
        {
            AllPlayers = new Dictionary<byte, PlayerData>();
            playersArray = new List<PlayerData>();

            deadPlayers = new Dictionary<byte, DeadPlayerData>();
            myData = new MyPlayerData();
            TotalTasks = 0;
            CompleteTasks = 0;

            VentMap = new Dictionary<string, VentData>();

            OriginalSpeed = 2.5f;

            GameRule = new GameRule();
            GameMode = Module.CustomGameModes.GetGameMode(CustomOptionHolder.gameMode.getSelection());

            Ghost = null;
            CountDownMessage = null;

            EstimationAI = new Roles.RoleAI.EstimationAI();

            UtilityTimer = new UtilityTimer();

            SynchronizeData = new SynchronizeData();
            ColliderManager = new Objects.ColliderManager();

            Timer = 300f;

            IsCanceled = false;

            RitualData = new RitualData();
        }

        //データを消去します。
        private void Clear()
        {
            Module.Information.UpperInformationManager.RemoveAll();

            AllPlayers.Clear();
            playersArray.Clear();
            deadPlayers.Clear();

            foreach(PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
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
            
            foreach(PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (player.PlayerId == playerId)
                {
                    name = player.name;
                    outfit = player.CurrentOutfit;
                }
            }

            var data = new PlayerData(Helpers.playerById(playerId), name, outfit, role);
            AllPlayers.Add(playerId, data);
            while(playersArray.Count <= playerId)
            {
                playersArray.Add(null);
            }
            playersArray[playerId] = data;
        }

        public void LoadMapData()
        {
            foreach (Vent vent in ShipStatus.Instance.AllVents)
            {
                VentMap.Add(vent.gameObject.name, new VentData(vent));
            }

            Map.MapEditor.FixTasks(PlayerControl.GameOptions.MapId);


            if (CustomOptionHolder.mapOptions.getBool())
            {
                Map.MapEditor.OptimizeMap(PlayerControl.GameOptions.MapId);
                Map.MapEditor.AddVents(PlayerControl.GameOptions.MapId);
                Map.MapEditor.MapCustomize(PlayerControl.GameOptions.MapId);
            }

            if (CustomOptionHolder.TasksOption.getBool())
            {
                Map.MapEditor.AddWirings(PlayerControl.GameOptions.MapId);
            }

            Rooms = new List<SystemTypes>();
            foreach(SystemTypes type in ShipStatus.Instance.FastRooms.Keys)
            {
                Rooms.Add(type);
            }
        }

        public void ModifyShipStatus()
        {
            if (CustomOptionHolder.mapOptions.getBool())
            {
                Map.MapEditor.ModifySabotage(PlayerControl.GameOptions.MapId);
            }
        }

        public PlayerData? GetPlayerData(byte playerId)
        {
            if (playersArray.Count <= playerId) return null;
            return playersArray[playerId];
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
            if (Timer > 0f)
            {
                if(!ExileController.Instance)Timer -= Time.deltaTime;
            }
        }
    }
    
}
