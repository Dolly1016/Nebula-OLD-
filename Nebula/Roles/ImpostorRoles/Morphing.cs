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
    public class Morphing : Role
    {
        public class MorphEvent : Events.LocalEvent
        {
            public byte PlayerId { get; private set; }
            public byte TargetId { get; private set; }

            public override void OnTerminal()
            {
                if (Nebula.Events.GlobalEvent.GetAllowUpdateOutfit())
                    Helpers.playerById(PlayerId).ResetOutfit();
            }

            public override void OnActivate()
            {
                if(Nebula.Events.GlobalEvent.GetAllowUpdateOutfit())
                    Helpers.playerById(PlayerId).SetOutfit(Helpers.playerById(TargetId));
            }

            public MorphEvent(byte playerId,byte targetId):base(Roles.Morphing.morphDurationOption.getFloat())
            {
                PlayerId = playerId;
                TargetId = targetId;
                SpreadOverMeeting = false;
            }

        }

        private CustomButton morphButton;

        private Module.CustomOption morphCoolDownOption;
        private Module.CustomOption morphDurationOption;

        private byte morphId;

        private Sprite sampleButtonSprite = null;
        public Sprite getSampleButtonSprite()
        {
            if (sampleButtonSprite) return sampleButtonSprite;
            sampleButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SampleButton.png", 115f);
            return sampleButtonSprite;
        }

        private Sprite morphButtonSprite = null;
        public Sprite getMorphButtonSprite()
        {
            if (morphButtonSprite) return morphButtonSprite;
            morphButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.MorphButton.png", 115f);
            return morphButtonSprite;
        }


        public override void LoadOptionData()
        {
            morphCoolDownOption = CreateOption(Color.white, "morphCoolDown", 25f, 10f, 60f, 5f);
            morphCoolDownOption.suffix = "second";

            morphDurationOption = CreateOption(Color.white, "morphDuration", 15f, 5f, 30f, 2.5f);
            morphDurationOption.suffix = "second";
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            morphId = Byte.MaxValue;

            if (morphButton != null)
            {
                morphButton.Destroy();
            }
            morphButton = new CustomButton(
                () =>
                {
                    if (morphId == Byte.MaxValue)
                    {
                        morphButton.Timer = 3f;
                        morphButton.isEffectActive = false;
                        morphId = Game.GameData.data.myData.currentTarget.PlayerId;
                        Game.GameData.data.myData.currentTarget = null;
                        morphButton.Sprite = getMorphButtonSprite();
                        morphButton.SetLabel("button.label.morph");
                    }
                    else
                    {
                        RPCEventInvoker.Morph(morphId);
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && (morphId!=Byte.MaxValue||Game.GameData.data.myData.currentTarget!=null); },
                () => {
                    morphButton.Timer = morphButton.MaxTimer;
                    morphButton.isEffectActive = false;
                    morphButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                    RPCEventInvoker.MorphCancel();
                },
                getSampleButtonSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                true,
                morphDurationOption.getFloat(),
                () => { morphButton.Timer = morphButton.MaxTimer; },
                false,
                "button.label.sample"
            );
            morphButton.MaxTimer = morphCoolDownOption.getFloat();
            morphButton.EffectDuration = morphDurationOption.getFloat();
            morphButton.SetSuspendAction(()=> {
                morphButton.Timer = morphButton.MaxTimer;
                morphButton.isEffectActive = false;
                morphButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                RPCEventInvoker.MorphCancel();
            });
        }

        public override void ButtonActivate()
        {
            morphButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            morphButton.setActive(false);
        }

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }

        public override void OnMeetingEnd()
        {
            morphId = Byte.MaxValue;
            morphButton.Sprite = getSampleButtonSprite();
            morphButton.SetLabel(null);
        }

        public override void CleanUp()
        {
            if (morphButton != null)
            {
                morphButton.Destroy();
                morphButton = null;
            }
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Arsonist);
        }

        public Morphing()
                : base("Morphing", "morphing", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            morphButton = null;
        }
    }
}
