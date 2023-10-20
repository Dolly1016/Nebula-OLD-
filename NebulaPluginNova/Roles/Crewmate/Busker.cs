using Nebula.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;


public class Busker : ConfigurableStandardRole
{
    static public Busker MyRole = new Busker();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "busker";
    public override Color RoleColor => new Color(255f / 255f, 172f / 255f, 117f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration PseudocideCoolDownOption = null!;
    private NebulaConfiguration PseudocideDurationOption = null!;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        PseudocideCoolDownOption = new(RoleConfig, "pseudocideCoolDown", null, 5f, 60f, 2.5f, 20f, 20f) { Decorator = NebulaConfiguration.SecDecorator };
        PseudocideDurationOption = new(RoleConfig, "pseudocideDuration", null, 5f, 60f, 2.5f, 10f, 10f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    public class Instance : Crewmate.Instance
    {
        static private ISpriteLoader pseudocideButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.BuskPseudocideButton.png", 115f);
        static private ISpriteLoader reviveButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.BuskReviveButton.png", 115f);


        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                var pseudocideButton = Bind(new ModAbilityButton()).KeyBind(NebulaInput.GetInput(KeyAssignmentType.Ability));
                var reviveButon = Bind(new ModAbilityButton()).KeyBind(NebulaInput.GetInput(KeyAssignmentType.Ability));

                pseudocideButton.SetSprite(pseudocideButtonSprite.GetSprite());
                pseudocideButton.Availability = (button) => MyPlayer.MyControl.CanMove;
                pseudocideButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                pseudocideButton.CoolDownTimer = Bind(new Timer(0f, MyRole.PseudocideCoolDownOption.GetFloat()).SetAsAbilityCoolDown().Start());
                pseudocideButton.OnClick = (button) => {
                    NebulaManager.Instance.ScheduleDelayAction(() => {
                        MyPlayer.MyControl.ModKill(MyPlayer.MyControl, false, PlayerState.Suicide, null, false);
                        reviveButon.ActivateEffect();
                    });
                };
                pseudocideButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                pseudocideButton.SetLabel("pseudocide");

                reviveButon.SetSprite(reviveButtonSprite.GetSprite());
                reviveButon.Availability = (button) => MyPlayer.MyControl.CanMove && MapData.GetCurrentMapData().CheckMapArea(PlayerControl.LocalPlayer.transform.position);
                reviveButon.Visibility = (button) => button.EffectActive;
                reviveButon.EffectTimer = Bind(new Timer(0f, MyRole.PseudocideDurationOption.GetFloat()));
                reviveButon.OnClick = (button) => {
                    using (RPCRouter.CreateSection("ReviveBusker")) {
                        MyPlayer.MyControl.ModRevive(MyPlayer.MyControl.transform.position, true, false);
                        MyPlayer.MyControl.ModDive(false);
                    }
                    reviveButon.InactivateEffect();
                    pseudocideButton.StartCoolDown();
                };
                reviveButon.OnEffectEnd = (button) =>
                {
                    if (MyPlayer.IsDead) NebulaGameManager.Instance!.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Accident, null, 1 << MyPlayer.PlayerId);
                };
                reviveButon.SetLabelType(ModAbilityButton.LabelType.Standard);
                reviveButon.SetLabel("revive");

            }
        }
    }
}
