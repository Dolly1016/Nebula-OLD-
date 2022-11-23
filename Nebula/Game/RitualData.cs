using Hazel;

namespace Nebula.Game
{
    public class RitualData
    {
        public enum RitualGameStage
        {
            MendingStage,
            EscapeStage
        }
        public class RitualPlayerData
        {
            public Vector2 SpawnPos;
            public int[] Perks;
        }

        public class TaskData {
            public byte id;
            public int num;
            public SystemTypes[] rooms;

            public TaskData(byte id,int num,SystemTypes[] rooms)
            {
                this.id = id;
                this.num = num;
                this.rooms = rooms;
            }

            public TaskData(MessageReader reader)
            {
                this.id = reader.ReadByte();
                this.num = reader.ReadInt32();
                byte n = reader.ReadByte();
                this.rooms = new SystemTypes[n];
                for(int i = 0; i < n; i++)
                {
                    this.rooms[i] = (SystemTypes)reader.ReadByte();
                }
            }

            public void Serialize(MessageWriter writer)
            {
                writer.Write(id);
                writer.Write(num);
                writer.Write((byte)rooms.Length);
                for(int i = 0; i < rooms.Length; i++)
                {
                    writer.Write((byte)rooms[i]);
                }
            }

            static public TaskData Deserialize(MessageReader reader)
            {
                return new TaskData(reader);
            }
        }

        public List<TaskData> TaskList;
        public Dictionary<byte, RitualPlayerData> PlayerData;
        //情報を受け取ったプレイヤーの人数
        public int ReceivedPlayers;
        //ゲームの進行度
        public RitualGameStage GameStage;

        public RitualData()
        {
            TaskList = new List<TaskData>();
            PlayerData = new Dictionary<byte, RitualPlayerData>();
            ReceivedPlayers = 1;
            GameStage = RitualGameStage.MendingStage;
        }

        public List<Map.RitualSpawnCandidate> GetRitualSpawnCandidates(List<Map.RitualSpawnCandidate> spawnCandidates, int num)
        {
            List<Map.RitualSpawnCandidate> list = new List<Map.RitualSpawnCandidate>();
            foreach(var c in spawnCandidates)
            {
                if (c.subPos!=null && c.subPos.Length + 1 >= num) list.Add(c);
            }
            return list;
        }

        public List<Map.RitualSpawnCandidate> GetRitualSpawnCandidates(List<Map.RitualSpawnCandidate> spawnCandidates, Vector2 pos,float distance)
        {
            List<Map.RitualSpawnCandidate> list = new List<Map.RitualSpawnCandidate>();
            foreach (var c in spawnCandidates)
            {
                if ((c.pos-pos).magnitude>distance) list.Add(c);
            }
            return list;
        }

        public void CheckAndSynchronize(bool isHost)
        {
            if (ReceivedPlayers < PlayerControl.AllPlayerControls.Count) return;

            if (TaskList.Count == 0)
            {
                if (!isHost) return;

                //ホストの場合全ての初期化をここで行う

                var mapData = Map.MapData.GetCurrentMapData();

                /* タスク初期化 ここから */

                HashSet<SystemTypes> usedSystemTypes=new HashSet<SystemTypes>();
                List<SystemTypes[]> unusedCandidates = new List<SystemTypes[]>(mapData.RitualRooms);
                List<SystemTypes[]> candidates = new List<SystemTypes[]>();
                int missions = (int)CustomOptionHolder.NumOfMissionsOption.getFloat();
                SystemTypes[] selected;
                int rnd;

                //複数部屋の候補が入りうる数
                int multiRooms = 2;

                for(int i = 0; i < missions; i++)
                {
                    candidates.Clear();

                    foreach(var arr in unusedCandidates)
                    {
                        if (multiRooms > 0 ? arr.Length > 1 : arr.Length == 1)
                            candidates.Add(arr);
                    }

                    if (candidates.Count > 0)
                    {
                        selected = candidates[NebulaPlugin.rnd.Next(candidates.Count)];
                    }
                    else
                    {
                        selected = unusedCandidates[NebulaPlugin.rnd.Next(unusedCandidates.Count)];
                    }

                    selected = selected.OrderBy(i => Guid.NewGuid()).ToArray();
                    TaskList.Add(new TaskData(0, (int)CustomOptionHolder.LengthOfMissionOption.getFloat(), selected));
                    unusedCandidates.Remove(selected);
                    unusedCandidates.RemoveAll((r) => selected.Any((s) => r.Contains(s)));
                    multiRooms--;

                    /* タスク初期化 ここまで */

                    /* スポーン位置抽選 ここから */

                    List<byte> killers = new List<byte>();
                    List<byte> crewmates=new List<byte>();
                    foreach (var p in Game.GameData.data.AllPlayers.Values)
                    {
                        if (p.role == Roles.Roles.RitualKiller)
                            killers.Add(p.id);
                        else
                            crewmates.Add(p.id);
                    }
                    killers = killers.OrderBy(e => Guid.NewGuid()).ToList();
                    crewmates = crewmates.OrderBy(e => Guid.NewGuid()).ToList();

                    //キラーのスポーン位置を設定
                    var spawnCandidates = GetRitualSpawnCandidates(mapData.RitualSpawnLocations, (killers.Count > 3) ? 3 : killers.Count);
                    var killerLoc = spawnCandidates[NebulaPlugin.rnd.Next(spawnCandidates.Count)];

                    //複数人がスポーンするスポーン位置の数
                    int multiplePlayersSpawner = PlayerControl.AllPlayerControls.Count / 4;

                    int index = -1;
                    foreach(byte killerId in killers)
                    {
                        if (index == -1) RegisterPlayerSpawnData(killerId, killerLoc.GetPos());
                        else RegisterPlayerSpawnData(killerId, killerLoc.subPos[index%killerLoc.subPos.Length].GetPos());
                        index++;
                    }

                    //クルーの湧きうる位置
                    var crewmatesSpawnCandidates = GetRitualSpawnCandidates(mapData.RitualSpawnLocations, killerLoc.pos, 12f);


                    var multiSpawnCandidates = GetRitualSpawnCandidates(mapData.RitualSpawnLocations, 2);
                    Map.RitualSpawnCandidate loc;

                    for(int n=0;n< multiplePlayersSpawner; n++)
                    {
                        if (multiSpawnCandidates.Count == 0) break;

                        loc=multiSpawnCandidates[NebulaPlugin.rnd.Next(multiSpawnCandidates.Count)];

                        if (crewmates.Count == 0) break;
                        
                        RegisterPlayerSpawnData(crewmates[0],loc.GetPos());
                        crewmates.RemoveAt(0);

                        for(int s = 0; s < loc.subPos.Length; s++)
                        {
                            if (crewmates.Count == 0) break;

                            RegisterPlayerSpawnData(crewmates[0], loc.subPos[s].GetPos());
                            crewmates.RemoveAt(0);
                        }
                        
                        crewmatesSpawnCandidates.Remove(loc);
                        multiSpawnCandidates.Remove(loc);
                    }

                    while (crewmates.Count > 0)
                    {
                        loc = crewmatesSpawnCandidates[NebulaPlugin.rnd.Next(crewmatesSpawnCandidates.Count)];
                        RegisterPlayerSpawnData(crewmates[0], loc.GetPos());

                        crewmates.RemoveAt(0);
                        crewmatesSpawnCandidates.Remove(loc);
                    }

                    /* スポーン位置抽選 ここまで */
                }
                RPCEventInvoker.InitializeRitualData(TaskList, PlayerData);
            }

            RPCEventInvoker.Synchronize(SynchronizeTag.RitualInitialize,PlayerControl.LocalPlayer.PlayerId);
        }

        public void RegisterPlayerData(byte PlayerId,int[] Perks)
        {
            if (!this.PlayerData.ContainsKey(PlayerId))
            {
                this.PlayerData[PlayerId] = new RitualPlayerData();
            }
            this.PlayerData[PlayerId].Perks = Perks;
            ReceivedPlayers++;
        }

        public void RegisterPlayerSpawnData(byte PlayerId, Vector2 SpawnPos)
        {
            if (!this.PlayerData.ContainsKey(PlayerId))
            {
                this.PlayerData[PlayerId] = new RitualPlayerData();
            }
            this.PlayerData[PlayerId].SpawnPos = SpawnPos;

        }

        public void AddTaskData(TaskData taskData)
        {
            TaskList.Add(taskData);
        }

        public void SpawnAllPlayers()
        {
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                Patches.RitualPatch.OnSpawn(p);
            }
        }
    }
}
