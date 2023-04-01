using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.HnSCrewmateRoles;

public class Crewmate : Role
{
    protected TMPro.TextMeshPro ventButtonUsesString;
    protected GameObject ventButtonUsesObject;
    protected int leftCanUseVent=0;
    public int leftReviveId;
    public override bool ShowInHelpWindow => false;

    static private CustomButton reviveButton = null;

    private SpriteLoader reviveButtonSprite = new SpriteLoader("Nebula.Resources.ReviveButton.png", 115f, "ui.button.crewmate.revive");

    public override void ButtonInitialize(HudManager __instance)
    {
        var ventButton = FastDestroyableSingleton<HudManager>.Instance.ImpostorVentButton;
        try
        {
            leftCanUseVent = GameOptionsManager.Instance.currentHideNSeekGameOptions.GetInt(Int32OptionNames.CrewmateVentUses);
        }
        catch { leftCanUseVent = 1; }
        ventButtonUsesObject = ventButton.ShowUsesIcon(0, out ventButtonUsesString);
        ventButtonUsesString.text = leftCanUseVent.ToString();
        ventButton.gameObject.GetComponent<SpriteRenderer>().sprite = RoleManager.Instance.AllRoles.First(r=>r.Role==RoleTypes.Engineer).Ability.Image;
        ventButton.transform.GetChild(1).GetComponent<TMPro.TextMeshPro>().outlineColor = Palette.CrewmateBlue;
        VentDurationMaxTimer= GameOptionsManager.Instance.currentHideNSeekGameOptions.GetFloat(FloatOptionNames.CrewmateTimeInVent);

        if (reviveButton != null)
        {
            reviveButton.Destroy();
        }
        reviveButton = new CustomButton(
            () => {
                float additional = 0f, ratio = 1f;
                Perk.PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetReviveCost(p, ref additional, ref ratio));
                reviveButton.Timer = (reviveButton.Timer + additional) * ratio;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                if (reviveButton.isEffectActive && deadBodyId == byte.MaxValue)
                {
                    reviveButton.Timer = 0f;
                    reviveButton.isEffectActive = false;
                }
                return PlayerControl.LocalPlayer.CanMove && deadBodyId != byte.MaxValue && Game.GameData.data.myData.getGlobalData().GetRoleData(leftReviveId) > 0;
            },
            () =>
            {
                reviveButton.Timer = reviveButton.MaxTimer;
                reviveButton.isEffectActive = false;
            },
            reviveButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            10f,
            () =>
            {
                if (deadBodyId == byte.MaxValue) return;

                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftReviveId, -1);
                RPCEventInvoker.RevivePlayer(Helpers.playerById(deadBodyId));
            },
            "button.label.revive",
            ImageNames.VitalsButton
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        reviveButton.MaxTimer = 20f;

        int charge = 1;
        Perk.PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetReviveCharge(p, ref charge));
        RPCEventInvoker.UpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftReviveId, charge);
    }

    public override void OnUpdateRoleData(int dataId, int newData)
    {
        if (dataId == leftReviveId)
        {
            if (newData <= 0) newData = 0;
            reviveButton.UsesText.text = newData.ToString();
        }
    }

    public override void FinalizeInGame(PlayerControl __instance)
    {
        if (HudManager.InstanceExists)
        {
            var ventButton = FastDestroyableSingleton<HudManager>.Instance.ImpostorVentButton;
            ventButton.gameObject.GetComponent<SpriteRenderer>().sprite = CustomButton.OriginalVentButtonSprite;
            ventButton.transform.GetChild(1).GetComponent<TMPro.TextMeshPro>().outlineColor = Palette.ImpostorRed;
        }
    }
    public override void MyUpdate()
    {
        VentPermission = (!PlayerControl.LocalPlayer.inVent && leftCanUseVent <= 0) ? VentPermission.CanNotUse : VentPermission.CanUseUnlimittedVent;
    }

    public override void OnEnterVent(Vent vent)
    {
        leftCanUseVent--;
        if (ventButtonUsesObject)
            ventButtonUsesString.text = leftCanUseVent.ToString();
    }

    private byte deadBodyId = byte.MaxValue;

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        DeadBody body = Patches.PlayerControlPatch.SetMyDeadTarget();
        if (body)
        {
            deadBodyId = body.ParentId;
        }
        else
        {
            deadBodyId = byte.MaxValue;
        }
        Patches.PlayerControlPatch.SetDeadBodyOutline(body, Color.yellow);
    }

    public override void CleanUp()
    {
        base.CleanUp();

        if (reviveButton != null)
        {
            reviveButton.Destroy();
            reviveButton = null;
        }
    }

    public override bool CheckWin(PlayerControl player, EndCondition winReason)
    {
        if (winReason != EndCondition.CrewmateWinHnS) return false;
        if (!CustomOptionHolder.MustDoTasksToWinOption.getBool()) return true;
        if (player.Data.IsDead) return false;

        var tasks = player.GetModData().Tasks;
        return tasks.Completed >= tasks.Quota;
    }

    public Crewmate()
            : base("Hider", "hider", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 CrewmateRoles.Crewmate.crewmateSideSet, CrewmateRoles.Crewmate.crewmateSideSet, new HashSet<Patches.EndCondition>(),
                 false, VentPermission.CanUseLimittedVent, true, false, false)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;
        canReport = false;

        VentColor = Palette.CrewmateBlue;

        leftReviveId = Game.GameData.RegisterRoleDataId("crewmate.revive");
    }
}