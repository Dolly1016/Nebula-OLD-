using Nebula.Configuration;
using Nebula.Player;
using Sentry.Unity.NativeUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Roles.Crewmate.Phosphorus;
using static UnityEngine.UI.GridLayoutGroup;

namespace Nebula.Roles.Impostor;

[NebulaRPCHolder]
public class Raider : ConfigurableStandardRole
{
    static public Raider MyRole = new Raider();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "raider";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private KillCoolDownConfiguration ThrowCoolDownOption;
    private NebulaConfiguration AxeSizeOption;
    private NebulaConfiguration AxeSpeedOption;
    private NebulaConfiguration CanKillImpostorOption;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        ThrowCoolDownOption = new(RoleConfig, "throwCoolDown", KillCoolDownConfiguration.KillCoolDownType.Immediate, 2.5f, 10f, 60f, -40f, 40f, 0.125f, 0.125f, 2f, 20f, -10f, 1f);
        AxeSizeOption = new(RoleConfig, "axeSize", null, 0.25f, 4f, 0.25f, 1f, 1f) { Decorator = NebulaConfiguration.OddsDecorator };
        AxeSpeedOption = new(RoleConfig, "axeSpeed", null, 0.5f, 4f, 0.25f, 1f, 1f) { Decorator = NebulaConfiguration.OddsDecorator };
        CanKillImpostorOption = new(RoleConfig, "canKillImpostor", null, false, false);
    }

    [NebulaPreLoad]
    public class RaiderAxe : NebulaSyncStandardObject
    {
        public static string MyTag = "RaiderAxe";
        
        private static SpriteLoader staticAxeSprite = SpriteLoader.FromResource("Nebula.Resources.RaiderAxe.png", 150f);
        private static SpriteLoader thrownAxeSprite = SpriteLoader.FromResource("Nebula.Resources.RaiderAxeThrown.png", 150f);
        private static SpriteLoader stuckAxeSprite = SpriteLoader.FromResource("Nebula.Resources.RaiderAxeCrashed.png", 150f);

        private float thrownAngle = 0f;
        private int state = 0;
        private float speed = MyRole.AxeSpeedOption.GetFloat();
        private bool killed = false;
        private float thrownTime = 0f;

        private PlayerModInfo owner;
        public RaiderAxe(PlayerControl owner) : base(owner.GetTruePosition(),ZOption.Front,false,staticAxeSprite.GetSprite())
        {
            this.owner = owner.GetModInfo()!;
        }

        public override void Update()
        {
            if (state == 0)
            {
                if (owner.AmOwner) owner.RequireUpdateMouseAngle();
                MyRenderer.transform.localEulerAngles = new Vector3(0, 0, owner.MouseAngle * 180f / Mathf.PI);
                var pos = owner.MyControl.transform.position + new Vector3(Mathf.Cos(owner.MouseAngle), Mathf.Sin(owner.MouseAngle), -1f) * 0.67f;
                var diff = (pos - MyRenderer.transform.position) * Time.deltaTime * 7.5f;
                Position += (Vector2)diff;
                MyRenderer.flipY = Mathf.Cos(owner.MouseAngle) < 0f;
            }
            else if (state == 1)
            {
                //進行方向ベクトル
                var vec = new Vector2(Mathf.Cos(thrownAngle), Mathf.Sin(thrownAngle));

                if (owner.AmOwner)
                {
                    var pos = Position;
                    var size = MyRole.AxeSizeOption.GetFloat();
                    foreach(var p in PlayerControl.AllPlayerControls)
                    {
                        if (p.Data.IsDead || p.AmOwner) continue;
                        if (!MyRole.CanKillImpostorOption && p.Data.Role.IsImpostor) continue;

                        if (p.GetTruePosition().Distance(pos) < size * 0.4f)
                        {
                            PlayerControl.LocalPlayer.ModKill(p, false, PlayerState.Beaten, EventDetail.Kill);
                            killed = true;
                        }

                    }
                }

                if (NebulaPhysicsHelpers.AnyNonTriggersBetween(MyRenderer.transform.position, vec, speed * 4f * Time.deltaTime, Constants.ShipAndAllObjectsMask, out var d))
                {
                    state = 2;
                    MyRenderer.sprite = stuckAxeSprite.GetSprite();
                    MyRenderer.transform.eulerAngles = new Vector3(0f, 0f, thrownAngle);

                    if (owner.AmOwner && !killed)
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Missed, NebulaGameManager.Instance.CurrentTime - thrownTime, PlayerControl.LocalPlayer, 0);
                }
                else
                {
                    MyRenderer.transform.localEulerAngles += new Vector3(0f, 0f, MyRenderer.flipY ? Time.deltaTime * 2000f : Time.deltaTime * -2000f);
                }

                Position += vec * d;
            }
            else if (state == 2) { }
        }

        public void Throw(Vector2 pos, float angle)
        {
            thrownAngle = angle;
            state = 1;
            Position = pos;
            ZOrder = ZOption.Just;
            CanSeeInShadow = true;
            MyRenderer.sprite = thrownAxeSprite.GetSprite();
            thrownTime = NebulaGameManager.Instance.CurrentTime;
        }

        public static void Load()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new RaiderAxe(Helpers.GetPlayer((byte)args[0])!));
        }
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? equipButton = null;
        private ModAbilityButton? killButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.AxeButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public RaiderAxe? MyAxe = null;
        public override bool HasVanillaKillButton => false;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                equipButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                equipButton.SetSprite(buttonSprite.GetSprite());
                equipButton.Availability = (button) => MyPlayer.MyControl.CanMove;
                equipButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                equipButton.OnClick = (button) =>
                {
                    if (MyAxe == null)
                        equipButton.SetLabel("unequip");
                    else
                        equipButton.SetLabel("equip");

                    if (MyAxe == null) EquipAxe(); else UnequipAxe();
                };
                equipButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                equipButton.SetLabel("equip");

                killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(KeyCode.Q);
                killButton.Availability = (button) => MyAxe != null && MyPlayer.MyControl.CanMove;
                killButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                killButton.OnClick = (button) =>
                {
                    MyAxe.Throw(MyAxe.Position,MyPlayer.MouseAngle);
                    MyAxe = null;
                    button.StartCoolDown();
                    equipButton.SetLabel("equip");
                };
                killButton.CoolDownTimer = Bind(new Timer(MyRole.ThrowCoolDownOption.KillCoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                killButton.SetLabel("throw");
            }
        }

        public override void OnMeetingStart()
        {
            UnequipAxe();
            equipButton?.SetLabel("equip");
        }

        void EquipAxe()
        {
            MyAxe = (NebulaSyncObject.RpcInstantiate(RaiderAxe.MyTag, new float[] { (float)PlayerControl.LocalPlayer.PlayerId }) as RaiderAxe);
        }

        void UnequipAxe()
        {
            if(MyAxe != null) NebulaSyncObject.RpcDestroy(MyAxe.ObjectId);
            MyAxe = null;
        }

        protected override void OnInactivated()
        {
            UnequipAxe();
        }
    }

    static RemoteProcess<(int objectId, Vector2 pos, float angle)> RpcThrow = new(
        "ThrowAxe",
        (message,_) => {
            var axe = NebulaSyncObject.GetObject<RaiderAxe>(message.objectId);
            axe.Throw(message.pos, message.angle);
        }
        );
}
