﻿using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnhollowerBaseLib;
using System.Collections;
using System;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace Nebula.Roles.ComplexRoles
{
    class FGuesser : Role
    {
        private Module.CustomOption chanceToSpawnAsImpostor;

        static public Color Color = new Color(255f / 255f, 255f / 255f, 0f / 255f);

        public int ChanceToSpawnAsImpostor()
        {
            return chanceToSpawnAsImpostor.getSelection();
        }

        private static Sprite targetSprite;
        public static Sprite getTargetSprite()
        {
            if (targetSprite) return targetSprite;
            targetSprite = Helpers.loadSpriteFromResources("Nebula.Resources.TargetIcon.png", 150f);
            return targetSprite;
        }

        //Complexなロールカテゴリーについてのみ呼ばれます。
        public override Patches.AssignRoles.RoleAllocation[] GetComplexAllocations()
        {
            Patches.AssignRoles.RoleAllocation[] result = new Patches.AssignRoles.RoleAllocation[(int)roleCountOption.getFloat()];

            int damneds = 0;
            double rnd = NebulaPlugin.rnd.NextDouble();
            double rate = (double)ChanceToSpawnAsImpostor() / 10.0;
            while (rnd < rate)
            {
                damneds++;
                rate *= (double)ChanceToSpawnAsImpostor() / 10.0;
                if (damneds >= result.Length)
                {
                    break;
                }
            }

            int chance = roleChanceOption.getSelection();
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Patches.AssignRoles.RoleAllocation(i < damneds ? Roles.EvilGuesser : Roles.NiceGuesser, chance);
            }

            return result;
        }

        public override void LoadOptionData()
        {
            chanceToSpawnAsImpostor = CreateOption(Color.white, "chanceToSpawnAsImpostor", CustomOptionHolder.rates);
        }

        public FGuesser()
                : base("Guesser", "guesser", Color, RoleCategory.Complex, Side.Crewmate, Side.Crewmate,
                     new HashSet<Side>(), new HashSet<Side>(), new HashSet<Patches.EndCondition>(),
                     false, false, false, false, false)
        {

        }
    }

    public class Guesser : Role
    {
        //インポスターはModで操作するFakeTaskは所持していない
        public Guesser(string name, string localizeName, bool isImpostor)
                : base(name, localizeName,
                     isImpostor ? Palette.ImpostorRed : Palette.CrewmateBlue,
                     isImpostor ? RoleCategory.Impostor : RoleCategory.Crewmate,
                     isImpostor ? Side.Impostor : Side.Crewmate, isImpostor ? Side.Impostor : Side.Crewmate,
                     isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                     isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                     isImpostor ? ImpostorRoles.Impostor.impostorEndSet : CrewmateRoles.Crewmate.crewmateEndSet,
                     false, isImpostor, isImpostor, false, isImpostor)
        {
            IsGuessableRole = false;
        }

        private static GameObject guesserUI;
        static void guesserOnClick(int buttonTarget, MeetingHud __instance)
        {
            if (guesserUI != null || !(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted)) return;
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

            Transform container = UnityEngine.Object.Instantiate(__instance.transform.FindChild("PhoneUI"), __instance.transform);
            container.transform.localPosition = new Vector3(0, 0, -5f);
            guesserUI = container.gameObject;

            int i = 0;
            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
            var textTemplate = __instance.playerStates[0].NameText;

            Transform exitButtonParent = (new GameObject()).transform;
            exitButtonParent.SetParent(container);
            Transform exitButton = UnityEngine.Object.Instantiate(buttonTemplate.transform, exitButtonParent);
            Transform exitButtonMask = UnityEngine.Object.Instantiate(maskTemplate, exitButtonParent);
            exitButton.gameObject.GetComponent<SpriteRenderer>().sprite = smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
            exitButtonParent.transform.localPosition = new Vector3(2.725f, 2.1f, -5);
            exitButtonParent.transform.localScale = new Vector3(0.25f, 0.9f, 1);
            exitButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            exitButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
                __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                UnityEngine.Object.Destroy(container.gameObject);
            }));

            List<Transform> buttons = new List<Transform>();
            Transform selectedButton = null;

            foreach (Role role in Roles.AllRoles)
            {
                //撃てないロールを除外する
                if (!role.IsGuessableRole || role.category == RoleCategory.Complex) continue;
                Transform buttonParent = (new GameObject()).transform;
                buttonParent.SetParent(container);
                Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
                Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
                TMPro.TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
                button.GetComponent<SpriteRenderer>().sprite = DestroyableSingleton<HatManager>.Instance.AllNamePlates[0].Image;
                buttons.Add(button);
                int row = i / 5, col = i % 5;
                buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -5);
                buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
                label.text = Helpers.cs(role.color, Language.Language.GetString("role."+role.localizeName+".name"));
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
                label.transform.localScale *= 1.7f;
                int copiedIndex = i;

                button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
                if (!PlayerControl.LocalPlayer.Data.IsDead) button.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() => {
                    if (selectedButton != button)
                    {
                        selectedButton = button;
                        buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Color.red : Color.white);
                    }
                    else
                    {
                        PlayerControl focusedTarget = Helpers.playerById((byte)__instance.playerStates[buttonTarget].TargetPlayerId);
                        if (!(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted) || focusedTarget == null) return;

                        PlayerControl dyingTarget = (focusedTarget.GetModData().role == role) ? focusedTarget : PlayerControl.LocalPlayer;

                        // Reset the GUI
                        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        UnityEngine.Object.Destroy(container.gameObject);
                        //if (Guesser.hasMultipleShotsPerMeeting && Guesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) > 1 && dyingTarget != PlayerControl.LocalPlayer)
                            __instance.playerStates.ToList().ForEach(x => { if (x.TargetPlayerId == dyingTarget.PlayerId && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });
                        //else
                        //   __instance.playerStates.ToList().ForEach(x => { if (x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });

                        // Shoot player and send chat info if activated
                        RPCEventInvoker.CloseUpKill(PlayerControl.LocalPlayer, dyingTarget);
                    }
                }));

                i++;
            }
            container.transform.localScale *= 0.75f;
        }

        public override void SetupMeetingButton(MeetingHud __instance)
        {
            if (!PlayerControl.LocalPlayer.Data.IsDead)
            {
                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                    GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                    targetBox.name = "ShootButton";
                    targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
                    SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                    renderer.sprite = ComplexRoles.FGuesser.getTargetSprite();
                    PassiveButton button = targetBox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners();
                    int copiedIndex = i;
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => guesserOnClick(copiedIndex, __instance)));
                }
            }
        }

    }
}
