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
    public class Marionette : Role
    {
        private CustomButton marionetteButton;
        private CustomButton cameraButton;

        private Module.CustomOption swapCoolDownOption;

        private CustomObject? decoy;
        private SpriteRenderer? decoyIndicator;

        private Sprite decoyButtonSprite = null;
        public Sprite getDecoyButtonSprite()
        {
            if (decoyButtonSprite) return decoyButtonSprite;
            decoyButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DecoyButton.png", 115f);
            return decoyButtonSprite;
        }

        private Sprite swapButtonSprite = null;
        public Sprite getSwapButtonSprite()
        {
            if (swapButtonSprite) return swapButtonSprite;
            swapButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DecoySwapButton.png", 115f);
            return swapButtonSprite;
        }

        private Sprite monitorButtonSprite = null;
        public Sprite getMonitorButtonSprite()
        {
            if (monitorButtonSprite) return monitorButtonSprite;
            monitorButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DecoyMonitorButton.png", 115f);
            return monitorButtonSprite;
        }

        public override void LoadOptionData()
        {
            swapCoolDownOption = CreateOption(Color.white, "swapCoolDown", 5f, 0f, 40f, 0.5f);
            swapCoolDownOption.suffix = "second";
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            decoy = null;
            decoyIndicator = null;

            if (marionetteButton != null)
            {
                marionetteButton.Destroy();
            }
            marionetteButton = new CustomButton(
                () =>
                {
                    if (decoy == null)
                    {
                        marionetteButton.isEffectActive = false;
                        decoy = RPCEventInvoker.ObjectInstantiate(Objects.CustomObject.Type.Decoy, PlayerControl.LocalPlayer.transform.position);
                        Game.GameData.data.myData.currentTarget = null;
                        marionetteButton.Sprite = getSwapButtonSprite();
                        marionetteButton.SetLabel("button.label.swap");

                        marionetteButton.Timer = marionetteButton.MaxTimer = swapCoolDownOption.getFloat();
                    }
                    else
                    {
                        marionetteButton.Timer = marionetteButton.MaxTimer;
                        RPCEventInvoker.DecoySwap(decoy);
                        if (HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer) HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove || HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer; },
                () => {
                    marionetteButton.Timer = 10f;
                },
                getDecoyButtonSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.decoy"
            );
            marionetteButton.MaxTimer = 10f;

            if (cameraButton != null)
            {
                cameraButton.Destroy();
            }
            cameraButton = new CustomButton(
                () =>
                {
                    if (HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer) HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
                    else HudManager.Instance.PlayerCam.SetTargetWithLight(decoy.Behaviour);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead && decoy != null; },
                () => { return PlayerControl.LocalPlayer.CanMove || HudManager.Instance.PlayerCam.Target != PlayerControl.LocalPlayer; },
                () => {
                    marionetteButton.Timer = marionetteButton.MaxTimer;
                },
                getMonitorButtonSprite(),
                new Vector3(-2.7f, 0f, 0),
                __instance,
                KeyCode.G,
                false,
                "button.label.monitor"
            );
        }

        public override void CleanUp()
        {
            if (marionetteButton != null)
            {
                marionetteButton.Destroy();
                marionetteButton = null;
            }
            if (cameraButton != null)
            {
                cameraButton.Destroy();
                cameraButton = null;
            }
            decoy = null;
            decoyIndicator = null;
        }

        public override void FinalizeInGame(PlayerControl __instance)
        {
            HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
            if(decoyIndicator!=null && decoyIndicator.gameObject) GameObject.Destroy(decoyIndicator.gameObject);
            decoyIndicator = null;
        }

        public override void OnMeetingStart()
        {
            HudManager.Instance.PlayerCam.SetTargetWithLight(PlayerControl.LocalPlayer);
        }

        //デコイをマップに表示
        public override void MyMapUpdate(MapBehaviour mapBehaviour)
        {
            if (!mapBehaviour.GetTrackOverlay()) return;
            if (!mapBehaviour.GetTrackOverlay().activeSelf) return;

            if (decoy == null)
            {
                if (decoyIndicator != null) GameObject.Destroy(decoyIndicator.gameObject);
                return;
            }

            if (decoyIndicator == null)
            {
                decoyIndicator = GameObject.Instantiate(mapBehaviour.HerePoint,mapBehaviour.GetTrackOverlay().transform);
                PlayerMaterial.SetColors(Palette.DisabledGrey, decoyIndicator);
            }

            decoyIndicator.transform.localPosition = MapBehaviourExpansion.ConvertMapLocalPosition(decoy.GameObject.transform.position, 16);
            
        }


        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Reaper);
        }

        public Marionette()
                : base("Marionette", "marionette", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            marionetteButton = null;
        }
    }
}
