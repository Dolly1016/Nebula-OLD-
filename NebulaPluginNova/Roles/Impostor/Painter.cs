using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

public class Painter : ConfigurableStandardRole
{
    static public Painter MyRole = new Painter();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "painter";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration SampleCoolDownOption;
    private NebulaConfiguration PaintCoolDownOption;
    private NebulaConfiguration LoseSampleOnMeetingOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        SampleCoolDownOption = new NebulaConfiguration(RoleConfig, "sampleCoolDown", null, 0f, 60f, 2.5f, 15f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
        PaintCoolDownOption = new NebulaConfiguration(RoleConfig, "paintCoolDown", null, 5f, 60f, 5f, 30f, 30f) { Decorator = NebulaConfiguration.SecDecorator };
        LoseSampleOnMeetingOption = new NebulaConfiguration(RoleConfig, "loseSampleOnMeeting", null, false, false);
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? sampleButton = null;
        private ModAbilityButton? paintButton = null;

        static private ISpriteLoader sampleButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SampleButton.png", 115f);
        static private ISpriteLoader paintButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.MorphButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                GameData.PlayerOutfit? sample = null;
                PoolablePlayer? sampleIcon = null;
                var sampleTracker = Bind(ObjectTrackers.ForPlayer(1.2f, MyPlayer.MyControl, (p) => p.PlayerId != MyPlayer.PlayerId && !p.Data.IsDead));

                sampleButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                sampleButton.SetSprite(sampleButtonSprite.GetSprite());
                sampleButton.Availability = (button) => sampleTracker.CurrentTarget != null && MyPlayer.MyControl.CanMove;
                sampleButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                sampleButton.OnClick = (button) => {
                    if (paintButton.CoolDownTimer.CurrentTime < 5f) paintButton.CoolDownTimer.SetTime(5f).Resume();
                    sample = sampleTracker.CurrentTarget!.GetModInfo().GetOutfit(75);

                    if (sampleIcon != null) GameObject.Destroy(sampleIcon.gameObject);
                    sampleIcon = AmongUsUtil.GetPlayerIcon(sample, paintButton.VanillaButton.transform, new Vector3(-0.4f, 0.35f, -0.5f), new(0.3f, 0.3f)).SetAlpha(0.5f);
                };
                sampleButton.CoolDownTimer = Bind(new Timer(MyRole.SampleCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                sampleButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                sampleButton.SetLabel("sample");

                paintButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.G);
                paintButton.SetSprite(paintButtonSprite.GetSprite());
                paintButton.Availability = (button) => sampleTracker.CurrentTarget != null && MyPlayer.MyControl.CanMove;
                paintButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                paintButton.OnClick = (button) => {
                    PlayerModInfo.RpcAddOutfit.Invoke(new(sampleTracker.CurrentTarget!.PlayerId, new("Paint", 40, false, sample ?? MyPlayer.GetOutfit(75))));
                };
                paintButton.OnMeeting = (button) =>
                {
                    if (MyRole.LoseSampleOnMeetingOption)
                    {
                        if (sampleIcon != null) GameObject.Destroy(sampleIcon.gameObject);
                        sampleIcon = null;
                        sample = null;
                    }
                };
                paintButton.CoolDownTimer = Bind(new Timer(MyRole.PaintCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                paintButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                paintButton.SetLabel("paint");
            }
        }
    }
}

