using static Il2CppSystem.Globalization.CultureInfo;
using static Rewired.Controller;

namespace Nebula.Roles.HnSImpostorRoles;

public static class HnSImpostorSystem
{
    public static CustomButton GenerateKillButton(HudManager __instance)
    {
        CustomButton killButton = null;
        var data = PlayerControl.LocalPlayer.GetModData();
        killButton = new CustomButton(
                () =>
                {
                    PlayerControl? target = Game.GameData.data.myData.currentTarget;
                    if (target != null) Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, target, Game.PlayerData.PlayerStatus.Dead, false, true);

                    float additional = 0f, ratio = 1f;

                    Perk.PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetKillCoolDown(p, target != null, ref additional, ref ratio));
                    killButton.Timer = (killButton.MaxTimer + additional) * ratio;

                    if (target == null)
                    {
                        float sa = 0f, sr = 1f;
                        float ta = 0f, tr = 1f;
                        Perk.PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetFailedKillPenalty(p, ref sa, ref sr, ref ta, ref tr));
                        sr = Mathf.Min(0, sr);
                        tr = Mathf.Min(0, tr);
                        RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, (killButton.Timer + ta) * tr, (0.25f + sa) * sr, false));
                    }


                    Game.GameData.data.myData.currentTarget = null;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && !data.Property.UnderTheFloor && !data.Attribute.HasAttribute(Game.PlayerAttribute.CannotKill); },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent,
                __instance,
                Module.NebulaInputManager.modKillInput.keyCode
            ).SetTimer(5f);
        killButton.MaxTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
        killButton.SetButtonCoolDownOption(true);

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

    static private CustomButton killButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        base.ButtonInitialize(__instance);

        if (killButton != null) killButton.Destroy();
        killButton = HnSImpostorSystem.GenerateKillButton(__instance);
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        HnSImpostorSystem.MyPlayerControlUpdate();
    }

    public override void CleanUp()
    {
        base.CleanUp();

        if (killButton != null)
        {
            killButton.Destroy();
            killButton = null;
        }
    }

    public HnSReaper()
            : base("HnSReaper", "reaperHnS", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorEndSet,
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

