using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;
using Nebula.Objects;
using Nebula.Utilities;

namespace Nebula.Roles.CrewmateRoles
{
    public class Agent : Template.ExemptTasks
    {
        static public Color RoleColor = new Color(166f / 255f, 183f / 255f, 144f / 255f);

        private CustomButton agentButton;

        private TMPro.TMP_Text ventButtonString;
        public int remainingVentsDataId { get; private set; }

        private Module.CustomOption maxVentsOption;
        private Module.CustomOption actOverOption;
        private Module.CustomOption madmateKillCoolDownOption;
        public override void LoadOptionData()
        {
            base.LoadOptionData();

            actOverOption = CreateOption(Color.white, "actOverTasks", 1f, 1f, 10f, 1f);

            maxVentsOption = CreateOption(Color.white, "maxVents", 3f, 0f, 20f, 1f);

            madmateKillCoolDownOption = CreateOption(Color.white, "killCoolDownBonus", 5f, 2.5f, 20f, 2.5f).
                AddCustomPrerequisite(() => { return CanBeMadmate && Roles.SecondaryMadmate.IsSpawnable(); });
            madmateKillCoolDownOption.suffix = "second";
        }

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.AgentButton.png", 115f);
            return buttonSprite;
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            __instance.GetModData().SetRoleData(remainingVentsDataId, (int)maxVentsOption.getFloat());
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

                    return task.AllTasks == task.Completed && PlayerControl.LocalPlayer.CanMove && task.Quota > 0;

                },
                () => { agentButton.Timer = 0; },
                getButtonSprite(),
                new Vector3(-1.8f, 0, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.agent"
            );
            agentButton.MaxTimer = agentButton.Timer = 0;

            ventButtonString = FastDestroyableSingleton<HudManager>.Instance.ImpostorVentButton.CreateButtonUpperText();
            ventButtonString.text = (int)maxVentsOption.getFloat() + "/" + (int)maxVentsOption.getFloat();
        }

        public override void MyUpdate()
        {
            var data = PlayerControl.LocalPlayer.GetModData();
            if (data == null) return;

            VentPermission = (!PlayerControl.LocalPlayer.inVent && data.GetRoleData(remainingVentsDataId) <= 0) ? VentPermission.CanNotUse : VentPermission.CanUseUnlimittedVent;
        }

        public override void OnEnterVent(Vent vent) {
            PlayerControl.LocalPlayer.GetModData().AddRoleData(remainingVentsDataId, -1);
            int remain= PlayerControl.LocalPlayer.GetModData().GetRoleData(remainingVentsDataId);
            ventButtonString.text = (int)remain + "/" + (int)maxVentsOption.getFloat();
        }


        public override void CleanUp()
        {
            if (agentButton != null)
            {
                agentButton.Destroy();
                agentButton = null;
            }
            if (ventButtonString != null)
            {
                UnityEngine.Object.Destroy(ventButtonString.gameObject);
                ventButtonString = null;
            }
        }

        public override void OnTaskComplete() {
            if (!PlayerControl.LocalPlayer.IsMadmate()) return;

            //Madmate設定 インポスターのキルクールを進める
            RPCEventInvoker.EditCoolDown(CoolDownType.ImpostorsKill, madmateKillCoolDownOption.getFloat());
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.EvilGuesser);
            RelatedRoles.Add(Roles.NiceGuesser);
            RelatedRoles.Add(Roles.EvilAce);
        }

        public Agent()
            : base("Agent", "agent", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanUseUnlimittedVent, true, false, false)
        {
            agentButton = null;

            remainingVentsDataId = Game.GameData.RegisterRoleDataId("agent.remainVents");

            VentColor = Palette.CrewmateBlue;

            FakeTaskIsExecutable = true;
        }
    }
}
