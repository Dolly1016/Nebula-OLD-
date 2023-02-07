using JetBrains.Annotations;
using Nebula.Expansion;
using Nebula.Map;
using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.NeutralRoles;

public class Spectre : Role
{
    static public Color RoleColor = new Color(185f / 255f, 152f / 255f, 197f / 255f);

    public Module.CustomOption spawnImmoralistOption;
    private Module.CustomOption occupyDoubleRoleCountOption;
    public Module.CustomOption numOfTheFriedRequireToWinOption;
    private Module.CustomOption clarifyChargeOption;
    private Module.CustomOption clarifyDurationOption;
    private Module.CustomOption clarifyCoolDownOption;
    public Module.CustomOption canTakeOverSabotageWinOption;
    public Module.CustomOption canTakeOverTaskWinOption;
    public Module.CustomOption lastImpostorCanGuessSpectreOption;
    private Module.CustomOption ventCoolDownOption;
    private Module.CustomOption ventDurationOption;

    private List<Objects.Arrow?> impostorArrows;

    int clarifyChargeId;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.SpectreButton.png", 115f);

    public SpriteLoader spectreConsoleSprite = new SpriteLoader("Nebula.Resources.SpectreMinigameConsole.png", 100f);
    public SpriteLoader spectreConsoleEatenSprite = new SpriteLoader("Nebula.Resources.SpectreMinigameConsoleUsed.png", 100f);

    static Dictionary<byte, List<FriedTaskData>> friedTaskPos = new Dictionary<byte, List<FriedTaskData>>();
    public Dictionary<int, GameObject> FriedConsoles = new Dictionary<int, GameObject>();

    class FriedTaskData : PointData
    {
        public float z { get; private set; }
        public float usableDistance { get; private set; }
        public FriedTaskData(string name, Vector2 pos,float z) : base(name, pos) {
            this.z = z;
            usableDistance = 0.7f;
        }
        public FriedTaskData(string name, Vector2 pos) : base(name, pos) {
            z = pos.y / 1000f + 0.001f;
            usableDistance = 0.7f;
        }

        public FriedTaskData SetUsableDistance(float distance)
        {
            usableDistance= distance;
            return this;
        }
    }

    public override void CustomizeMap(byte mapId)
    {
        if (!IsSpawnable()) return;


        FriedConsoles.Clear();

        int i = 0;

        foreach(var data in friedTaskPos[mapId])
        {
            var console = ConsoleExpansion.GenerateConsole(new Vector3(data.point.x, data.point.y, data.z), "NoS-SpectreFried-" + data.name, spectreConsoleSprite.GetSprite());
            console.usableDistance = data.usableDistance;

            {
                console.gameObject.layer = LayerExpansion.GetDefaultLayer();
                var sObj = new GameObject("inShaddow");
                sObj.transform.SetParent(console.transform);
                sObj.transform.localPosition = Vector2.zero;
                sObj.transform.localScale = Vector2.one;
                sObj.layer = LayerExpansion.GetShadowLayer();
                sObj.AddComponent<SpriteRenderer>().sprite = spectreConsoleEatenSprite.GetSprite();
            }

            FriedConsoles.Add(i,console.gameObject);
            i++;
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

    public override void LoadOptionData()
    {
        spawnImmoralistOption = CreateOption(Color.white, "spawnImmoralist", true);
        occupyDoubleRoleCountOption = CreateOption(Color.white, "occupyDoubleRoleCount", true).AddPrerequisite(spawnImmoralistOption);

        numOfTheFriedRequireToWinOption = CreateOption(Color.white, "numOfTheFriedRequiredToWin", 5f, 1f, 16f, 1f);

        clarifyChargeOption = CreateOption(Color.white, "clarifyCharge", 1f, 0f, 16f, 1f);
        clarifyDurationOption = CreateOption(Color.white, "clarifyDuration", 10f, 5f, 40f, 2.5f);
        clarifyDurationOption.suffix = "second";
        clarifyCoolDownOption = CreateOption(Color.white, "clarifyCoolDown", 10f, 5f, 40f, 5f);
        clarifyCoolDownOption.suffix = "second";

        lastImpostorCanGuessSpectreOption = CreateOption(Color.white, "lastImpostorCanGuessSpectre", true);

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

    /*
    public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
    {
        initialTasks.Clear();
        int tasks = (int)clarifyChargeOption.getFloat();

        var unused = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
        foreach (var t in ShipStatus.Instance.NormalTasks)
        {
            if (t.TaskType == TaskTypes.PickUpTowels) continue;
            if (t.TaskType == TaskTypes.UploadData) continue;
            unused.Add(t);
        }
        Extensions.Shuffle<NormalPlayerTask>(unused.Cast<Il2CppSystem.Collections.Generic.IList<NormalPlayerTask>>(), 0);

        for (int i = 0; i < tasks; i++) initialTasks.Add(new GameData.TaskInfo((byte)unused[i].Index, (uint)i));
    }
    */

    public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
    {
        initialTasks.Clear();
        initialTasks.Add(new GameData.TaskInfo(byte.MaxValue - 3, 0));   
    }

    /*
    public override void OnTaskComplete()
    {
        RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, clarifyChargeId, 1);
        spectreButton.UsesText.text = Game.GameData.data.myData.getGlobalData().GetRoleData(clarifyChargeId).ToString();
    }
    */

    public override void MyPlayerControlUpdate()
    {
        int i = 0;
        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if ((p.Data.Role.IsImpostor || p.GetModData().role.DeceiveImpostorInNameDisplay) && !p.Data.IsDead)
            {
                if (impostorArrows.Count >= i) impostorArrows.Add(null);

                var arrow = impostorArrows[i];
                RoleSystem.TrackSystem.PlayerTrack_MyControlUpdate(ref arrow, p, Palette.ImpostorRed);
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

    public Spectre()
    : base("Spectre", "spectre", RoleColor, RoleCategory.Neutral, Side.Spectre, Side.Spectre,
         new HashSet<Side>() { Side.Spectre }, new HashSet<Side>() { Side.Spectre },
         new HashSet<Patches.EndCondition>() { EndCondition.SpectreWin },
         true, VentPermission.CanUseLimittedVent, true, true, true)
    {
        clarifyChargeId = Game.GameData.RegisterRoleDataId("spectre.clarifyCharge");
        FixedRoleCount = true;
        canReport = false;
        canFixEmergencySabotage = false;
        impostorArrows = new List<Arrow?>();

        List<FriedTaskData> points;

        points = new List<FriedTaskData>{
            new FriedTaskData("Cafe0", new Vector2(1.88f, -1.97f)).SetUsableDistance(1.2f),
            new FriedTaskData("Cafe1", new Vector2(-3.31f, 3.13f)).SetUsableDistance(1.2f),
            new FriedTaskData("Right", new Vector2(12.27f, -3.12f)),
            new FriedTaskData("Shields", new Vector2(9.84f, -12.82f)),
            new FriedTaskData("Storage", new Vector2(-0.65f, -14.6f)),
            new FriedTaskData("Electrical", new Vector2(-7.67f, -12.10f)),
            new FriedTaskData("Lower", new Vector2(-15.24f, -9.93f)),
            new FriedTaskData("Reactor", new Vector2(-22.57f, -6.64f)),
            new FriedTaskData("MedBay", new Vector2(-7.78f, -1.49f)),
            new FriedTaskData("Admin", new Vector2(5.99f, -9.88f))
        };
        friedTaskPos.Add(0,points);

        points = new List<FriedTaskData> {
            new FriedTaskData("Launchpad", new Vector2(-3.25f, 3.79f)),
            new FriedTaskData("Lower", new Vector2(6.47f, -0.82f)),
            new FriedTaskData("MedBay", new Vector2(15.57f, 1.21f)),
            new FriedTaskData("Locker", new Vector2(8.91f, 5.61f)),
            new FriedTaskData("Laboratory", new Vector2(10.71f, 12.96f),0.013f).SetUsableDistance(1f),
            new FriedTaskData("Upper", new Vector2(5.26f, 13.44f)),
            new FriedTaskData("Admin", new Vector2(19.47f, 20.61f)),
            new FriedTaskData("Greenhouse", new Vector2(21.07f, 24.30f),0.025f),
            new FriedTaskData("NextToAdmin", new Vector2(18.60f, 16.36f)),
            new FriedTaskData("Cafeteria", new Vector2(27.18f, 3.57f),-0.5f).SetUsableDistance(0.85f)
        };
        friedTaskPos.Add(1, points);

        points = new List<FriedTaskData>(){
            new FriedTaskData("Dropship", new Vector2(20.50f, -7.95f)),
            new FriedTaskData("Laboratory0", new Vector2(29.66f, -8.19f)),
            new FriedTaskData("Laboratory1", new Vector2(37.78f, -7.02f)).SetUsableDistance(0.8f),
            new FriedTaskData("Specimen", new Vector2(36.67f, -21.25f)),
            new FriedTaskData("SpecimenToAdmin", new Vector2(27.34f, -20.68f)),
            new FriedTaskData("Admin", new Vector2(22.25f, -25.14f)),
            new FriedTaskData("Office", new Vector2(20.82f, -17.05f),-0.017f),
            new FriedTaskData("Weapons", new Vector2(13.47f, -24.20f)),
            new FriedTaskData("Comms", new Vector2(11.50f, -16.84f)),
            new FriedTaskData("LifeSupp", new Vector2(0.81f, -22.02f)),
            new FriedTaskData("Electrical", new Vector2(4.60f, -9.20f)),
            new FriedTaskData("Storage", new Vector2(19.79f, -12.47f))
        };
        friedTaskPos.Add(2, points);

        points = new List<FriedTaskData>(){
            new FriedTaskData("MainHall0", new Vector2(13.17f, -2.20f)),
            new FriedTaskData("MainHall1", new Vector2(8.43f, 1.85f)),
            new FriedTaskData("MainHall2", new Vector2(5.14f, 3.31f)),
            new FriedTaskData("Engine", new Vector2(1.49f, -2.17f)).SetUsableDistance(0.2f),
            new FriedTaskData("Vault", new Vector2(-6.73f, 10.43f)),
            new FriedTaskData("MeetingRoom", new Vector2(3.32f, 15.72f)),
            new FriedTaskData("Comms", new Vector2(-14.38f, 0.87f)),
            new FriedTaskData("Cockpit", new Vector2(-17.51f, 0.92f)),
            new FriedTaskData("Armory", new Vector2(-15.04f, -9.26f)),
            new FriedTaskData("ViewingDeck", new Vector2(-13.05f, -14.77f)),
            new FriedTaskData("Security", new Vector2(5.05f, -10.35f)),
            new FriedTaskData("Medical0", new Vector2(24.32f, -8.76f)),
            new FriedTaskData("Medical1", new Vector2(29.39f, -7.40f),-0.007f).SetUsableDistance(0.8f),
            new FriedTaskData("CargoBay", new Vector2(35.96f, 1.71f)),
            new FriedTaskData("Lounge", new Vector2(24.41f, 6.34f)),
            new FriedTaskData("Shower", new Vector2(21.27f, 0.06f),0f),
        };
        friedTaskPos.Add(4, points);
    }
}
