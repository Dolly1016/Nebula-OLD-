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

    public override RoleInstance CreateInstance(PlayerModInfo player, int[]? arguments) => new Instance(player);

    private NebulaConfiguration EatCoolDownOption;
    private NebulaConfiguration NumToEatenToWinOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        EatCoolDownOption = new NebulaConfiguration(RoleConfig, "eatCoolDown", null, 5f, 60f, 5f, 20f, 20f) { Decorator = NebulaConfiguration.SecDecorator };
        NumToEatenToWinOption = new NebulaConfiguration(RoleConfig, "numToTheEatenToWin", null, 1, 8, 3, 3);
    }


    public class Instance : RoleInstance
    {
        private ModAbilityButton? eatButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.EatButton.png", 115f);

        public override AbstractRole Role => MyRole;
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
                eatButton.CoolDownTimer = Bind(new Timer(0f, MyRole.EatCoolDownOption.GetFloat()!.Value));
                eatButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                eatButton.SetLabel("eat");
                usesIcon.text= leftEaten.ToString();
            }
        }

        public override void OnGameStart()
        {
            eatButton?.StartCoolDown();
        }

        public override void OnGameReenabled()
        {
            eatButton?.StartCoolDown();
        }
    }
}
