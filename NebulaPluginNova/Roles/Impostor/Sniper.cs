using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

[NebulaRPCHolder]
public class Sniper : ConfigurableStandardRole
{
    static public Sniper MyRole = new Sniper();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "sniper";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private KillCoolDownConfiguration SnipeCoolDownOption;
    private NebulaConfiguration ShotSizeOption;
    private NebulaConfiguration ShotEffectiveRangeOption;
    private NebulaConfiguration ShotNoticeRangeOption;
    private NebulaConfiguration StoreRifleOnFireOption;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        SnipeCoolDownOption = new(RoleConfig, "snipeCoolDown", KillCoolDownConfiguration.KillCoolDownType.Immediate, 2.5f, 10f, 60f, -40f, 40f, 0.125f, 0.125f, 2f, 20f, -10f, 1f);
        ShotSizeOption = new(RoleConfig, "shotSize", null, 0.25f, 4f, 0.25f, 1f, 1f);
        ShotEffectiveRangeOption = new(RoleConfig, "shotEffectiveRange", null, 2.5f, 40f, 2.5f, 20f, 20f);
        ShotNoticeRangeOption = new(RoleConfig, "shotNoticeRange", null, 2.5f, 40f, 2.5f, 15f, 15f);
    }

    [NebulaRPCHolder]
    public class SniperRifle : INebulaScriptComponent
    {
        public PlayerModInfo Owner { get; private set; }
        private SpriteRenderer Renderer { get; set; }
        private static SpriteLoader rifleSprite = SpriteLoader.FromResource("Nebula.Resources.SniperRifle.png", 100f);
        public SniperRifle(PlayerModInfo owner) : base()
        {
            Owner = owner;
            Renderer = UnityHelper.CreateObject<SpriteRenderer>("SniperRifle", null, owner.MyControl.transform.position, LayerExpansion.GetObjectsLayer());
            Renderer.sprite = rifleSprite.GetSprite();
            Renderer.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }

        public override void Update()
        {
            if (Owner.AmOwner) Owner.RequireUpdateMouseAngle();
            Renderer.transform.localEulerAngles = new Vector3(0, 0, Owner.MouseAngle * 180f / Mathf.PI);
            var pos = PlayerControl.LocalPlayer.transform.position + new Vector3(Mathf.Cos(Owner.MouseAngle), Mathf.Sin(Owner.MouseAngle), -1f) * 0.87f;
            var diff = (pos - Renderer.transform.position) * Time.deltaTime * 7.5f;
            Renderer.transform.position += diff;
            Renderer.flipY = Mathf.Cos(Owner.MouseAngle) < 0f;
        }

        public override void OnReleased()
        {
            if (Renderer) GameObject.Destroy(Renderer.gameObject);
            Renderer = null;
        }

        public PlayerModInfo? GetTarget(float width,float maxLength)
        {
            float minLength = maxLength;
            PlayerModInfo? result = null;

            foreach(var p in NebulaGameManager.Instance.AllPlayerInfo())
            {
                if (p.IsDead || p.AmOwner) continue;

                //インポスターは無視
                if (p.Role.Role.RoleCategory == RoleCategory.ImpostorRole) continue;

                var pos = p.MyControl.GetTruePosition();
                Vector2 diff = pos - (Vector2)Renderer.transform.position;

                //移動と回転を施したベクトル
                var vec = diff.Rotate(-Renderer.transform.eulerAngles.z);

                if(vec.x>0 && vec.x< minLength && Mathf.Abs(vec.y) < width / 2f)
                {
                    result = p;
                    minLength= vec.x;
                }
            }

            return result;
        }
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? equipButton = null;
        private ModAbilityButton? killButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SnipeButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public SniperRifle? MyRifle = null;
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
                    RpcEquip.Invoke((MyPlayer.PlayerId, MyRifle == null));
                };
                equipButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                equipButton.SetLabel("equip");

                killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(KeyCode.Q);
                killButton.Availability = (button) => MyRifle != null && MyPlayer.MyControl.CanMove;
                killButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                killButton.OnClick = (button) =>
                {
                    var target = MyRifle?.GetTarget(MyRole.ShotSizeOption.GetFloat()!.Value, MyRole.ShotEffectiveRangeOption.GetFloat()!.Value);
                    if (target != null)
                    {
                        MyPlayer.MyControl.ModKill(target!.MyControl, false, PlayerState.Sniped, EventDetail.Kill);
                    }
                    else
                    {
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Missed, MyPlayer.MyControl, 0);
                    }
                    Sniper.RpcShowNotice.Invoke(MyPlayer.MyControl.GetTruePosition());

                    if (MyRole.StoreRifleOnFireOption.GetBool()!.Value) RpcEquip.Invoke((MyPlayer.PlayerId, false));

                    button.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(new Timer(MyRole.SnipeCoolDownOption.KillCoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                killButton.SetLabel("snipe");
            }
        }

        public override void OnMeetingStart()
        {

            equipButton?.SetLabel("equip");
        }

        void EquipRifle()
        {
            MyRifle = Bind(new SniperRifle(MyPlayer));
        }

        void UnequipRifle()
        {
            if (MyRifle != null) MyRifle.Release();
            MyRifle = null;
        }

        static RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipRifle",
        (message, _) =>
        {
            var role = NebulaGameManager.Instance?.GetModPlayerInfo(message.playerId)?.Role;
            if (role is Sniper.Instance sniper)
            {
                if (message.equip)
                    sniper.EquipRifle();
                else
                    sniper.UnequipRifle();
            }
        }
        );
    }

    private static SpriteLoader snipeNoticeSprite = SpriteLoader.FromResource("Nebula.Resources.SniperRifleArrow.png", 200f);
    public static RemoteProcess<Vector2> RpcShowNotice = RemotePrimitiveProcess.OfVector2(
        "ShowSnipeNotice",
        (message, _) =>
        {
            if ((message - (Vector2)PlayerControl.LocalPlayer.transform.position).magnitude < Sniper.MyRole.ShotNoticeRangeOption.GetFloat()!.Value)
            {
                var arrow = new Arrow(snipeNoticeSprite.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true };
                arrow.TargetPos = message;
                NebulaManager.Instance.StartCoroutine(Effects.Sequence(Effects.Wait(3f), ManagedEffects.Action(() => arrow.IsDisappearing = true).WrapToIl2Cpp()));
            }
        }
        );
}
