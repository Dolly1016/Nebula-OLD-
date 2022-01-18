using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Booster : Role
    {
        static public Color Color = new Color(121f / 255f, 175f / 255f, 206f / 255f);

        private CustomButton boostButton;

        private Module.CustomOption boostCooldownOption;
        private Module.CustomOption boostDurationOption;
        private Module.CustomOption boostSpeedOption;

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.BoostButton.png", 115f);
            return buttonSprite;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (boostButton != null)
            {
                boostButton.Destroy();
            }
            boostButton = new CustomButton(
                () =>
                {
                    RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(boostDurationOption.getFloat(), boostSpeedOption.getFloat(), false));

                    boostButton.Timer = boostButton.MaxTimer;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () =>
                {
                    boostButton.Timer = boostButton.MaxTimer;
                    boostButton.isEffectActive = false;
                    boostButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                },
                getButtonSprite(),
                new Vector3(0f, 1f, 0),
                __instance,
                KeyCode.F,
                true,
               boostDurationOption.getFloat(),
               () => { boostButton.Timer = boostButton.MaxTimer; }
            );
            boostButton.MaxTimer = boostCooldownOption.getFloat();
        }
    
        public override void ButtonActivate()
        {
            boostButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            boostButton.setActive(false);
        }

        public override void ButtonCleanUp()
        {
            if (boostButton != null)
            {
                boostButton.Destroy();
                boostButton = null;
            }
        }

        public override void LoadOptionData()
        {
            boostCooldownOption = CreateOption(Color.white, "boostCoolDown", 20f, 10f, 60f, 5f);
            boostCooldownOption.suffix = "second";

            boostDurationOption = CreateOption(Color.white, "boostDuration", 10f, 5f, 30f, 5f);
            boostDurationOption.suffix = "second";

            boostSpeedOption = CreateOption(Color.white, "boostSpeed", 2f, 1.25f, 3f, 0.25f);
            boostSpeedOption.suffix = "cross";
        }

        public Booster()
            : base("Booster", "booster", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, false, false, false, false)
        {
            boostButton = null;
        }
    }
}
