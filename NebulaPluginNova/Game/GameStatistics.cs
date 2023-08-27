using Il2CppInterop.Runtime.Injection;
using Nebula.Modules;
using System.Collections;

namespace Nebula.Game;

[NebulaPreLoad]
public static class EventDetail
{
    static public TranslatableTag Kill = new("statistics.events.kill");
    static public TranslatableTag Exiled = new("statistics.events.exiled");
    static public TranslatableTag Misfire = new("statistics.events.misfire");
    static public TranslatableTag GameStart = new("statistics.events.startGame");
    static public TranslatableTag GameEnd = new("statistics.events.endGame");
    static public TranslatableTag MeetingEnd = new("statistics.events.endMeeting");
    static public TranslatableTag Report = new("statistics.events.report");
    static public TranslatableTag BaitReport = new("statistics.events.baitReport");
    static public TranslatableTag EmergencyButton = new("statistics.events.emergency");
    static public TranslatableTag MayorButton = new("statistics.events.mayorEmergency");
    static public TranslatableTag Disconnect = new("statistics.events.disconnect");
    static public TranslatableTag Revive = new("statistics.events.revive");
    static public TranslatableTag Eat = new("statistics.events.eat");
    static public TranslatableTag Clean = new("statistics.events.clean");
}

public enum GameStatisticsGatherTag
{
    Spawn
}

[NebulaRPCHolder]
public class GameStatistics
{
    public class EventVariation
    {
        static Dictionary<int, EventVariation> AllEvents = new();
        static private DividedSpriteLoader iconSprite = DividedSpriteLoader.FromResource("Nebula.Resources.GameStatisticsIcon.png", 100f, 8, 1);
        static public EventVariation Kill = new(0, iconSprite.WrapLoader(0), iconSprite.WrapLoader(0), true, true);
        static public EventVariation Exile = new(1, iconSprite.WrapLoader(2), iconSprite.WrapLoader(2), false, false);
        static public EventVariation GameStart = new(2, iconSprite.WrapLoader(1), iconSprite.WrapLoader(1), true, false);
        static public EventVariation GameEnd = new(3, iconSprite.WrapLoader(1), iconSprite.WrapLoader(1), true, false);
        static public EventVariation MeetingEnd = new(4, iconSprite.WrapLoader(1), iconSprite.WrapLoader(1), true, false);
        static public EventVariation Report = new(5, iconSprite.WrapLoader(4), iconSprite.WrapLoader(4), true, false);
        static public EventVariation EmergencyButton = new(6, iconSprite.WrapLoader(3), iconSprite.WrapLoader(3), true, false);
        static public EventVariation Disconnect = new(7, iconSprite.WrapLoader(5), iconSprite.WrapLoader(5), false, false);
        static public EventVariation Revive = new(8, iconSprite.WrapLoader(6), iconSprite.WrapLoader(6), true, false);
        static public EventVariation CreanBody = new(9, iconSprite.WrapLoader(7), iconSprite.WrapLoader(7), true, false);

        public int Id { get; private init; }
        public ISpriteLoader? EventIcon { get; private init; }
        public ISpriteLoader? InteractionIcon { get; private init; }
        public bool ShowPlayerPosition { get; private init; }
        public bool CanCombine { get; private init; }
        public EventVariation(int id, ISpriteLoader? eventIcon, ISpriteLoader? interactionIcon, bool showPlayerPosition, bool canCombine)
        {
            Id = id;
            EventIcon = eventIcon;
            InteractionIcon = interactionIcon;
            CanCombine = canCombine;

            AllEvents.Add(id, this);
            ShowPlayerPosition = showPlayerPosition;
        }
        static public EventVariation ValueOf(int id) => AllEvents[id];
    }

    public class Event
    {
        public EventVariation Variation { get; private init; }
        public float Time { get; private init; }
        public byte? SourceId { get; private init; }
        public int TargetIdMask { get; private set; }
        public Tuple<byte, Vector2>[] Position { get; private init; }
        public TranslatableTag? RelatedTag { get; set; } = null;


        public Event(EventVariation variation, byte? sourceId, int targetIdMask,GameStatisticsGatherTag? positionTag = null)
            : this(variation, NebulaGameManager.Instance.CurrentTime, sourceId, targetIdMask,positionTag) { }

        public Event(EventVariation variation, float time, byte? sourceId, int targetIdMask, GameStatisticsGatherTag? positionTag)
        {
            Variation = variation;
            Time = time;
            SourceId = sourceId;
            TargetIdMask = targetIdMask;

            if (variation.ShowPlayerPosition)
            {
                List<Tuple<byte, Vector2>> list = new();
                foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    if (p.Data.IsDead && p.PlayerId != sourceId && ((TargetIdMask & (1 << p.PlayerId)) == 0)) continue;

                    if (positionTag != null)
                        list.Add(new Tuple<byte, Vector2>(p.PlayerId, NebulaGameManager.Instance.GameStatistics.Gathering[positionTag.Value][p.PlayerId]));
                    else
                        list.Add(new Tuple<byte, Vector2>(p.PlayerId, p.transform.position));
                }
                Position = list.ToArray();
            }
            else
            {
                Position = new Tuple<byte, Vector2>[0];
            }
        }

        public bool IsSimilar(Event target)
        {
            if (!Variation.CanCombine) return false;
            return Variation == target.Variation && SourceId == target.SourceId && RelatedTag == target.RelatedTag;
        }

        public void Combine(Event target)
        {
            TargetIdMask |= target.TargetIdMask;
        }
    }

    private List<Event> AllEvents { get; set; } = new List<Event>();
    public Event[] Sealed { get => AllEvents.ToArray(); }

    public Dictionary<GameStatisticsGatherTag, Dictionary<byte, Vector2>> Gathering { get; set; } = new();

    public void RecordEvent(Event statisticsEvent)
    {
        if (statisticsEvent.Variation.CanCombine)
        {
            //末尾から検索
            for (int i = AllEvents.Count - 1; i >= 0; i--)
            {
                //ある程度以上離れた時間のイベントまで来たら検索をやめる
                if (statisticsEvent.Time - AllEvents[i].Time > 5f) break;

                if (AllEvents[i].IsSimilar(statisticsEvent))
                {
                    AllEvents[i].Combine(statisticsEvent);
                    return;
                }
            }
        }
        AllEvents.Add(statisticsEvent);
    }

    public class RecordMessage
    {
        public EventVariation Variation;
        public int RelatedTagId;
        public byte? SourceId;
        public int TargetIdMask = 0;

        public RecordMessage() { }
        public RecordMessage(EventVariation variation, TranslatableTag tag,PlayerControl source,params PlayerControl[] target)
        {
            Variation = variation;
            RelatedTagId = tag.Id;
            SourceId = source.PlayerId;
            foreach (var t in target) TargetIdMask |= 1 << t.PlayerId;
        }
    }

    static public RemoteProcess<RecordMessage> RpcRecord = new RemoteProcess<RecordMessage>(
        "RecordStatistics",
        (writer, message) =>
        {
            writer.Write(message.Variation.Id);
            writer.Write(message.RelatedTagId);
            writer.Write(message.SourceId ?? Byte.MaxValue);
            writer.Write(message.TargetIdMask);
        },
       (reader) =>
       {
           var message = new RecordMessage();
           message.Variation = EventVariation.ValueOf(reader.ReadInt32());
           message.RelatedTagId = reader.ReadInt32();
           message.SourceId = reader.ReadByte();
           if (message.SourceId == Byte.MaxValue) message.SourceId = null;
           message.TargetIdMask = reader.ReadInt32();
           return message;
       },
       (message, isCalledByMe) =>
       {
           NebulaGameManager.Instance?.GameStatistics.RecordEvent(new Event(message.Variation, message.SourceId, message.TargetIdMask) { RelatedTag = TranslatableTag.ValueOf(message.RelatedTagId) });
       });

    static public RemoteProcess<Tuple<GameStatisticsGatherTag,byte, Vector2>> RpcPoolPosition = new RemoteProcess<Tuple<GameStatisticsGatherTag,byte, Vector2>>(
        "PoolPosition",
        (writer, message) => {
            writer.Write((int)message.Item1);
            writer.Write(message.Item2);
            writer.Write(message.Item3.x);
            writer.Write(message.Item3.y);
        },
        (reader) => new Tuple<GameStatisticsGatherTag,byte, Vector2>((GameStatisticsGatherTag)reader.ReadInt32(),reader.ReadByte(),new(reader.ReadSingle(),reader.ReadSingle())),
        (message, calledByMe) =>
        {
            if (NebulaGameManager.Instance == null) return;

            if (!NebulaGameManager.Instance!.GameStatistics.Gathering.ContainsKey((GameStatisticsGatherTag)message.Item1))
                NebulaGameManager.Instance!.GameStatistics.Gathering.Add((GameStatisticsGatherTag)message.Item1, new());

            NebulaGameManager.Instance!.GameStatistics.Gathering[(GameStatisticsGatherTag)message.Item1][message.Item2] = message.Item3;
        }
        );
}

public class CriticalPoint : MonoBehaviour
{
    static CriticalPoint()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CriticalPoint>();
    }
    static private ResourceExpandableSpriteLoader momentSprite = new("Nebula.Resources.GameStatisticsMoment.png", 100f);
    static private ResourceExpandableSpriteLoader momentRingSprite = new("Nebula.Resources.GameStatisticsMomentRing.png", 100f);

    public int IndexMin { get; private set; }
    public int IndexMax { get; private set; }
    GameObject ring;
    public GameStatisticsViewer MyViewer;

    public void SetIndex(int min,int max)
    {
        IndexMin = min; IndexMax = max;
    }

    public bool Contains(int index) => IndexMin <= index && index <= IndexMax;

    public void Start()
    {
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = momentSprite.GetSprite();
        renderer.color = GameStatisticsViewer.MainColor;
        renderer.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        var ringRenderer = UnityHelper.CreateObject<SpriteRenderer>("Ring", transform, Vector3.zero);
        ringRenderer.sprite = momentRingSprite.GetSprite();
        ringRenderer.color = GameStatisticsViewer.MainColor;
        ringRenderer.gameObject.SetActive(false);
        ring = ringRenderer.gameObject;

        
        var button = renderer.gameObject.SetUpButton(true);
        button.OnMouseOver.AddListener(() =>
        {
            renderer.transform.localScale = new Vector3(1f, 1f, 1f);
            MyViewer.OnMouseOver(IndexMin);
        });
        button.OnMouseOut.AddListener(() =>
        {
            renderer.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            MyViewer.OnMouseOut(IndexMin);
        });
        button.OnClick.AddListener(() =>
        {
            MyViewer.OnSelect(ring.active ? -1 : IndexMin);
        });
        
        var collider = renderer.gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.09f;
    }

    public void OnSomeIndexSelected(int selected)
    {
        ring.gameObject.SetActive(Contains(selected));
    }
}

public class GameStatisticsViewer : MonoBehaviour
{
    static GameStatisticsViewer()
    {
        ClassInjector.RegisterTypeInIl2Cpp<GameStatisticsViewer>();
    }

    LineRenderer timelineBack, timelineFront;
    GameObject minimap;
    GameObject baseOnMinimap,detailHolder;
    AlphaPulse mapColor;
    GameStatistics.Event[] allStatistics;
    GameStatistics.Event? eventPiled, eventSelected, currentShown;
    GameObject CriticalPoints;

    public PoolablePlayer PlayerPrefab;
    public TMPro.TextMeshPro GameEndText;

    static private ResourceExpandableSpriteLoader backgroundSprite = new("Nebula.Resources.StatisticsBackground.png",100f);
    static public GameStatisticsViewer Instance { get; private set; }

    public SpriteRenderer CreateBackground(Vector2 size,Transform transform)
    {
        var renderer = UnityHelper.CreateObject<SpriteRenderer>("Background",transform,new Vector3(0,0,1f));
        renderer.sprite = backgroundSprite.GetSprite();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.color = MainColor;
        renderer.size = size;
        return renderer;
    }

    public void Start()
    {
        allStatistics = NebulaGameManager.Instance.GameStatistics.Sealed;
        if (allStatistics.Length == 0) return;

        timelineBack = UnityHelper.SetUpLineRenderer("TimelineBack", transform, new Vector3(0, 0, -10f), LayerExpansion.GetUILayer(), 0.014f);
        timelineFront = UnityHelper.SetUpLineRenderer("TimelineFront", transform, new Vector3(0, 0, -15f), LayerExpansion.GetUILayer(), 0.014f);

        minimap = UnityHelper.CreateObject("Minimap",transform, new Vector3(0, -1.62f, 0));
        var scaledMinimap = UnityHelper.CreateObject("Scaled", minimap.transform, new Vector3(0, 0, 0));
        scaledMinimap.transform.localScale = new Vector3(0.45f, 0.45f, 1);
        var minimapRenderer = GameObject.Instantiate(NebulaGameManager.Instance.RuntimeAsset.MinimapObjPrefab, scaledMinimap.transform);
        minimapRenderer.gameObject.name = "MapGraphic";
        minimapRenderer.transform.localScale = new Vector3(1f, 1f, 1f);
        minimapRenderer.transform.localPosition = Vector3.zero;
        mapColor = minimapRenderer.GetComponent<AlphaPulse>();
        mapColor.SetColor(MainColor);
        CreateBackground(new Vector2(4.6f, 2.8f), minimap.transform);
        baseOnMinimap = UnityHelper.CreateObject("Scaler", scaledMinimap.transform,NebulaGameManager.Instance.RuntimeAsset.MinimapPrefab.HerePoint.transform.parent.localPosition);
        detailHolder = UnityHelper.CreateObject("Detail", transform, new Vector3(0, -3.5f, 0));
        Hide();

        CriticalPoints = UnityHelper.CreateObject("CriticalMoments",transform,Vector3.zero);

        StartCoroutine(CoShowTimeLine().WrapToIl2Cpp());
    }

    public void Update()
    {
        GameStatistics.Event? willShown = eventPiled ?? eventSelected;
        if(willShown != currentShown)
        {
            if (willShown == null)
                Hide();
            else
                Show(willShown);
            currentShown = willShown;
        }
    }

    private const float LineHalfWidth = 2.5f;
    public static readonly Color MainColor = new Color(0f, 242f / 255f, 156f / 255f);
    private const float BackColorRate = 0.4f;

    private IEnumerator CoShowCriticalMoment(float p,int indexMin,int indexMax)
    {
        var point = UnityHelper.CreateObject<CriticalPoint>("Moment",CriticalPoints.transform, new Vector3((p * 2f - 1f) * LineHalfWidth, 0f, -20f - indexMin));
        point.MyViewer = this;
        point.SetIndex(indexMin, indexMax);
        yield return null;
    }

    public void OnSelect(int index)
    {
        eventSelected = index >= 0 ? allStatistics[index] : null;
        CriticalPoints.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) => obj.GetComponent<CriticalPoint>().OnSomeIndexSelected(index)));
    }

    private void ShowCriticalMoment(float p,ref int index)
    {
        var sum = allStatistics[allStatistics.Length - 1].Time - allStatistics[0].Time;
        int indexMin = index;
        while (index + 1 < allStatistics.Length && allStatistics[index + 1].Time - allStatistics[indexMin].Time < sum*0.01f)
        {
            index++;
        }
        int indexMax = index;
        
        //ゲーム終了と結合する際は後ろに揃える
        if (indexMax == allStatistics.Length - 1) p = 1f;

        StartCoroutine(CoShowCriticalMoment(p, indexMin,indexMax).WrapToIl2Cpp());
        index = indexMax + 1;
    }

    private IEnumerator CoShowTimeLine()
    {
        StartCoroutine(CoShowTimeBackLine().WrapToIl2Cpp());
        yield return new WaitForSeconds(1.4f);

        timelineFront.SetPosition(0, new Vector3(-LineHalfWidth, 0));
        timelineFront.SetPosition(1, new Vector3(-LineHalfWidth, 0));
        timelineFront.SetColors(MainColor, MainColor);

        float p = 0f;

        float minTime = allStatistics[0].Time;
        float maxTime = allStatistics[allStatistics.Length - 1].Time;
        int index = 0;

        ShowCriticalMoment(0, ref index);

        float ToP(float p) => (p - minTime) / (maxTime - minTime);

        while (p < 1f)
        {
            while (index < (allStatistics.Length - 1) && ToP(allStatistics[index].Time) < p) ShowCriticalMoment(ToP(allStatistics[index].Time), ref index);

            timelineFront.SetPosition(1, new Vector3(LineHalfWidth * (p * 2f - 1f), 0));
            p += Time.deltaTime / 3f;
            yield return null;
        }
        while (index < allStatistics.Length) ShowCriticalMoment(ToP(allStatistics[index].Time), ref index);
        timelineFront.SetPosition(1, new Vector3(LineHalfWidth, 0));
    }
    private IEnumerator CoShowTimeBackLine()
    {
        float t = 0f;

        timelineBack.SetPosition(0, new Vector3(-LineHalfWidth, 0));
        timelineBack.SetColors(MainColor * BackColorRate, MainColor.AlphaMultiplied(0));

        while (true)
        {
            float log = Mathf.Log(t + 1f, 1.92f);
            float exp = t > 1.3f ? Mathf.Pow((t - 1.3f) * 0.86f, 3f) : 0f;
            t += Time.deltaTime;

            timelineBack.SetPosition(1, new Vector3(log < 1 ? log * LineHalfWidth : LineHalfWidth, 0));
            float a = exp;
            if (log > 1) a += ((log - 1) / log) * 0.3f * LineHalfWidth;
            timelineBack.endColor = MainColor.AlphaMultiplied(a > 1f ? 1f : a) * BackColorRate;

            if (log > 1f && a > 1f) break;

            yield return null;
        }

        timelineBack.SetPosition(1, new Vector3(LineHalfWidth, 0));
        timelineBack.endColor = MainColor * BackColorRate;
    }

    public void ClearDetail(bool onlyMinimap)
    {
        baseOnMinimap.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => GameObject.Destroy(c)));
        if (!onlyMinimap) detailHolder.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => GameObject.Destroy(c)));
    }

    public void Hide()
    {
        minimap.SetActive(false);
        detailHolder.SetActive(false);
    }
    public void Show(GameStatistics.Event statisticsEvent){
        //対象となるCriticalPointを探す
        int index = 0, indexMin = 0, indexMax = 0;
        while (allStatistics[index] != statisticsEvent) index++;
        CriticalPoints.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) => {
            var criticalPoint = obj.GetComponent<CriticalPoint>();
            if (criticalPoint.Contains(index))
            {
                indexMin = criticalPoint.IndexMin;
                indexMax = criticalPoint.IndexMax;
            }
        }));

        //CriticalPointが一致しない場合は詳細も含めてリセットする
        var lastIndex = Array.IndexOf(allStatistics, currentShown);
        var requireGenerateDetail = !(indexMin <= lastIndex && lastIndex <= indexMax);
        ClearDetail(!requireGenerateDetail);

        minimap.SetActive(true);
        detailHolder.SetActive(true);

        foreach (var pos in statisticsEvent.Position)
        {
            var renderer = GameObject.Instantiate(NebulaGameManager.Instance.RuntimeAsset.MinimapPrefab.HerePoint, baseOnMinimap.transform);
            PlayerMaterial.SetColors(pos.Item1, renderer);
            renderer.transform.localPosition = (Vector3)(pos.Item2 / NebulaGameManager.Instance.RuntimeAsset.MapScale) + new Vector3(0, 0, -1f);
        }

        
        int num = 0;
        void EventToDetailShower(int eventIndex)
        {
            GameStatistics.Event target = allStatistics[eventIndex];

            GameObject detail = UnityHelper.CreateObject("EventDetail", detailHolder.transform, new Vector3(0, -0.76f * num, -10f));
            
            var backGround = CreateBackground(new Vector2(3.4f, 0.7f), detail.transform);

            var collider = detail.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3.4f, 0.7f);
            var button = detail.gameObject.SetUpButton(true, null);
            button.OnClick.RemoveAllListeners();
            button.OnMouseOver.AddListener(() =>
            {
                OnMouseOver(eventIndex);
                backGround.color = Color.Lerp(MainColor,Color.white,0.5f);
            });
            button.OnMouseOut.AddListener(() =>
            {
                OnMouseOver(eventIndex);
                backGround.color = MainColor;
            });

            List<GameObject> objects = new();

            Il2CppArgument<PoolablePlayer> GeneratePlayerView(byte id)
            {
                PoolablePlayer player = GameObject.Instantiate(PlayerPrefab, detail.transform);
                var info = NebulaGameManager.Instance.GetModPlayerInfo(id);
                player.UpdateFromPlayerOutfit(info.DefaultOutfit, PlayerMaterial.MaskType.None, false, true, null);
                player.ToggleName(true);
                player.SetName(info.DefaultName, new Vector3(3.1f, 3.1f, 1f), Color.white, -15f);
                player.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
                player.cosmetics.nameText.transform.parent.localPosition += new Vector3(0f, -1.05f, 0f);
                return player;
            }

            if (target.SourceId.HasValue) objects.Add(GeneratePlayerView(target.SourceId.Value).Value.gameObject);
            
            SpriteRenderer icon = UnityHelper.CreateObject<SpriteRenderer>("Icon", detail.transform, new Vector3(0, 0, -1f));
            icon.sprite = target.Variation.InteractionIcon.GetSprite();
            icon.transform.localScale=new Vector3(0.7f,0.7f,1f);
            if(target.RelatedTag != null)
            {
                var text = GameObject.Instantiate(GameEndText, icon.transform);
                text.text = target.RelatedTag.Text;
                text.color = Color.white;
                text.outlineWidth = 0.1f;
                text.transform.localPosition = new Vector3(0f, -0.18f, -1f);
                text.transform.localScale = new Vector3(0.2f / 0.7f, 0.2f / 0.7f, 1f);
                icon.transform.localPosition += new Vector3(0f, 0.05f, 0f);
            }
            objects.Add(icon.gameObject);

            foreach(var p in NebulaGameManager.Instance.AllPlayerInfo())
                if((target.TargetIdMask & (1 << p.PlayerId)) != 0)
                    objects.Add(GeneratePlayerView(p.PlayerId).Value.gameObject);

            float width = Mathf.Min(1.2f, (float)(objects.Count - 1) * 0.5f);
            for (int i = 0;i<objects.Count;i++)
            {
                float pos = objects.Count == 1 ? 0 : width * ((float)i / (objects.Count - 1) * 2f - 1f);
                objects[i].transform.localPosition += new Vector3(pos, 0, 0f);
            }

            num++;
        }


        if (requireGenerateDetail)
        {
            for (int i = indexMin; i <= indexMax; i++)
            {
                EventToDetailShower(i);
            }
        }
        
    }

    
    public void OnMouseOver(int index) {
        eventPiled = allStatistics[index];
    }
    public void OnMouseOut(int index) {
        if (eventPiled == allStatistics[index]) eventPiled = null;
    }
    
}