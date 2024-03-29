﻿using AmongUs.GameOptions;
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

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration CleanCoolDownOption = null!;
    private NebulaConfiguration SyncKillAndCleanCoolDownOption = null!;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        CleanCoolDownOption = new NebulaConfiguration(RoleConfig, "cleanCoolDown", null, 5f, 60f, 2.5f, 30f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
        SyncKillAndCleanCoolDownOption = new NebulaConfiguration(RoleConfig, "syncKillAndCleanCoolDown", null, true, true);
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? cleanButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.CleanButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnKillPlayer(PlayerControl target)
        {
            if (AmOwner)
            {
                cleanButton?.CoolDownTimer?.Start(MyRole.SyncKillAndCleanCoolDownOption ? null : 5f);
            }
        }

        public override void OnActivated()
        {
            base.OnActivated();

            if (AmOwner)
            {
                var cleanTracker = Bind(ObjectTrackers.ForDeadBody(null, MyPlayer.MyControl, (d) => true));

                cleanButton = Bind(new ModAbilityButton()).KeyBind(KeyAssignmentType.Ability);
                cleanButton.SetSprite(buttonSprite.GetSprite());
                cleanButton.Availability = (button) => cleanTracker.CurrentTarget != null && MyPlayer.MyControl.CanMove;
                cleanButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                cleanButton.OnClick = (button) => {
                    AmongUsUtil.RpcCleanDeadBody(cleanTracker.CurrentTarget!.ParentId,MyPlayer.PlayerId,EventDetail.Clean);
                    if (MyRole.SyncKillAndCleanCoolDownOption) PlayerControl.LocalPlayer.killTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
                };
                cleanButton.CoolDownTimer = Bind(new Timer(MyRole.CleanCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                cleanButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                cleanButton.SetLabel("clean");
            }
        }
    }
}
