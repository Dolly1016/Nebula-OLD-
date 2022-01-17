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

namespace Nebula.Roles.ImpostorRoles
{
    public class Reaper : Role
    {
        /* ボタン */
        private CustomButton dragButton;
        public override void ButtonInitialize(HudManager __instance)
        {
            if (dragButton != null)
            {
                dragButton.Destroy();
            }
            dragButton = new CustomButton(
                () =>
                {
                    byte target;
                    if (Game.GameData.data.myData.getGlobalData().dragPlayerId != Byte.MaxValue)
                    {
                        target = Byte.MaxValue;
                    }
                    else
                    {
                        target = (byte)deadBodyId;
                    }

                    MessageWriter dragAndDropWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DragAndDropPlayer, Hazel.SendOption.Reliable, -1);
                    dragAndDropWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                    dragAndDropWriter.Write(target);
                    AmongUsClient.Instance.FinishRpcImmediately(dragAndDropWriter);
                    RPCEvents.DragAndDropPlayer(PlayerControl.LocalPlayer.PlayerId, target);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => { },
                getDragButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );

            dragButton.MaxTimer = 1;
            dragButton.Timer = 0;
        }

        public override void ButtonActivate()
        {
            dragButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            dragButton.setActive(false);
        }

        /* 画像 */

        private Sprite dragButtonSprite = null;
        public Sprite getDragButtonSprite()
        {
            if (dragButtonSprite) return dragButtonSprite;
            dragButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DragButton.png", 115f);
            return dragButtonSprite;
        }

        private Sprite dropButtonSprite = null;
        public Sprite getDropButtonSprite()
        {
            if (dropButtonSprite) return dropButtonSprite;
            dropButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DropButton.png", 115f);
            return dropButtonSprite;
        }

        /* 各種変数 */
        public byte deadBodyId;


        public override void MyPlayerControlUpdate()
        {
            if (Game.GameData.data.myData.getGlobalData() == null) return;

            if (Game.GameData.data.myData.getGlobalData().dragPlayerId == byte.MaxValue)
            {
                dragButton.Sprite = getDragButtonSprite();

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
            else
            {
                dragButton.Sprite = getDropButtonSprite();
            }
        }

        public override void ButtonCleanUp()
        {
            if (dragButton != null)
            {
                dragButton.Destroy();
                dragButton = null;
            }
        }

        private void ConnectVent(bool connect)
        {
            Dictionary<string, Vent> ventMap = Game.GameData.data.VentMap;
            switch (PlayerControl.GameOptions.MapId)
            {
                case 0:
                    //Skeld
                    ventMap["WeaponsVent"].Left = connect ? ventMap["CafeVent"] : null;
                    ventMap["CafeVent"].Center = connect ? ventMap["WeaponsVent"] : null;

                    ventMap["NavVentNorth"].Right = connect ? ventMap["NavVentSouth"] : null;
                    ventMap["NavVentSouth"].Right = connect ? ventMap["NavVentNorth"] : null;

                    ventMap["ReactorVent"].Left = connect ? ventMap["UpperReactorVent"] : null;
                    ventMap["UpperReactorVent"].Left = connect ? ventMap["ReactorVent"] : null;

                    ventMap["ReactorVent"].Center = connect ? ventMap["SecurityVent"] : null;
                    ventMap["SecurityVent"].Center = connect ? ventMap["ReactorVent"] : null;

                    ventMap["MedVent"].Center = connect ? ventMap["AdminVent"] : null;
                    ventMap["AdminVent"].Left = connect ? ventMap["MedVent"] : null;
                    break;
                case 2:
                    //Polus
                    ventMap["CommsVent"].Center = connect ? ventMap["ElecFenceVent"] : null;
                    ventMap["ElecFenceVent"].Center = connect ? ventMap["CommsVent"] : null;

                    ventMap["ElectricalVent"].Center = connect ? ventMap["ElectricBuildingVent"] : null;
                    ventMap["ElectricBuildingVent"].Center = connect ? ventMap["ElectricalVent"] : null;

                    ventMap["ScienceBuildingVent"].Right = connect ? ventMap["BathroomVent"] : null;
                    ventMap["BathroomVent"].Center = connect ? ventMap["ScienceBuildingVent"] : null;

                    ventMap["AdminVent"].Center = connect ? ventMap["OfficeVent"] : null;
                    ventMap["OfficeVent"].Center = connect ? ventMap["AdminVent"] : null;
                    break;
                case 4:
                    //Airship
                    ventMap["VaultVent"].Right = connect ? ventMap["GaproomVent1"] : null;
                    ventMap["GaproomVent1"].Left = connect ? ventMap["VaultVent"] : null;

                    ventMap["EjectionVent"].Right = connect ? ventMap["KitchenVent"] : null;
                    ventMap["KitchenVent"].Left = connect ? ventMap["EjectionVent"] : null;

                    ventMap["HallwayVent1"].Right = connect ? ventMap["HallwayVent2"] : null;
                    ventMap["HallwayVent2"].Center = connect ? ventMap["HallwayVent1"] : null;

                    ventMap["GaproomVent2"].Center = connect ? ventMap["RecordsVent"] : null;
                    ventMap["RecordsVent"].Center = connect ? ventMap["GaproomVent2"] : null;
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
            dragButton = null;
        }
    }
}
