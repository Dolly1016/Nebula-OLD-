using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

public class Provocateur : ConfigurableStandardRole
{
    static public Provocateur MyRole = new Provocateur();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "provocateur";
    public override Color RoleColor => new Color(112f / 255f, 255f / 255f, 89f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration EmbroilCoolDownOption = null!;
    private NebulaConfiguration EmbroilAdditionalCoolDownOption = null!;
    private NebulaConfiguration EmbroilDurationOption = null!;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        EmbroilCoolDownOption = new(RoleConfig, "embroilCoolDown", null, 5f, 60f, 2.5f, 20f, 20f) { Decorator = NebulaConfiguration.SecDecorator };
        EmbroilAdditionalCoolDownOption = new(RoleConfig, "embroilAdditionalCoolDown", null, 0f, 30f, 2.5f, 5f, 5f) { Decorator = NebulaConfiguration.SecDecorator };
        EmbroilDurationOption = new(RoleConfig, "embroilCoolDown", null, 1f, 20f, 1f, 5f, 5f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    public class Instance : Crewmate.Instance
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player){}

        private ModAbilityButton embroilButton = null!;
        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.EmbroilButton.png", 115f);
        
        public override void OnActivated()
        {
            if (AmOwner)
            {
                embroilButton = Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.Ability);
                embroilButton.SetSprite(buttonSprite.GetSprite());
                embroilButton.Availability = (button) => MyPlayer.MyControl.CanMove;
                embroilButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                embroilButton.OnClick = (button) => {
                    button.ActivateEffect();
                    button.CoolDownTimer?.Expand(MyRole.EmbroilAdditionalCoolDownOption.GetFloat());
                };
                embroilButton.OnEffectEnd = (button) => embroilButton.StartCoolDown();
                embroilButton.CoolDownTimer = Bind(new Timer(0f, MyRole.EmbroilCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                embroilButton.EffectTimer = Bind(new Timer(0f, MyRole.EmbroilDurationOption.GetFloat()));
                embroilButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                embroilButton.SetLabel("embroil");
            }
        }

        public override void OnMurdered(PlayerControl murderer)
        {
            if (murderer.PlayerId == MyPlayer.PlayerId) return;

            if (AmOwner && embroilButton.EffectActive && !murderer.Data.IsDead)
            {
                MyPlayer.MyControl.ModKill(murderer,false,PlayerState.Embroiled,EventDetail.Embroil);
            }
        }

        public override void OnExiled()
        {
            if (!AmOwner) return;

            var voters = MeetingHudExtension.LastVotedForMap
                .Where(entry => entry.Value == MyPlayer.PlayerId && entry.Key != MyPlayer.PlayerId)
                .Select(entry => NebulaGameManager.Instance!.GetModPlayerInfo(entry.Key))
                .Where(p => !p!.IsDead)
                .ToArray();

            if (voters.Length == 0) return;
            voters[System.Random.Shared.Next(voters.Length)]!.MyControl.ModMarkAsExtraVictim(MyPlayer.MyControl, PlayerState.Embroiled, EventDetail.Embroil);
        }
    }
}

