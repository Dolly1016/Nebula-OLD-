using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using TMPro;
using PowerTools;

namespace Nebula.Patches
{
	[HarmonyPatch]
	class PrespawnPatch
    {
		private static PassiveButton? selected = null;

		[HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt))]
		public static class PrespawnSpawnAtPatch
		{
			public static bool Prefix(SpawnInMinigame __instance, [HarmonyArgument(0)] Vector3 spawnAt)
			{
				if (!CustomOptionHolder.synchronizedSpawning.getBool()) return true;

				RPCEventInvoker.Synchronize(Game.SynchronizeTag.PreSpawnMinigame,PlayerControl.LocalPlayer.PlayerId);
				if (__instance.amClosing != Minigame.CloseState.None)
				{
					return false;
				}
				if (__instance.gotButton) return false;

				__instance.gotButton = true;


				foreach (var button in __instance.LocationButtons)
				{
					button.enabled = false;
				}

				__instance.StartCoroutine(Effects.Lerp(10f, (Il2CppSystem.Action<float>)((p) =>
				{
					float time = p * 10f;
					

					foreach (var button in __instance.LocationButtons)
					{
                        if (selected == button)
                        {
							if (time > 0.3f)
							{
								float x = button.transform.localPosition.x;
								if (x < 0f) x += 10f * Time.deltaTime;
								if (x > 0f) x -= 10f * Time.deltaTime;
								if (Mathf.Abs(x) < 10f * Time.deltaTime) x = 0f;
								button.transform.localPosition = new Vector3(x, button.transform.localPosition.y, button.transform.localPosition.z);
							}
						}
                        else
                        {
							var color = button.GetComponent<SpriteRenderer>().color;
							float a = color.a;
							if (a > 0f) a -= 2f * Time.deltaTime;
							if (a < 0f) a = 0f;
							button.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, a);
							button.GetComponentInChildren<TextMeshPro>().color = new Color(1f, 1f, 1f, a);
						}

						if (__instance.amClosing != Minigame.CloseState.None) return;

						if (Game.GameData.data.SynchronizeData.Align(Game.SynchronizeTag.PreSpawnMinigame, false) || p==1f)
						{
							PlayerControl.LocalPlayer.gameObject.SetActive(true);
							__instance.StopAllCoroutines();
							PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(spawnAt);
							DestroyableSingleton<HudManager>.Instance.PlayerCam.SnapToTarget();
							__instance.Close();
						}
					}

				})));

				return false;
			}
		}

		[HarmonyPatch(typeof(SpawnInMinigame._RunTimer_d__10), nameof(SpawnInMinigame._RunTimer_d__10.MoveNext))]
		public static class PrespawnTextPatch
		{
			public static void Postfix(SpawnInMinigame._RunTimer_d__10 __instance)
			{
				if (!CustomOptionHolder.synchronizedSpawning.getBool()) return;
				
				if (selected!=null)
					__instance.__4__this.Text.text = Language.Language.GetString("game.minigame.waitSpawning");
			}
		}

		[HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.Begin))]
		public static class PrespawnBeginPatch
        {
			public static void Postfix(SpawnInMinigame __instance)
            {
				selected = null;				

				if (!CustomOptionHolder.synchronizedSpawning.getBool()) return;

				foreach(var button in __instance.LocationButtons)
                {
					button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
					{
						if (selected == null)
							selected = button;
					}
					));
                }
            }
        }

		[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.PrespawnStep))]
		public static class PrespawnStepPatch
		{
			public static void Postfix(ShipStatus __instance, ref Il2CppSystem.Collections.IEnumerator __result)
			{
				if (!CustomOptionHolder.mapOptions.getBool() || !CustomOptionHolder.multipleSpawnPoints.getBool()) return;

				if (Map.MapData.GetCurrentMapData().SpawnOriginalPositionAtFirst && !ExileController.Instance) return;

				var spawnCandidates = Map.MapData.GetCurrentMapData().SpawnCandidates;
				if (spawnCandidates.Count < 3) return;

				SpawnInMinigame spawnInMinigame = UnityEngine.Object.Instantiate<SpawnInMinigame>(Map.MapData.MapDatabase[4].Assets.gameObject.GetComponent<AirshipStatus>().SpawnInGame);

				spawnInMinigame.transform.SetParent(Camera.main.transform, false);
				spawnInMinigame.transform.localPosition = new Vector3(0f, 0f, -600f);

				/* Begin (Minigame) */
				Minigame.Instance = spawnInMinigame;
				SpawnInMinigame.Instance = spawnInMinigame;
				spawnInMinigame.MyTask = null;
				spawnInMinigame.MyNormTask = null;
				if (PlayerControl.LocalPlayer)
				{
					if (MapBehaviour.Instance)
					{
						MapBehaviour.Instance.Close();
					}
					PlayerControl.LocalPlayer.NetTransform.Halt();
				}
				spawnInMinigame.StartCoroutine(spawnInMinigame.CoAnimateOpen());
				/* Begin (Minigame) */

				/* Begin (SpawnInMinigame) */

				var randomArray=Helpers.GetRandomArray(spawnCandidates.Count);
				for (int i = 0; i < spawnInMinigame.LocationButtons.Length; i++)
				{
					PassiveButton passiveButton = spawnInMinigame.LocationButtons[i];
					
					int index = randomArray[i];

					spawnCandidates[index].ReloadTexture();

					passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
					{
						spawnInMinigame.SpawnAt(spawnCandidates[index].SpawnLocation);
					}));
					passiveButton.OnMouseOver.AddListener(new System.Action( ()=>HudManager.Instance.StartCoroutine(spawnCandidates[index].GetEnumerator(passiveButton.GetComponent<SpriteRenderer>()))));

					passiveButton.GetComponent<SpriteAnim>().Stop();
					passiveButton.GetComponent<SpriteRenderer>().sprite = spawnCandidates[index].GetSprite();
					passiveButton.GetComponentInChildren<TextMeshPro>().text = Language.Language.GetString("game.spawnLocation." + spawnCandidates[index].LocationKey);
					ButtonAnimRolloverHandler component = passiveButton.GetComponent<ButtonAnimRolloverHandler>();
					component.StaticOutImage = spawnCandidates[index].GetSprite();
					component.RolloverAnim = new AnimationClip();
					component.HoverSound = spawnCandidates[index].GetAudioClip() ?? spawnInMinigame.DefaultRolloverSound;
				}
				PlayerControl.LocalPlayer.gameObject.SetActive(false);
				PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(new Vector2(20f, 40f));

				PrespawnBeginPatch.Postfix(spawnInMinigame);

				spawnInMinigame.StartCoroutine(spawnInMinigame.RunTimer());
				ControllerManager.Instance.OpenOverlayMenu(spawnInMinigame.name, null, spawnInMinigame.DefaultButtonSelected, spawnInMinigame.ControllerSelectable, false);
				PlayerControl.HideCursorTemporarily();
				ConsoleJoystick.SetMode_Menu();

				/* Begin (SpawnInMinigame) */

				__result = spawnInMinigame.WaitForFinish();

			}
		}
	}
}
