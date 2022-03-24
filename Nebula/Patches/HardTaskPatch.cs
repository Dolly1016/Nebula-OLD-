using HarmonyLib;
using Hazel;
using System;
using System.Linq;
using System.Collections.Generic;
using Rewired;
using UnityEngine;

namespace Nebula.Patches
{
	[Harmony]
	public class HardTaskPatch
	{
		//数字タスクの一部迷彩化
		[HarmonyPatch(typeof(UnlockManifoldsMinigame), nameof(UnlockManifoldsMinigame.Begin))]
		public static class UnlockManifoldsPatch
		{
			static private Sprite emptySprite = null;
			static private Sprite GetEmptySprite()
			{
				if (emptySprite) return emptySprite;
				emptySprite = Helpers.loadSpriteFromResources("Nebula.Resources.EmptyManifolds.png", 100f);
				return emptySprite;

			}

			public static void Postfix(UnlockManifoldsMinigame __instance)
			{
				if (!CustomOptionHolder.TasksOption.getBool() || !CustomOptionHolder.MeistersManifoldsOption.getBool()) return;

				var randomArray = Helpers.GetRandomArray(10);
				int hideButtons = NebulaPlugin.rnd.Next(3, 5);
				for (int i = 0; i < hideButtons; i++)
				{
					__instance.Buttons[randomArray[i]].sprite = GetEmptySprite();
				}
			}
		}

		//葉っぱタスクの葉っぱ数増加
		[HarmonyPatch(typeof(LeafMinigame), nameof(LeafMinigame.Begin))]
		public static class LeavesPatch
		{
			public static bool Prefix(LeafMinigame __instance, [HarmonyArgument(0)] PlayerTask task)
			{
				if (!CustomOptionHolder.TasksOption.getBool() || !CustomOptionHolder.MeistersFilterOption.getBool()) return true;

				task.gameObject.GetComponent<NormalPlayerTask>().MaxStep = 20;
				return true;
			}
		}

		//給油タスクのステップ数改変
		[HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.Initialize))]
		public static class VeryLongTaskPatch
		{
			public static void Postfix(NormalPlayerTask __instance)
			{
				if (!CustomOptionHolder.TasksOption.getBool() || !CustomOptionHolder.MeistersFuelEnginesOption.getBool()) return;

				if (__instance.TaskType == TaskTypes.FuelEngines && __instance.StartAt == SystemTypes.Storage)
				{
					__instance.MaxStep *= 2;
				}
			}
		}

		//給油タスクの仕様変更
		[HarmonyPatch(typeof(RefuelStage), nameof(RefuelStage.FixedUpdate))]
		public static class RefuelStagePatch
		{
			private static bool IsHardMode(RefuelStage __instance) { return __instance.MyNormTask.MaxStep == 4; }

			public static bool Prefix(RefuelStage __instance)
			{
				if (!CustomOptionHolder.TasksOption.getBool() || !CustomOptionHolder.MeistersFuelEnginesOption.getBool()) return true;

				if (ReInput.players.GetPlayer(0).GetButton(21))
				{
					if (!__instance.isDown)
					{
						__instance.usingController = true;
						__instance.Refuel();
					}
				}
				else if (__instance.isDown && __instance.usingController)
				{
					__instance.usingController = false;
					__instance.Refuel();
				}
				if (__instance.complete)
				{
					return false;
				}
				if (__instance.isDown && __instance.timer < 1f)
				{

					__instance.timer += (Time.fixedDeltaTime / __instance.RefuelDuration) * ((IsHardMode(__instance) && __instance.MyNormTask.Data[1] % 2 == 1) ? 0.5f : 1f);

					__instance.MyNormTask.Data[0] = (byte)Mathf.Min(255f, __instance.timer * 255f);
					if (__instance.timer >= ((IsHardMode(__instance) && __instance.MyNormTask.Data[1] % 2 == 1) ? 0.5f : 1f))
					{
						__instance.complete = true;
						if (__instance.greenLight)
						{
							__instance.greenLight.color = __instance.green;
						}
						if (__instance.redLight)
						{
							__instance.redLight.color = __instance.darkRed;
						}
						if (__instance.MyNormTask.MaxStep == 1)
						{
							__instance.MyNormTask.NextStep();
						}
						else if (__instance.MyNormTask.StartAt == SystemTypes.CargoBay || __instance.MyNormTask.StartAt == SystemTypes.Engine)
						{
							__instance.MyNormTask.Data[0] = 0;
							__instance.MyNormTask.Data[1] = (byte)(BoolRange.Next(0.5f) ? 1 : 2);
							__instance.MyNormTask.NextStep();
						}
						else
						{
							__instance.MyNormTask.Data[0] = 0;

							if ((__instance.MyNormTask.TaskStep % 2) == 0 && __instance.MyNormTask.Data[1] % 2 == 1)
								__instance.MyNormTask.Data[1]--;
							else
								__instance.MyNormTask.Data[1]++;

							if (__instance.MyNormTask.Data[1] % 2 == 0)
							{
								__instance.MyNormTask.NextStep();
							}
							__instance.MyNormTask.UpdateArrow();
						}
					}
				}

				if (IsHardMode(__instance) &&
					((__instance.MyNormTask.Data[1] % 2 == 1 && __instance.MyNormTask.TaskStep % 2 == 1) ||
					(__instance.complete && __instance.MyNormTask.Data[1] % 2 == 0 && __instance.MyNormTask.TaskStep % 2 == 0)))
					__instance.destGauge.value = __instance.timer + 0.5f;
				else
					__instance.destGauge.value = __instance.timer;

				if (__instance.srcGauge)
				{
					__instance.srcGauge.value = 1f - (IsHardMode(__instance) ? 2f : 1f) * __instance.timer;
				}

				return false;
			}
		}

		//配線タスクの順番ランダム化
		[HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.PickRandomConsoles))]
		public static class RandomTaskPatch
		{
			static public void Postfix(NormalPlayerTask __instance, [HarmonyArgument(0)] TaskTypes taskType, [HarmonyArgument(1)] ref UnhollowerBaseLib.Il2CppStructArray<byte> consoleIds)
            {
				if (!CustomOptionHolder.TasksOption.getBool() || !CustomOptionHolder.RandomizedWiringOption.getBool()) return;
				
				var newArray = consoleIds.OrderBy(i => Guid.NewGuid()).ToArray();
				for (int i= 0;i<newArray.Length;i++)
					consoleIds[i] = newArray[i];
             
			}
		}
	}
}
