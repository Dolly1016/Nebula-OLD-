using Nebula.Game;

namespace Nebula.Roles.ImpostorRoles;

public class Reaper : Template.Draggable
{
    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(DaDSprite,"role.reaper.help.dragAndDrop",0.3f)
    };

    private Module.CustomOption ventCoolDownOption;
    private Module.CustomOption ventDurationOption;
    private void ConnectVent(bool connect)
    {
        Dictionary<string, VentData> ventMap = Game.GameData.data.VentMap;
        switch (GameOptionsManager.Instance.CurrentGameOptions.MapId)
        {
            case 0:
                //Skeld
                ventMap["NavVentNorth"].Vent.Right = connect ? ventMap["NavVentSouth"] : new Vent();
                ventMap["NavVentSouth"].Vent.Right = connect ? ventMap["NavVentNorth"] : new Vent();

                ventMap["ShieldsVent"].Vent.Left = connect ? ventMap["WeaponsVent"] : new Vent();
                ventMap["WeaponsVent"].Vent.Center = connect ? ventMap["ShieldsVent"] : new Vent();

                ventMap["ReactorVent"].Vent.Left = connect ? ventMap["UpperReactorVent"] : new Vent();
                ventMap["UpperReactorVent"].Vent.Left = connect ? ventMap["ReactorVent"] : new Vent();

                ventMap["SecurityVent"].Vent.Center = connect ? ventMap["ReactorVent"] : new Vent();
                ventMap["ReactorVent"].Vent.Center = connect ? ventMap["SecurityVent"] : new Vent();

                ventMap["REngineVent"].Vent.Center = connect ? ventMap["LEngineVent"] : new Vent();
                ventMap["LEngineVent"].Vent.Center = connect ? ventMap["REngineVent"] : new Vent();

                if (ventMap.ContainsKey("StorageVent"))
                {
                    ventMap["AdminVent"].Vent.Center = connect ? ventMap["StorageVent"] : new Vent();
                    ventMap["StorageVent"].Vent.Left = connect ? ventMap["ElecVent"] : new Vent();
                    ventMap["StorageVent"].Vent.Right = connect ? ventMap["AdminVent"] : new Vent();

                    ventMap["StorageVent"].Vent.Center = connect ? ventMap["CafeUpperVent"] : new Vent();
                }
                else
                {
                    ventMap["AdminVent"].Vent.Center = connect ? ventMap["MedVent"] : new Vent();
                    ventMap["MedVent"].Vent.Center = connect ? ventMap["AdminVent"] : new Vent();
                }

                if (ventMap.ContainsKey("CafeUpperVent"))
                {
                    ventMap["CafeUpperVent"].Vent.Left = connect ? ventMap["LEngineVent"] : new Vent();
                    ventMap["LEngineVent"].Vent.Right = connect ? ventMap["CafeUpperVent"] : new Vent();

                    ventMap["CafeUpperVent"].Vent.Center = connect ? ventMap["StorageVent"] : new Vent();

                    ventMap["CafeUpperVent"].Vent.Right = connect ? ventMap["WeaponsVent"] : new Vent();
                    ventMap["WeaponsVent"].Vent.Left = connect ? ventMap["CafeUpperVent"] : new Vent();
                }
                else
                {
                    ventMap["CafeVent"].Vent.Center = connect ? ventMap["WeaponsVent"] : new Vent();
                    ventMap["WeaponsVent"].Vent.Center = connect ? ventMap["CafeVent"] : new Vent();
                }

                break;
            case 2:
                //Polus
                ventMap["CommsVent"].Vent.Center = connect ? ventMap["ElecFenceVent"] : new Vent();
                ventMap["ElecFenceVent"].Vent.Center = connect ? ventMap["CommsVent"] : new Vent();

                ventMap["ElectricalVent"].Vent.Center = connect ? ventMap["ElectricBuildingVent"] : new Vent();
                ventMap["ElectricBuildingVent"].Vent.Center = connect ? ventMap["ElectricalVent"] : new Vent();

                ventMap["ScienceBuildingVent"].Vent.Right = connect ? ventMap["BathroomVent"] : new Vent();
                ventMap["BathroomVent"].Vent.Center = connect ? ventMap["ScienceBuildingVent"] : new Vent();

                ventMap["SouthVent"].Vent.Center = connect ? ventMap["OfficeVent"] : new Vent();
                ventMap["OfficeVent"].Vent.Center = connect ? ventMap["SouthVent"] : new Vent();

                if (ventMap.ContainsKey("SpecimenVent"))
                {
                    ventMap["AdminVent"].Vent.Center = connect ? ventMap["SpecimenVent"] : new Vent();
                    ventMap["SpecimenVent"].Vent.Left = connect ? ventMap["AdminVent"] : new Vent();

                    ventMap["SubBathroomVent"].Vent.Center = connect ? ventMap["SpecimenVent"] : new Vent();
                    ventMap["SpecimenVent"].Vent.Right = connect ? ventMap["SubBathroomVent"] : new Vent();
                }
                break;
            case 4:
                //Airship
                ventMap["VaultVent"].Vent.Right = connect ? ventMap["GaproomVent1"] : new Vent();
                ventMap["GaproomVent1"].Vent.Center = connect ? ventMap["VaultVent"] : new Vent();

                ventMap["EjectionVent"].Vent.Right = connect ? ventMap["KitchenVent"] : new Vent();
                ventMap["KitchenVent"].Vent.Center = connect ? ventMap["EjectionVent"] : new Vent();

                ventMap["HallwayVent1"].Vent.Center = connect ? ventMap["HallwayVent2"] : new Vent();
                ventMap["HallwayVent2"].Vent.Center = connect ? ventMap["HallwayVent1"] : new Vent();

                ventMap["GaproomVent2"].Vent.Center = connect ? ventMap["RecordsVent"] : new Vent();
                ventMap["RecordsVent"].Vent.Center = connect ? ventMap["GaproomVent2"] : new Vent();

                if (ventMap.ContainsKey("ElectricalVent"))
                {
                    ventMap["MeetingVent"].Vent.Left = connect ? ventMap["GaproomVent1"] : new Vent();

                    ventMap["ElectricalVent"].Vent.Left = connect ? ventMap["MeetingVent"] : new Vent();
                    //ventMap["MeetingVent"].Vent.Right = connect ? ventMap["ElectricalVent"] : new Vent();

                    ventMap["ShowersVent"].Vent.Center = connect ? ventMap["ElectricalVent"] : new Vent();
                    ventMap["ElectricalVent"].Vent.Right = connect ? ventMap["ShowersVent"] : new Vent();
                }
                break;
        }
    }

    public override void LoadOptionData()
    {
        ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 10f, 10f, 60f, 2.5f);
        ventCoolDownOption.suffix = "second";
        ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 10f, 60f, 2.5f);
        ventDurationOption.suffix = "second";
    }

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);
        ConnectVent(true);
        VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
        VentDurationMaxTimer = ventDurationOption.getFloat();
    }

    public override void FinalizeInGame(PlayerControl __instance)
    {
        ConnectVent(false);
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Jester);
        RelatedRoles.Add(Roles.Necromancer);
    }

    public Reaper()
            : base("Reaper", "reaper", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseLimittedVent, true, true, true)
    {
    }
}
