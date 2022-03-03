using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;
using Nebula.Game;

namespace Nebula.Roles.ImpostorRoles
{
    public class Reaper : Template.Draggable
    {
        private Module.CustomOption ventCoolDownOption;
        private Module.CustomOption ventDurationOption;
        private void ConnectVent(bool connect)
        {
            Dictionary<string, VentData> ventMap = Game.GameData.data.VentMap;
            switch (PlayerControl.GameOptions.MapId)
            {
                case 0:
                    //Skeld
                    ventMap["NavVentNorth"].Vent.Right = connect ? ventMap["NavVentSouth"] : null;
                    ventMap["NavVentSouth"].Vent.Right = connect ? ventMap["NavVentNorth"] : null;

                    ventMap["ShieldsVent"].Vent.Left = connect ? ventMap["WeaponsVent"] : null;
                    ventMap["WeaponsVent"].Vent.Center = connect ? ventMap["ShieldsVent"] : null;

                    ventMap["ReactorVent"].Vent.Left = connect ? ventMap["UpperReactorVent"] : null;
                    ventMap["UpperReactorVent"].Vent.Left = connect ? ventMap["ReactorVent"] : null;

                    ventMap["SecurityVent"].Vent.Center = connect ? ventMap["ReactorVent"] : null;
                    ventMap["ReactorVent"].Vent.Center = connect ? ventMap["SecurityVent"] : null;

                    ventMap["REngineVent"].Vent.Center = connect ? ventMap["LEngineVent"] : null;
                    ventMap["LEngineVent"].Vent.Center = connect ? ventMap["REngineVent"] : null;

                    if (ventMap.ContainsKey("StorageVent"))
                    {
                        ventMap["AdminVent"].Vent.Center = connect ? ventMap["StorageVent"] : null;
                        ventMap["StorageVent"].Vent.Left = connect ? ventMap["ElecVent"] : null;
                        ventMap["StorageVent"].Vent.Right = connect ? ventMap["AdminVent"] : null;

                        ventMap["StorageVent"].Vent.Center = connect ? ventMap["CafeUpperVent"] : null;
                    }
                    else
                    {
                        ventMap["AdminVent"].Vent.Center = connect ? ventMap["MedVent"] : null;
                        ventMap["MedVent"].Vent.Center = connect ? ventMap["AdminVent"] : null;
                    }

                    if (ventMap.ContainsKey("CafeUpperVent"))
                    {
                        ventMap["CafeUpperVent"].Vent.Left = connect ? ventMap["LEngineVent"] : null;
                        ventMap["LEngineVent"].Vent.Right = connect ? ventMap["CafeUpperVent"] : null;

                        ventMap["CafeUpperVent"].Vent.Center = connect ? ventMap["StorageVent"] : null;

                        ventMap["CafeUpperVent"].Vent.Right = connect ? ventMap["WeaponsVent"] : null;
                        ventMap["WeaponsVent"].Vent.Left = connect ? ventMap["CafeUpperVent"] : null;
                    }
                    else
                    {
                        ventMap["CafeVent"].Vent.Center = connect ? ventMap["WeaponsVent"] : null;
                        ventMap["WeaponsVent"].Vent.Center = connect ? ventMap["CafeVent"] : null;
                    }

                    break;
                case 2:
                    //Polus
                    ventMap["CommsVent"].Vent.Center = connect ? ventMap["ElecFenceVent"] : null;
                    ventMap["ElecFenceVent"].Vent.Center = connect ? ventMap["CommsVent"] : null;

                    ventMap["ElectricalVent"].Vent.Center = connect ? ventMap["ElectricBuildingVent"] : null;
                    ventMap["ElectricBuildingVent"].Vent.Center = connect ? ventMap["ElectricalVent"] : null;

                    ventMap["ScienceBuildingVent"].Vent.Right = connect ? ventMap["BathroomVent"] : null;
                    ventMap["BathroomVent"].Vent.Center = connect ? ventMap["ScienceBuildingVent"] : null;

                    ventMap["AdminVent"].Vent.Center = connect ? ventMap["OfficeVent"] : null;
                    ventMap["OfficeVent"].Vent.Center = connect ? ventMap["AdminVent"] : null;
                    break;
                case 4:
                    //Airship
                    ventMap["VaultVent"].Vent.Right = connect ? ventMap["GaproomVent1"] : null;
                    ventMap["GaproomVent1"].Vent.Left = connect ? ventMap["VaultVent"] : null;

                    ventMap["EjectionVent"].Vent.Right = connect ? ventMap["KitchenVent"] : null;
                    ventMap["KitchenVent"].Vent.Left = connect ? ventMap["EjectionVent"] : null;

                    ventMap["HallwayVent1"].Vent.Right = connect ? ventMap["HallwayVent2"] : null;
                    ventMap["HallwayVent2"].Vent.Center = connect ? ventMap["HallwayVent1"] : null;

                    ventMap["GaproomVent2"].Vent.Center = connect ? ventMap["RecordsVent"] : null;
                    ventMap["RecordsVent"].Vent.Center = connect ? ventMap["GaproomVent2"] : null;
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
}
