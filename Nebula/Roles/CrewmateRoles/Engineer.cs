using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;
using Nebula.Patches;


namespace Nebula.Roles.CrewmateRoles
{
    public class Engineer : Role
    {
        static public Color Color = new Color(0f / 255f, 21f / 255f, 255f / 255f);

        private CustomButton repairButton;

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.RepairButton.png", 115f);
            return buttonSprite;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (repairButton != null)
            {
                repairButton.Destroy();
            }
            repairButton = new CustomButton(
                () => {
                    Helpers.RepairSabotage();
                    repairButton.setActive(false);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => {
                    return Helpers.SabotageIsActive() && PlayerControl.LocalPlayer.CanMove;

                },
                () => { repairButton.Timer = 0; },
                getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            repairButton.MaxTimer = repairButton.Timer = 0;
        }

        public override void ButtonActivate()
        {
            repairButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            repairButton.setActive(false);
        }

        public override void ButtonCleanUp()
        {
            if (repairButton != null)
            {
                repairButton.Destroy();
                repairButton = null;
            }
        }

        public override void LoadOptionData()
        {
        }

        public Engineer()
            : base("Engineer", "Engineer", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, true, true, false, false)
        {
            ventColor = Palette.CrewmateBlue;
        }
    }
}
