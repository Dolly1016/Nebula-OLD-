using Epic.OnlineServices.Stats;
using Nebula.Configuration;
using System;

namespace Nebula.Roles.Crewmate;

public class Sheriff : ConfigurableStandardRole
{
    static public Sheriff MyRole = new Sheriff();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "sheriff";
    public override Color RoleColor => new Color(240f / 255f, 191f / 255f, 0f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private KillCoolDownConfiguration KillCoolDownOption;
    private NebulaConfiguration CanKillMadmateOption;
    
    protected override void LoadOptions()
    {
        base.LoadOptions();

        KillCoolDownOption = new(RoleConfig, "killCoolDown", KillCoolDownConfiguration.KillCoolDownType.Relative, 2.5f, 10f, 60f, -40f, 40f, 0.125f, 0.125f, 2f, 25f, -5f, 1f);
        CanKillMadmateOption = new(RoleConfig, "canKillMadmate", null, false, false);
    }

    public class Instance : Crewmate.Instance
    {
        private ModAbilityButton? killButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SheriffKillButton.png", 100f);
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        private bool CanKill(PlayerControl target)
        {
            var info = target.GetModInfo();
            if (info.Role.Role == Madmate.MyRole) return Sheriff.MyRole.CanKillMadmateOption.GetBool()!.Value;
            if (info.Role.Role.RoleCategory == RoleCategory.CrewmateRole) return false;
            return true;
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                var killTracker = Bind(ObjectTrackers.ForPlayer(1.2f, MyPlayer.MyControl, (p) => p.PlayerId != MyPlayer.PlayerId && !p.Data.IsDead));
                killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(KeyCode.F);
                killButton.SetSprite(buttonSprite.GetSprite());
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.MyControl.CanMove;
                killButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                killButton.OnClick = (button) => {
                    if (CanKill(killTracker.CurrentTarget!)) 
                        MyPlayer.MyControl.ModKill(killTracker.CurrentTarget!, true, PlayerState.Dead, EventDetail.Kill); 
                    else
                    {
                        MyPlayer.MyControl.ModKill(MyPlayer.MyControl, true, PlayerState.Misfired, null); 
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Misfire, MyPlayer.MyControl, killTracker.CurrentTarget!);
                    }
                    button.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(new Timer(MyRole.KillCoolDownOption.KillCoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                killButton.SetLabel("kill");
            }
        }

    }
}

