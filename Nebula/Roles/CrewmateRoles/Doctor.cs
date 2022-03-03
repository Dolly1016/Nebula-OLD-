using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Doctor : Role
    {
        static public Color Color = new Color(128f / 255f, 255f / 255f, 221f / 255f);

        private List<Tuple<byte,TMPro.TextMeshPro>> StatusTexts = new List<Tuple<byte, TMPro.TextMeshPro>>();

        private CustomButton vitalButton;

        private Module.CustomOption mobileGadgetChargesOption;
        private Module.CustomOption maxMobileGadgetChargesOption;
        private Module.CustomOption chargesPerTasksOption;

        private float gadgetTimer=0f;

        private Minigame vitalsMinigame=null;

        private static Sprite vitalsSprite;
        public static Sprite getVitalsSprite()
        {
            if (vitalsSprite) return vitalsSprite;
            vitalsSprite = HudManager.Instance.UseButton.fastUseSettings[ImageNames.VitalsButton].Image;
            return vitalsSprite;
        }

        public override void OnVitalsOpen(VitalsMinigame __instance)
        {
            bool IsMobileGadget = vitalsMinigame == __instance;
            
            foreach (VitalsPanel panel in __instance.vitals)
            {
                TMPro.TextMeshPro text = UnityEngine.Object.Instantiate(__instance.SabText, panel.transform);
                StatusTexts.Add(new Tuple<byte, TMPro.TextMeshPro>(panel.PlayerInfo.PlayerId,text));
                UnityEngine.Object.DestroyImmediate(text.GetComponent<AlphaBlink>());
                text.gameObject.SetActive(false);
                text.transform.localScale = Vector3.one * 0.5f;
                text.transform.localPosition = new Vector3(-0.75f, -0.23f, 0f);
                text.color = new Color(0.8f,0.8f,0.8f);

                if (panel.IsDiscon)
                {
                    text.gameObject.SetActive(true);
                    text.text = Language.Language.GetString("status."+Game.GameData.data.players[panel.PlayerInfo.PlayerId].Status.Status);
                }
            }

            if (IsMobileGadget) { 
                __instance.BatteryText.gameObject.SetActive(true);
                __instance.BatteryText.transform.localPosition = new Vector3(2.2f, -2.45f, 0f);
                foreach(var sprite in __instance.BatteryText.gameObject.GetComponentsInChildren<SpriteRenderer>())
                {
                    sprite.transform.localPosition = new Vector3(-0.45f,0f);
                }
            }
        }

        public override void VitalsUpdate(VitalsMinigame __instance)
        {
            bool IsMobileGadget = vitalsMinigame == __instance;

            if (IsMobileGadget)
            {
                if (gadgetTimer > 0f)
                {
                    gadgetTimer -= Time.deltaTime;
                    __instance.BatteryText.text = Language.Language.GetString("role.doctor.gadgetLeft").Replace("%SECOND%", string.Format("{0:f1}", gadgetTimer));
                }
                else
                {
                    __instance.BatteryText.text = Language.Language.GetString("role.doctor.batteryIsEmpty");

                    foreach(var panel in __instance.vitals)
                    {
                        if(panel.gameObject.active)panel.gameObject.SetActive(false);
                    }
                }
            }


            foreach (var tuple in StatusTexts)
            {
                if (tuple.Item2.gameObject.active) continue;

                if (__instance.vitals[tuple.Item1].IsDiscon)
                {
                    tuple.Item2.gameObject.SetActive(true);
                    tuple.Item2.text = Language.Language.GetString("status." + Game.GameData.data.players[tuple.Item1].Status.Status);
                }
            }
        }

        public override void OnTaskComplete() {
            gadgetTimer += chargesPerTasksOption.getFloat();
            if (gadgetTimer > maxMobileGadgetChargesOption.getFloat()) gadgetTimer = maxMobileGadgetChargesOption.getFloat();
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (vitalButton != null)
            {
                vitalButton.Destroy();
            }
            vitalButton = new CustomButton(
                () =>
                {
                    if (vitalsMinigame == null)
                    {
                        var e = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault(x => x.gameObject.name.Contains("panel_vitals"));
                        if (e == null || Camera.main == null) return;
                        vitalsMinigame = UnityEngine.Object.Instantiate(e.MinigamePrefab, Camera.main.transform, false);
                    }
                    vitalsMinigame.transform.SetParent(Camera.main.transform, false);
                    vitalsMinigame.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                    vitalsMinigame.Begin(null);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return gadgetTimer>0f && PlayerControl.LocalPlayer.CanMove; },
                () => { },
                getVitalsSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.vital",
                ImageNames.VitalsButton
            );
            vitalButton.Timer = vitalButton.MaxTimer = 0f;
        }

        public override void ButtonActivate()
        {
            vitalButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            vitalButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (vitalButton != null)
            {
                vitalButton.Destroy();
                vitalButton = null;
            }

            if (vitalsMinigame != null)
            {
                UnityEngine.Object.Destroy(vitalsMinigame);
                vitalsMinigame = null;
            }
        }

        public override void LoadOptionData()
        {
            mobileGadgetChargesOption = CreateOption(Color.white, "mobileGadgetCharges", 5f, 0f, 60f, 1f);
            mobileGadgetChargesOption.suffix = "second";

            maxMobileGadgetChargesOption = CreateOption(Color.white, "maxMobileGadgetCharges", 10f, 1f, 60f, 1f);
            maxMobileGadgetChargesOption.suffix = "second";

            chargesPerTasksOption = CreateOption(Color.white, "chargesPerTasks", 1f, 0f, 10f, 0.25f);
            chargesPerTasksOption.suffix = "second";
        }


        public override void Initialize(PlayerControl __instance)
        {
            foreach(var text in StatusTexts)
            {
                if (text.Item2) UnityEngine.Object.Destroy(text.Item2);
            }
            StatusTexts.Clear();

            gadgetTimer = mobileGadgetChargesOption.getFloat();
            if (gadgetTimer > maxMobileGadgetChargesOption.getFloat()) gadgetTimer = maxMobileGadgetChargesOption.getFloat();

            vitalsMinigame = null;
        }

        public Doctor()
                : base("Doctor", "doctor", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
        }
    }
}
