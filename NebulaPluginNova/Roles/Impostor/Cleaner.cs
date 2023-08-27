using AmongUs.GameOptions;
using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Rewired.UnknownControllerHat;

namespace Nebula.Roles.Impostor;

public class Cleaner : ConfigurableStandardRole
{
    static public Cleaner MyRole = new Cleaner();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "cleaner";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerControl player, int[]? arguments) => new Instance(player);

    private NebulaConfiguration CleanCoolDownOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        CleanCoolDownOption = new NebulaConfiguration(RoleConfig, "cleanCoolDown", null, 5f, 60f, 2.5f, 30f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? cleanButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.CleanButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public Instance(PlayerControl player) : base(player)
        {
        }

        public override void OnKillPlayer(PlayerControl target)
        {
            if (AmOwner)
            {
                cleanButton.CoolDownTimer.Start();
            }
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                var cleanTracker = Bind(ObjectTrackers.ForDeadBody(1.2f, player, (d) => true));

                cleanButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                cleanButton.SetSprite(buttonSprite.GetSprite());
                cleanButton.Availability = (button) => cleanTracker.CurrentTarget != null && player.CanMove;
                cleanButton.Visibility = (button) => !player.Data.IsDead;
                cleanButton.OnClick = (button) => {
                    AmongUsUtil.RpcCleanDeadBody(cleanTracker.CurrentTarget!.ParentId,player.PlayerId,EventDetail.Clean);
                    PlayerControl.LocalPlayer.killTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
                };
                cleanButton.CoolDownTimer = Bind(new Timer(0f, MyRole.CleanCoolDownOption.GetFloat()!.Value));
                cleanButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                cleanButton.SetLabel("clean");
            }
        }

        public override void OnGameStart()
        {
            cleanButton?.StartCoolDown();
        }

        public override void OnGameReenabled()
        {
            cleanButton?.StartCoolDown();
        }
    }
}
