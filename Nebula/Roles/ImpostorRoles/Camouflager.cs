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
    public class Camouflager : Role
    {
        private CustomButton camouflageButton;

        private Module.CustomOption camouflageCoolDownOption;
        private Module.CustomOption camouflageDurationOption;

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.CamoButton.png", 115f);
            return buttonSprite;
        }

        public override void LoadOptionData()
        {
            camouflageCoolDownOption = CreateOption(Color.white, "camouflageCoolDown", 25f, 10f, 60f, 5f);
            camouflageCoolDownOption.suffix = "second";

            camouflageDurationOption = CreateOption(Color.white, "camouflageDuration", 15f, 5f, 30f, 2.5f);
            camouflageDurationOption.suffix = "second";
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (camouflageButton != null)
            {
                camouflageButton.Destroy();
            }
            camouflageButton = new CustomButton(
                () =>
                {
                    MessageWriter camouflageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GlobalEvent, Hazel.SendOption.Reliable, -1);
                    camouflageWriter.Write(Events.GlobalEvent.Type.Camouflage.Id);
                    camouflageWriter.Write(camouflageDurationOption.getFloat());
                    AmongUsClient.Instance.FinishRpcImmediately(camouflageWriter);
                    RPCEvents.GlobalEvent(Events.GlobalEvent.Type.Camouflage.Id, camouflageDurationOption.getFloat());
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => { 
                    camouflageButton.Timer = camouflageButton.MaxTimer;
                    camouflageButton.isEffectActive = false;
                    camouflageButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                },
                getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F,
                true,
                camouflageDurationOption.getFloat(),
                () => { camouflageButton.Timer = camouflageButton.MaxTimer; }
            );
            camouflageButton.MaxTimer = camouflageCoolDownOption.getFloat();
            camouflageButton.EffectDuration = camouflageDurationOption.getFloat();
        }

        public override void ButtonActivate()
        {
            camouflageButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            camouflageButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (camouflageButton != null)
            {
                camouflageButton.Destroy();
                camouflageButton = null;
            }
        }

        //インポスターはModで操作するFakeTaskは所持していない
        public Camouflager()
                : base("Camouflager", "camouflager", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     false, true, true, false, true)
        {
            camouflageButton = null;
        }
    }
}
