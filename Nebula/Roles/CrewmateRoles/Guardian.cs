using Nebula.Module;

namespace Nebula.Roles.CrewmateRoles;

public class Guardian : Role
{
    static public Color RoleColor = new Color(171f / 255f, 131f / 255f, 85f / 255f);

    private CustomButton antennaButton;
    private CustomButton guardButton;
    private HashSet<Objects.CustomObject> myAntennaSet = new HashSet<CustomObject>();
    private Utilities.ObjectPool<SpriteRenderer>? indicatorsPool = null;

    private CustomOption maxAntennaOption;
    private CustomOption placeCoolDownOption;
    private CustomOption antennaEffectiveRangeOption;
    private CustomOption alertModeOption;
    private CustomOption alertIntervalOption;
    private CustomOption showGuardFlashOption;
    private CustomOption canIdentifyDeadBodyOption;

    private PlayerControl? killer;
    private float killerPingTimer;

    public int remainAntennasId { get; private set; }
    public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(remainAntennasId, "Antennas", 0, 20) }; }


    private PlayerControl? guardPlayer;

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Navvy);
        RelatedRoles.Add(Roles.NiceTrapper);
        RelatedRoles.Add(Roles.EvilTrapper);
    }

    public override void LoadOptionData()
    {
        maxAntennaOption = CreateOption(Color.white, "maxAntennas", 3f, 1f, 15f, 1f);
        placeCoolDownOption = CreateOption(Color.white, "placeCoolDown", 15f, 5f, 60f, 2.5f);
        placeCoolDownOption.suffix = "second";
        antennaEffectiveRangeOption = CreateOption(Color.white, "antennaEffectiveRange", 5f, 1.25f, 20f, 1.25f);
        antennaEffectiveRangeOption.suffix = "cross";
        alertModeOption = CreateOption(Color.white, "alertMode", false);
        alertIntervalOption = CreateOption(Color.white, "alertInterval", 10f, 5f, 30f, 2.5f).AddPrerequisite(alertModeOption);
        alertIntervalOption.suffix = "second";
        canIdentifyDeadBodyOption = CreateOption(Color.white, "canIdentifyDeadBody", false);
        showGuardFlashOption = CreateOption(Color.white, "showGuardFlash", false).AddInvPrerequisite(alertModeOption);
    }

    public override void Initialize(PlayerControl __instance)
    {
        indicatorsPool = null;
        myAntennaSet.Clear();
        guardPlayer = null;
    }

    public override void FinalizeInGame(PlayerControl __instance)
    {
        if (indicatorsPool != null) indicatorsPool.Destroy();
        if (guardPlayer != null) RPCEventInvoker.RemoveGuardian(guardPlayer, PlayerControl.LocalPlayer);
    }

    public override void OnDied()
    {
        if (guardPlayer != null) RPCEventInvoker.RemoveGuardian(guardPlayer, PlayerControl.LocalPlayer);
    }

    public override void MyMapUpdate(MapBehaviour mapBehaviour)
    {
        if (!mapBehaviour.GetTrackOverlay()) return;
        if (!mapBehaviour.GetTrackOverlay().activeSelf) return;

        if (indicatorsPool == null)
        {
            indicatorsPool = new Utilities.ObjectPool<SpriteRenderer>(mapBehaviour.HerePoint, mapBehaviour.GetTrackOverlay().transform);
            indicatorsPool.SetInitializer((renderer) =>
            {
                PlayerMaterial.SetColors(Palette.DisabledGrey, renderer);
            });
        }

        indicatorsPool.Reclaim();

        if (MeetingHud.Instance) return;

        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == PlayerControl.LocalPlayer) continue;
            if (p.Data.IsDead || !p.Visible || p.GetModData().isInvisiblePlayer) continue;

            bool showFlag = false;
            foreach (var a in myAntennaSet)
            {
                if (a.PassedMeetings == 0) continue;

                var vec = p.transform.position - a.GameObject.transform.position;
                float mag = vec.magnitude;
                if (mag > antennaEffectiveRangeOption.getFloat()) continue;
                if (PhysicsHelpers.AnyNonTriggersBetween(a.GameObject.transform.position, vec.normalized, mag, Constants.ShipAndAllObjectsMask)) continue;

                showFlag = true;
                break;
            }
            if (showFlag)
            {
                var icon = indicatorsPool.Get();
                icon.transform.localPosition = MapBehaviourExpansion.ConvertMapLocalPosition(p.transform.position, p.PlayerId);
                PlayerMaterial.SetColors(Module.DynamicColors.IsLightColor(Palette.PlayerColors[p.GetModData().CurrentOutfit.ColorId]) ? Color.white : Palette.DisabledGrey, icon);
            }
        }

        //死体も表示
        foreach (var p in Helpers.AllDeadBodies())
        {
            bool showFlag = false;
            foreach (var a in myAntennaSet)
            {
                if (a.PassedMeetings == 0) continue;

                var vec = p.transform.position - a.GameObject.transform.position;
                float mag = vec.magnitude;
                if (mag > antennaEffectiveRangeOption.getFloat()) continue;
                if (PhysicsHelpers.AnyNonTriggersBetween(a.GameObject.transform.position, vec.normalized, mag, Constants.ShipAndAllObjectsMask)) continue;

                showFlag = true;
                break;
            }
            if (showFlag)
            {
                var icon = indicatorsPool.Get();
                icon.transform.localPosition = MapBehaviourExpansion.ConvertMapLocalPosition(p.transform.position, p.ParentId);
                if (canIdentifyDeadBodyOption.getBool())
                    PlayerMaterial.SetColors(Color.red, icon);
                else
                    PlayerMaterial.SetColors(Module.DynamicColors.IsLightColor(Palette.PlayerColors[p.GetModData().GetOutfitData(0).ColorId]) ? Color.white : Palette.DisabledGrey, icon);
            }
        }
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        __instance.GetModData().SetRoleData(remainAntennasId, (int)maxAntennaOption.getFloat());
    }

    private static SpriteLoader placeButtonSprite = new SpriteLoader("Nebula.Resources.AntennaButton.png", 115f, "ui.button.guardian.place");
    private static SpriteLoader guardButtonSprite = new SpriteLoader("Nebula.Resources.GuardButton.png", 115f, "ui.button.guardian.guard");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(placeButtonSprite,"role.guardian.help.antenna",0.3f),
            new HelpSprite(guardButtonSprite,"role.guardian.help.guard",0.3f)
    };

    public override void MyPlayerControlUpdate()
    {
        if (guardPlayer == null)
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget();
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }
        else
        {
            Patches.PlayerControlPatch.SetPlayerOutline(guardPlayer, RoleColor);
        }

        if (killer != null && !killer.Data.IsDead && !MeetingHud.Instance && !ExileController.Instance)
        {
            killerPingTimer -= Time.deltaTime;
            if (killerPingTimer < 0f)
            {
                Helpers.Ping(killer.GetTruePosition(),false);
                killerPingTimer = alertIntervalOption.getFloat();
            }
        }
    }

    public override void OnAnyoneGuarded(byte murderId, byte targetId)
    {
        if (guardPlayer == null) return;

        
        if (targetId == guardPlayer.PlayerId)
        {
            if (alertModeOption.getBool())
            {
                killer = Helpers.playerById(murderId);
                killerPingTimer = 0f;
                RPCEventInvoker.RemoveGuardian(guardPlayer, PlayerControl.LocalPlayer);
            }
            else if(showGuardFlashOption.getBool())
                Helpers.PlayQuickFlash(RoleColor);
        }
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        killer = null;

        if (antennaButton != null)
        {
            antennaButton.Destroy();
        }
        antennaButton = new CustomButton(
            () =>
            {
                var obj = RPCEventInvoker.ObjectInstantiate(CustomObject.Type.Antenna, PlayerControl.LocalPlayer.transform.position);
                new Objects.EffectCircle(obj.GameObject, Palette.CrewmateBlue,antennaEffectiveRangeOption.getFloat());
                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, remainAntennasId, -1);
                myAntennaSet.Add(obj);
                antennaButton.Timer = antennaButton.MaxTimer;
                antennaButton.UsesText.text = Game.GameData.data.myData.getGlobalData().GetRoleData(remainAntennasId).ToString();
            },
            () =>
            {
                return !PlayerControl.LocalPlayer.Data.IsDead && Game.GameData.data.myData.getGlobalData().GetRoleData(remainAntennasId) > 0;
            },
            () =>
            {
                int remain = Game.GameData.data.myData.getGlobalData().GetRoleData(remainAntennasId);
                return remain > 0 && PlayerControl.LocalPlayer.CanMove;
            },
            () => { antennaButton.Timer = antennaButton.MaxTimer; },
            placeButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.place"
        ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
        antennaButton.UsesText.text = Game.GameData.data.myData.getGlobalData().GetRoleData(remainAntennasId).ToString();
        antennaButton.MaxTimer = placeCoolDownOption.getFloat();

        if (guardButton != null)
        {
            guardButton.Destroy();
        }
        guardButton = new CustomButton(
            () =>
            {
                var target = Game.GameData.data.myData.currentTarget;
                RPCEventInvoker.AddGuardian(target, PlayerControl.LocalPlayer);
                guardButton.UpperText.text = target.name;
                guardPlayer = target;
            },
            () =>
            {
                return !PlayerControl.LocalPlayer.Data.IsDead;
            },
            () =>
            {
                return guardPlayer == null && Game.GameData.data.myData.currentTarget != null && PlayerControl.LocalPlayer.CanMove;
            },
            () => { guardButton.Timer = guardButton.MaxTimer; },
            guardButtonSprite.GetSprite(),
           Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.secondaryAbilityInput.keyCode,
            "button.label.guard"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        guardButton.MaxTimer = 10f;
    }

    public override void CleanUp()
    {
        indicatorsPool = null;
        myAntennaSet.Clear();

        if (antennaButton != null)
        {
            antennaButton.Destroy();
            antennaButton = null;
        }

        if (guardButton != null)
        {
            guardButton.Destroy();
            guardButton = null;
        }
    }

    public Guardian()
        : base("Guardian", "guardian", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, false, false)
    {

        remainAntennasId = Game.GameData.RegisterRoleDataId("guardian.remainAntennas");
    }
}
