using Il2CppSystem.Threading.Tasks;

namespace Nebula.Roles.Perk.CrewmatePerks;

public class Sprint : Perk
{
    public override bool IsAvailable => true;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.BoostButton.png", 115f, "ui.button.perk.boost");

    public override void ButtonInitialize(PerkHolder.PerkInstance perkData, Action<CustomButton> buttonRegister)
    {
        CustomButton button = null;
        button = new CustomButton(
            () =>
            {
                RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, IP(1, PerkPropertyType.Second),1f + IP(0, PerkPropertyType.Percentage), false));
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () =>
            {
                button.Timer = button.MaxTimer;
                button.isEffectActive = false;
                button.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                RPCEventInvoker.UpdatePlayerVisibility(PlayerControl.LocalPlayer.PlayerId, true);
            },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            HudManager.Instance,
            null,
            true,
           IP(1, PerkPropertyType.Second),
           () =>
           {
               button.Timer = button.MaxTimer;
           },
            "button.label.boost"
        ).SetTimer(10f);
        button.MaxTimer = IP(2,PerkPropertyType.Second);

        buttonRegister.Invoke(button);
    }

    public Sprint(int id) : base(id, "sprint", true, 0, 6, new Color(0.3f,0.45f,0.7f))
    {
        ImportantProperties = new float[] { 50f, 2f, 40f };
    }
}
