using System;
using System.Collections.Generic;
using System.Text;
using static Il2CppSystem.Globalization.CultureInfo;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Roles.ImpostorRoles;

public class Executioner : Role
{
    public override void LoadOptionData()
    {
    }

    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        if (data.currentTarget != null) Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);
        data.currentTarget= null;
    }

    static private CustomButton killButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        Game.GameData.data.myData.currentTarget = null;

        if (killButton != null)
        {
            killButton.Destroy();
        }
        killButton = new CustomButton(
            () =>
            {
                RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, 3f, 0f, false));
                PlayerControl.LocalPlayer.lightSource.StartCoroutine(RoleSystem.WarpSystem.CoOrient(PlayerControl.LocalPlayer.lightSource, 0.6f, 2.4f,
                    (p) =>
                    {
                        Game.MyPlayerData data = Game.GameData.data.myData;
                        data.currentTarget = p;
                    }, (p) =>
                    {
                        if (p != null)
                        {
                            RPCEventInvoker.ObjectInstantiate(CustomObject.Type.TeleportEvidence,PlayerControl.LocalPlayer.GetTruePosition());
                            Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, p, Game.PlayerData.PlayerStatus.Dead, false, true);
                        }
                    }).WrapToIl2Cpp());
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () => { killButton.Timer = killButton.MaxTimer; },
            __instance.KillButton.graphic.sprite,
            Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent,
            __instance,
            Module.NebulaInputManager.modKillInput.keyCode,
            true,
        3.1f,
        () => {
            killButton.Timer = killButton.MaxTimer;
        }
        ).SetTimer(CustomOptionHolder.InitialKillCoolDownOption.getFloat());
        killButton.MaxTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
        killButton.SetButtonCoolDownOption(true);
    }
    public override void CleanUp()
    {
        if (killButton != null)
        {
            killButton.Destroy();
            killButton = null;
        }
    }


    public Executioner()
            : base("Executioner", "executioner", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        //通常のキルボタンは使用しない
        HideKillButtonEvenImpostor = true;
    }
}
