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
        private void ConnectVent(bool connect)
        {
            Dictionary<string, VentData> ventMap = Game.GameData.data.VentMap;
            switch (PlayerControl.GameOptions.MapId)
            {
                case 0:
                    //Skeld
                    ventMap["WeaponsVent"].Vent.Left = connect ? ventMap["CafeVent"] : null;
                    ventMap["CafeVent"].Vent.Center = connect ? ventMap["WeaponsVent"] : null;

                    ventMap["NavVentNorth"].Vent.Right = connect ? ventMap["NavVentSouth"] : null;
                    ventMap["NavVentSouth"].Vent.Right = connect ? ventMap["NavVentNorth"] : null;

                    ventMap["ReactorVent"].Vent.Left = connect ? ventMap["UpperReactorVent"] : null;
                    ventMap["UpperReactorVent"].Vent.Left = connect ? ventMap["ReactorVent"] : null;

                    ventMap["ReactorVent"].Vent.Center = connect ? ventMap["SecurityVent"] : null;
                    ventMap["SecurityVent"].Vent.Center = connect ? ventMap["ReactorVent"] : null;

                    ventMap["MedVent"].Vent.Center = connect ? ventMap["AdminVent"] : null;
                    ventMap["AdminVent"].Vent.Left = connect ? ventMap["MedVent"] : null;
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

        public override void Initialize(PlayerControl __instance)
        {
            ConnectVent(true);
        }

        public override void FinalizeInGame(PlayerControl __instance)
        {
            ConnectVent(false);
        }

        //インポスターはModで操作するFakeTaskは所持していない
        public Reaper()
                : base("Reaper", "reaper", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     false, true, true, false, true)
        {
        }
    }
}
