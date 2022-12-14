using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Nebula.Map;
public class ObjectData
{
    public string Name;
    public Vector2 Position;
    public SystemTypes Room;
    public float MaxTime;
    public float Distance;

    public ObjectData(string name, Vector2 pos, SystemTypes room, float time, float distance)
    {
        Name = name;
        Position = pos;
        Room = room;
        MaxTime = time;
        Distance = distance;
    }
}

public class SabotageData
{
    public SystemTypes Room { get; private set; }
    public Vector3 Position { get; private set; }
    public bool IsLeadingSabotage { get; private set; }
    public bool IsUrgent { get; private set; }

    public SabotageData(SystemTypes Room, Vector3 Position, bool IsLeadingSabotage, bool IsUrgent)
    {
        this.Room = Room;
        this.Position = Position;
        this.IsLeadingSabotage = IsLeadingSabotage;
        this.IsUrgent = IsUrgent;
    }
}

public class RitualSpawnCandidate
{
    public Vector2 pos;
    public float range;
    public RitualSpawnCandidate[]? subPos;

    public RitualSpawnCandidate(Vector2 pos, float range = 0f, RitualSpawnCandidate[]? subPos = null)
    {
        this.pos = pos;
        this.range = range;
        this.subPos = subPos;
    }

    public Vector2 GetPos()
    {
        if (range > 0)
        {
            float angle = (float)NebulaPlugin.rnd.NextDouble() * Mathf.PI * 2f;
            float range = (float)NebulaPlugin.rnd.NextDouble() * this.range;
            return pos + new Vector2(Mathf.Cos(angle) * range, Mathf.Sin(angle) * range);
        }
        return pos;
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

public class VectorRange
{
    float xMin, xMax, yMin, yMax;

    public VectorRange(float x, float y)
    {
        this.xMin = this.xMax = x;
        this.yMin = this.yMax = y;
    }

    public VectorRange(float x1, float x2, float y1, float y2)
    {
        if (x1 < x2)
        {
            this.xMin = x1;
            this.xMax = x2;
        }
        else
        {
            this.xMin = x2;
            this.xMax = x1;
        }

        if (y1 < y2)
        {
            this.yMin = y1;
            this.yMax = y2;
        }
        else
        {
            this.yMin = y2;
            this.yMax = y1;
        }
    }

    public Vector2 GetVector()
    {
        return new Vector2((float)NebulaPlugin.rnd.NextDouble() * (xMax - xMin) + xMin, (float)NebulaPlugin.rnd.NextDouble() * (yMax - yMin) + yMin);
    }
}

public class SpawnPointData
{
    public Vector2 spawnPoint;
    public string name;
    public Module.CustomOption option;

    public SpawnPointData(string name, Vector2 spawnPoint)
    {
        this.name = name;
        this.spawnPoint = spawnPoint;
    }

    static string[] mapNames = { "skeld", "mira", "polus", "undefined", "airship" };
    public void CreateOption(byte mapId)
    {
        option = Module.CustomOption.Create(Color.white, "option.spawnMethod.location." + name, true, CustomOptionHolder.spawnMethod, false, true).SetIdentifier("option.spawnMethod.location." + mapNames[mapId] + "." + name);
    }
}

public class MapData
{
    //Skeld=0,MIRA=1,Polus=2,AirShip=4

    public ShipStatus Assets;
    public int MapId { get; }

    public bool IsModMap { get; }

    public static Dictionary<int, MapData> MapDatabase = new Dictionary<int, MapData>();


    public Dictionary<SystemTypes, SabotageData> SabotageMap;

    //マップ内の代表点
    public HashSet<Vector2> MapPositions;

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

    //Opportunistのタスク対象となるオブジェクト
    public List<ObjectData> Objects;

    //Ritualスポーン位置候補
    public List<RitualSpawnCandidate> RitualSpawnLocations;
    //Ritualミッション部屋候補(ビットフラグタグと候補)
    public List<SystemTypes[]> RitualRooms;
    //Ritualミッション位置候補
    public Dictionary<SystemTypes, List<VectorRange>> RitualMissionPositions;
    //ランダムスポーン位置候補
    public List<SpawnPointData> SpawnPoints;

    public List<Vector2> ValidSpawnPoints
    {
        get
        {
            List<Vector2> list = new List<Vector2>();
            foreach (var sPoint in SpawnPoints)
                if (sPoint.option.getBool()) list.Add(sPoint.spawnPoint);

            return list;
        }
    }

    public void SetUpSpawnPointButton(GameObject obj, Action reopener)
    {
        foreach (var point in SpawnPoints)
        {
            NebulaPlugin.Instance.Logger.Print("1");
            PassiveButton button = Module.MetaScreen.MSDesigner.AddSubButton(obj, new Vector2(2.4f, 0.4f), "Point", point.option.getName(), point.option.getBool() ? Color.yellow : Color.white);
            button.transform.localPosition = (Vector3)ConvertMinimapPosition(point.spawnPoint) + new Vector3(0f, 0f, -5f);
            button.transform.localScale /= (obj.transform.localScale.x / 0.75f);

            SpriteRenderer renderer = button.GetComponent<SpriteRenderer>();
            TMPro.TextMeshPro text = button.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();

            renderer.size = new Vector2(text.preferredWidth + 0.3f, renderer.size.y);
            button.GetComponent<BoxCollider2D>().size = renderer.size;

            var option = point.option;
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                option.addSelection(1);
                reopener();
            }));

        }
    }

    public void SetUpSpawnPointInfo(GameObject obj)
    {
        switch (CustomOptionHolder.spawnMethod.getSelection())
        {
            case 2:
                foreach (var vec in ValidSpawnPoints)
                {
                    var text = Module.MetaScreen.MSDesigner.AddSubText(obj, 0.4f, 2f, "★", TMPro.FontStyles.Bold, TMPro.TextAlignmentOptions.Center);
                    text.color = Color.yellow;
                    text.transform.localPosition = (Vector3)ConvertMinimapPosition(vec) + new Vector3(0f, 0f, -5f);
                    text.transform.localScale /= (obj.transform.localScale.x / 0.75f);

                }
                break;
            case 1:
                foreach (var point in SpawnCandidates)
                {
                    var text = Module.MetaScreen.MSDesigner.AddSubText(obj, 0.4f, 2f, "★", TMPro.FontStyles.Bold, TMPro.TextAlignmentOptions.Center);
                    text.color = Color.yellow;
                    text.transform.localPosition = (Vector3)ConvertMinimapPosition(point.SpawnLocation) + new Vector3(0f, 0f, -5f);
                    text.transform.localScale /= (obj.transform.localScale.x / 0.75f);

                }
                break;
        }
    }

    public void RegisterRitualMissionPosition(SystemTypes room, VectorRange range)
    {
        if (!RitualMissionPositions.ContainsKey(room))
        {
            List<VectorRange> list = new List<VectorRange>();
            list.Add(range);
            RitualMissionPositions.Add(room, list);
        }
        else
        {
            RitualMissionPositions[room].Add(range);
        }
    }

    public void RegisterObjectPosition(string objectName, Vector2 pos, SystemTypes room, float maxTime, float distance = 0.6f)
    {
        Objects.Add(new ObjectData(objectName, pos, room, maxTime, distance));
    }


    public static void Load()
    {
        new Database.SkeldData();
        new Database.MIRAData();
        new Database.PolusData();
        new Database.AirshipData();
        new MapData(5);
    }

    public static void CreateOptionData()
    {
        foreach (var mapData in MapDatabase)
        {
            foreach (var point in mapData.Value.SpawnPoints)
            {
                point.CreateOption((byte)mapData.Value.MapId);
            }
        }
    }
    public static Map.MapData GetCurrentMapData()
    {
        if (MapDatabase.ContainsKey(GameOptionsManager.Instance.CurrentGameOptions.MapId))
        {
            return MapDatabase[GameOptionsManager.Instance.CurrentGameOptions.MapId];
        }
        else
        {
            return MapDatabase[5];
        }
    }

    public bool isOnTheShip(Vector2 pos)
    {
        int num = Physics2D.OverlapCircleNonAlloc(pos, 0.23f, PhysicsHelpers.colliderHits, Constants.ShipAndAllObjectsMask);
        if (num > 0)
        {
            for (int i = 0; i < num; i++)
            {
                if (!PhysicsHelpers.colliderHits[i].isTrigger) return false;
            }
        }

        Vector2 vector;
        float magnitude;

        foreach (Vector2 p in MapPositions)
        {
            vector = p - pos;
            magnitude = vector.magnitude;
            if (magnitude < 12.0f)
            {
                if (!PhysicsHelpers.AnyNonTriggersBetween(pos, vector.normalized, magnitude, Constants.ShipAndAllObjectsMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public int isOnTheShip_Debug(Vector2 pos)
    {
        int num = 0;

        Vector2 vector;
        float magnitude;

        foreach (Vector2 p in MapPositions)
        {
            vector = p - pos;
            magnitude = vector.magnitude;

            if (magnitude < 12.0f)
            {
                if (!PhysicsHelpers.AnyNonTriggersBetween(pos, vector.normalized, magnitude, Constants.ShipAndAllObjectsMask))
                {
                    num++;
                }
            }
        }
        return num;
    }

    public int OutputMap(Vector2 pos, Vector2 size, string fileName)
    {
        int x1, y1, x2, y2;
        x1 = (int)(pos.x * 10);
        y1 = (int)(pos.y * 10);
        x2 = x1 + (int)(size.x * 10);
        y2 = y1 + (int)(size.y * 10);
        int temp;
        if (x1 > x2)
        {
            temp = x1;
            x1 = x2;
            x2 = temp;
        }
        if (y1 > y2)
        {
            temp = y1;
            y1 = y2;
            y2 = temp;
        }

        Color color = new Color(40 / 255f, 40 / 255f, 40 / 255f);
        var texture = new Texture2D(x2 - x1, y2 - y1, TextureFormat.RGB24, false);

        int num;
        int r = 0;
        for (int y = y1; y < y2; y++)
        {
            for (int x = x1; x < x2; x++)
            {
                num = isOnTheShip_Debug(new Vector2(((float)x) / 10f, ((float)y) / 10f));
                //if (num > 20) num = 20;
                texture.SetPixel(x - x1, y - y1, (num == 0) ? color : new Color((num > 1 ? 100 : 0) / 255f, (150 + (num * 5)) / 255f, 0));
                if (num > 0) r++;
            }
        }

        texture.Apply();

        byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(texture));
        //保存
        File.WriteAllBytes(fileName + ".png", bytes);

        return r;
    }

    public MapData(int mapId)
    {
        MapId = mapId;
        MapDatabase[mapId] = this;

        IsModMap = mapId >= 5;

        SabotageMap = new Dictionary<SystemTypes, SabotageData>();
        MapPositions = new HashSet<Vector2>();

        SpawnCandidates = new List<SpawnCandidate>();
        SpawnOriginalPositionAtFirst = false;

        DoorHackingCanBlockSabotage = false;

        HasDefaultPrespawnMinigame = false;

        MapScale = 1f;
        DoorHackingDuration = 10f;

        Objects = new List<ObjectData>();

        RitualRooms = new List<SystemTypes[]>();
        RitualSpawnLocations = new List<RitualSpawnCandidate>();
        RitualMissionPositions = new Dictionary<SystemTypes, List<VectorRange>>();

        SpawnPoints = new List<SpawnPointData>();
    }

    public void LoadAssets(AmongUsClient __instance)
    {
        if (IsModMap) return;

        AssetReference assetReference = __instance.ShipPrefabs.ToArray()[MapId];
        AsyncOperationHandle<GameObject> asset = assetReference.LoadAsset<GameObject>();
        asset.WaitForCompletion();
        Assets = assetReference.Asset.Cast<GameObject>().GetComponent<ShipStatus>();
    }

    public Sprite GetMapSprite()
    {
        return Assets.MapPrefab.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().sprite;
    }

    public Material GetMapMaterial()
    {
        return new Material(Assets.MapPrefab.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().material);
    }

    public Vector2 ConvertMinimapPosition(Vector2 pos)
    {
        return (Vector2)(pos / Assets.MapScale) + (Vector2)Assets.MapPrefab.transform.GetChild(5).localPosition;
    }


    public bool PlayInitialPrespawnMinigame
    {
        get
        {
            if (HasDefaultPrespawnMinigame) return true;

            return (SpawnCandidates.Count >= 3 && !SpawnOriginalPositionAtFirst && CustomOptionHolder.mapOptions.getBool() && CustomOptionHolder.spawnMethod.getSelection() == 1);
        }
    }
}