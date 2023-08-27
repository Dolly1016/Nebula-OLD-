using Nebula.Game;
using static Il2CppSystem.Globalization.CultureInfo;
using static Rewired.Controller;

namespace Nebula.Roles.HnSImpostorRoles;

public static class HnSImpostorSystem
{
    public static ModAbilityButton GenerateKillButton()
    {
        var killButton = new ModAbilityButton(HudManager.Instance.KillButton.graphic.sprite, Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent);
        killButton.SetLabelType(ModAbilityButton.LabelType.Impostor).SetLabelLocalized("button.label.kill");
        killButton.MyAttribute = new SimpleAbilityAttribute(
            HnSModificator.GetDefaultCoolDown(),
            HnSModificator.GetDefaultCoolDown(),
            new SimpleButtonEvent((button) =>
            {
                var target = Game.GameData.data.myData.currentTarget;
                if (target != null) Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, target, Game.PlayerData.PlayerStatus.Dead, false, true);
                killButton.MyAttribute.StartCoolingDown(HnSModificator.StartKillCoolDown(target == null));
            }, Module.NebulaInputManager.modKillInput.keyCode)
            {

            });

        return killButton;
    }

    public static void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;

        float range = GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.KillDistance), 0, 2)];
        float additional = 0f, ratio = 1f;
        Perk.PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetKillRange(p, ref additional, ref ratio));
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget((range + additional) * ratio, true);

        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);
    }
}

public class HnSReaper : Template.Draggable
{
    public override bool ShowInHelpWindow => false;

    static private ModAbilityButton killButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        base.ButtonInitialize(__instance);

        killButton?.Destroy();
        killButton = HnSImpostorSystem.GenerateKillButton();
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        HnSImpostorSystem.MyPlayerControlUpdate();
    }

    public override void CleanUp()
    {
        base.CleanUp();

        killButton?.Destroy();
        killButton = null;
    }

    public HnSReaper()
            : base("HnSReaper", "reaperHnS", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.Impostor.impostorSideSet, Impostor.Impostor.impostorSideSet, Impostor.Impostor.impostorEndSet,
                 true, VentPermission.CanNotUse, false, true, true)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;
        canInvokeSabotage = false;
        HideKillButtonEvenImpostor = true;
        canReport = false;
    }
}

