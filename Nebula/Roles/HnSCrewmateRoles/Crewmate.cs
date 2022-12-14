using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.HnSCrewmateRoles;

public class Crewmate : Role
{
    protected TMPro.TextMeshPro ventButtonUsesString;
    protected GameObject ventButtonUsesObject;
    protected int leftCanUseVent=0;

    public override bool ShowInHelpWindow => false;

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

    public Crewmate()
            : base("Crewmate", "crewmate", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 CrewmateRoles.Crewmate.crewmateSideSet, CrewmateRoles.Crewmate.crewmateSideSet, CrewmateRoles.Crewmate.crewmateEndSet,
                 false, VentPermission.CanUseUnlimittedVent, true, false, false)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;

        VentColor = Palette.CrewmateBlue;
    }
}