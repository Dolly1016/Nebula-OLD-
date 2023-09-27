using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

public class Reaper : ConfigurableStandardRole
{

    static public Reaper MyRole = new Reaper();

    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "reaper";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private new VentConfiguration VentConfiguration = null!;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        VentConfiguration = new(RoleConfig, null, (5f, 60f, 15f), (2.5f, 30f, 10f));
    }

    public class Instance : Impostor.Instance
    {
        public override AbstractRole Role => MyRole;
        private Scripts.Draggable? draggable = null;
        private Timer ventCoolDown = new Timer(MyRole.VentConfiguration.CoolDown).SetAsAbilityCoolDown().Start();
        private Timer ventDuration = new(MyRole.VentConfiguration.Duration);
        public override Timer? VentCoolDown => ventCoolDown;
        public override Timer? VentDuration => ventDuration;

        public Instance(PlayerModInfo player) : base(player)
        {
            draggable = Bind(new Scripts.Draggable());
        }

        private Vent? GetVent(string name)
        {
            return ShipStatus.Instance.AllVents.FirstOrDefault(v=>v.name == name);
        }

        private void EditVentInfo(bool activate)
        {
            switch (AmongUsUtil.CurrentMapId)
            {
                case 0:
                    //Skeld
                    GetVent("NavVentNorth")!.Right = activate ? GetVent("NavVentSouth") : null;
                    GetVent("NavVentSouth")!.Right = activate ? GetVent("NavVentNorth") : null;

                    GetVent("ShieldsVent")!.Left = activate ? GetVent("WeaponsVent") : null;
                    GetVent("WeaponsVent")!.Center = activate ? GetVent("ShieldsVent") : null;

                    GetVent("ReactorVent")!.Left = activate ? GetVent("UpperReactorVent") : null;
                    GetVent("UpperReactorVent")!.Left = activate ? GetVent("ReactorVent") : null;

                    GetVent("SecurityVent")!.Center = activate ? GetVent("ReactorVent") : null;
                    GetVent("ReactorVent")!.Center = activate ? GetVent("SecurityVent") : null;

                    GetVent("REngineVent")!.Center = activate ? GetVent("LEngineVent") : null;
                    GetVent("LEngineVent")!.Center = activate ? GetVent("REngineVent") : null;

                    if (GetVent("StorageVent") != null)
                    {
                        GetVent("AdminVent")!.Center = activate ? GetVent("StorageVent") : null;
                        GetVent("StorageVent")!.Left = activate ? GetVent("ElecVent") : null;
                        GetVent("StorageVent")!.Right = activate ? GetVent("AdminVent") : null;

                        GetVent("StorageVent")!.Center = activate ? GetVent("CafeUpperVent") : null;
                    }
                    else
                    {
                        GetVent("AdminVent")!.Center = activate ? GetVent("MedVent") : null;
                        GetVent("MedVent")!.Center = activate ? GetVent("AdminVent") : null;
                    }

                    if (GetVent("CafeUpperVent") != null)
                    {
                        GetVent("CafeUpperVent")!.Left = activate ? GetVent("LEngineVent") : null;
                        GetVent("LEngineVent")!.Right = activate ? GetVent("CafeUpperVent") : null;

                        GetVent("CafeUpperVent")!.Center = activate ? GetVent("StorageVent") : null;

                        GetVent("CafeUpperVent")!.Right = activate ? GetVent("WeaponsVent") : null;
                        GetVent("WeaponsVent")!.Left = activate ? GetVent("CafeUpperVent") : null;
                    }
                    else
                    {
                        GetVent("CafeVent")!.Center = activate ? GetVent("WeaponsVent") : null;
                        GetVent("WeaponsVent")!.Center = activate ? GetVent("CafeVent") : null;
                    }

                    break;
                case 2:
                    //Polus
                    GetVent("CommsVent")!.Center = activate ? GetVent("ElecFenceVent") : null;
                    GetVent("ElecFenceVent")!.Center = activate ? GetVent("CommsVent") : null;

                    GetVent("ElectricalVent")!.Center = activate ? GetVent("ElectricBuildingVent") : null;
                    GetVent("ElectricBuildingVent")!.Center = activate ? GetVent("ElectricalVent") : null;

                    GetVent("ScienceBuildingVent")!.Right = activate ? GetVent("BathroomVent") : null;
                    GetVent("BathroomVent")!.Center = activate ? GetVent("ScienceBuildingVent") : null;

                    GetVent("SouthVent")!.Center = activate ? GetVent("OfficeVent") : null;
                    GetVent("OfficeVent")!.Center = activate ? GetVent("SouthVent") : null;

                    if (GetVent("SpecimenVent") != null)
                    {
                        GetVent("AdminVent")!.Center = activate ? GetVent("SpecimenVent") : null;
                        GetVent("SpecimenVent")!.Left = activate ? GetVent("AdminVent") : null;

                        GetVent("SubBathroomVent")!.Center = activate ? GetVent("SpecimenVent") : null;
                        GetVent("SpecimenVent")!.Right = activate ? GetVent("SubBathroomVent") : null;
                    }
                    break;
                case 4:
                    //Airship
                    GetVent("VaultVent")!.Right = activate ? GetVent("GaproomVent1") : null;
                    GetVent("GaproomVent1")!.Center = activate ? GetVent("VaultVent") : null;

                    GetVent("EjectionVent")!.Right = activate ? GetVent("KitchenVent") : null;
                    GetVent("KitchenVent")!.Center = activate ? GetVent("EjectionVent") : null;

                    GetVent("HallwayVent1")!.Center = activate ? GetVent("HallwayVent2") : null;
                    GetVent("HallwayVent2")!.Center = activate ? GetVent("HallwayVent1") : null;

                    GetVent("GaproomVent2")!.Center = activate ? GetVent("RecordsVent") : null;
                    GetVent("RecordsVent")!.Center = activate ? GetVent("GaproomVent2") : null;

                    if (GetVent("ElectricalVent") != null)
                    {
                        GetVent("MeetingVent")!.Left = activate ? GetVent("GaproomVent1") : null;

                        GetVent("ElectricalVent")!.Left = activate ? GetVent("MeetingVent") : null;
                        //GetVent("MeetingVent").Right = activate ? GetVent("ElectricalVent") : null;

                        GetVent("ShowersVent")!.Center = activate ? GetVent("ElectricalVent") : null;
                        GetVent("ElectricalVent")!.Right = activate ? GetVent("ShowersVent") : null;
                    }
                    break;
            }
        }

        public override void OnActivated()
        {
            base.OnActivated();

            draggable?.OnActivated(this);
            EditVentInfo(true);
        }

        public override void OnDead()
        {
            draggable?.OnDead(this);
        }

        protected override void OnInactivated()
        {
            draggable?.OnInactivated(this);
            EditVentInfo(false);
        }
    }
}
