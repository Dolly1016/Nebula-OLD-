using Epic.OnlineServices.UI;
using JetBrains.Annotations;
using Nebula.Expansion;
using Nebula.Game;
using Nebula.Map;
using Nebula.Module;
using Nebula.Patches;
using Nebula.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using static Il2CppSystem.Globalization.CultureInfo;

namespace Nebula.Roles.NeutralRoles;

public class Spectre : Role
{
    static public Color RoleColor = new Color(185f / 255f, 152f / 255f, 197f / 255f);

    public Module.CustomOption spawnImmoralistOption;
    private Module.CustomOption occupyDoubleRoleCountOption;
    public Module.CustomOption spectreTaskOption;
    public Module.CustomOption numOfTheFriedRequireToWinOption;
    private Module.CustomOption clarifyChargeOption;
    private Module.CustomOption clarifyDurationOption;
    private Module.CustomOption clarifyCoolDownOption;
    public Module.CustomOption canTakeOverSabotageWinOption;
    public Module.CustomOption canTakeOverTaskWinOption;
    public Module.CustomOption canFixEmergencySabotageOption;
    public Module.CustomOption lastImpostorCanGuessSpectreOption;
    private Module.CustomOption ventCoolDownOption;
    private Module.CustomOption ventDurationOption;

    private List<Objects.Arrow?> impostorArrows;

    int clarifyChargeId;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.SpectreButton.png", 115f, "ui.button.spectre.clarify");

    public SpriteLoader spectreLetterConsoleSprite = new SpriteLoader("Nebula.Resources.SpectreMinigameConsoleLetter.png", 100f);

    public SpriteLoader spectreFriedConsoleSprite = new SpriteLoader("Nebula.Resources.SpectreMinigameConsole.png", 100f);
    public SpriteLoader spectreFriedConsoleEatenSprite = new SpriteLoader("Nebula.Resources.SpectreMinigameConsoleUsed.png", 100f);

    public SpriteLoader spectreRancorConsoleSprite = new SpriteLoader("Nebula.Resources.SpectreMinigameConsoleStatue.png", 100f);
    public SpriteLoader spectreRancorConsoleBrokenSprite = new SpriteLoader("Nebula.Resources.SpectreMinigameConsoleStatueBroken.png", 100f);

    public ISpriteLoader[] spectreFoxAnimationSprites,spectreStatueSprites;

    public SpriteLoader GetConsoleUnusedSprite()
    {
        switch (spectreTaskOption.getSelection())
        {
            case 1:
                return spectreFriedConsoleSprite;
            case 2:
                return spectreRancorConsoleSprite;
        }
        return spectreFriedConsoleSprite;
    }

    public SpriteLoader GetConsoleUsedSprite()
    {
        switch (spectreTaskOption.getSelection())
        {
            case 1:
                return spectreFriedConsoleEatenSprite;
            case 2:
                return spectreRancorConsoleBrokenSprite;
        }
        return spectreFriedConsoleEatenSprite;
    }

    public SpriteLoader GetConsoleInShadowSprite()
    {
        switch (spectreTaskOption.getSelection())
        {
            case 1:
                return spectreFriedConsoleEatenSprite;
            case 2:
                return spectreRancorConsoleSprite;
        }
        return spectreFriedConsoleEatenSprite;
    }

    public CustomTaskSetting friedTaskSetting = new();
    public CustomTaskSetting letterTaskSetting = new();
    public CustomTaskSetting statueTaskSetting = new();

    public Dictionary<int, GameObject> CustomConsoles = new();

    public override bool CanFixEmergencySabotage { get { return canFixEmergencySabotageOption.getBool(); } }

    public class CustomTaskData : PointData
    {
        public float z { get; private set; }
        public float usableDistance { get; private set; }
        public CustomTaskData(string name, Vector2 pos,float z) : base(name, pos) {
            this.z = z;
            usableDistance = 0.7f;
        }
        public CustomTaskData(string name, Vector2 pos) : base(name, pos) {
            z = pos.y / 1000f + 0.001f;
            usableDistance = 0.7f;
        }

        public CustomTaskData SetUsableDistance(float distance)
        {
            usableDistance= distance;
            return this;
        }
    }

    public class CustomTaskSetting
    {
        private Dictionary<byte, System.Tuple<List<CustomTaskData>, CustomOption>> TaskDic = new();
        public void AddSetting(byte mapId,List<CustomTaskData> taskSettings,CustomOption option)
        {
            TaskDic[mapId] = new(taskSettings, option);
        }

        public void ForAllValidLoc(byte mapId,Action<CustomTaskData> process)
        {
            var setting = TaskDic[mapId];
            for (int i=0;i<setting.Item1.Count;i++) if ((setting.Item2.selection & (1 << i)) != 0) process(setting.Item1[i]);
        }

        public System.Tuple<List<CustomTaskData>, CustomOption> GetSetting(byte mapId)
        {
            return TaskDic[mapId];
        }
    }

    public override void CustomizeMap(byte mapId)
    {
        if (HnSModificator.IsHnSGame) return;

        if (!IsSpawnable()) return;

        CustomConsoles.Clear();

        int i = 0;

        switch (spectreTaskOption.selection)
        {
            case 1:
                friedTaskSetting.ForAllValidLoc(mapId, (loc) =>
                {
                    var console = ConsoleExpansion.GenerateConsole<Console>(new Vector3(loc.point.x, loc.point.y, loc.z), "NoS-SpectreFried-" + loc.name, GetConsoleUnusedSprite().GetSprite());
                    console.usableDistance = loc.usableDistance;

                    console.gameObject.layer = LayerExpansion.GetDefaultLayer();
                    var sObj = new GameObject("inShaddow");
                    sObj.transform.SetParent(console.transform);
                    sObj.transform.localPosition = Vector2.zero;
                    sObj.transform.localScale = Vector2.one;
                    sObj.layer = LayerExpansion.GetShadowLayer();
                    sObj.AddComponent<SpriteRenderer>().sprite = GetConsoleInShadowSprite().GetSprite();

                    CustomConsoles.Add(i, console.gameObject);
                    i++;
                });
                break;
            case 2:
                letterTaskSetting.ForAllValidLoc(mapId, (loc) =>
                {
                    var console = ConsoleExpansion.GenerateConsole<Console>(new Vector3(loc.point.x, loc.point.y, loc.z), "NoS-SpectreLetter-" + loc.name, spectreLetterConsoleSprite.GetSprite());
                    console.usableDistance = loc.usableDistance;
                });

                statueTaskSetting.ForAllValidLoc(mapId, (loc) =>
                {
                    var console = ConsoleExpansion.GenerateConsole<Console>(new Vector3(loc.point.x, loc.point.y, loc.z), "NoS-SpectreStatue-" + loc.name, GetConsoleUnusedSprite().GetSprite());
                    console.usableDistance = loc.usableDistance;

                    console.gameObject.layer = LayerExpansion.GetDefaultLayer();
                    var sObj = new GameObject("inShaddow");
                    sObj.transform.SetParent(console.transform);
                    sObj.transform.localPosition = Vector2.zero;
                    sObj.transform.localScale = Vector2.one;
                    sObj.layer = LayerExpansion.GetShadowLayer();
                    sObj.AddComponent<SpriteRenderer>().sprite = GetConsoleInShadowSprite().GetSprite();

                    CustomConsoles.Add(i, console.gameObject);
                    i++;
                });
                break;
        }

    }

    private CustomButton spectreButton;

    public override void ButtonInitialize(HudManager __instance)
    {
        if (spectreButton != null)
        {
            spectreButton.Destroy();
        }
        spectreButton = new CustomButton(
            () =>
            {
                RPCEventInvoker.EmitAttributeFactor(PlayerControl.LocalPlayer, new Game.PlayerAttributeFactor(Game.PlayerAttribute.Invisible, clarifyDurationOption.getFloat(), 0, false));
                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, clarifyChargeId, -1);
                spectreButton.UsesText.text = Game.GameData.data.myData.getGlobalData().GetRoleData(clarifyChargeId).ToString();
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.getGlobalData().GetRoleData(clarifyChargeId) > 0;
            },
            () => { spectreButton.Timer = 0; },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode, true,
            clarifyDurationOption.getFloat(),
           () =>
           {
               spectreButton.Timer = spectreButton.MaxTimer;
               RPCEventInvoker.UpdatePlayerVisibility(PlayerControl.LocalPlayer.PlayerId, true);
           },
            "button.label.clarify"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        spectreButton.MaxTimer = clarifyCoolDownOption.getFloat();
        spectreButton.UsesText.text = ((int)clarifyChargeOption.getFloat()).ToString();
        RPCEventInvoker.UpdateRoleData(PlayerControl.LocalPlayer.PlayerId, clarifyChargeId, ((int)clarifyChargeOption.getFloat()));
    }

    public override void CleanUp()
    {
        if (spectreButton != null)
        {
            spectreButton.Destroy();
            spectreButton = null;
        }
        foreach (var a in impostorArrows) if (a != null) GameObject.Destroy(a.arrow);
        impostorArrows.Clear();
    }

    //道連れ
    public override void OnExiledPre(byte[] voters)
    {
        foreach(var p in Game.GameData.data.AllPlayers.Values)
            if (p.IsAlive && p.role == Roles.Immoralist) RPCEventInvoker.UncheckedExilePlayer(p.id, Game.PlayerData.PlayerStatus.Suicide.Id);
    }

    public override void OnMurdered(byte murderId)
    {
        foreach (var p in Game.GameData.data.AllPlayers.Values)
            if (p.IsAlive && p.role == Roles.Immoralist) RPCEventInvoker.UncheckedMurderPlayer(p.id, p.id, Game.PlayerData.PlayerStatus.Suicide.Id, false);        
    }

    //上記で殺しきれない場合
    public override void OnDied()
    {
        foreach (var p in Game.GameData.data.AllPlayers.Values)
            if (p.IsAlive && p.role == Roles.Immoralist) RPCEventInvoker.UncheckedExilePlayer(p.id, Game.PlayerData.PlayerStatus.Suicide.Id);
    }

    public override void GlobalFinalizeInGame(PlayerControl __instance)
    {
        if (__instance.Data.IsDead) return;

        foreach (var p in Game.GameData.data.AllPlayers.Values)
            if (p.IsAlive && p.role == Roles.Immoralist)
            {
                if(MeetingHud.Instance || ExileController.Instance)
                    RPCEventInvoker.UncheckedExilePlayer(p.id, Game.PlayerData.PlayerStatus.Suicide.Id);
                else
                    RPCEventInvoker.UncheckedMurderPlayer(p.id, p.id, Game.PlayerData.PlayerStatus.Suicide.Id, false);
            }
    }

    private Action<byte> getRefresher(params Tuple<CustomTaskSetting,Sprite?>[] settings) 
    {
        Action<byte> refresher = null;
        refresher = (mapId) => MetaDialog.OpenMapDialog(mapId, true, (obj, id) =>
        {
            var mapData = Map.MapData.MapDatabase[id];
            foreach (var setting in settings)
            {
                var option = setting.Item1.GetSetting(id).Item2;
                int index = 0;
                foreach (PointData point in setting.Item1.GetSetting(id).Item1)
                {
                    bool flag = ((option.selection & 1 << index) != 0);
                    PassiveButton button = Module.MetaScreen.MSDesigner.AddSubButton(obj, new Vector2(2.4f, 0.4f), "Point", setting.Item2 == null ? (flag ? "o" : "-") : "", flag ? Color.yellow : Color.white);
                    button.transform.localPosition = (Vector3)mapData.ConvertMinimapPosition(point.point) + new Vector3(0f, 0f, -5f);
                    button.transform.localScale /= (obj.transform.localScale.x / 0.75f);

                    SpriteRenderer renderer = button.GetComponent<SpriteRenderer>();

                    var text = button.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();

                    var spriteObj = new GameObject("Icon");
                    spriteObj.transform.SetParent(button.transform, false);
                    spriteObj.transform.localPosition = new Vector3(0,0,-1f);
                    spriteObj.layer = LayerExpansion.GetUILayer();
                    var iconRenderer = spriteObj.AddComponent<SpriteRenderer>();
                    iconRenderer.sprite = setting.Item2;

                    if (setting.Item2 == null)
                        renderer.size = new Vector2(text.preferredWidth + 0.3f, renderer.size.y);
                    else
                    {
                        var sprite = setting.Item2;
                        float size = Mathf.Max(sprite.rect.size.x, sprite.rect.size.y);
                        spriteObj.transform.localScale = new Vector2(40f / size, 40f / size);
                        renderer.size = new Vector2(0.55f, 0.55f);
                        if (!flag) iconRenderer.color = Color.white.RGBMultiplied(0.6f);
                    }
                    button.GetComponent<BoxCollider2D>().size = renderer.size;

                    int i = index;
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        int selection = option.selection;
                        selection ^= 1 << i;
                        option.updateSelection(selection);
                        MetaDialog.EraseDialog(1);
                        refresher(id);
                    }));
                    index++;
                }
            }
        });
        return refresher;
    }

    private void SetUpCustomTaskOption()
    {
        friedTaskSetting.AddSetting(0,
            new List<CustomTaskData>{
            new CustomTaskData("Cafe0", new Vector2(1.88f, -1.97f)).SetUsableDistance(1.2f),
            new CustomTaskData("Cafe1", new Vector2(-3.31f, 3.13f)).SetUsableDistance(1.2f),
            new CustomTaskData("Right", new Vector2(12.27f, -3.12f)),
            new CustomTaskData("Shields", new Vector2(9.84f, -12.82f)),
            new CustomTaskData("Storage", new Vector2(-0.65f, -14.6f)),
            new CustomTaskData("Electrical", new Vector2(-7.67f, -12.10f)),
            new CustomTaskData("Lower", new Vector2(-15.24f, -9.93f)),
            new CustomTaskData("Reactor", new Vector2(-22.57f, -6.64f)),
            new CustomTaskData("MedBay", new Vector2(-7.78f, -1.49f)),
            new CustomTaskData("Admin", new Vector2(5.99f, -9.88f))
        }, CreateMetaOption(Color.white, "spectreTask.eatTheFried.skeld", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        friedTaskSetting.AddSetting(1,
            new List<CustomTaskData> {
            new CustomTaskData("Launchpad", new Vector2(-3.25f, 3.79f)),
            new CustomTaskData("Lower", new Vector2(6.47f, -0.82f)),
            new CustomTaskData("MedBay", new Vector2(15.57f, 1.21f)),
            new CustomTaskData("Locker", new Vector2(8.91f, 5.61f)),
            new CustomTaskData("Laboratory", new Vector2(10.71f, 12.96f),0.013f).SetUsableDistance(1f),
            new CustomTaskData("Upper", new Vector2(5.26f, 13.44f)),
            new CustomTaskData("Admin", new Vector2(19.47f, 20.61f)),
            new CustomTaskData("Greenhouse", new Vector2(21.07f, 24.30f),0.025f),
            new CustomTaskData("NextToAdmin", new Vector2(18.60f, 16.36f)),
            new CustomTaskData("Cafeteria", new Vector2(27.18f, 3.57f),-0.5f).SetUsableDistance(0.85f)
        }, CreateMetaOption(Color.white, "spectreTask.eatTheFried.mira", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        friedTaskSetting.AddSetting(2,
            new List<CustomTaskData>(){
            new CustomTaskData("Dropship", new Vector2(20.50f, -7.95f)),
            new CustomTaskData("Laboratory0", new Vector2(29.66f, -8.19f)),
            new CustomTaskData("Laboratory1", new Vector2(37.78f, -7.02f)).SetUsableDistance(0.8f),
            new CustomTaskData("Specimen", new Vector2(36.67f, -21.25f)),
            new CustomTaskData("SpecimenToAdmin", new Vector2(27.34f, -20.68f)),
            new CustomTaskData("Admin", new Vector2(22.25f, -25.14f)),
            new CustomTaskData("Office", new Vector2(20.82f, -17.05f),-0.017f),
            new CustomTaskData("Weapons", new Vector2(13.47f, -24.20f)),
            new CustomTaskData("Comms", new Vector2(11.50f, -16.84f)),
            new CustomTaskData("LifeSupp", new Vector2(0.81f, -22.02f)),
            new CustomTaskData("Electrical", new Vector2(4.60f, -9.20f)),
            new CustomTaskData("Storage", new Vector2(19.79f, -12.47f))
        }, CreateMetaOption(Color.white, "spectreTask.eatTheFried.polus", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        friedTaskSetting.AddSetting(4,
            new List<CustomTaskData>(){
            new CustomTaskData("MainHall0", new Vector2(13.17f, -2.20f)),
            new CustomTaskData("MainHall1", new Vector2(8.43f, 1.85f)),
            new CustomTaskData("MainHall2", new Vector2(5.14f, 3.31f)),
            new CustomTaskData("Engine", new Vector2(1.49f, -2.17f)).SetUsableDistance(0.2f),
            new CustomTaskData("Vault", new Vector2(-6.73f, 10.43f)),
            new CustomTaskData("MeetingRoom", new Vector2(3.32f, 15.72f)),
            new CustomTaskData("Comms", new Vector2(-14.38f, 0.87f)),
            new CustomTaskData("Cockpit", new Vector2(-17.51f, 0.92f)),
            new CustomTaskData("Armory", new Vector2(-15.04f, -9.26f)),
            new CustomTaskData("ViewingDeck", new Vector2(-13.05f, -14.77f)),
            new CustomTaskData("Security", new Vector2(5.05f, -10.35f)),
            new CustomTaskData("Medical0", new Vector2(24.32f, -8.76f)),
            new CustomTaskData("Medical1", new Vector2(29.39f, -7.40f),-0.007f).SetUsableDistance(0.8f),
            new CustomTaskData("CargoBay", new Vector2(35.96f, 1.71f)),
            new CustomTaskData("Lounge", new Vector2(24.41f, 6.34f)),
            new CustomTaskData("Shower", new Vector2(21.27f, 0.06f),0f),
        }, CreateMetaOption(Color.white, "spectreTask.eatTheFried.airship", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        letterTaskSetting.AddSetting(0,
            new List<CustomTaskData>{
            new CustomTaskData("MedBay", new Vector2(-7.78f, -1.49f)),
            new CustomTaskData("Comms", new Vector2(2.438f,-15.0722f)),
            new CustomTaskData("Reactor", new Vector2(-21.6699f, -4.2203f)),
            new CustomTaskData("LifeSupport", new Vector2(5.4116f, -4.2365f)),
            new CustomTaskData("Electrical", new Vector2(-7.67f, -12.10f))
        }, CreateMetaOption(Color.white, "spectreTask.letter.skeld", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        letterTaskSetting.AddSetting(1,
            new List<CustomTaskData> {
            new CustomTaskData("Launchpad", new Vector2(-5.5496f, -2.1019f)),
            new CustomTaskData("MedBay", new Vector2(16.8141f, -1.4631f)),
            new CustomTaskData("Locker", new Vector2(8.91f, 5.61f)),
            new CustomTaskData("Upper", new Vector2(5.26f, 13.44f)),
            new CustomTaskData("Laboratory", new Vector2(10.71f, 12.96f),0.013f).SetUsableDistance(1f),
            new CustomTaskData("Office", new Vector2(15.8369f, 20.2052f)),
            new CustomTaskData("Storage", new Vector2(20.3024f, 4.734f)),
        }, CreateMetaOption(Color.white, "spectreTask.letter.mira", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));
        
        letterTaskSetting.AddSetting(2,
            new List<CustomTaskData>(){
            new CustomTaskData("Dropship", new Vector2(18.8381f, -1.1588f)),
            new CustomTaskData("Drill", new Vector2(28.1577f, -7.4765f)),
            new CustomTaskData("SpecimenToAdmin", new Vector2(27.34f, -20.68f)),
            new CustomTaskData("Storage", new Vector2(19.79f, -12.47f)),
            new CustomTaskData("Electrical",new Vector2(11.9516f, -13.3554f)),
            new CustomTaskData("Comms", new Vector2(11.50f, -16.84f)),
            new CustomTaskData("Admin",new Vector2(22.2758f, -25.1438f))
        }, CreateMetaOption(Color.white, "spectreTask.letter.polus", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        letterTaskSetting.AddSetting(4,
            new List<CustomTaskData>(){
            new CustomTaskData("Engine", new Vector2(1.49f, -2.17f)).SetUsableDistance(0.2f),
            new CustomTaskData("Cockpit", new Vector2(-20.2177f, -0.4666f)),
            new CustomTaskData("Armory", new Vector2(-15.04f, -9.26f)),
            new CustomTaskData("Security", new Vector2(5.05f, -10.35f)),
            new CustomTaskData("Electrical", new Vector2(9.9102f, -6.1868f)),
            new CustomTaskData("Medical1", new Vector2(29.39f, -7.40f),-0.007f).SetUsableDistance(0.8f),
            new CustomTaskData("CargoBay", new Vector2(32.5286f, -1.5272f)),
            new CustomTaskData("Shower", new Vector2(21.27f, 0.06f),0f),
            new CustomTaskData("MainHall2", new Vector2(5.14f, 3.31f))
        }, CreateMetaOption(Color.white, "spectreTask.letter.airship", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        statueTaskSetting.AddSetting(0,
            new List<CustomTaskData>{
            new CustomTaskData("Storage", new Vector2(-0.4934f, -14.4472f)),
            new CustomTaskData("Cafeteria", new Vector2(0.5851f, 5.6232f)),
            new CustomTaskData("RightSide", new Vector2(12.3742f, -3.0637f)),
            new CustomTaskData("LeftSide", new Vector2(-11.6061f, -11.3865f)),
            new CustomTaskData("Reactor", new Vector2(-19.458f, -6.8135f)),
            new CustomTaskData("UpperEngine", new Vector2(-17.929f, 2.5338f)),
            new CustomTaskData("MedBay", new Vector2(-9.3721f, -5.1098f))
        }, CreateMetaOption(Color.white, "spectreTask.statue.skeld", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        statueTaskSetting.AddSetting(1,
            new List<CustomTaskData> {
            new CustomTaskData("Launchpad", new Vector2(-3.25f, 3.79f)),
            new CustomTaskData("Lower", new Vector2(6.4548f, -0.6719f)),
            new CustomTaskData("Comms", new Vector2(16.3502f, 2.9761f)),
            new CustomTaskData("Locker", new Vector2(10.3955f, 0.6231f)),
            new CustomTaskData("Laboratory", new Vector2(11.9223f, 10.3604f)),
            new CustomTaskData("Reactor", new Vector2(2.4664f, 13.5236f)),
            new CustomTaskData("Office", new Vector2(19.629f, 20.6572f)),
            new CustomTaskData("Greenhouse", new Vector2(15.8851f, 24.3287f)),
            new CustomTaskData("Cafeteria", new Vector2(28.8174f, 0.0511f))
        }, CreateMetaOption(Color.white, "spectreTask.statue.mira", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        statueTaskSetting.AddSetting(2,
            new List<CustomTaskData>(){
            new CustomTaskData("Upper", new Vector2(20.7383f, -7.959f)),
            new CustomTaskData("Laboratory", new Vector2(32.1845f,-10.0475f)),
            new CustomTaskData("UpperDecon", new Vector2(40.6586f, -10.4651f)),
            new CustomTaskData("Specimen", new Vector2(37.4256f, -21.9897f)),
            new CustomTaskData("SpecimenToAdmin", new Vector2(27.34f, -20.68f)),
            new CustomTaskData("Office",new Vector2(30.8732f, -17.2265f)),
            new CustomTaskData("Electrical",new Vector2(7.2648f, -13.0184f)),
            new CustomTaskData("LifeSupp", new Vector2(0.81f, -22.02f)),
            new CustomTaskData("Weapons",new Vector2(12.6854f, -24.6149f))
        }, CreateMetaOption(Color.white, "spectreTask.statue.polus", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));

        statueTaskSetting.AddSetting(4,
            new List<CustomTaskData>(){
            new CustomTaskData("Comms", new Vector2(-12.1675f, 0.9115f)),
            new CustomTaskData("Cockpit", new Vector2(-17.51f, 0.92f)),
            new CustomTaskData("ViewingDeck", new Vector2(-13.0425f, -14.714f)),
            new CustomTaskData("Security", new Vector2(5.8124f, -14.4985f)),
            new CustomTaskData("Electrical", new Vector2(20.128f, -3.8049f)),
            new CustomTaskData("Medical", new Vector2(23.4159f, -5.3001f)),
            new CustomTaskData("CargoBay", new Vector2(39.2912f, -3.4117f)),
            new CustomTaskData("Lounge", new Vector2(22.3679f, 10.7894f)),
            new CustomTaskData("GapRoom", new Vector2(13.5349f, 8.1746f)),
            new CustomTaskData("MainHall", new Vector2(8.43f, 1.85f)),
            new CustomTaskData("Vault", new Vector2(-8.8081f, 4.8566f)),
            new CustomTaskData("MeetingRoom1", new Vector2(3.6384f, 14.8067f)),
            new CustomTaskData("MeetingRoom2", new Vector2(17.0308f, 14.6693f)),
        }, CreateMetaOption(Color.white, "spectreTask.statue.airship", int.MaxValue).HiddenOnDisplay(true).HiddenOnMetaScreen(true));
    }

    public override void LoadOptionData()
    {
        spawnImmoralistOption = CreateOption(Color.white, "spawnImmoralist", true);
        occupyDoubleRoleCountOption = CreateOption(Color.white, "occupyDoubleRoleCount", true).AddPrerequisite(spawnImmoralistOption);

        spectreTaskOption = CreateOption(Color.white, "spectreTask", new string[] { "option.switch.off","role.spectre.spectreTask.eatTheFried", "role.spectre.spectreTask.deliveryRancor" },(object)"role.spectre.spectreTask.eatTheFried");
        numOfTheFriedRequireToWinOption = CreateOption(Color.white, "numOfTheFriedRequiredToWin", 5f, 1f, 16f, 1f).AddCustomPrerequisite(() => spectreTaskOption.getSelection() == 1);

        SetUpCustomTaskOption();

        spectreTaskOption.alternativeOptionScreenBuilder = (refresher) =>
        {
            MetaScreenContent getSuitableContent()
            {
                if (spectreTaskOption.getSelection() == 1)
                    return new MSButton(1.6f, 0.4f, "Customize", TMPro.FontStyles.Bold, () =>
                    {
                        Action<byte> refresher = getRefresher(new Tuple<CustomTaskSetting, Sprite?>(friedTaskSetting, spectreFriedConsoleSprite.GetSprite()));
                        refresher(GameOptionsManager.Instance.CurrentGameOptions.MapId);
                    });
                else if (spectreTaskOption.getSelection() == 2)
                    return new MSButton(1.6f, 0.4f, "Customize", TMPro.FontStyles.Bold, () =>
                    {
                        Action<byte> refresher = getRefresher(new Tuple<CustomTaskSetting, Sprite?>(letterTaskSetting, spectreLetterConsoleSprite.GetSprite()), new Tuple<CustomTaskSetting, Sprite?>(statueTaskSetting, spectreRancorConsoleSprite.GetSprite()));
                        refresher(GameOptionsManager.Instance.CurrentGameOptions.MapId);
                    });
                else
                    return new MSMargin(1.7f);
            }

            return new MetaScreenContent[][] {
                    new MetaScreenContent[]
                    {
                        new MSMargin(1.9f),
                       new CustomOption.MSOptionString(spectreTaskOption,3f, spectreTaskOption.getName(), 2f, 0.8f, TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold),
                    new MSString(0.2f, ":", TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                    new MSButton(0.4f, 0.4f, "<<", TMPro.FontStyles.Bold, () =>
                    {
                        spectreTaskOption.addSelection(-1);
                        refresher();
                    }),
                    new MSString(1.5f, spectreTaskOption.getString(), 2f, 0.6f, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold),
                    new MSButton(0.4f, 0.4f, ">>", TMPro.FontStyles.Bold, () =>
                    {
                        spectreTaskOption.addSelection(1);
                        refresher();
                    }),
                    new MSMargin(0.2f),
                    getSuitableContent(),
                    new MSMargin(1f)
                    }
                };
        };

        clarifyChargeOption = CreateOption(Color.white, "clarifyCharge", 1f, 0f, 16f, 1f);
        clarifyDurationOption = CreateOption(Color.white, "clarifyDuration", 10f, 5f, 40f, 2.5f);
        clarifyDurationOption.suffix = "second";
        clarifyCoolDownOption = CreateOption(Color.white, "clarifyCoolDown", 10f, 5f, 40f, 5f);
        clarifyCoolDownOption.suffix = "second";

        lastImpostorCanGuessSpectreOption = CreateOption(Color.white, "lastImpostorCanGuessSpectre", true);

        canFixEmergencySabotageOption = CreateOption(Color.white, "canFixEmergencySabotage", false);

        canTakeOverSabotageWinOption = CreateOption(Color.white, "canTakeOverSabotageWin", true);
        canTakeOverTaskWinOption = CreateOption(Color.white, "canTakeOverTaskWin", true);

        ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f);
        ventCoolDownOption.suffix = "second";
        ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f);
        ventDurationOption.suffix = "second";   
    }

    public override Role[] AssignedRoles => spawnImmoralistOption.getBool() ? (new Role[] { this, Roles.Immoralist }) : (new Role[] { this });
    public override int AssignmentCost => (spawnImmoralistOption.getBool() && occupyDoubleRoleCountOption.getBool()) ? 2 : 1;
    public override int GetCustomRoleCount() => 1;
    public override bool HasExecutableFakeTask(byte playerId) => true;

    public override IEnumerable<Assignable> GetFollowRoles()
    {
        yield return Roles.Immoralist;
    }

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);

        VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
        VentDurationMaxTimer = ventDurationOption.getFloat();
    }

    public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
    {
        if (PlayerControl.LocalPlayer.GetModData().role == Roles.Immoralist)
            displayColor = RoleColor;
    }

    public override bool CanKnowImpostors { get => true; }

    public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
    {
        initialTasks.Clear();
        if (spectreTaskOption.getSelection() != 0) initialTasks.Add(new GameData.TaskInfo(byte.MaxValue - 3, 0));
    }

    public override void OnMeetingEnd()
    {
        foreach(var task in PlayerControl.LocalPlayer.myTasks.GetFastEnumerator())
        {
            var rancor = task.TryCast<SpectreRancorTask>();
            if (rancor)
                rancor.OnMeetingEnd();
        }
    }

    SpriteLoader arrowSprite = new SpriteLoader("role.spectre.arrow");
    public override void MyPlayerControlUpdate()
    {
        int i = 0;
        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if ((p.Data.Role.IsImpostor || p.GetModData().role.DeceiveImpostorInNameDisplay) && !p.Data.IsDead)
            {
                if (impostorArrows.Count >= i) impostorArrows.Add(null);

                var arrow = impostorArrows[i];
                RoleSystem.TrackSystem.PlayerTrack_MyControlUpdate(ref arrow, p, Palette.ImpostorRed,arrowSprite);
                impostorArrows[i] = arrow;

                i++;
            }
        }
        int removed = impostorArrows.Count - i;
        for (; i < impostorArrows.Count; i++) if (impostorArrows[i] != null) GameObject.Destroy(impostorArrows[i].arrow);
        impostorArrows.RemoveRange(impostorArrows.Count - removed, removed);
    }

    //ラストインポスターに推察チャンスを与える
    public override void OnAnyoneDied(byte playerId)
    {
        if (!lastImpostorCanGuessSpectreOption.getBool()) return;

        int impostors = 0;
        byte impostorId = 0;
        foreach(var p in Game.GameData.data.AllPlayers.Values)
        {
            if (!p.IsAlive) continue;
            if (p.role.side != Side.Impostor) continue;
            if (p.HasExtraRole(Roles.LastImpostor)) return;

            impostors++;
            impostorId = p.id;
        }
        if (impostors != 1) return;

        RPCEventInvoker.AddExtraRole(Helpers.playerById(impostorId), Roles.LastImpostor, 0);
    }

    public IEnumerator CoAnimateFox(SpriteRenderer renderer,float duration)
    {
        float t = 0f;
        int i = 0;
        while (true)
        {
            t -= Time.deltaTime;
            duration -= Time.deltaTime;
            if (t < 0f)
            {
                renderer.sprite = spectreFoxAnimationSprites[i].GetSprite();
                i++;
                if (i == 15 && duration > 2.2f) i = 11;
                if (i >= 25) break;

                t = 0.115f;
            }
            yield return null;
        }
    }

    public Spectre()
    : base("Spectre", "spectre", RoleColor, RoleCategory.Neutral, Side.Spectre, Side.Spectre,
         new HashSet<Side>() { Side.Spectre }, new HashSet<Side>() { Side.Spectre },
         new HashSet<Patches.EndCondition>() { EndCondition.SpectreWin },
         true, VentPermission.CanUseLimittedVent, true, true, true)
    {
        clarifyChargeId = Game.GameData.RegisterRoleDataId("spectre.clarifyCharge");
        FixedRoleCount = true;
        canReport = false;
        impostorArrows = new List<Arrow?>();

        spectreFoxAnimationSprites = new ISpriteLoader[25];
        for(int i = 0; i < 25; i++)
            spectreFoxAnimationSprites[i] = new AssetSpriteLoader(AssetLoader.NebulaMainAsset,
                string.Format("assets/Animations/Fox/{0:00}.png", i), 115f);

        spectreStatueSprites = new ISpriteLoader[5];
        for (int i = 0; i < 5; i++)
            spectreStatueSprites[i] = new AssetSpriteLoader(AssetLoader.NebulaMainAsset,
                string.Format("assets/Minigames/SpectreStatueMinigame/Statue{0}.png", i + 1), 100f);

    }
}
