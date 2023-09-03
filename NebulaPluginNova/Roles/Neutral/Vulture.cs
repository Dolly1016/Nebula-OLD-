using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Neutral;

public class Vulture : ConfigurableStandardRole
{
    static public Vulture MyRole = new Vulture();
    static public Team MyTeam = new("teams.vulture", MyRole.RoleColor, TeamRevealType.OnlyMe);

    public override RoleCategory RoleCategory => RoleCategory.NeutralRole;

    public override string LocalizedName => "vulture";
    public override Color RoleColor => new Color(140f / 255f, 70f / 255f, 18f / 255f);
    public override Team Team => MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration EatCoolDownOption;
    private NebulaConfiguration NumToEatenToWinOption;
    private VentConfiguration VentConfiguration;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        VentConfiguration = new(RoleConfig, null, (5f, 60f, 15f), (2.5f, 30f, 10f));

        EatCoolDownOption = new NebulaConfiguration(RoleConfig, "eatCoolDown", null, 5f, 60f, 5f, 20f, 20f) { Decorator = NebulaConfiguration.SecDecorator };
        NumToEatenToWinOption = new NebulaConfiguration(RoleConfig, "numToTheEatenToWin", null, 1, 8, 3, 3);
    }


    public class Instance : RoleInstance
    {
        private ModAbilityButton? eatButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.EatButton.png", 115f);

        public override AbstractRole Role => MyRole;

        private Timer ventCoolDown = new Timer(MyRole.VentConfiguration.CoolDown).SetAsAbilityCoolDown().Start();
        private Timer ventDuration = new(MyRole.VentConfiguration.Duration);
        public override Timer? VentCoolDown => ventCoolDown;
        public override Timer? VentDuration => ventDuration;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                int leftEaten = MyRole.NumToEatenToWinOption.GetMappedInt()!.Value;

                var eatTracker = Bind(ObjectTrackers.ForDeadBody(1.2f, MyPlayer.MyControl, (d) => true));

                eatButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                var usesIcon = eatButton.ShowUsesIcon(2);
                eatButton.SetSprite(buttonSprite.GetSprite());
                eatButton.Availability = (button) => eatTracker.CurrentTarget != null && MyPlayer.MyControl.CanMove;
                eatButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                eatButton.OnClick = (button) => {
                    AmongUsUtil.RpcCleanDeadBody(eatTracker.CurrentTarget!.ParentId,MyPlayer.PlayerId,EventDetail.Eat);
                    leftEaten--;
                    usesIcon.text=leftEaten.ToString();

                    if (leftEaten <= 0) NebulaGameManager.Instance.RpcInvokeSpecialWin(NebulaGameEnd.VultureWin, 1 << MyPlayer.PlayerId);
                };
                eatButton.CoolDownTimer = Bind(new Timer(MyRole.EatCoolDownOption.GetFloat()!.Value).SetAsAbilityCoolDown().Start());
                eatButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                eatButton.SetLabel("eat");
                usesIcon.text= leftEaten.ToString();
            }
        }
    }
}
