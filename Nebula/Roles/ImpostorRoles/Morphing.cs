﻿using System;
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
            private Game.PlayerData.PlayerOutfitData outfit;

            public override void OnTerminal()
            {
                Helpers.GetModData(PlayerId).RemoveOutfit(outfit);
            }

            public override void OnActivate()
            {
                Helpers.GetModData(PlayerId).AddOutfit(outfit);
            }

            public MorphEvent(byte playerId, Game.PlayerData.PlayerOutfitData outfit) :base(Roles.Morphing.morphDurationOption.getFloat())
            {
                PlayerId = playerId;
                this.outfit = outfit;
                SpreadOverMeeting = false;
            }

        }

        private CustomButton morphButton;

        private Module.CustomOption morphCoolDownOption;
        private Module.CustomOption morphDurationOption;

        private PlayerControl? morphTarget;
        private Game.PlayerData.PlayerOutfitData morphOutfit;
        private Objects.Arrow? arrow;

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

            morphDurationOption = CreateOption(Color.white, "morphDuration", 15f, 5f, 40f, 2.5f);
            morphDurationOption.suffix = "second";
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            morphTarget = null;

            if (morphButton != null)
            {
                morphButton.Destroy();
            }
            morphButton = new CustomButton(
                () =>
                {
                    if (morphTarget == null)
                    {
                        morphButton.Timer = 3f;
                        morphButton.isEffectActive = false;
                        morphTarget = Game.GameData.data.myData.currentTarget;
                        Game.GameData.data.myData.currentTarget = null;
                        morphButton.Sprite = getMorphButtonSprite();
                        morphButton.SetLabel("button.label.morph");
                        morphOutfit = morphTarget.GetModData().GetOutfitData(50).Clone(80);
                    }
                    else
                    {
                        RPCEventInvoker.Morph(morphOutfit);
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && (morphTarget!=null||Game.GameData.data.myData.currentTarget!=null); },
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

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

            RoleSystem.TrackSystem.PlayerTrack_MyControlUpdate(ref arrow,morphTarget);
        }

        public override void OnMeetingEnd()
        {
            morphTarget = null;
            morphButton.Sprite = getSampleButtonSprite();
            morphButton.SetLabel("button.label.sample");
        }

        public override void CleanUp()
        {
            if (morphButton != null)
            {
                morphButton.Destroy();
                morphButton = null;
            }
            if (arrow != null)
            {
                GameObject.Destroy(arrow.arrow);
                arrow = null;
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
