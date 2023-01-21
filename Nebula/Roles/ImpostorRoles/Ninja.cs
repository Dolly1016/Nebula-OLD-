using Nebula.Expansion;
using Rewired;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.ImpostorRoles
{
    public class Ninja : Role
    {
        private CustomButton hideButton;

        private Module.CustomOption hideCoolDownOption;

        private SpriteLoader ninjaButtonSprite = new SpriteLoader("Nebula.Resources.NinjaVentButton.png", 115f);
        public override HelpSprite[] helpSprite => new HelpSprite[]
        {
            new HelpSprite(ninjaButtonSprite,"role.ninja.help.vent",0.3f)
        };

        public override void LoadOptionData()
        {
            hideCoolDownOption = CreateOption(Color.white, "evasionCoolDown", 20f, 2.5f, 60f, 2.5f);
            hideCoolDownOption.suffix = "second";
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (hideButton != null)
            {
                hideButton.Destroy();
            }
            hideButton = new CustomButton(
                () =>
                {
                    float min = 0f;
                    Vent? vent = null;
                    foreach(var v in ShipStatus.Instance.AllVents)
                    {
                        float d = PlayerControl.LocalPlayer.transform.position.Distance(v.gameObject.transform.position);
                        if (vent==null || d < min)
                        {
                            min = d;
                            vent = v;
                        }
                    }
                    if (vent == null) return;

                    RPCEventInvoker.EnterRemoteVent(PlayerControl.LocalPlayer.transform.position,vent);

                    hideButton.Timer = hideButton.MaxTimer;
                },
                () => {
                    if (PlayerControl.LocalPlayer.Data.IsDead) return false;
                    if(HudManager.Instance.ImpostorVentButton.currentTarget==null && !(PlayerControl.LocalPlayer.walkingToVent || PlayerControl.LocalPlayer.inVent) && !PlayerControl.LocalPlayer.IsPlaying(PlayerControl.LocalPlayer.MyPhysics.Animations.group.ExitVentAnim))
                    {
                        HudManager.Instance.ImpostorVentButton.gameObject.SetActive(false);
                        hideButton.actionButton.transform.position = HudManager.Instance.ImpostorVentButton.gameObject.transform.position;
                        return true;
                    }
                    return false;
                },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () =>
                {
                    hideButton.Timer = hideButton.MaxTimer;
                    hideButton.isEffectActive = false;
                    hideButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                },
                ninjaButtonSprite.GetSprite(),
                Expansion.GridArrangeExpansion.GridArrangeParameter.IgnoredContent,
            __instance,
                Rewired.ReInput.mapping.GetKeyboardMapInstance(0, 0).GetButtonMapsWithAction(50)[0].keyCode,
                "button.label.ninja"
            ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
            hideButton.MaxTimer = hideCoolDownOption.getFloat();

        }

        public override void CleanUp()
        {
            if (hideButton != null)
            {
                hideButton.Destroy();
                hideButton = null;
            }
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Opportunist);
            RelatedRoles.Add(Roles.Empiric);
            RelatedRoles.Add(Roles.Alien);
        }

        public Ninja()
                : base("Ninja", "ninja", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            hideButton = null;
        }
    }
}
