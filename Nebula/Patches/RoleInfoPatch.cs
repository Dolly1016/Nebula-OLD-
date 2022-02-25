using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Nebula.Patches
{
    [Harmony]
    public class CustomOverlays
    {

        private static Sprite colorBG;
        private static SpriteRenderer meetingUnderlay;
        private static SpriteRenderer infoUnderlay;
        private static TMPro.TextMeshPro infoOverlayRules;
        private static TMPro.TextMeshPro infoOverlayRoles;
        public static bool overlayShown = false;

        public static void Reset()
        {
            hideBlackBG();
            hideInfoOverlay();
            UnityEngine.Object.Destroy(meetingUnderlay);
            UnityEngine.Object.Destroy(infoUnderlay);
            UnityEngine.Object.Destroy(infoOverlayRules);
            UnityEngine.Object.Destroy(infoOverlayRoles);
            meetingUnderlay = infoUnderlay = null;
            infoOverlayRules = infoOverlayRoles = null;
            overlayShown = false;
        }

        private static bool Initialize()
        {
            HudManager hudManager = DestroyableSingleton<HudManager>.Instance;
            if (hudManager == null) return false;

            if (colorBG == null)
            {
                var texture = new Texture2D(100,100, TextureFormat.ARGB32, false);

                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        texture.SetPixel(x, y, Color.white);
                texture.Apply();

                colorBG = Helpers.loadSpriteFromResources(texture, 100f,new Rect(0f,0f,100f,-100f));
            }

            if (meetingUnderlay == null)
            {
                meetingUnderlay = UnityEngine.Object.Instantiate(hudManager.FullScreen, hudManager.transform);
                meetingUnderlay.transform.localPosition = new Vector3(0f, 0f, 20f);
                meetingUnderlay.gameObject.SetActive(true);
                meetingUnderlay.enabled = false;
            }

            if (infoUnderlay == null)
            {
                infoUnderlay = UnityEngine.Object.Instantiate(meetingUnderlay, hudManager.transform);
                infoUnderlay.transform.localPosition = new Vector3(0f, 0f, -900f);
                infoUnderlay.gameObject.SetActive(true);
                infoUnderlay.enabled = false;
            }

            if (infoOverlayRules == null)
            {
                infoOverlayRules = UnityEngine.Object.Instantiate(hudManager.TaskText, hudManager.transform);
                infoOverlayRules.fontSize = infoOverlayRules.fontSizeMin = infoOverlayRules.fontSizeMax = 1.15f;
                infoOverlayRules.autoSizeTextContainer = false;
                infoOverlayRules.enableWordWrapping = false;
                infoOverlayRules.alignment = TMPro.TextAlignmentOptions.TopLeft;
                infoOverlayRules.transform.position = Vector3.zero;
                infoOverlayRules.transform.localPosition = new Vector3(-2.5f, 1.15f, -910f);
                infoOverlayRules.transform.localScale = Vector3.one;
                infoOverlayRules.color = Palette.White;
                infoOverlayRules.enabled = false;
            }

            if (infoOverlayRoles == null)
            {
                infoOverlayRoles = UnityEngine.Object.Instantiate(infoOverlayRules, hudManager.transform);
                infoOverlayRoles.maxVisibleLines = 28;
                infoOverlayRoles.fontSize = infoOverlayRoles.fontSizeMin = infoOverlayRoles.fontSizeMax = 1.15f;
                infoOverlayRoles.outlineWidth += 0.02f;
                infoOverlayRoles.autoSizeTextContainer = false;
                infoOverlayRoles.enableWordWrapping = false;
                infoOverlayRoles.alignment = TMPro.TextAlignmentOptions.TopLeft;
                infoOverlayRoles.transform.position = Vector3.zero;
                infoOverlayRoles.transform.localPosition = infoOverlayRules.transform.localPosition + new Vector3(2.5f, 0.0f, 0.0f);
                infoOverlayRoles.transform.localScale = Vector3.one;
                infoOverlayRoles.color = Palette.White;
                infoOverlayRoles.enabled = false;
            }

            return true;
        }

        private static void showBlackBG()
        {
            if (HudManager.Instance == null) return;
            if (!Initialize()) return;

            meetingUnderlay.sprite = colorBG;
            meetingUnderlay.enabled = true;
            meetingUnderlay.transform.localScale = new Vector3(400f, 400f, 1f);
            var clearBlack = new Color32(0, 0, 0, 0);

            HudManager.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
            {
                meetingUnderlay.color = Color.Lerp(clearBlack, Palette.Black, t);
            })));
        }

        private static void hideBlackBG()
        {
            if (meetingUnderlay == null) return;
            meetingUnderlay.enabled = false;
        }

        public static void Hide()
        {
            CustomOverlays.hideInfoOverlay();
        }

        public static void OnMeetingEnd()
        {
            Hide();
            CustomOverlays.hideBlackBG();
        }
        public static void OnMeetingStart()
        {
            CustomOverlays.showBlackBG();
        }


        private static void showInfoOverlay()
        {
            if (overlayShown) return;

            HudManager hudManager = DestroyableSingleton<HudManager>.Instance;
            if (ShipStatus.Instance == null || PlayerControl.LocalPlayer == null || hudManager == null || HudManager.Instance.isIntroDisplayed || (!PlayerControl.LocalPlayer.CanMove && MeetingHud.Instance == null))
                return;

            if (!Initialize()) return;

            if (MapBehaviour.Instance != null)
                MapBehaviour.Instance.Close();

            hudManager.SetHudActive(false);

            overlayShown = true;

            Transform parent;
            if (MeetingHud.Instance != null)
                parent = MeetingHud.Instance.transform;
            else
                parent = hudManager.transform;

            infoUnderlay.transform.parent = parent;
            infoOverlayRules.transform.parent = parent;
            infoOverlayRoles.transform.parent = parent;

            infoUnderlay.sprite = colorBG;
            infoUnderlay.color = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            infoUnderlay.transform.localScale = new Vector3(4.5f, 5f, 1f);
            infoUnderlay.enabled = true;

            infoOverlayRules.text = "";
            infoOverlayRules.enabled = true;

            string rolesText = "";
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) =>
            {
                string roleDesc = Language.Language.GetString("role."+ role.LocalizeName+".info");
                rolesText += $"<size=150%>{Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name"))}</size>" +
                    (roleDesc != "" ? $"\n{roleDesc}" : "") + "\n\n";
            });

            infoOverlayRoles.text = rolesText;
            infoOverlayRoles.enabled = true;

            var underlayTransparent = new Color(0.1f, 0.1f, 0.1f, 0.0f);
            var underlayOpaque = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            HudManager.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
            {
                infoUnderlay.color = Color.Lerp(underlayTransparent, underlayOpaque, t);
                infoOverlayRules.color = Color.Lerp(Palette.ClearWhite, Palette.White, t);
                infoOverlayRoles.color = Color.Lerp(Palette.ClearWhite, Palette.White, t);
            })));
        }

        private static void hideInfoOverlay()
        {
            if (!overlayShown) return;

            if (MeetingHud.Instance == null) DestroyableSingleton<HudManager>.Instance.SetHudActive(true);

            overlayShown = false;
            var underlayTransparent = new Color(0.1f, 0.1f, 0.1f, 0.0f);
            var underlayOpaque = new Color(0.1f, 0.1f, 0.1f, 0.88f);

            HudManager.Instance.StartCoroutine(Effects.Lerp(0.2f, new Action<float>(t =>
            {
                if (infoUnderlay != null)
                {
                    infoUnderlay.color = Color.Lerp(underlayOpaque, underlayTransparent, t);
                    if (t >= 1.0f) infoUnderlay.enabled = false;
                }

                if (infoOverlayRules != null)
                {
                    infoOverlayRules.color = Color.Lerp(Palette.White, Palette.ClearWhite, t);
                    if (t >= 1.0f) infoOverlayRules.enabled = false;
                }

                if (infoOverlayRoles != null)
                {
                    infoOverlayRoles.color = Color.Lerp(Palette.White, Palette.ClearWhite, t);
                    if (t >= 1.0f) infoOverlayRoles.enabled = false;
                }
            })));
        }

        public static void toggleInfoOverlay()
        {
            if (overlayShown)
                hideInfoOverlay();
            else
                showInfoOverlay();
        }

        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        public static class CustomOverlayKeybinds
        {
            public static void Postfix(KeyboardJoystick __instance)
            {
                if (Input.GetKeyDown(KeyCode.H) && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                {
                    toggleInfoOverlay();
                }
            }
        }
    }
}
