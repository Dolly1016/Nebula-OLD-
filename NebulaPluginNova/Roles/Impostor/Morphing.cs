using Nebula.Configuration;
using Nebula.Roles.Impostor;
using Nebula.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

public class Morphing : ConfigurableStandardRole
{
    static public Morphing MyRole = new Morphing();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "morphing";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[]? arguments) => new Instance(player);

    private NebulaConfiguration SampleCoolDownOption;
    private NebulaConfiguration MorphCoolDownOption;
    private NebulaConfiguration MorphDurationOption;
    private NebulaConfiguration LoseSampleOnMeetingOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        SampleCoolDownOption = new NebulaConfiguration(RoleConfig, "sampleCoolDown", null, 0f, 60f, 2.5f, 15f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
        MorphCoolDownOption = new NebulaConfiguration(RoleConfig, "morphCoolDown", null, 5f, 60f, 5f, 30f, 30f) { Decorator = NebulaConfiguration.SecDecorator };
        MorphDurationOption = new NebulaConfiguration(RoleConfig, "morphDuration", null, 5f, 120f, 2.5f, 25f, 25f) { Decorator = NebulaConfiguration.SecDecorator };
        LoseSampleOnMeetingOption = new NebulaConfiguration(RoleConfig, "loseSampleOnMeeting", null, false, false);
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? sampleButton = null;
        private ModAbilityButton? morphButton = null;

        static private ISpriteLoader sampleButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SampleButton.png", 115f);
        static private ISpriteLoader morphButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.MorphButton.png", 115f);
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
                    if (morphButton.CoolDownTimer.CurrentTime < 5f) morphButton.CoolDownTimer.SetTime(5f).Resume();
                    sample = sampleTracker.CurrentTarget!.GetModInfo().GetOutfit(75);

                    if (sampleIcon != null) GameObject.Destroy(sampleIcon.gameObject);
                    sampleIcon = AmongUsUtil.GetPlayerIcon(sample, morphButton.VanillaButton.transform, new Vector3(-0.4f, 0.35f, -0.5f), new(0.3f, 0.3f)).SetAlpha(0.5f);
                };
                sampleButton.CoolDownTimer = Bind(new Timer(MyRole.SampleCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                sampleButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                sampleButton.SetLabel("sample");

                morphButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.G);
                morphButton.SetSprite(morphButtonSprite.GetSprite());
                morphButton.Availability = (button) => MyPlayer.MyControl.CanMove && sample != null;
                morphButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                morphButton.OnClick = (button) => {
                    button.ToggleEffect();
                };
                morphButton.OnEffectStart = (button) =>
                {
                    PlayerModInfo.RpcAddOutfit.Invoke(new(PlayerControl.LocalPlayer.PlayerId, new("Morphing", 50, true, sample)));
                };
                morphButton.OnEffectEnd = (button) =>
                {
                    PlayerModInfo.RpcRemoveOutfit.Invoke(new(PlayerControl.LocalPlayer.PlayerId, "Morphing"));
                    morphButton.CoolDownTimer.Start();
                };
                morphButton.OnMeeting = (button) =>
                {
                    morphButton.InactivateEffect();

                    if (MyRole.LoseSampleOnMeetingOption)
                    {
                        if (sampleIcon != null) GameObject.Destroy(sampleIcon.gameObject);
                        sampleIcon = null;
                        sample = null;
                    }
                };
                morphButton.CoolDownTimer = Bind(new Timer(MyRole.MorphCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                morphButton.EffectTimer = Bind(new Timer(MyRole.MorphDurationOption.GetFloat()));
                morphButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                morphButton.SetLabel("morph");
            }
        }
    }
}
