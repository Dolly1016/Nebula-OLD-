using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.HnSImpostorRoles;

public class Impostor : Template.Draggable
{
    public override bool ShowInHelpWindow => false;

    static private CustomButton killButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        base.ButtonInitialize(__instance);

        if (killButton != null)
        {
            killButton.Destroy();
        }
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
                    RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, (killButton.Timer + ta) * tr, (0.25f + sa) * sr, false));
                }
                
                
                Game.GameData.data.myData.currentTarget = null;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () => { killButton.Timer = killButton.MaxTimer; },
            __instance.KillButton.graphic.sprite,
            Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent,
            __instance,
            Module.NebulaInputManager.modKillInput.keyCode
        ).SetTimer(5f);
        killButton.MaxTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
        killButton.SetButtonCoolDownOption(true);
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        Game.MyPlayerData data = Game.GameData.data.myData;
        
        float range = GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.KillDistance), 0, 2)];
        float additional = 0f, ratio = 1f;
        Perk.PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetKillRange(p, ref additional, ref ratio));
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget((range + additional) * ratio, true);

        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);
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

    public Impostor(string roleName="Impostor",string localizedName="impostor")
            : base(roleName, localizedName, Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorEndSet,
                 true, VentPermission.CanNotUse, false, true, true)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;
        canInvokeSabotage = false;
        HideKillButtonEvenImpostor = true;
    }
}

