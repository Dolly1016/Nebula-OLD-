using HarmonyLib;
using Nebula.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Nebula.Patches;

[HarmonyPatch(typeof(AssetReference), nameof(AssetReference.InstantiateAsync),typeof(Transform),typeof(bool))]
public static class LoadShipInstancePatch
{
    static bool Prefix(AssetReference __instance,ref AsyncOperationHandle<GameObject> __result)
    {
        foreach(var reference in AmongUsClient.Instance.ShipPrefabs)
        {
            if (!reference.AssetGUID.Equals(__instance.AssetGUID)) continue;

            //ShipStatusの生成時
            __result = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(__instance.RuntimeKey, null, false, false);
            NebulaGameManager.Instance.RuntimeAsset.SetHandle(__result);
            __result.Acquire();
            return false;
        }
        return true;
    }
}
