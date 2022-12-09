namespace Nebula.Roles.CrewmateRoles;

public class Spy : Role
{
    public bool validSpyFlag;

    private Module.CustomOption impostorCanKillImpostorOption;
    private Module.CustomOption ventCoolDownOption;
    private Module.CustomOption ventDurationOption;

    public bool CanKillImpostor()
    {
        return impostorCanKillImpostorOption.getBool() && validSpyFlag;
    }

    public override void LoadOptionData()
    {
        impostorCanKillImpostorOption = CreateOption(Color.white, "impostorCanKillImpostor", true);
        ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f);
        ventCoolDownOption.suffix = "second";
        ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f);
        ventDurationOption.suffix = "second";
    }

    public override void Initialize(PlayerControl __instance)
    {
        VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
        VentDurationMaxTimer = ventDurationOption.getFloat();
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        validSpyFlag = true;
    }

    public override void StaticInitialize()
    {
        validSpyFlag = false;
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Seer);
        RelatedRoles.Add(Roles.Empiric);
        RelatedRoles.Add(Roles.Bait);
        RelatedRoles.Add(Roles.Provocateur);
    }

    public override bool IsUnsuitable { get { return GameOptionsManager.Instance.CurrentGameOptions.NumImpostors <= 1 || PlayerControl.AllPlayerControls.Count < 7; } }

    public Spy()
            : base("Spy", "spy", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, ImpostorRoles.Impostor.impostorSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanUseLimittedVent, false, false, true)
    {
        DeceiveImpostorInNameDisplay = true;
    }
}
