using Nebula.Module;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles;

public class LastImpostor : ExtraRole
{
    private CustomButton GuessSpectreButton;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.SpectreGuessButton.png", 115f);

    public override void Assignment(Patches.AssignMap assignMap)
    {

    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        base.GlobalInitialize(__instance);
    }

    public override void EditDisplayRoleName(byte playerId, ref string roleName, bool isIntro)
    {
        if (isIntro) return;
        roleName += Helpers.cs(Palette.ImpostorRed, " LI");
    }

    public override void LoadOptionData()
    {

    }

    bool canGuess;
    List<PlayerControl> alives = new List<PlayerControl>();

    public override void OnMeetingEnd()
    {
        alives.Clear();
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p.PlayerId != PlayerControl.LocalPlayer.PlayerId && !p.Data.IsDead) alives.Add(p);
        }
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        OnMeetingEnd();

        canGuess = true;

        if (GuessSpectreButton != null)
        {
            GuessSpectreButton.Destroy();
        }
        GuessSpectreButton = new CustomButton(
            () =>
            {
                int height = 1 + (alives.Count - 1) / 3;
                var dialog = MetaDialog.OpenDialog(new Vector2(8f, 1.2f + (float)height * 0.52f), Language.Language.GetString("role.lastImpostor.guessGuiTitle"));
                var designers = dialog.Split(3, 0.2f);
                int i = 0;
                foreach (var player in alives)
                {
                    PlayerControl p = player;

                    var button = designers[i].AddPlayerButton(new Vector2(2.3f, 0.4f), p, true);
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        MetaDialog.EraseDialog(1);
                        canGuess = false;

                        if (p.Data.IsDead) return;

                        if (p.GetModData().role == Roles.Spectre)
                            RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId, p.PlayerId, Game.PlayerData.PlayerStatus.Guessed.Id, false);
                        else
                            RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.PlayerId, Game.PlayerData.PlayerStatus.Suicide.Id, false);
                    }));

                    i = (i + 1) % 3;
                }
            },
            () => { return alives.Count <= 3 && canGuess && !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () => { },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.LeftSideContent,
            __instance,
            null,
            "button.label.guess"
        ).SetTimer(0f);
        GuessSpectreButton.Timer = GuessSpectreButton.MaxTimer = 0;

    }

    public override void CleanUp()
    {
        if (GuessSpectreButton != null)
        {
            GuessSpectreButton.Destroy();
            GuessSpectreButton = null;
        }
    }

    public LastImpostor() : base("LastImpostor", "lastImpostor", Palette.ImpostorRed, 0)
    {
        IsHideRole = true;
    }
}
