using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
/*
namespace Nebula.Patches
{
	[HarmonyPatch]
	class PrespawnPatch
    {
		[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.PrespawnStep))]
        public static class PrespawnStepPatch
        {
			public static void Postfix(ShipStatus __instance,ref Il2CppSystem.Collections.IEnumerator __result)
            {
				
				if (PlayerControl.GameOptions.MapId == 2)
				{
					SpawnInMinigame spawnInMinigame = UnityEngine.Object.Instantiate<SpawnInMinigame>(Resources.FindObjectsOfTypeAll(SpawnInMinigame.Il2CppType)[0] as SpawnInMinigame);

					
					//spawnInMinigame.Locations = new UnhollowerBaseLib.Il2CppReferenceArray<SpawnInMinigame.SpawnLocation>(3);
					//for (int i = 0; i < 3; i++)
					//{
					//	spawnInMinigame.Locations[i] = new SpawnInMinigame.SpawnLocation();
					//	spawnInMinigame.Locations[i].Name = StringNames.Specimens;
					//	spawnInMinigame.Locations[i].Location = new Vector3(10,10);
					//}
					

					spawnInMinigame.transform.SetParent(Camera.main.transform, false);
					spawnInMinigame.transform.localPosition = new Vector3(0f, 0f, -600f);
					spawnInMinigame.Begin(null);
					__result = spawnInMinigame.WaitForFinish();
				}
            }
        }
	}
}
*/