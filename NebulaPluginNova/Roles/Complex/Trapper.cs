﻿using NAudio.CoreAudioApi;
using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Complex;

file static class TrapperSystem
{
    private static SpriteLoader?[] buttonSprites = new SpriteLoader?[] { 
        SpriteLoader.FromResource("Nebula.Resources.Buttons.AccelTrapButton.png",115f),
        SpriteLoader.FromResource("Nebula.Resources.Buttons.DecelTrapButton.png",115f),
        SpriteLoader.FromResource("Nebula.Resources.Buttons.CommTrapButton.png",115f),
        SpriteLoader.FromResource("Nebula.Resources.Buttons.KillTrapButton.png",115f),
        null
    };
    private const int KillTrapId = 3;
    public static void OnActivated(RoleInstance myRole, int[] buttonVariation, List<Trapper.Trap> localTraps)
    {
        int buttonIndex = 0;

        var placeButton = myRole.Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.Ability).SubKeyBind(KeyAssignmentType.AidAction);
        placeButton.SetSprite(buttonSprites[buttonVariation[0]]?.GetSprite());
        placeButton.Availability = (button) => myRole.MyPlayer.MyControl.CanMove;
        placeButton.Visibility = (button) => !myRole.MyPlayer.MyControl.Data.IsDead;
        placeButton.OnClick = (button) =>
        {
            float duration = Trapper.PlaceDurationOption.GetFloat();
            NebulaAsset.PlaySE(duration < 3f ? NebulaAudioClip.Trapper2s : NebulaAudioClip.Trapper3s);
            button.ActivateEffect();
            PlayerModInfo.RpcAddModulator.Invoke((myRole.MyPlayer.PlayerId, new SpeedModulator(0f, true, duration, false, 10, 0)));
        };
        placeButton.OnEffectEnd = (button) => 
        {
            localTraps.Add(Trapper.Trap.GenerateTrap(buttonVariation[buttonIndex], myRole.MyPlayer.MyControl.GetTruePosition()));
            if (buttonVariation[buttonIndex] == KillTrapId) NebulaAsset.RpcPlaySE.Invoke((NebulaAudioClip.TrapperKillTrap, PlayerControl.LocalPlayer.transform.position, Trapper.KillTrapSoundDistanceOption.GetFloat() * 0.6f, Trapper.KillTrapSoundDistanceOption.GetFloat()));
        };
        placeButton.OnSubAction = (button) =>
        {
            if (button.EffectActive) return;
            buttonIndex = (buttonIndex + 1) % buttonVariation.Length;
            placeButton.SetSprite(buttonSprites[buttonVariation[0]]?.GetSprite());
        };
        placeButton.CoolDownTimer = myRole.Bind(new Timer(0f, Trapper.PlaceCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
        placeButton.EffectTimer = myRole.Bind(new Timer(0f, Trapper.PlaceDurationOption.GetFloat()));
        placeButton.SetLabelType(ModAbilityButton.LabelType.Standard);
        placeButton.SetLabel("place");
    }

    public static void OnMeetingStart(List<Trapper.Trap> localTraps, List<Trapper.Trap>? killTraps)
    {
        foreach (var lTrap in localTraps)
        {
            var gTrap = NebulaSyncObject.RpcInstantiate(Trapper.Trap.MyGlobalTag, new float[] { lTrap.TypeId, lTrap.Position.x, lTrap.Position.y }) as Trapper.Trap;
            if (gTrap != null) gTrap.AmOwner = true;
            NebulaSyncObject.LocalDestroy(lTrap.ObjectId);
            if (gTrap?.TypeId == KillTrapId) killTraps?.Add(gTrap!);
        }
        localTraps.Clear();
    }

}

[NebulaRPCHolder]
public class Trapper : ConfigurableStandardRole
{
    static public Trapper MyNiceRole = new(false);
    static public Trapper MyEvilRole = new(true);

    public bool IsEvil { get; private set; }
    public override RoleCategory RoleCategory => IsEvil ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole;

    public override string LocalizedName => IsEvil ? "evilTrapper" : "niceTrapper";
    public override Color RoleColor => IsEvil ? Palette.ImpostorRed : new Color(206f / 255f, 219f / 255f, 96f / 255f);
    public override Team Team => IsEvil ? Impostor.Impostor.MyTeam : Crewmate.Crewmate.MyTeam;
    public override IEnumerable<IAssignableBase> RelatedOnConfig() { if (MyNiceRole != this) yield return MyNiceRole; if (MyEvilRole != this) yield return MyEvilRole; }

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => IsEvil ? new EvilInstance(player) : new NiceInstance(player);

    static public NebulaConfiguration NumOfChargesOption = null!;
    static public NebulaConfiguration PlaceCoolDownOption = null!;
    static public NebulaConfiguration PlaceDurationOption = null!;
    static public NebulaConfiguration KillTrapSoundDistanceOption = null!;
    static public NebulaConfiguration CostOfKillTrapOption = null!;
    static public NebulaConfiguration CostOfCommTrapOption = null!;

    private NebulaConfiguration? CommonEditorOption;

    [NebulaPreLoad]
    public class Trap : NebulaSyncStandardObject
    {
        public static string MyGlobalTag = "TrapGlobal";
        public static string MyLocalTag = "TrapLocal";

        static SpriteLoader[] trapSprites = new SpriteLoader[] {
            SpriteLoader.FromResource("Nebula.Resources.AccelTrap.png",150f),
            SpriteLoader.FromResource("Nebula.Resources.DecelTrap.png",150f),
            SpriteLoader.FromResource("Nebula.Resources.CommTrap.png",150f),
            SpriteLoader.FromResource("Nebula.Resources.KillTrap.png",150f),
            SpriteLoader.FromResource("Nebula.Resources.KillTrapBroken.png",150f)
        };

        public int TypeId;
        public bool AmOwner = false;

        public Trap(Vector2 pos,int type, bool isLocal) : base(pos, ZOption.Back, true, trapSprites[type].GetSprite(), isLocal) {
            TypeId = type;

            //不可視
            if (TypeId >= 2) Color = Color.clear;
        }

        public static void Load()
        {
            NebulaSyncObject.RegisterInstantiater(MyGlobalTag, (args) => new Trap(new(args[1], args[2]), (int)args[0], false));
            NebulaSyncObject.RegisterInstantiater(MyLocalTag, (args) => new Trap(new(args[1], args[2]), (int)args[0], true));
        }

        static public Trap GenerateTrap(int type,Vector2 pos)
        {
            return (NebulaSyncObject.LocalInstantiate(MyLocalTag, new float[] { (float)type, pos.x, pos.y }) as Trap)!;
        }

        public void SetSpriteAsUsedKillTrap()
        {
            Sprite = trapSprites[4].GetSprite();
            Color = Color.white;
        }
    }

    public Trapper(bool isEvil)
    {
        IsEvil = isEvil;
    }

    protected override void LoadOptions()
    {
        base.LoadOptions();


        PlaceCoolDownOption ??= new NebulaConfiguration(null, "role.trapper.placeCoolDown", null, 5f, 60f, 5f, 20f, 20f) { Decorator = NebulaConfiguration.SecDecorator };
        PlaceDurationOption ??= new NebulaConfiguration(null, "role.trapper.placeDuration", null, 1f, 3f, 0.5f, 2f, 2f) { Decorator = NebulaConfiguration.SecDecorator };
        NumOfChargesOption ??= new NebulaConfiguration(null, "role.trapper.numOfCharges", null, 1, 15, 3, 3);

        if (IsEvil)
        {
            KillTrapSoundDistanceOption = new NebulaConfiguration(RoleConfig, "killTrapSoundDistanceOption", null, 0f, 20f, 1.25f, 10f, 10f) { Decorator = NebulaConfiguration.OddsDecorator };
            CostOfKillTrapOption = new NebulaConfiguration(RoleConfig, "costOfKillTrap", null, 1, 5, 1, 2, 2);
        }
        else
        {
            CostOfCommTrapOption = new NebulaConfiguration(RoleConfig, "costOfCommTrap", null, 1, 5, 1, 2, 2);
        }

        var commonOptions = new NebulaConfiguration[] { PlaceCoolDownOption, PlaceDurationOption, NumOfChargesOption };
        foreach (var option in commonOptions) option.Title = new CombinedComponent(new TranslateTextComponent("role.general.common"), new RawTextComponent(" "), new TranslateTextComponent(option.Id));

        CommonEditorOption = new NebulaConfiguration(RoleConfig, () => {
            MetaContext context = new();
            foreach (var option in commonOptions) context.Append(option.GetEditor()!);
            return context;
        });
    }

    public class NiceInstance : Crewmate.Crewmate.Instance
    {
        public override AbstractRole Role => MyNiceRole;
        private int leftCharge = NumOfChargesOption;
        private List<Trap> localTraps = new();
        public NiceInstance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner) TrapperSystem.OnActivated(this, new int[] { 0, 1, 2 },localTraps);
        }

        public override void OnMeetingStart()
        {
            if (AmOwner) TrapperSystem.OnMeetingStart(localTraps, null);
        }


    }

    public class EvilInstance : Impostor.Impostor.Instance
    {
        public override AbstractRole Role => MyEvilRole;
        private int leftCharge = NumOfChargesOption;
        private List<Trap> localTraps = new(), killTraps = new();
        public EvilInstance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner) TrapperSystem.OnActivated(this, new int[] { 0, 1, 3 }, localTraps);
        }

        public override void OnMeetingStart()
        {
            if (AmOwner) TrapperSystem.OnMeetingStart(localTraps,killTraps);
        }

        public override void LocalUpdate()
        {
            if (!(PlayerControl.LocalPlayer.killTimer > 0f)) {
                killTraps.RemoveAll((killTrap) => {
                foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
                {
                    if (p.AmOwner) continue;
                    if (p.MyControl.Data.Role.IsImpostor) continue;

                    if (p.MyControl.transform.position.Distance(killTrap.Position) < 0.1f)
                    {
                            using (RPCRouter.CreateSection("TrapKill"))
                            {
                                PlayerControl.LocalPlayer.ModKill(p.MyControl,false,PlayerState.Trapped,EventDetail.Trap);
                                RpcTrapKill.Invoke(killTrap.ObjectId);
                            }

                            return true;
                        }
                    }
                    return false;
                });
            }
        }

    }

    static private RemoteProcess<int> RpcTrapKill = RemotePrimitiveProcess.OfInteger(
        "UseKillTrap",
        (message, _) =>
        {
            NebulaSyncObject.GetObject<Trap>(message)?.SetSpriteAsUsedKillTrap();
        }
        );
}