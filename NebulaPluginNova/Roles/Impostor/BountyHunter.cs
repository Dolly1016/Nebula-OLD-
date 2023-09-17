using AmongUs.GameOptions;
using Nebula.Configuration;
using Nebula.Modules.ScriptComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

public class BountyHunter : ConfigurableStandardRole
{
    static public BountyHunter MyRole = new BountyHunter();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "bountyHunter";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private KillCoolDownConfiguration BountyKillCoolDownOption;
    private KillCoolDownConfiguration OthersKillCoolDownOption;
    private NebulaConfiguration ShowBountyArrowOption;
    private NebulaConfiguration ArrowUpdateIntervalOption;
    private NebulaConfiguration ChangeBountyIntervalOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        BountyKillCoolDownOption = new(RoleConfig, "bountyKillCoolDown", KillCoolDownConfiguration.KillCoolDownType.Ratio, 2.5f, 5f, 60f, -30f, 30f, 0.125f, 0.125f, 2f, 10f, -10f, 0.5f);
        OthersKillCoolDownOption = new(RoleConfig, "othersKillCoolDown", KillCoolDownConfiguration.KillCoolDownType.Ratio, 2.5f, 5f, 60f, -30f, 30f, 0.125f, 0.125f, 2f, 40f, 20f, 2f);
        ChangeBountyIntervalOption = new(RoleConfig, "changeBountyInterval", null, 5f, 120f, 5f, 45f, 45f) { Decorator = NebulaConfiguration.SecDecorator };
        ShowBountyArrowOption = new(RoleConfig, "showBountyArrow", null, true, true);
        ArrowUpdateIntervalOption = new(RoleConfig, "arrowUpdateInterval", null, 5f, 60f, 2.5f, 10f, 10f) { Decorator = NebulaConfiguration.SecDecorator, Predicate = () => ShowBountyArrowOption };
    }

    float MaxKillCoolDown => Mathf.Max(BountyKillCoolDownOption.KillCoolDown, OthersKillCoolDownOption.KillCoolDown, AmongUsUtil.VanillaKillCoolDown);

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? killButton = null;

        public override AbstractRole Role => MyRole;
        public override bool HasVanillaKillButton => false;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        private byte currentBounty = 0;

        PoolablePlayer bountyIcon;
        Timer bountyTimer;
        Timer arrowTimer;
        Arrow bountyArrow;
        bool CanBeBounty(PlayerControl target) => true;
        void ChangeBounty()
        {
            var arr = PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => !p.AmOwner && !p.Data.IsDead && CanBeBounty(p)).ToArray();
            if (arr.Length == 0) currentBounty = byte.MaxValue;
            else currentBounty = arr[System.Random.Shared.Next(arr.Length)].PlayerId;

            if (currentBounty == byte.MaxValue)
                bountyIcon.gameObject.SetActive(false);
            else
            {
                bountyIcon.gameObject.SetActive(true);
                bountyIcon.UpdateFromPlayerOutfit(NebulaGameManager.Instance!.GetModPlayerInfo(currentBounty)!.DefaultOutfit, PlayerMaterial.MaskType.None, false, true);
            }
            UpdateArrow();

            bountyTimer.Start();
        }

        void UpdateArrow()
        {
            var target = NebulaGameManager.Instance?.GetModPlayerInfo(currentBounty);
            if (target==null)
            {
                bountyArrow.IsActive= false;
            }
            else
            {
                bountyArrow.IsActive= true;
                bountyArrow.TargetPos = target.MyControl.transform.localPosition;
            }

            arrowTimer.Start();
        }

        void UpdateTimer()
        {
            if (!bountyTimer.IsInProcess)
            {
                ChangeBounty();
            }
            bountyIcon.SetName(Mathf.CeilToInt(bountyTimer.CurrentTime).ToString());

            if (!arrowTimer.IsInProcess)
            {
                UpdateArrow();
            }
        }

        public override void OnActivated()
        {
            base.OnActivated();

            if (AmOwner)
            {
                bountyTimer = Bind(new Timer(MyRole.ChangeBountyIntervalOption.GetFloat())).Start();
                arrowTimer = Bind(new Timer(MyRole.ArrowUpdateIntervalOption.GetFloat())).Start();

                var killTracker = Bind(ObjectTrackers.ForPlayer(1.2f, MyPlayer.MyControl, (p) => !p.Data.Role.IsImpostor));

                killButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.Q);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.MyControl.CanMove;
                killButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                killButton.OnClick = (button) => {
                    PlayerControl.LocalPlayer.ModKill(killTracker.CurrentTarget, true, PlayerState.Dead, EventDetail.Kill);

                    if(killTracker.CurrentTarget.PlayerId == currentBounty)
                    {
                        ChangeBounty();
                        button.CoolDownTimer!.Start(MyRole.BountyKillCoolDownOption.KillCoolDown);
                    }
                    else
                    {
                        button.CoolDownTimer!.Start(MyRole.OthersKillCoolDownOption.KillCoolDown);
                    }

                };
                killButton.CoolDownTimer = Bind(new AdvancedTimer(AmongUsUtil.VanillaKillCoolDown, MyRole.MaxKillCoolDown).SetDefault(AmongUsUtil.VanillaKillCoolDown).SetAsKillCoolDown().Start(10f));
                killButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.SetLabel("kill");

                var iconHolder = HudContent.InstantiateContent("BountyHolder",true);
                Bind(iconHolder.gameObject);
                bountyIcon = AmongUsUtil.GetPlayerIcon(MyPlayer.DefaultOutfit, iconHolder.transform, Vector3.zero, Vector3.one * 0.5f);
                bountyIcon.ToggleName(true);
                bountyIcon.SetName("", Vector3.one * 4f, Color.white, -1f);

                bountyArrow = Bind(new Arrow().SetColor(Palette.ImpostorRed));

                ChangeBounty();
            }
        }

        public override void LocalUpdate()
        {
            UpdateTimer();
        }

        public override void OnMeetingEnd()
        {
            if (!AmOwner) return;

            //死亡しているプレイヤーであれば切り替える
            if (NebulaGameManager.Instance?.GetModPlayerInfo(currentBounty)?.IsDead ?? true) ChangeBounty();
        } 
    }
}
