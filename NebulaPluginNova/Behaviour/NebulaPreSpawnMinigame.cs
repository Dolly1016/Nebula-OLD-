using Il2CppInterop.Runtime.Injection;
using Nebula.Configuration;
using Nebula.Modules;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Nebula.Behaviour;

public class NebulaPreSpawnLocation
{
    static public string[] MapName = new string[] { "Skeld", "Mira", "Polus", "Invalid", "Airship" };

    static public NebulaPreSpawnLocation[][] Locations = new NebulaPreSpawnLocation[][]{
        new NebulaPreSpawnLocation[]{ 
            new("Admin", new Vector2(2.9753f, -7.4595f)),
            new("Cafeteria", new Vector2(-0.8721f, 3.6115f)),
            new("Comms", new Vector2(4.5986f, -15.618f)),
            new("Electrical", new Vector2(-7.6091f, -8.7664f)),
            new("LifeSupport", new Vector2(6.5236f, -3.5375f)),
            new("LowerEngine", new Vector2(-17.1282f, -13.2787f)),
            new("MedBay", new Vector2(-8.6636f, -4.4547f)),
            new("Navigation", new Vector2(16.6989f, -4.7736f)),
            new("Reactor", new Vector2(-20.9127f, -5.5454f)),
            new("Security", new Vector2(-13.2544f, -4.1371f)),
            new("Shields", new Vector2(9.1997f, -12.3562f)),
            new("Storage", new Vector2(-2.3901f, -15.1296f)),
            new("UpperEngine", new Vector2(-17.6972f, -0.9157f)),
            new("Weapons", new Vector2(9.5354f, 1.3911f))
        },
        new NebulaPreSpawnLocation[]{
            new("Admin", new Vector2(19.4462f, 19.0366f)),
            new("Balcony", new Vector2(26.7091f, -1.9142f)),
            new("Cafeteria", new Vector2(25.433f, 2.553f)),
            new("Comms", new Vector2(14.4909f, 4.0153f)),
            new("Decontamination", new Vector2(6.1333f, 6.27f)),
            new("Greenhouse", new Vector2(17.857f, 23.5425f)),
            new("Laboratory", new Vector2(9.0136f, 12.081f)),
            new("Launchpad", new Vector2(-4.4f, 2.1969f)),
            new("LockerRoom", new Vector2(9.0862f, 1.3112f)),
            new("MedBay", new Vector2(15.3094f, -0.4085f)),
            new("Office", new Vector2(14.7004f, 20.0933f)),
            new("Reactor", new Vector2(2.4809f, 13.2443f)),
            new("Rendezvous", new Vector2(17.8176f, 11.3095f)),
            new("Storage", new Vector2(19.9159f, 4.718f))
        },
        new NebulaPreSpawnLocation[]{
            new("Abditory", new Vector2(25.7226f, -12.8779f)),
            new("Admin", new Vector2(21.1384f, -22.7731f)),
            new("Drill", new Vector2(27.5518f, -7.3609f)),
            new("Dropship", new Vector2(16.6f, -1.5f)),
            new("Ejection", new Vector2(32.1547f, -15.7529f)),
            new("Electrical", new Vector2(7.4f, -9.6f)),
            new("Laboratory", new Vector2(34.8f, -6.0f)),
            new("LifeSupport", new Vector2(3.5f, -21.5f)),
            new("Office", new Vector2(19.5f, -17.6f)),
            new("Security", new Vector2(3.0694f, -11.9939f)),
            new("Snowdrift", new Vector2(12.918f, -13.0296f)),
            new("Specimens", new Vector2(36.5f, -21.2f)),
            new("Storage", new Vector2(20.6f, -11.7f)),
            new("Weapons", new Vector2(12.2f, -23.3f))
        },
        new NebulaPreSpawnLocation[0],
        new NebulaPreSpawnLocation[]{ 
            new("Brig",0,140),
            new("Engine",1,180),
            new("Hallway",2,226),
            new("Kitchen",3,140),
            new("Record",4,173),
            new("Storage",5,188),
            new("Armory", new Vector2(-10.141f, -6.3739f)),
            new("Cockpit", new Vector2(-23.5643f, -1.4405f)),
            new("Comms", new Vector2(-12.9433f, 1.4259f)),
            new("Electrical", new Vector2(16.3201f, -8.808f)),
            new("GapRoom", new Vector2(11.9727f, 8.6011f)),
            new("Lounge", new Vector2(24.8702f, 6.459f)),
            new("Medical", new Vector2(28.4471f, -5.8789f)),
            new("MeetingRoom", new Vector2(11.1469f, 16.0138f)),
            new("Security", new Vector2(7.0693f, -11.6312f)),
            new("Shower", new Vector2(24.0106f, 2.0266f)),
            new("Toilet", new Vector2(32.3184f, 7.0118f)),
            new("Vault", new Vector2(-8.789f, 8.049f)),
            new("ViewingDeck", new Vector2(-13.9798f, -15.8316f))
        }
        };

    public string LocationName { get; private init; }
    public string? AudioClip { get; private init; }
    public Vector2? Position { get; set; }
    public int? VanillaIndex { get; private init; }
    public string? AssetImagePath(byte mapId) => "assets/SpawnCandidates/" + MapName[mapId] + "/" + LocationName + ".png";
    public IDividedSpriteLoader GetSprite(byte mapId) => new XOnlyDividedSpriteLoader(new AssetTextureLoader(AssetImagePath(mapId)), 115f, ImageSize, true) { Pivot = new(0.5f, 0f) };
    private int ImageSize { get; set; } = 200;
    public NebulaPreSpawnLocation(string locationName,Vector2 pos,string? audioClip = null) { 
        this.LocationName = locationName;
        this.Position = pos;
        this.AudioClip = audioClip;
    }

    public NebulaPreSpawnLocation(string locationName, int vanillaIndex,int imageSize)
    {
        this.LocationName= locationName;
        this.VanillaIndex= vanillaIndex;
        this.ImageSize = imageSize;
    }
}

public class NebulaPreSpawnMinigame : Minigame
{

    static NebulaPreSpawnMinigame() => ClassInjector.RegisterTypeInIl2Cpp<NebulaPreSpawnMinigame>();
    public NebulaPreSpawnMinigame(System.IntPtr ptr) : base(ptr) { }
    public NebulaPreSpawnMinigame() : base(ClassInjector.DerivedConstructorPointer<NebulaPreSpawnMinigame>())
    { ClassInjector.DerivedConstructorBody(this); }

    TMPro.TextMeshPro UnderText;

    Vector2 spawnAt = new Vector2(0, 0);

    public static NebulaPreSpawnLocation[] PreSpawnLocations { get
        {
            byte mapId = AmongUsUtil.CurrentMapId;
            var cand = NebulaPreSpawnLocation.Locations[mapId];

            //スポーンポイントを抽出する
            if (GeneralConfigurations.SpawnMethodOption.CurrentValue == 0)
            {
                //Default
                cand = cand.Where(l => l.VanillaIndex.HasValue).ToArray();
            }

            return cand;
        } }
    public IEnumerator CoClose()
    {
        var gathering = NebulaGameManager.Instance.GameStatistics.Gathering[GameStatisticsGatherTag.Spawn];
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p.Data.IsDead) continue;
            if (!gathering.ContainsKey(p.PlayerId)) continue;

            p.NetTransform.SnapTo(gathering[p.PlayerId]);
        }
        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(spawnAt);

        for (float timer = 0f; timer < 0.25f; timer += Time.deltaTime)
        {
            float num = timer / 0.25f;
            base.transform.localPosition = new Vector3(0f, Mathf.SmoothStep(0f, -8f, num), -50f);
            yield return null;
        }
        GameObject.Destroy(base.gameObject);
    }

    public override void Begin(PlayerTask task)
    {
        Minigame.Instance = this;
        this.amOpening = true;
        this.amClosing = Minigame.CloseState.None;

        if (PlayerControl.LocalPlayer) PlayerControl.LocalPlayer.NetTransform.Halt();
        
        byte mapId = AmongUsUtil.CurrentMapId;

        var cand = PreSpawnLocations;
        var rand = Helpers.GetRandomArray(cand.Length);

        SpawnInMinigame? minigamePrefab = null;
        if (mapId == 4) minigamePrefab = ShipStatus.Instance.Cast<AirshipStatus>().SpawnInGame;

        UnderText = GameObject.Instantiate(VanillaAsset.StandardTextPrefab,transform);
        UnderText.transform.localPosition = new Vector3(0,-1.18f,0f);
        UnderText.transform.localScale= Vector3.one;
        UnderText.fontSizeMax = 4;
        UnderText.fontSizeMin = 1;
        UnderText.fontSize = 4;
        UnderText.rectTransform.sizeDelta = new Vector2(6.2f, 0.68f);

        UnderText.text = "";


        int candidates = GeneralConfigurations.SpawnCandidatesOption.GetMappedInt().Value;
        if (GeneralConfigurations.SpawnMethodOption.CurrentValue == 2) candidates = 1;

        Tuple<SpriteRenderer, TextMeshPro>[] allButton = new Tuple<SpriteRenderer, TextMeshPro>[candidates];
        IDividedSpriteLoader[] allSprite = new IDividedSpriteLoader[candidates];
        Coroutine? currentAnim = null;

        IEnumerator CoAnim(SpriteRenderer renderer,IDividedSpriteLoader sprite)
        {
            float t = 0f;
            int i = 0;
            while (true)
            {
                t += Time.deltaTime;
                if (t > 0.06f)
                {
                    i++;
                    if (i >= sprite.Length) break;

                    renderer.sprite = sprite.GetSprite(i);
                    t = 0f;
                }

                yield return null;
            }
            renderer.sprite = sprite.GetSprite(0);
        }

        bool gotSelect = false;

        IEnumerator CoFadeOutOthers(int selected)
        {
            float othersAlpha = 1f;

            while (true)
            {
                othersAlpha = Mathf.Clamp01(othersAlpha - Time.deltaTime * 1.45f);

                for (int i = 0; i < candidates; i++)
                {
                    if (i == selected) continue;
                    allButton[i].Item1.color = Color.white.AlphaMultiplied(othersAlpha);
                    allButton[i].Item2.color = Color.white.AlphaMultiplied(othersAlpha);
                }

                yield return null;
            }
        }

        IEnumerator CoSelect(int selected)
        {
            gotSelect = true;
            spawnAt= cand[rand[selected]].Position.Value;

            foreach (var button in allButton) button.Item1.GetComponent<PassiveButton>().enabled = false;
            if (currentAnim != null) StopCoroutine(currentAnim);

            StartCoroutine(CoFadeOutOthers(selected).WrapToIl2Cpp());

            var selectedTransform = allButton[selected].Item1.transform;

            for(int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.02f);
                allButton[selected].Item1.color = Color.clear;
                allButton[selected].Item2.color = Color.clear;
                yield return new WaitForSeconds(0.02f);
                allButton[selected].Item1.color = Color.white;
                allButton[selected].Item2.color = Color.white;
            }

            StartCoroutine(CoAnim(allButton[selected].Item1, allSprite[selected]).WrapToIl2Cpp());

            //点滅しおわってからスポーン
            NebulaGameManager.Instance.RpcPreSpawn(PlayerControl.LocalPlayer.PlayerId, spawnAt);

            while (true)
            {
                selectedTransform.localPosition -= new Vector3(selectedTransform.localPosition.x * Time.deltaTime * 3.2f, 0f, 0f);
                selectedTransform.localScale -= (selectedTransform.localScale - new Vector3(1f, 1f, 1f)) * Time.deltaTime * 3.2f;

                yield return null;
            }
        }

        //選択肢が2つ以上ある場合
        IEnumerator CoCountDown()
        {
            float t = 10f;
            int ceilCount = -1;

            while (t > 0f)
            {
                t -= Time.deltaTime;
                int next = Mathf.CeilToInt(t);
                if (next != ceilCount)
                {
                    ceilCount = next;
                    UnderText.text = Language.Translate("game.prespawn.countdown").Replace("%SECOND%", ceilCount.ToString());

                    if (ceilCount <= 0)
                    {
                        if (!gotSelect) StartCoroutine(CoSelect(System.Random.Shared.Next(candidates)).WrapToIl2Cpp());
                        break;
                    }
                }

                if(gotSelect) break;

                yield return null;
            }
            UnderText.text = Language.Translate("game.prespawn.waiting");
        }

        //選択肢が1つしかない場合
        IEnumerator CoSpawning()
        {
            UnderText.text = Language.Translate("game.prespawn.spawnIn");
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(CoAnim(allButton[0].Item1, allSprite[0]).WrapToIl2Cpp());
            yield return new WaitForSeconds(1.4f);
            StartCoroutine(CoSelect(0).WrapToIl2Cpp());
        }

        float width = new float[] { 0f, 0f, 2.8f, 2.2f, 2.2f, 1.9f, 1.7f, 1.5f, 1.26f }[candidates];
        float scale = new float[] { 1f, 1f, 1f, 1f, 1f, 0.95f, 0.8f, 0.7f, 0.6f }[candidates];
        
        foreach(var loc in cand)
        {
            if (loc.VanillaIndex.HasValue)
                loc.Position = minigamePrefab!.Locations[loc.VanillaIndex.Value].Location;
        }

        for (int i = 0; i < candidates; i++){
            int copiedIndex = i;

            var loc = cand[rand[i]];

            var sprite = loc.GetSprite(mapId);
            allSprite[i] = sprite;

            var renderer = UnityHelper.CreateObject<SpriteRenderer>("Button", transform, new Vector3(width * (float)(i - (candidates - 1) / 2f), -0.25f, 0), LayerExpansion.GetUILayer());
            renderer.transform.localScale = new Vector3(scale, scale, 1f);

            renderer.sprite = sprite.GetSprite(0);

            var text = GameObject.Instantiate(VanillaAsset.StandardTextPrefab,renderer.transform);
            text.transform.localPosition=new Vector3(0f,-0.31f,0f);
            text.transform.localScale= Vector3.one;
            text.fontSize = 2.85f;
            text.fontSizeMax = 3f;
            text.fontSizeMin = 1f;
            text.text = Language.Translate("location." + NebulaPreSpawnLocation.MapName[mapId].HeadLower()+"."+loc.LocationName.HeadLower());
            text.font = VanillaAsset.PreSpawnFont;
            AudioClip hoverClip = VanillaAsset.HoverClip;

            if (loc.VanillaIndex.HasValue)
                hoverClip = minigamePrefab!.Locations[loc.VanillaIndex.Value].RolloverSfx;
            

            var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.5f, 1.45f);
            collider.isTrigger = true;

            var button = renderer.gameObject.SetUpButton();
            button.OnMouseOver.AddListener(() => {
                SoundManager.instance.PlaySound(hoverClip, false, 0.8f);
                text.color = Color.green;

                if (currentAnim != null) StopCoroutine(currentAnim);

                for (int i = 0; i < candidates; i++) allButton[i].Item1.sprite = allSprite[i].GetSprite(0);
                
                currentAnim = StartCoroutine(CoAnim(renderer,sprite).WrapToIl2Cpp());
            });
            button.OnMouseOut.AddListener(() => {
                text.color = Color.white;
            });
            button.OnClick.AddListener(() => {
                text.color = Color.white;
                StartCoroutine(CoSelect(copiedIndex).WrapToIl2Cpp());
            });

            //候補数が1つしかない場合は押させない
            button.enabled = candidates > 1;

            allButton[i] = new(renderer,text);
        }

        StartCoroutine(candidates == 1 ? CoSpawning().WrapToIl2Cpp() : CoCountDown().WrapToIl2Cpp());

        //Dummyのスポーン先を決定する
        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (!p.isDummy) continue;

            GameStatistics.RpcPoolPosition.LocalInvoke(new(GameStatisticsGatherTag.Spawn, p.PlayerId, cand[System.Random.Shared.Next(cand.Length)].Position.Value));
        }

    }

    public override void Close()
    {
        ControllerManager.Instance.CloseOverlayMenu(base.name);
        this.amClosing = Minigame.CloseState.Closing;
        StartCoroutine(CoClose().WrapToIl2Cpp());
    }

    public IEnumerator WaitForFinish()
    {
        yield return null;
        while (this.amClosing == Minigame.CloseState.None)
        {
            yield return null;
        }
        yield break;
    }
}
