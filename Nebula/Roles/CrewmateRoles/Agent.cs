using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Agent : Template.ExemptTasks
    {
        static public Color Color = new Color(166f / 255f, 183f / 255f, 144f / 255f);

        private CustomButton agentButton;

        private Module.CustomOption actOverOption;
        public override void LoadOptionData()
        {
            base.LoadOptionData();

            actOverOption = CreateOption(Color.white, "actOverTasks", 1f, 1f, 10f, 1f);
        }

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.AgentButton.png", 115f);
            return buttonSprite;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (agentButton != null)
            {
                agentButton.Destroy();
            }
            agentButton = new CustomButton(
                () => {
                    Game.TaskData task=PlayerControl.LocalPlayer.GetModData().Tasks;

                    RPCEventInvoker.RefreshTasks(PlayerControl.LocalPlayer.PlayerId, (int)actOverOption.getFloat(), 0, 0.2f);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => {
                    Game.TaskData task = PlayerControl.LocalPlayer.GetModData().Tasks;

                    return task.AllTasks==task.Completed && PlayerControl.LocalPlayer.CanMove;

                },
                () => { agentButton.Timer = 0; },
                getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            agentButton.MaxTimer = agentButton.Timer = 0;
        }

        public override void ButtonActivate()
        {
            agentButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            agentButton.setActive(false);
        }


        public override void IntroInitialize(PlayerControl __instance)
        {
            base.IntroInitialize(__instance);
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.EvilGuesser);
            RelatedRoles.Add(Roles.NiceGuesser);
            RelatedRoles.Add(Roles.EvilAce);
        }

        public Agent()
            : base("Agent", "agent", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
            
        }
    }
}
