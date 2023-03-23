using Il2CppSystem.Threading.Tasks;
using Nebula.Game;
using Nebula.Roles.ComplexRoles;

namespace Nebula.Roles.Perk.CrewmatePerks;

public class Tempest : Perk
{
    public override bool IsAvailable => true;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.AccelTrapButton.png", 115f, "ui.button.perk.placeAccelTrap");

    public override void ButtonInitialize(PerkHolder.PerkInstance perkData, Action<CustomButton> buttonRegister)
    {
        bool isUsed = false;
        CustomButton button = null;

        button = new CustomButton(
            () =>
            {
                RPCEventInvoker.ObjectInstantiate(CustomObject.Type.AccelTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset + new Vector3(0f, 0.05f, 0f));

                RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(2, IP(0, PerkPropertyType.Second), 0f, false));
                button.Timer = IP(0, PerkPropertyType.Second);
                Objects.SoundPlayer.PlaySound(Module.AudioAsset.PlaceTrap2s);
            },
            () => { return !isUsed && !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove && !isUsed; },
            () => { button.Timer = button.MaxTimer; },
            FTrapper.accelButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            HudManager.Instance,
            null,
            true,
            IP(0, PerkPropertyType.Second),
            () =>
            {
                button.Timer = button.MaxTimer;
                isUsed = true;
                button.UsesText.text = "0";
            }, "button.label.place"
        ).SetTimer(10f);
        button.UsesText.text = "1";
        button.MaxTimer = 10f;
        button.EffectDuration = IP(0,PerkPropertyType.Second);
        button.SetUsesIcon(0);

        buttonRegister.Invoke(button);
    }

    public Tempest(int id) : base(id, "tempest", true, 4, 5, new Color(0.2f, 0.4f, 0.85f))
    {
        ImportantProperties = new float[] { 2f, 3f, 60f };

        Objects.CustomObject.RegisterUpdater((player) =>
        {
            if (!HnSModificator.IsHnSGame) return;

            CustomObject trap = Objects.CustomObject.GetTarget(0.875f / 2f, player, (obj) => { return obj.PassedMeetings > 0; }, Objects.ObjectTypes.VisibleTrap.AccelTrap, Objects.ObjectTypes.VisibleTrap.DecelTrap);
            if (trap == null) return;

            if (trap.ObjectType == Objects.ObjectTypes.VisibleTrap.AccelTrap)
            {
                RPCEventInvoker.EmitSpeedFactor(player,
                    new Game.SpeedFactor(1, IP(1, PerkPropertyType.Second), 1f + IP(2, PerkPropertyType.Percentage), false));
            }
        });
    }
}
