using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using BepInEx.IL2CPP.Utils.Collections;
using Nebula.Utilities;

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

	[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
	class StartMeetingPatch
	{
		public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo meetingTarget)
		{
			EyesightPatch.ObserverMode = false;
		}
	}

	[HarmonyPriority(100)]
	[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
	class EyesightPatch
	{
		static private float Distance = 1f;
		static private float ObserverModeRate = 1f;
		static public bool ObserverMode = false;

		private static IEnumerator GetEnumerator(HudManager __instance)
        {
			while (true)
			{
				if (ObserverMode) break;
				if (Camera.main.orthographicSize == 3f)
				{
					Transform transform = __instance.transform.Find("Buttons");
					if (transform)
					{
						transform.gameObject.SetActive(!ObserverMode);
					}
					break;
				}
				yield return null;
			}
			yield break;
        }

		public static void Postfix(HudManager __instance)
		{
			bool lastObserverMode = ObserverMode;

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

            if (ObserverMode != lastObserverMode)
            {
				__instance.StartCoroutine(GetEnumerator(__instance).WrapToIl2Cpp());
            }


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
			__instance.UICamera.orthographicSize = 3.0f;
			__instance.transform.localScale = Vector3.one;


			if (HudManager.InstanceExists) {
				Transform transform;

				transform = __instance.transform.Find("TaskDisplay");
				if (transform) transform.gameObject.SetActive(!ObserverMode && ShipStatus.Instance);

				if (ObserverMode)
				{
					transform = __instance.transform.Find("Buttons");
					if (transform)
					{
						transform.gameObject.SetActive(!ObserverMode);
					}
				}
			}
		}
	}
}