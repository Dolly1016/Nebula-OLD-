using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch]
public static class RuntimePrefabPatch {
    [HarmonyPatch(typeof(AmongUsClient),nameof(AmongUsClient.Awake)),HarmonyPostfix]
    public static void PlayerDisplayPatch(AmongUsClient __instance) {
        if (RuntimePrefabs.PlayerDisplayPrefab != null) return;

        __instance.PlayerPrefab.gameObject.SetActive(false);
        var prefab = UnityEngine.Object.Instantiate(__instance.PlayerPrefab.gameObject);
        __instance.PlayerPrefab.gameObject.SetActive(true);

        RuntimePrefabs.PlayerDisplayPrefab = prefab.AddComponent<PlayerDisplay>();

        GameObject.Destroy(prefab.GetComponent<PlayerControl>());
        GameObject.Destroy(prefab.GetComponent<PlayerPhysics>());
        GameObject.Destroy(prefab.GetComponent<Rigidbody2D>());
        GameObject.Destroy(prefab.GetComponent<CircleCollider2D>());
        GameObject.Destroy(prefab.GetComponent<CustomNetworkTransform>());
        GameObject.Destroy(prefab.GetComponent<BoxCollider2D>());
        GameObject.Destroy(prefab.GetComponent<DummyBehaviour>());
        GameObject.Destroy(prefab.GetComponent<AudioSource>());
        GameObject.Destroy(prefab.GetComponent<PassiveButton>());
        GameObject.Destroy(prefab.GetComponent<HnSImpostorScreamSfx>());

        prefab.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(prefab);
    }
}