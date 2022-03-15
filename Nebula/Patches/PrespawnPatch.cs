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
