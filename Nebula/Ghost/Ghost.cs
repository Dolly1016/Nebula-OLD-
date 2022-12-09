namespace Nebula.Ghost;

public class Ghost
{
    //0fを基準に1fを標準の値とする
    public Dictionary<SystemTypes, float> SabotageMood;
    public List<SystemTypes> SabotageKeys;
    //0fを基準に1fを標準の値とする
    public Dictionary<SystemTypes, float> DoorMood;
    public List<SystemTypes> DoorKeys;
    //0fを基準に1fを標準の値とする
    public float Urgency;

    //各種クールダウン
    public float SabotageCoolDown;
    public Dictionary<SystemTypes, float> DoorCoolDown;

    //サボタージュ更新間隔
    protected float SabotageUpdateDuration, SabotageUpdateDurationWidth;
    private float SabotageUpdateCoolDown;



    public bool InSabotage { get; private set; }

    public Vector3 Position;

    HashSet<GhostAI> AISet;

    static HashSet<Func<Ghost>> AllGhosts = new HashSet<Func<Ghost>>();

    static public void Load()
    {
        AllGhosts.Add(() => new Ghosts.TestGhost());
    }

    private void CalcAI()
    {
        uint priority = 0;
        uint nextPriority;

        //優先度の値が小さいものから実行する
        do
        {
            nextPriority = uint.MaxValue;

            foreach (GhostAI ai in AISet)
            {
                if (ai.Priority == priority)
                {
                    ai.Update(this);
                }
                else if (ai.Priority > priority && ai.Priority < nextPriority)
                {
                    nextPriority = ai.Priority;
                }
            }

            priority = nextPriority;

        } while (nextPriority != uint.MaxValue);
    }

    //サボタージュ更新
    private void SabotageUpdate()
    {
        //会議中は何もしない
        if (MeetingHud.Instance != null) return;
        //追放中もなにもしない
        if (ExileController.Instance != null && SpawnInMinigame.Instance == null) return;

        if (!InSabotage)
        {
            if (SabotageCoolDown < 0f)
            {
                float maximum = SabotageMood.Max(entry => entry.Value);
                if (maximum > 1f)
                {
                    KeyValuePair<SystemTypes, float>? sabotage = SabotageMood.FirstOrDefault(entry => entry.Value == maximum);
                    if (sabotage != null)
                    {
                        Agent.SabotageManager.BeginSabotage(sabotage.Value.Key);
                        SabotageCoolDown = 20f;
                        InSabotage = true;
                    }
                }
            }
        }

        //ドア閉鎖
        if (!InSabotage || !Map.MapData.GetCurrentMapData().DoorHackingCanBlockSabotage)
        {
            foreach (var room in DoorMood)
            {
                NebulaPlugin.Instance.Logger.Print("Mood:" + room.Value + ", CoolDown:" + DoorCoolDown[room.Key]);
                if (room.Value > 1f && DoorCoolDown[room.Key] < 0f)
                {
                    Agent.SabotageManager.BeginDoorSabotage(room.Key);
                    DoorCoolDown[room.Key] = 30f;
                    if (Map.MapData.GetCurrentMapData().DoorHackingCanBlockSabotage)
                        if (SabotageCoolDown < 12f) SabotageCoolDown = 12f;
                }
            }
        }
    }


    public virtual void Update()
    {
        foreach (var room in SabotageKeys)
        {
            SabotageMood[room] = 0f;
        }
        foreach (var room in DoorKeys)
        {
            DoorMood[room] = 0f;
        }
        Urgency = 0f;

        /* 値のリセットここまで */

        /* 情報の更新 */
        InSabotage = Agent.SabotageManager.ExistAnySabotages();
        if (!InSabotage) SabotageCoolDown -= Time.deltaTime;


        foreach (var room in DoorKeys)
        {
            DoorCoolDown[room] -= Time.deltaTime;
        }

        //AIの計算
        CalcAI();

        //サボタージュ
        SabotageUpdateCoolDown -= Time.deltaTime;
        if (SabotageUpdateCoolDown < 0f)
        {
            SabotageUpdate();

            SabotageUpdateCoolDown = SabotageUpdateDuration + ((float)NebulaPlugin.rnd.NextDouble() - 0.5f) * SabotageUpdateDurationWidth;
        }
    }

    protected void AddAI(GhostAI ghostAI)
    {
        AISet.Add(ghostAI);
    }

    public Ghost()
    {
        SabotageMood = new Dictionary<SystemTypes, float>();
        DoorMood = new Dictionary<SystemTypes, float>();
        AISet = new HashSet<GhostAI>();
        DoorCoolDown = new Dictionary<SystemTypes, float>();

        SabotageCoolDown = 20f;
        InSabotage = false;

        Map.MapData map = Map.MapData.MapDatabase[GameOptionsManager.Instance.CurrentGameOptions.MapId];

        foreach (var entry in map.SabotageMap)
        {
            SabotageMood[entry.Key] = 0f;
        }


        SabotageKeys = new List<SystemTypes>(SabotageMood.Keys);
        DoorKeys = new List<SystemTypes>(DoorMood.Keys);

        //デフォルトの更新間隔
        SabotageUpdateDuration = 1.2f;
        SabotageUpdateDurationWidth = 0.7f;
        //最初12秒間の猶予がある
        SabotageUpdateCoolDown = 12f;
    }
}