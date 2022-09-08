using System;
using System.Collections.Generic;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Roles.NeutralRoles
{
    public class Jester : Template.Draggable , Template.HasWinTrigger
    {
        static public Color RoleColor = new Color(253f / 255f, 84f / 255f, 167f / 255f);

        public bool WinTrigger { get; set; } = false;
        public byte Winner { get; set; } = Byte.MaxValue;

        private Module.CustomOption canUseVentsOption;
        private Module.CustomOption canInvokeSabotageOption;
        private Module.CustomOption ventCoolDownOption;
        private Module.CustomOption ventDurationOption;
        private Module.CustomOption canFixSabotageOption;
        private Module.CustomOption canDragBodiesOption;
        private Module.CustomOption canFireBlankShotsOption;

        private Objects.CustomButton blankButton;

        private Sprite blankButtonSprite = null;
        public Sprite getBlankButtonSprite()
        {
            if (blankButtonSprite) return blankButtonSprite;
            blankButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SnipeButton.png", 115f);
            return blankButtonSprite;
        }

        public override bool OnExiledPost(byte[] voters,byte playerId)
        {
            if (playerId == PlayerControl.LocalPlayer.PlayerId)
                RPCEventInvoker.WinTrigger(this);

            return false;    
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            WinTrigger = false;
        }

        public override void GlobalIntroInitialize(PlayerControl __instance)
        {
            canMoveInVents = canUseVentsOption.getBool();
            VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseLimittedVent : VentPermission.CanNotUse;
            canInvokeSabotage = canInvokeSabotageOption.getBool();
            canFixSabotage = canFixSabotageOption.getBool();
        }

        public override void LoadOptionData()
        {
            canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
            canInvokeSabotageOption = CreateOption(Color.white, "canInvokeSabotage", true);
            canFixSabotageOption = CreateOption(Color.white, "canFixLightsAndComms", true);

            ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f);
            ventCoolDownOption.suffix = "second";
            ventCoolDownOption.AddPrerequisite(canUseVentsOption);
            ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f);
            ventDurationOption.suffix = "second";
            ventDurationOption.AddPrerequisite(canUseVentsOption);

            canDragBodiesOption = CreateOption(Color.white, "canDragBodies", true);
            canFireBlankShotsOption = CreateOption(Color.white, "canFireBlankShots", true);
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (canDragBodiesOption.getBool())
            {
                base.ButtonInitialize(__instance);
            }
            else
            {
                base.CleanUp();
            }
            
            if (blankButton != null)
            {
                blankButton.Destroy();
            }
            if (canFireBlankShotsOption.getBool())
            {
                blankButton = new CustomButton(
                    () =>
                    {
                        RPCEventInvoker.SniperShot();
                        blankButton.Timer = blankButton.MaxTimer;
                    },
                    () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                    () => { return PlayerControl.LocalPlayer.CanMove; },
                    () => { blankButton.Timer = blankButton.MaxTimer; },
                    getBlankButtonSprite(),
                    new Vector3(0f, 1f, 0),
                    __instance,
                    KeyCode.Q,
                    false,
                    "button.label.blank"
                );
                blankButton.MaxTimer = 5.0f;
            }
            else
            {
                blankButton = null;
            }
        }

        public override void CleanUp()
        {
            base.CleanUp();
            if (blankButton != null)
            {
                blankButton.Destroy();
                blankButton = null;
            }
        }


        public override void Initialize(PlayerControl __instance)
        {
            VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
            VentDurationMaxTimer = ventDurationOption.getFloat();
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Necromancer);
            RelatedRoles.Add(Roles.Reaper);
        }

        public Jester()
            : base("Jester", "jester", RoleColor, RoleCategory.Neutral, Side.Jester, Side.Jester,
                 new HashSet<Side>() { Side.Jester }, new HashSet<Side>() { Side.Jester },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.JesterWin },
                 true, VentPermission.CanUseLimittedVent, true, false, false)
        {
            Patches.EndCondition.JesterWin.TriggerRole = this;
        }
    }
}