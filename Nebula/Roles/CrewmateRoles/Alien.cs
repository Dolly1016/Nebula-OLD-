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
    public class Alien : Role
    {
        private CustomButton emiButton;

        private Module.CustomOption emiCoolDownOption;
        private Module.CustomOption emiDurationOption;
        private Module.CustomOption emiRangeOption;
        private Module.CustomOption emiInhibitsCrewmatesOption;

        public override void LoadOptionData()
        {
            emiCoolDownOption = CreateOption(Color.white, "emiCoolDown", 25f, 10f, 60f, 5f);
            emiCoolDownOption.suffix = "second";

            emiDurationOption = CreateOption(Color.white, "emiDuration", 15f, 5f, 30f, 2.5f);
            emiDurationOption.suffix = "second";

            emiRangeOption = CreateOption(Color.white, "emiRange", 1f, 0.5f, 5f, 0.5f);
            emiRangeOption.suffix = "cross";

            emiInhibitsCrewmatesOption = CreateOption(Color.white, "emiInhibitsCrewmates", true);
        }


        static public Color Color = new Color(187f / 255f, 109f / 255f, 178f / 255f);

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.EMIButton.png", 115f);
            return buttonSprite;
        }

        public override void GlobalUpdate(byte playerId)
        {
            if (PlayerControl.LocalPlayer.PlayerId == playerId) return;
            if (!Events.GlobalEvent.IsActive(Events.GlobalEvent.Type.EMI)) return;
            if (Game.GameData.data.myData.getGlobalData().role == Roles.Alien) return;

            //クールダウン上昇
            if (emiRangeOption.getFloat() > PlayerControl.LocalPlayer.transform.position.Distance(Helpers.playerById(playerId).transform.position))
            {
                PlayerControl.LocalPlayer.killTimer += Time.deltaTime;
                foreach (CustomButton button in CustomButton.buttons)
                {
                    if (button.isEffectActive) continue;

                    if (!emiInhibitsCrewmatesOption.getBool() && PlayerControl.LocalPlayer.GetModData().role.category == RoleCategory.Crewmate)
                    {
                        if (button.Timer > 1f)
                            button.Timer -= Time.deltaTime*0.5f;
                    }
                    else
                    {
                        if (button.MaxTimer > 0f)
                            if (button.Timer > 1f)
                                button.Timer += Time.deltaTime;
                    }
                }
            }
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (emiButton != null)
            {
                emiButton.Destroy();
            }
            emiButton = new CustomButton(
                () =>
                {
                    MessageWriter emiWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GlobalEvent, Hazel.SendOption.Reliable, -1);
                    emiWriter.Write(Events.GlobalEvent.Type.EMI.Id);
                    emiWriter.Write(emiDurationOption.getFloat());
                    AmongUsClient.Instance.FinishRpcImmediately(emiWriter);
                    RPCEvents.GlobalEvent(Events.GlobalEvent.Type.EMI.Id, emiDurationOption.getFloat());
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => {
                    emiButton.Timer = emiButton.MaxTimer;
                    emiButton.isEffectActive = false;
                    emiButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                },
                getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F,
                true,
                emiDurationOption.getFloat(),
                () => { emiButton.Timer = emiButton.MaxTimer; }
            );
            emiButton.MaxTimer = emiCoolDownOption.getFloat();
            emiButton.EffectDuration = emiDurationOption.getFloat();
        }

        public override void ButtonActivate()
        {
            emiButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            emiButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (emiButton != null)
            {
                emiButton.Destroy();
                emiButton = null;
            }
        }

        public Alien()
            : base("Alien", "alien", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
            IsGuessableRole = false;
            emiButton = null;
        }
    }
}
