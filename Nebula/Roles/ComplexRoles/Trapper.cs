using Nebula.Game;
using Rewired.Utils.Platforms.Windows;

namespace Nebula.Roles.ComplexRoles;

public class FTrapper : Template.HasBilateralness
{
    public Module.CustomOption maxTrapsOption;
    public Module.CustomOption placeCoolDownOption;
    public Module.CustomOption accelTrapSpeedOption;
    public Module.CustomOption decelTrapSpeedOption;
    public Module.CustomOption accelTrapDurationOption;
    public Module.CustomOption decelTrapDurationOption;
    public Module.CustomOption commButtonCostOption;
    public Module.CustomOption killButtonCostOption;
    public Module.CustomOption rootTimeOption;
    public Module.CustomOption killTrapAudibleDistanceOption;

    static public Color RoleColor = new Color(206f / 255f, 219f / 255f, 96f / 255f);

    public int remainTrapsId { get; private set; }

    public static SpriteLoader accelButtonSprite = new SpriteLoader("Nebula.Resources.AccelTrapButton.png", 115f);
    public static SpriteLoader decelButtonSprite = new SpriteLoader("Nebula.Resources.DecelTrapButton.png", 115f);
    public static SpriteLoader killButtonSprite = new SpriteLoader("Nebula.Resources.KillTrapButton.png", 115f);
    public static SpriteLoader commButtonSprite = new SpriteLoader("Nebula.Resources.CommTrapButton.png", 115f);
    public static SpriteLoader commArrowSprite = new SpriteLoader("role.trapper.arrow");

    public override void LoadOptionData()
    {
        base.LoadOptionData();

        TopOption.tab = Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.ImpostorRoles;

        maxTrapsOption = CreateOption(Color.white, "maxTraps", 5f, 1f, 15f, 1f);
        placeCoolDownOption = CreateOption(Color.white, "placeCoolDown", 15f, 5f, 60f, 2.5f);
        placeCoolDownOption.suffix = "second";
        accelTrapSpeedOption = CreateOption(Color.white, "accelSpeed", 1.5f, 1f, 2f, 0.125f);
        accelTrapSpeedOption.suffix = "cross";
        decelTrapSpeedOption = CreateOption(Color.white, "decelSpeed", 0.5f, 0.125f, 1f, 0.125f);
        decelTrapSpeedOption.suffix = "cross";
        accelTrapDurationOption = CreateOption(Color.white, "accelDuration", 5f, 2.5f, 30f, 2.5f);
        accelTrapDurationOption.suffix = "second";
        decelTrapDurationOption = CreateOption(Color.white, "decelDuration", 5f, 2.5f, 30f, 2.5f);
        decelTrapDurationOption.suffix = "second";

        commButtonCostOption = CreateOption(Color.white, "commTrapCost", 2f, 1f, 15f, 1f);
        commButtonCostOption.suffix = "cross";
        killButtonCostOption = CreateOption(Color.white, "killTrapCost", 2f, 1f, 15f, 1f);
        killButtonCostOption.suffix = "cross";

        rootTimeOption = CreateOption(Color.white, "rootTime", 2f, 2f, 3f, 0.5f);
        rootTimeOption.suffix = "second";

        killTrapAudibleDistanceOption = CreateOption(Color.white, "killTrapAudibleDistance", 10f, 2.5f, 40f, 2.5f);
        killTrapAudibleDistanceOption.suffix = "cross";

        FirstRole = Roles.NiceTrapper;
        SecondaryRole = Roles.EvilTrapper;
    }

    public FTrapper()
            : base("Trapper", "trapper", RoleColor)
    {
        remainTrapsId = Game.GameData.RegisterRoleDataId("trapper.remainTraps");

        Objects.CustomObject.RegisterUpdater((player) =>
        {
            if (HnSModificator.IsHnSGame) return;

            CustomObject trap = Objects.CustomObject.GetTarget(0.875f / 2f, player, (obj) => { return obj.PassedMeetings > 0; }, Objects.ObjectTypes.VisibleTrap.AccelTrap, Objects.ObjectTypes.VisibleTrap.DecelTrap);
            if (trap == null) return;

            if (trap.ObjectType == Objects.ObjectTypes.VisibleTrap.AccelTrap)
            {
                RPCEventInvoker.EmitSpeedFactor(player,
                    new Game.SpeedFactor(1, accelTrapDurationOption.getFloat(), accelTrapSpeedOption.getFloat(), false));
            }
            else
            {
                RPCEventInvoker.EmitSpeedFactor(player,
                    new Game.SpeedFactor(1, decelTrapDurationOption.getFloat(), decelTrapSpeedOption.getFloat(), false));
            }
        });
    }

    public override List<Role> GetImplicateRoles() { return new List<Role>() { Roles.EvilTrapper, Roles.NiceTrapper }; }
}

public class Trapper : Template.BilateralnessRole
{
    private CustomButton trapButton;
    private byte trapKind;
    private static List<byte> detectedPlayers = new List<byte>();

    public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(Roles.F_Trapper.remainTrapsId, "Traps", 0, 20) }; }


    //インポスターはModで操作するFakeTaskは所持していない
    public Trapper(string name, string localizeName, bool isImpostor)
            : base(name, localizeName,
                 isImpostor ? Palette.ImpostorRed : FTrapper.RoleColor,
                 isImpostor ? RoleCategory.Impostor : RoleCategory.Crewmate,
                 isImpostor ? Side.Impostor : Side.Crewmate, isImpostor ? Side.Impostor : Side.Crewmate,
                 isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                 isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                 isImpostor ? ImpostorRoles.Impostor.impostorEndSet : CrewmateRoles.Crewmate.crewmateEndSet,
                 false, isImpostor ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse,
                 isImpostor, isImpostor, isImpostor, () => { return Roles.F_Trapper; }, isImpostor)
    {
        IsHideRole = true;
        trapButton = null;
    }

    public override Assignable AssignableOnHelp => Roles.F_Trapper;

    public override HelpSprite[] helpSprite => new HelpSprite[] {
            new HelpSprite(FTrapper.accelButtonSprite,"role.trapper.help.accel",0.3f),
            new HelpSprite(FTrapper.decelButtonSprite,"role.trapper.help.decel",0.3f),
            (side==Side.Impostor) ? new HelpSprite(FTrapper.killButtonSprite,"role.trapper.help.kill",0.3f) : new HelpSprite(FTrapper.commButtonSprite,"role.trapper.help.comm",0.3f)
        };

    public override void GlobalInitialize(PlayerControl __instance)
    {
        __instance.GetModData().SetRoleData(Roles.F_Trapper.remainTrapsId, (int)Roles.F_Trapper.maxTrapsOption.getFloat());
    }

    public override void MyUpdate()
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) return;

        //探知されていないプレイヤーを除去する
        detectedPlayers.RemoveAll((id) =>
        {
            PlayerControl player = Helpers.playerById(id);
            float dis = 1.125f / 2f;
            foreach (CustomObject obj in CustomObject.Objects.Values)
            {
                if (obj.ObjectType == Objects.ObjectTypes.InvisibleTrap.CommTrap)
                {
                    if (obj.GameObject.transform.position.Distance(player.transform.position) < dis) return false;
                }
            }
            return true;
        });

        if (CustomObject.Objects.Values.Count > 0)
        {
            foreach (CustomObject obj in new List<CustomObject>(CustomObject.Objects.Values))
            {
                if (obj.PassedMeetings == 0) continue;
                if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId) continue;

                if (obj.ObjectType == Objects.ObjectTypes.InvisibleTrap.KillTrap)
                {
                    if (MeetingHud.Instance || ExileController.Instance) continue;
                    if (obj.Data[0] != 0) continue;
                    if (PlayerControl.LocalPlayer.killTimer > 0f) continue;

                    PlayerControl player = Patches.PlayerControlPatch.GetTarget(obj.GameObject.transform.position, 1.125f / 2f, side == Side.Impostor);
                    if (player != null)
                    {
                        if (player.Data.IsDead) continue;

                        RPCEventInvoker.ObjectUpdate(obj, 0);
                        Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, player, Game.PlayerData.PlayerStatus.Trapped, false, false);

                        PlayerControl.LocalPlayer.killTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
                    }

                }
                else if (obj.ObjectType == Objects.ObjectTypes.InvisibleTrap.CommTrap)
                {
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                    {
                        if (player.Data.IsDead) continue;
                        if (player.inVent) continue;
                        if (detectedPlayers.Contains(player.PlayerId)) continue;

                        if (player.transform.position.Distance(obj.GameObject.transform.position) < 1.125f / 2f)
                        {
                            Arrow arrow = new Arrow(Palette.PlayerColors[player.CurrentOutfit.ColorId],true, FTrapper.commArrowSprite.GetSprite());
                            arrow.arrow.SetActive(true);
                            arrow.Update(obj.GameObject.transform.position);
                            detectedPlayers.Add(player.PlayerId);

                            byte id = player.PlayerId;
                            Vector3 pos = obj.GameObject.transform.position;
                            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(5f, new Action<float>((p) =>
                            {
                                arrow.Update(pos);
                                if (p > 0.8f)
                                {
                                    arrow.image.color = new Color(arrow.image.color.r, arrow.image.color.g, arrow.image.color.b, (1f - p) * 5f);
                                }
                                if (p == 1f)
                                {
                                        //矢印を消す
                                        UnityEngine.Object.Destroy(arrow.arrow);
                                }
                            })));
                        }
                    }
                }
            }
        }
    }

    private void SetTrapType(int type)
    {
        trapKind = (byte)(type % 3);

        switch (trapKind)
        {
            case 0:
                trapButton.Sprite = FTrapper.accelButtonSprite.GetSprite();
                break;
            case 1:
                trapButton.Sprite = FTrapper.decelButtonSprite.GetSprite();
                break;
            case 2:
                if (side == Side.Impostor)
                {
                    trapButton.Sprite = FTrapper.killButtonSprite.GetSprite();
                }
                else
                {
                    trapButton.Sprite = FTrapper.commButtonSprite.GetSprite();
                }
                break;
        }
    }

    private void ChangeTrapType()
    {
        SetTrapType(trapKind + 1);
    }
    public override void ButtonInitialize(HudManager __instance)
    {
        if (trapButton != null)
        {
            trapButton.Destroy();
        }
        trapButton = new CustomButton(
            () =>
            {
                bool isKillTrap = false;
                switch (trapKind)
                {
                    case 0:
                        RPCEventInvoker.ObjectInstantiate(CustomObject.Type.AccelTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset + new Vector3(0f, 0.05f, 0f));
                        RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.F_Trapper.remainTrapsId, -1);
                        break;
                    case 1:
                        RPCEventInvoker.ObjectInstantiate(CustomObject.Type.DecelTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset + new Vector3(0f, 0.05f, 0f));
                        RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.F_Trapper.remainTrapsId, -1);
                        break;
                    case 2:
                        if (side == Side.Impostor)
                        {
                            RPCEventInvoker.ObjectInstantiate(CustomObject.Type.KillTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset + new Vector3(0f, 0.05f, 0f));
                            RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.F_Trapper.remainTrapsId, -(int)Roles.F_Trapper.killButtonCostOption.getFloat());
                            isKillTrap = true;
                        }
                        else
                        {
                            RPCEventInvoker.ObjectInstantiate(CustomObject.Type.CommTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset + new Vector3(0f, 0.05f, 0f));
                            RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.F_Trapper.remainTrapsId, -(int)Roles.F_Trapper.commButtonCostOption.getFloat());
                        }
                        break;
                }

                if (Roles.F_Trapper.rootTimeOption.getFloat() > 0f)
                    RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(2, Roles.F_Trapper.rootTimeOption.getFloat(), 0f, false));

                trapButton.Timer = Roles.F_Trapper.rootTimeOption.getFloat();

                Objects.SoundPlayer.PlaySound((trapButton.Timer < 3f) ? Module.AudioAsset.PlaceTrap2s : Module.AudioAsset.PlaceTrap3s);

                if (isKillTrap)
                {
                    float distance = Roles.F_Trapper.killTrapAudibleDistanceOption.getFloat();
                    PlayerControl.LocalPlayer.StartCoroutine(Effects.Lerp(trapButton.Timer, (Il2CppSystem.Action<float>)((p) =>
                    {
                        if (p == 1f) RPCEventInvoker.PlayDynamicSound(PlayerControl.LocalPlayer.transform.position,
                           Module.AudioAsset.PlaceKillTrap, distance, distance * 0.6f);
                    })));
                }
            },
            () =>
            {
                int remain = Game.GameData.data.myData.getGlobalData().GetRoleData(Roles.F_Trapper.remainTrapsId);
                trapButton.UsesText.text = remain.ToString();

                return remain > 0 && !PlayerControl.LocalPlayer.Data.IsDead;

            },
            () =>
            {
                if (!PlayerControl.LocalPlayer.CanMove) return false;

                int left = Game.GameData.data.myData.getGlobalData().GetRoleData(Roles.F_Trapper.remainTrapsId);

                switch (trapKind)
                {
                    case 0:
                    case 1:
                        return left >= 1;
                    case 2:
                        if (side == Side.Impostor)
                        {
                            return left >= (int)Roles.F_Trapper.killButtonCostOption.getFloat();
                        }
                        else
                        {
                            return left >= (int)Roles.F_Trapper.commButtonCostOption.getFloat();
                        }
                }
                return false;
            },
            () => { trapButton.Timer = trapButton.MaxTimer; },
            FTrapper.accelButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            Roles.F_Trapper.rootTimeOption.getFloat(),
            () =>
            {
                trapButton.Timer = trapButton.MaxTimer;
            }, "button.label.place"
        ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
        trapButton.MaxTimer = Roles.F_Trapper.placeCoolDownOption.getFloat();
        trapButton.EffectDuration = Roles.F_Trapper.rootTimeOption.getFloat();
        trapButton.SetUsesIcon(side == Side.Impostor ? 1 : 0);

        trapButton.SetAidAction(Module.NebulaInputManager.changeAbilityInput.keyCode, true, ChangeTrapType);

        trapKind = 0;

        //直近にすれ違った人をリセットする
        detectedPlayers.Clear();
    }
    public override void CleanUp()
    {
        if (trapButton != null)
        {
            trapButton.Destroy();
            trapButton = null;
        }
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Navvy);
        RelatedRoles.Add(Roles.Vulture);
    }
}