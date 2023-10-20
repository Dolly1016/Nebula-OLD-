using Nebula.Behaviour;

namespace Nebula.Roles.Impostor;

[NebulaRPCHolder]
public class Marionette : ConfigurableStandardRole
{
    static public Marionette MyRole = new Marionette();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "marionette";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration PlaceCoolDownOption = null!;
    private NebulaConfiguration SwapCoolDownOption = null!;
    private NebulaConfiguration DecoyDurationOption = null!;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        PlaceCoolDownOption = new(RoleConfig, "placeCoolDown", null, 5f, 60f, 2.5f, 20f, 20f) { Decorator = NebulaConfiguration.SecDecorator };
        SwapCoolDownOption = new(RoleConfig, "swapCoolDown", null, 2.5f, 60f, 2.5f, 10f, 10f) { Decorator = NebulaConfiguration.SecDecorator };
        DecoyDurationOption = new(RoleConfig, "decoyDuration", null, 5f, 180f, 5f, 40f, 40f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    [NebulaPreLoad]
    public class Decoy : NebulaSyncStandardObject
    {
        public static string MyTag = "Decoy";
        private static SpriteLoader decoySprite = SpriteLoader.FromResource("Nebula.Resources.Decoy.png", 150f);
        public Decoy(Vector2 pos,bool reverse) : base(pos,ZOption.Just,true, decoySprite.GetSprite()) {
            MyRenderer.flipX = reverse;
            MyBehaviour = MyRenderer.gameObject.AddComponent<EmptyBehaviour>();
        }

        public bool Flipped { get => MyRenderer.flipX; set => MyRenderer.flipX = value; }
        public EmptyBehaviour MyBehaviour = null!;

        public override void Update()
        {
            
        }

        public static void Load()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new Decoy(new Vector2(args[0], args[1]), args[2] < 0f));
        }
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? placeButton = null;
        private ModAbilityButton? destroyButton = null;
        private ModAbilityButton? swapButton = null;
        private ModAbilityButton? monitorButton = null;

        static private ISpriteLoader placeButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DecoyButton.png", 115f);
        static private ISpriteLoader destroyButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DecoyDestroyButton.png", 115f);
        static private ISpriteLoader swapButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DecoySwapButton.png", 115f);
        static private ISpriteLoader monitorButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DecoyMonitorButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public Decoy? MyDecoy = null;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnActivated()
        {
            base.OnActivated();

            if (AmOwner)
            {
                placeButton = Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.Ability);
                placeButton.SetSprite(placeButtonSprite.GetSprite());
                placeButton.Availability = (button) => MyPlayer.MyControl.CanMove;
                placeButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead && MyDecoy == null;
                placeButton.CoolDownTimer = Bind(new Timer(MyRole.PlaceCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                placeButton.OnClick = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        MyDecoy = (NebulaSyncObject.RpcInstantiate(Decoy.MyTag, new float[] {
                        PlayerControl.LocalPlayer.transform.localPosition.x,
                        PlayerControl.LocalPlayer.transform.localPosition.y,
                        PlayerControl.LocalPlayer.cosmetics.FlipX ? -1f : 1f }) as Decoy);

                        destroyButton!.ActivateEffect();
                        destroyButton.EffectTimer?.Start();
                    });
                    placeButton.StartCoolDown();
                    swapButton?.StartCoolDown();
                };
                placeButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                placeButton.SetLabel("place");

                destroyButton = Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.Ability);
                destroyButton.SetSprite(destroyButtonSprite.GetSprite());
                destroyButton.Availability = (button) => MyPlayer.MyControl.CanMove;
                destroyButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead && MyDecoy != null;
                destroyButton.EffectTimer = Bind(new Timer(MyRole.DecoyDurationOption.GetFloat()));
                destroyButton.OnClick = (button) =>
                {
                    destroyButton.InactivateEffect();
                };
                destroyButton.OnEffectEnd = (button) =>
                {
                    if (MyDecoy != null) NebulaSyncObject.RpcDestroy(MyDecoy!.ObjectId);
                    MyDecoy = null;

                    placeButton.StartCoolDown();
                };
                destroyButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                destroyButton.SetLabel("destroy");

                swapButton = Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.SecondaryAbility).SubKeyBind(KeyAssignmentType.AidAction);
                swapButton.SetSprite(swapButtonSprite.GetSprite());
                swapButton.Availability = (button) => MyPlayer.MyControl.CanMove || HudManager.Instance.PlayerCam.Target == MyDecoy?.MyBehaviour;
                swapButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead && MyDecoy != null;
                swapButton.CoolDownTimer = Bind(new Timer(MyRole.SwapCoolDownOption.GetFloat()));
                swapButton.OnClick = (button) =>
                {
                    DecoySwap.Invoke((MyPlayer.PlayerId, MyDecoy!.ObjectId));
                    button.StartCoolDown();
                    AmongUsUtil.SetCamTarget();
                };
                swapButton.OnSubAction = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        swapButton.ResetKeyBind();
                        monitorButton!.KeyBind(KeyAssignmentType.SecondaryAbility);
                        monitorButton!.SubKeyBind(KeyAssignmentType.AidAction);
                    });
                };
                swapButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                swapButton.SetLabel("swap");

                monitorButton = Bind(new ModAbilityButton());
                monitorButton.SetSprite(monitorButtonSprite.GetSprite());
                monitorButton.Availability = (button) => true;
                monitorButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead && MyDecoy != null;
                monitorButton.OnClick = (button) =>
                {
                    MyPlayer.MyControl.NetTransform.Halt();
                    if (HudManager.Instance.PlayerCam.Target == MyDecoy!.MyBehaviour)
                        AmongUsUtil.SetCamTarget();
                    else
                        AmongUsUtil.SetCamTarget(MyDecoy!.MyBehaviour);
                };
                monitorButton.OnSubAction = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        monitorButton.ResetKeyBind();
                        swapButton!.KeyBind(KeyAssignmentType.SecondaryAbility);
                        swapButton!.SubKeyBind(KeyAssignmentType.AidAction);
                    });
                };
                monitorButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                monitorButton.SetLabel("monitor");
            }
        }

        public override void LocalUpdate()
        {
        }
        public override void OnMeetingStart()
        {
            if (AmOwner)
            {
                if (MyDecoy != null) NebulaSyncObject.RpcDestroy(MyDecoy!.ObjectId);
                MyDecoy = null;

                monitorButton?.DoSubClick();
            }
        }

    }

    static private RemoteProcess<(byte playerId, int objId)> DecoySwap = new("DecoySwap",
        (message, _) => {
            var marionette = Helpers.GetPlayer(message.playerId);
            var decoy = NebulaSyncObject.GetObject<Decoy>(message.objId);
            if (marionette == null || decoy == null) return;
            var marionettePos = marionette.transform.localPosition;
            var marionetteFilp = marionette.cosmetics.FlipX;
            marionette.transform.localPosition = decoy.Position;
            marionette.cosmetics.SetFlipX(decoy.Flipped);
            decoy.Position = marionettePos;
            decoy.Flipped = marionetteFilp;
            marionette.NetTransform.Halt();
        }
        );
}

