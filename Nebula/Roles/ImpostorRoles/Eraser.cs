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
    public class Eraser : Role
    {
        private CustomButton eraserButton;

        private Module.CustomOption eraseCoolDownOption;
        private Module.CustomOption eraseCoolDownAdditionOption;

        private int eraseCountId;

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.EraseButton.png", 115f);
            return buttonSprite;
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Arsonist);
        }

        public override void LoadOptionData()
        {
            eraseCoolDownOption = CreateOption(Color.white, "eraseCoolDown", 25f, 10f, 60f, 5f);
            eraseCoolDownOption.suffix = "second";

            eraseCoolDownAdditionOption = CreateOption(Color.white, "eraseCoolDownAddition", 15f, 5f, 30f, 5f);
            eraseCoolDownAdditionOption.suffix = "second";
        }

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }


        public override void ButtonInitialize(HudManager __instance)
        {
            if (eraserButton != null)
            {
                eraserButton.Destroy();
            }
            eraserButton = new CustomButton(
                () =>
                {
                    RPCEventInvoker.ChangeRole(Game.GameData.data.myData.currentTarget,Roles.Crewmate);
                    Game.GameData.data.myData.currentTarget = null;

                    RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, eraseCountId, 1);
                    eraserButton.Timer = eraserButton.MaxTimer + eraseCoolDownAdditionOption.getFloat()*PlayerControl.LocalPlayer.GetModData().GetRoleData(eraseCountId);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.currentTarget!=null; },
                () => {
                    eraserButton.Timer = eraserButton.MaxTimer + eraseCoolDownAdditionOption.getFloat() * PlayerControl.LocalPlayer.GetModData().GetRoleData(eraseCountId); 
                },
                getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            eraserButton.MaxTimer = eraseCoolDownOption.getFloat();
        }

        public override void ButtonActivate()
        {
            eraserButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            eraserButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (eraserButton != null)
            {
                eraserButton.Destroy();
                eraserButton = null;
            }
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            __instance.GetModData().SetRoleData(eraseCountId, 0);
        }

        public Eraser()
                : base("Eraser", "eraser", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            eraserButton = null;

            eraseCountId = Game.GameData.RegisterRoleDataId("eraser.eraseCount");
        }
    }
}
