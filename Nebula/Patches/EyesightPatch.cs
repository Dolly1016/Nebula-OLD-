using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;

namespace Nebula.Patches
{
	public static class BeginHubHelper
    {
		private static void SetCamera(HudManager __instance, GameObject button)
        {
			var aPos=button.GetComponent<AspectPosition>();
			if (!aPos) return;
			aPos.parentCam = __instance.UICamera;
        }

		public static void Postfix(HudManager __instance)
		{
			var transform = __instance.transform.Find("Buttons");
			Transform child;
			if (transform)
			{
				child = transform.Find("TopRight");
				if (child)
					foreach (var aPos in child.GetComponentsInChildren<AspectPosition>())
						aPos.parentCam = __instance.UICamera;

				child = transform.Find("BottomRight");
				if (child)
					foreach (var aPos in child.GetComponentsInChildren<AspectPosition>())
						aPos.parentCam = __instance.UICamera;

				SetCamera(__instance, __instance.KillButton.gameObject);
				SetCamera(__instance, __instance.SabotageButton.gameObject);
				SetCamera(__instance, __instance.AbilityButton.gameObject);
				SetCamera(__instance, __instance.ImpostorVentButton.gameObject);
				SetCamera(__instance, __instance.ReportButton.gameObject);
				SetCamera(__instance, __instance.MapButton.gameObject);
			}
		}
	}

	[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
	class BeginHudPatch
	{
		public static void Postfix(HudManager __instance)
		{
			BeginHubHelper.Postfix(__instance);
		}
	}

	[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
	class BeginHudFinallyPatch
	{
		public static void Postfix(IntroCutscene __instance)
		{
			BeginHubHelper.Postfix(HudManager.Instance);
		}
	}

	[HarmonyPriority(100)]
	[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
	class EyesightPatch
	{
		static private float Distance = 1f;
		static private float ObserverModeRate = 1f;
		static public bool ObserverMode = false;

		public static void Postfix(HudManager __instance)
		{
			if (Game.GameData.data != null && Game.GameData.data.myData.CanSeeEveryoneInfo)
				if (Input.GetKeyDown(KeyCode.M)) ObserverMode = !ObserverMode;

			if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started
					|| !ShipStatus.Instance
					|| MeetingHud.Instance
					|| ExileController.Instance
					|| Minigame.Instance
					|| (MapBehaviour.Instance && MapBehaviour.Instance.IsOpen)
					|| (Game.GameData.data == null || !Game.GameData.data.myData.CanSeeEveryoneInfo))
				ObserverMode = false;


			if (ObserverMode)
			{
				float axis = Input.GetAxis("Mouse ScrollWheel");
				if (axis < 0f) Distance -= 0.25f;
				if (axis > 0f) Distance += 0.25f;
				Distance = Mathf.Clamp(Distance, 0.25f, 3.5f);
			}
			else
			{
				Distance = 1f;
			}

			float dis = ((ObserverMode ? 2.0f : 1.0f) - ObserverModeRate);
			if (Mathf.Abs(dis) > 0.01f)
				ObserverModeRate += ((ObserverMode ? 2.0f : 1.0f) - ObserverModeRate) * 0.3f;
			else
				ObserverModeRate = ObserverMode ? 2.0f : 1.0f;

			Camera.main.orthographicSize = 3.0f * Distance * ObserverModeRate;
			HudManager.Instance.UICamera.orthographicSize = 3.0f;
			HudManager.Instance.transform.localScale = Vector3.one;


			if (HudManager.Instance) {
				Transform transform;

				transform = HudManager.Instance.transform.Find("TaskDisplay");
				if (transform) transform.gameObject.SetActive(!ObserverMode && ShipStatus.Instance);

				transform = HudManager.Instance.transform.Find("Buttons");
				if (transform)
				{
					transform.gameObject.SetActive(!ObserverMode);
				}
			}

            if (PlayerControl.LocalPlayer)
            {
				var player = PlayerControl.LocalPlayer;

				player.Visible = !ObserverMode && !player.inVent;

                if (player.MyPhysics?.GlowAnimator != null)
                {
					player.MyPhysics.GlowAnimator.gameObject.SetActive(!ObserverMode && !ShipStatus.Instance);
				}
			}
		}
	}
}