using Nebula.Patches;

namespace Nebula.Roles.RitualRoles;

public class RitualKiller : RitualRole
{
    List<Objects.Arrow> Arrows;

    public RitualKiller()
            : base("Killer", "killer", Palette.ImpostorRed, RoleCategory.Impostor, Side.Killer, Side.Killer,
                 new HashSet<Side>(new Side[] { Side.Killer }), new HashSet<Side>(new Side[] { Side.Killer }), new HashSet<EndCondition>(new EndCondition[] { EndCondition.KillerWin }),
                 false, VentPermission.CanNotUse, false, true, true)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.Ritual;
        Arrows = new List<Objects.Arrow>();
        canInvokeSabotage = false;
        HideKillButtonEvenImpostor = true;
    }

    public override void Initialize(PlayerControl __instance)
    {
        Arrows.Clear();
    }

    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget();
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
    }

    Objects.CustomButton? killButton;

    public override void ButtonInitialize(HudManager __instance)
    {
        if (killButton != null)
        {
            killButton.Destroy();
        }

        killButton = new Objects.CustomButton(
            () =>
            {

            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                if (killButton.isEffectActive && Game.GameData.data.myData.currentTarget == null)
                {
                    killButton.Timer = CustomOptionHolder.RitualKillFailedPenaltyOption.getFloat();
                    killButton.isEffectActive = false;
                }
                return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove;
            },
            () => { killButton.Timer = killButton.MaxTimer; },
            __instance.KillButton.graphic.sprite,
            -1,
            __instance,
            Module.NebulaInputManager.modKillInput.keyCode,
            true,
            2f,
            () =>
            {
                if (Game.GameData.data.myData.currentTarget == null) return;
                Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget,
                    Game.PlayerData.PlayerStatus.Dead, false, true
                    );
                killButton.Timer = killButton.MaxTimer;
            }
        );
        killButton.MaxTimer = CustomOptionHolder.RitualKillCoolDownOption.getFloat();
    }

    public override void CleanUp()
    {
        if (killButton != null)
        {
            killButton.Destroy();
            killButton = null;
        }
    }
}
