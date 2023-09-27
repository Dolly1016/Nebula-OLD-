using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

public class Comet : ConfigurableStandardRole
{
    static public Comet MyRole = new Comet();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "comet";
    public override Color RoleColor => new Color(121f / 255f, 175f / 255f, 206f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration BrazeCoolDownOption;
    private NebulaConfiguration BrazeSpeedOption;
    private NebulaConfiguration BrazeDurationOption;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        BrazeCoolDownOption = new(RoleConfig, "brazeCoolDown", null, 5f, 60f, 2.5f, 20f, 20f) { Decorator = NebulaConfiguration.SecDecorator };
        BrazeSpeedOption = new(RoleConfig, "brazeSpeed", null, 0.5f, 3f, 0.125f, 1.5f, 1.5f) { Decorator = NebulaConfiguration.OddsDecorator };
        BrazeDurationOption = new(RoleConfig, "brazeDuration", null, 5f, 60f, 2.5f, 15f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    public class Instance : Crewmate.Instance
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player) { }

        private ModAbilityButton boostButton;
        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.BoostButton.png", 115f);

        public override void OnActivated()
        {
            if (AmOwner)
            {
                boostButton = Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.Ability);
                boostButton.SetSprite(buttonSprite.GetSprite());
                boostButton.Availability = (button) => MyPlayer.MyControl.CanMove;
                boostButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                boostButton.OnClick = (button) => {
                    button.ActivateEffect();
                    PlayerModInfo.RpcAddModulator.Invoke(new(MyPlayer.PlayerId, new(MyRole.BrazeSpeedOption.GetFloat(), true, MyRole.BrazeDurationOption.GetFloat(), false, 100, 1)));
                };
                boostButton.OnEffectEnd = (button) => boostButton.StartCoolDown();
                boostButton.CoolDownTimer = Bind(new Timer(0f, MyRole.BrazeCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                boostButton.EffectTimer = Bind(new Timer(0f, MyRole.BrazeDurationOption.GetFloat()));
                boostButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                boostButton.SetLabel("braze");
            }
        }
    }
}


