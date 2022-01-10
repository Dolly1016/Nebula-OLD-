using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.Crewmate
{
    public class SecurityGuard : Role
    {
        static public Color Color = new Color(171f / 255f, 153f / 255f, 67f / 255f);

        private CustomButton sealButton;
        private TMPro.TMP_Text sealButtonString;

        private Vent ventTarget = null;
        public int remainingScrews = 0;
        public int totalScrews = 0;

        private Module.CustomOption maxScrewsOption;

        private Sprite buttonSprite=null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.CloseVentButton.png", 115f);
            return buttonSprite;
        }

        private Sprite ventSealedSprite=null;
        public Sprite getVentSealedSprite()
        {
            if (ventSealedSprite) return ventSealedSprite;
            ventSealedSprite = Helpers.loadSpriteFromResources("Nebula.Resources.VentSealed.png", 160f);
            return ventSealedSprite;
        }

        private Sprite caveSealedSprite=null;
        public Sprite getCaveSealedSprite()
        {
            if (caveSealedSprite) return caveSealedSprite;
            caveSealedSprite = Helpers.loadSpriteFromResources("Nebula.Resources.CaveSealed.png", 160f);
            return caveSealedSprite;
        }

        public override void MyPlayerControlUpdate()
        {
            Vent target = null;
            Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
            float closestDistance = float.MaxValue;
            for (int i = 0; i < ShipStatus.Instance.AllVents.Length; i++)
            {
                Vent vent = ShipStatus.Instance.AllVents[i];
                if (vent.gameObject.name.StartsWith("%SEALED%")) continue;
                float distance = Vector2.Distance(vent.transform.position, truePosition);
                if (distance <= vent.UsableDistance && distance < closestDistance)
                {
                    closestDistance = distance;
                    target = vent;
                }
            }
            ventTarget = target;
        }


        public override void Initialize(PlayerControl __instance) {
            totalScrews = remainingScrews = (int)maxScrewsOption.getFloat();
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (sealButton != null)
            {
                sealButton.Destroy();
            }
            sealButton = new CustomButton(
                () => {
                    if (ventTarget != null)
                    { // Seal vent
                        MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SealVent, Hazel.SendOption.Reliable);
                        writer.WritePacked(ventTarget.Id);
                        writer.EndMessage();
                        RPCEvents.SealVent(ventTarget.Id);
                        ventTarget = null;
                        remainingScrews--;

                        sealButton.Timer = sealButton.MaxTimer;
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead && remainingScrews > 0; },
                () => {
                    sealButtonString.text = $"{remainingScrews}/{totalScrews}";

                    return (ventTarget != null) &&remainingScrews > 0 && PlayerControl.LocalPlayer.CanMove;
                    
                },
                () => { sealButton.Timer = sealButton.MaxTimer; },
                getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            sealButton.MaxTimer = 20;

            sealButtonString = GameObject.Instantiate(sealButton.actionButton.cooldownTimerText, sealButton.actionButton.cooldownTimerText.transform.parent);
            sealButtonString.text = "";
            sealButtonString.enableWordWrapping = false;
            sealButtonString.transform.localScale = Vector3.one * 0.5f;
            sealButtonString.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);
        }

        public override void OnMeetingEnd()
        {
            sealButton.Timer = sealButton.MaxTimer;
        }


        public override void CleanUp()
        {
            if (sealButton != null)
            {
                sealButton.Destroy();
                sealButton = null;

                sealButtonString = null;
            }
        }

        public override void LoadOptionData()
        {
            maxScrewsOption = CreateOption(Color.white, "maxScrews", 5f, 0f, 7f, 1f);
        }

        public SecurityGuard()
            : base("SecurityGuard", "securityGuard", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, false, false, false, false)
        {
            sealButton = null;
        }
    }
}
