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
    public class Pursuer : Role
    {
        private CustomButton searchButton;

        private Module.CustomOption searchCoolDownOption;
        private Module.CustomOption searchDurationOption;

        public override void LoadOptionData()
        {
            searchCoolDownOption = CreateOption(Color.white, "searchCoolDown", 20f, 5f, 60f, 5f);
            searchCoolDownOption.suffix = "second";

            searchDurationOption = CreateOption(Color.white, "searchDuration", 5f, 2.5f, 20f, 1.25f);
            searchDurationOption.suffix = "second";
        }


        static public Color Color = new Color(110f / 255f, 46f / 255f, 230f / 255f);

        private Dictionary<byte, Arrow> Arrows=new Dictionary<byte, Arrow>();

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SearchButton.png", 115f);
            return buttonSprite;
        }

        public override void MyPlayerControlUpdate()
        {
            RoleSystem.TrackSystem.MyControlUpdate(searchButton.isEffectActive&&!PlayerControl.LocalPlayer.Data.IsDead,Arrows);
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (searchButton != null)
            {
                searchButton.Destroy();
            }
            searchButton = RoleSystem.TrackSystem.ButtonInitialize(__instance,Arrows,
                getButtonSprite(),searchDurationOption.getFloat(),searchCoolDownOption.getFloat());
        }

        public override void ButtonActivate()
        {
            searchButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            searchButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (searchButton != null)
            {
                searchButton.Destroy();
                searchButton = null;
            }

            foreach(var arrow in Arrows.Values)
            {
                UnityEngine.Object.Destroy(arrow.arrow);
            }
            Arrows.Clear();
        }

        public Pursuer()
            : base("Pursuer", "pursuer", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, false, false, false, false)
        {
            searchButton = null;
        }
    }
}
