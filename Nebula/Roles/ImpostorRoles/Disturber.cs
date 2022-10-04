using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;

namespace Nebula.Roles.ImpostorRoles
{
    public class Disturber : Role
    {
        private CustomObject?[] PolesGuide;
        private List<CustomObject?[]> Poles;

        private CustomButton elecButton;

        private Texture2D elecAnimTexture = null;
        public Texture2D getElecAnimTexture()
        {
            if (elecAnimTexture) return elecAnimTexture;
            elecAnimTexture = Helpers.loadTextureFromResources("Nebula.Resources.ElecAnim.png");
            return elecAnimTexture;
        }

        private Texture2D elecAnimSubTexture = null;
        public Texture2D getElecAnimSubTexture()
        {
            if (elecAnimSubTexture) return elecAnimSubTexture;
            elecAnimSubTexture = Helpers.loadTextureFromResources("Nebula.Resources.ElecAnimSub.png");
            return elecAnimSubTexture;
        }

        private Sprite placeButtonSprite = null;
        public Sprite getPlaceButtonSprite()
        {
            if (placeButtonSprite) return placeButtonSprite;
            placeButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ElecPolePlaceButton.png", 115f);
            return placeButtonSprite;
        }


        private Module.CustomOption disturbCoolDownOption;
        public Module.CustomOption disturbDurationOption;
        public Module.CustomOption disturbBlackOutRateOption;
        private Module.CustomOption countOfBarriorsOption;
        private Module.CustomOption maxPoleDistanceOption;
        public Module.CustomOption ignoreBarriorsOption;

        public override void LoadOptionData()
        {
            disturbCoolDownOption = CreateOption(Color.white, "disturbCoolDown", 20f, 10f, 60f, 2.5f);
            disturbCoolDownOption.suffix = "second";

            disturbDurationOption = CreateOption(Color.white, "disturbDuration", 10f, 5f, 60f, 2.5f);
            disturbDurationOption.suffix = "second";

            disturbBlackOutRateOption = CreateOption(Color.white, "disturbBlackOutRate", 0.75f, 0.25f, 1f, 0.125f);
            disturbBlackOutRateOption.suffix = "cross";

            countOfBarriorsOption = CreateOption(Color.white, "countOfBarriors", 2f, 1f, 10f, 1f);

            maxPoleDistanceOption = CreateOption(Color.white, "maxPoleDistance", 2.5f, 1f, 10f, 0.25f);
            maxPoleDistanceOption.suffix = "cross";

            ignoreBarriorsOption = CreateOption(Color.white, "ignoreBarriors", new string[] { "role.disturber.ignoreBarriors.constant", "role.disturber.ignoreBarriors.impostors", "role.disturber.ignoreBarriors.onlyMyself" });
        }

        private void PlaceOnClick() {
            if (PolesGuide[0] == null)
            {                
                PolesGuide[0] = Objects.CustomObject.CreatePrivateObject(Objects.CustomObject.Type.ElecPoleGuide, PlayerControl.LocalPlayer.transform.position);
            }
            else if (PolesGuide[1] == null)    
            {
                if (PolesGuide[0].Data[0] == 0)
                {
                    PolesGuide[0].Data[0] = 1;
                    PolesGuide[1] = Objects.CustomObject.CreatePrivateObject(Objects.CustomObject.Type.ElecPoleGuide, PlayerControl.LocalPlayer.transform.position);
                }
            }else
            {
                if (PolesGuide[1].Data[0] == 0)
                {
                    int index = 0;
                    Poles.Add(new CustomObject?[2] { null, null });
                    for (int i = 0; i < 2; i++)
                    {
                        Poles[Poles.Count-1][i] = RPCEventInvoker.ObjectInstantiate(Objects.CustomObject.Type.ElecPole, PolesGuide[i].GameObject.transform.position);
                        PolesGuide[i].Destroy();
                        PolesGuide[i] = null;
                    }
                    return;
                }
            }
            elecButton.Timer = 0f;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (elecButton != null)
            {
                elecButton.Destroy();
            }
            elecButton = new CustomButton(
                () =>
                {
                    if (elecButton.HasEffect)
                    {
                        foreach(var pole in Poles)
                            RPCEventInvoker.DisturberInvoke(pole[0].Id, pole[1].Id);
                        RPCEventInvoker.GlobalEvent(Events.GlobalEvent.Type.BlackOut, disturbDurationOption.getFloat() + 2f, (ulong)(disturbBlackOutRateOption.getFloat() * 100f));
                    }
                    else
                    {
                        PlaceOnClick();
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return (elecButton.HasEffect ? !Helpers.SabotageIsActive() : Poles.Count < (int)countOfBarriorsOption.getFloat()) && PlayerControl.LocalPlayer.CanMove;  },
                () => {
                    elecButton.Timer = elecButton.MaxTimer;
                },
                getPlaceButtonSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                true,
                disturbDurationOption.getFloat(),
                () => { },
                false,
                "button.label.place"
            ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
            elecButton.MaxTimer = 10f;
            elecButton.HasEffect = false;
        }

        public override void Initialize(PlayerControl __instance)
        {
            for (int i = 0; i < 2; i++)
            {
                PolesGuide[i] = null;
                Poles = new List<CustomObject?[]>();
            }
        }
        public override void MyUpdate()
        {
            if (PolesGuide[0] == null)
            {
                if (elecButton.Timer < 0f && Poles.Count < countOfBarriorsOption.getFloat())
                {
                    PolesGuide[0] = Objects.CustomObject.CreatePrivateObject(Objects.CustomObject.Type.ElecPoleGuide, PlayerControl.LocalPlayer.transform.position);
                }
                return;
            }

            var pos = new Vector2(0f,-0.2f)+ (Vector2)PlayerControl.LocalPlayer.transform.position + (PlayerControl.LocalPlayer.MyPhysics.body.velocity.normalized * 0.3f);
            Vector2 vector = (pos - (Vector2)PlayerControl.LocalPlayer.transform.position);
            int index = (PolesGuide[1] == null) ? 0 : 1;
            PolesGuide[index].GameObject.transform.position = pos;
            if (PhysicsHelpers.AnyNonTriggersBetween(PlayerControl.LocalPlayer.transform.position, vector.normalized, vector.magnitude, Constants.ShipAndObjectsMask))
                PolesGuide[index].Data[0] = 2;
            else
                PolesGuide[index].Data[0] = 0;

            if (index == 1)
            {
                if (maxPoleDistanceOption.getFloat() < PolesGuide[0].GameObject.transform.position.Distance(PolesGuide[1].GameObject.transform.position))
                {
                    PolesGuide[1].Data[0] = 2;
                }
            }
        }
        
        public override void OnMeetingEnd()
        {
            for (int i=0;i<2;i++)
            {
                if (PolesGuide[i] != null)
                {
                    RPCEvents.ObjectDestroy(PolesGuide[i].Id);
                    PolesGuide[i] = null;
                }
            }
            if (Poles.Count == (int)countOfBarriorsOption.getFloat())
            {
                elecButton.HasEffect = true;
                elecButton.SetLabel("button.label.disturb");
                elecButton.MaxTimer = elecButton.Timer = disturbCoolDownOption.getFloat();
            }
        }

        public override void CleanUp()
        {
            if (elecButton != null)
            {
                elecButton.Destroy();
                elecButton = null;
            }
        }

        public override void OnRoleRelationSetting()
        {
            
        }

        public Disturber()
                : base("Disturber", "disturber", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            elecButton = null;
            Poles = new List<CustomObject?[]>();
            PolesGuide = new CustomObject?[2];
        }
    }
}
