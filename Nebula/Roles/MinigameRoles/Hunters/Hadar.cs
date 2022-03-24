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

namespace Nebula.Roles.MinigameRoles.Hunters
{
    public class Hadar : Role
    {
        static private CustomButton ventButton, auraButton;
        private float lightRadius=1f;

        private Sprite ventAppearButtonSprite = null, ventHideButtonSprite = null, auraButtonSprite = null;
        public Sprite GetVentAppearButtonSprite()
        {
            if (ventAppearButtonSprite) return ventAppearButtonSprite;
            ventAppearButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.HadarAppearButton.png", 115f);
            return ventAppearButtonSprite;
        }

        public Sprite GetVentHideButtonSprite()
        {
            if (ventHideButtonSprite) return ventHideButtonSprite;
            ventHideButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.HadarHideButton.png", 115f);
            return ventHideButtonSprite;
        }

        public Sprite GetAuraButtonSprite()
        {
            if (auraButtonSprite) return auraButtonSprite;
            auraButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ArrestButton.png", 115f);
            return auraButtonSprite;
        }

        public override void Initialize(PlayerControl __instance)
        {
            lightRadius = 1f;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (ventButton != null)
            {
                ventButton.Destroy();
            }
            ventButton = new CustomButton(
                () =>
                {
                    var property = PlayerControl.LocalPlayer.GetModData().Property;
                    ventButton.SetLabel(property.UnderTheFloor?
                        "button.label.hadar.hide" : "button.label.hadar.appear");
                    ventButton.Sprite = property.UnderTheFloor ?
                        GetVentHideButtonSprite() : GetVentAppearButtonSprite();
                    property.UnderTheFloor = !property.UnderTheFloor;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => { ventButton.Timer = ventButton.MaxTimer; },
                GetVentHideButtonSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.hadar.hide"
            );
            ventButton.MaxTimer = ventButton.Timer = 0f;
        }

        public override void ButtonActivate()
        {
            ventButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            ventButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (ventButton != null)
            {
                ventButton.Destroy();
                ventButton = null;
            }
        }

        public override void GetLightRadius(ref float radius) {
            if (PlayerControl.LocalPlayer.GetModData().Property.UnderTheFloor)
                lightRadius = 0f;
            else
                lightRadius += (1f - lightRadius) * 0.3f;

            radius *= lightRadius;
        }


        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            displayColor = Palette.ImpostorRed;
        }

        public Hadar()
                : base("Hadar", "hadar", Palette.ImpostorRed, RoleCategory.Crewmate, Side.GamePlayer, Side.GamePlayer,
                     Player.minigameSideSet, Player.minigameSideSet, new HashSet<EndCondition>() { EndCondition.MinigameHunterWin },
                     true, VentPermission.CanNotUse, false, false, true)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.Minigame;
            CanCallEmergencyMeeting = false;

            ventButton = null;
        }
    }
}
