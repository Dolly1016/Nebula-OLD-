using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using BepInEx.IL2CPP.Utils.Collections;
using UnityEngine.Rendering;

namespace Nebula.Patches
{

    [HarmonyPatch]
    class LightPatch
    {
        [HarmonyPatch(typeof(OneWayShadows), nameof(OneWayShadows.IsIgnored))]
        public static class OneWayShadowsPatch
        {
            public static void Postfix(OneWayShadows __instance, ref bool __result)
            {
                if (Game.GameData.data == null) return;
                if (!PlayerControl.LocalPlayer) return;

                var data = Game.GameData.data.myData.getGlobalData();
                if (data == null) return;

                __result |= data.role.UseImpostorLightRadius;
            }
        }

        [HarmonyPatch(typeof(ShadowCollab), nameof(ShadowCollab.OnEnable))]
        public static class ShadowCameraPatch
        {
            static public IEnumerator GetEnumerator(ShadowCollab __instance)
            {
                Camera cam = Camera.main;
                while (true)
                {
                    __instance.ShadowCamera.orthographicSize = cam.orthographicSize;
                    __instance.ShadowQuad.transform.localScale = new Vector3(cam.orthographicSize * cam.aspect, cam.orthographicSize) * 2f;
                    yield return null;
                }
            }

            public static void Prefix(ShadowCollab __instance)
            {
                __instance.StartCoroutine(GetEnumerator(__instance).WrapToIl2Cpp());
            }
        }

        static private bool isFirst = false;
        static private RenderTexture? targetTexture;

        static private RenderTexture getTargetTexture(LightSource __instance)
        {
            if (targetTexture != null) return targetTexture;

            if (SystemInfo.SupportsRenderTextureFormat(__instance.preferredRTFormat))
            {
                targetTexture = new RenderTexture(3, __instance.shadowmapResolution, 16, __instance.preferredRTFormat);
            }
            else
            {
                targetTexture = new RenderTexture(3, __instance.shadowmapResolution, 16, (RenderTextureFormat)16);
            }
            targetTexture.wrapModeU = (TextureWrapMode)1;
            targetTexture.wrapModeV = 0;
            targetTexture.filterMode = (FilterMode)1;
            targetTexture.Create();

            return targetTexture;
        }

        [HarmonyPatch(typeof(LightSource), nameof(LightSource.Update))]
        public static class UpdateLightPatch
        {
            public static void Draw(LightSource __instance, Vector2 pos, float radius)
            {
                __instance.vertCount = 0;
                
                Vector3 pos3 = new Vector3(pos.x, pos.y, pos.y / 1000f - 7f);
                __instance.child.transform.position = pos3;
                __instance.LightRadius = radius;
                __instance.Material.SetFloat("_LightRadius", radius);

                __instance.GPUShadows((Vector2)pos3);
            }

            public static bool Prefix(LightSource __instance)
            {
                float radius = __instance.LightRadius;

                isFirst = true;

                Draw(__instance, __instance.transform.position, __instance.LightRadius);
                isFirst = false;
                //Draw(__instance, new Vector2(0f, 0f), __instance.LightRadius);

                __instance.LightRadius = radius;
                __instance.child.transform.position = __instance.transform.position;

                return false;
            }
        }

        [HarmonyPatch(typeof(LightSource), nameof(LightSource.DrawOcclusion))]
        public static class DrawOcclusionPatch
        {

            public static bool Prefix(LightSource __instance, [HarmonyArgument(0)] float effectiveRadius)
            {
                if (__instance.cb == null)
                {
                    __instance.cb = new CommandBuffer();
                    __instance.cb.name = "Draw occlusion";
                }

                if (!__instance.shadowTexture || !__instance.shadowCasterMaterial) return false;

                float num = (float)__instance.shadowTexture.width;
                __instance.shadowCasterMaterial.SetFloat("_DepthCompressionValue", effectiveRadius);
                __instance.cb.Clear();


                var vec = __instance.child.transform.position;
                //__instance.cb.SetRenderTarget(__instance.shadowTexture);
                //if (isFirst) __instance.cb.ClearRenderTarget(true, true, new Color(1f, 1f, 1f, 1f));
                __instance.cb.SetRenderTarget(getTargetTexture(__instance));
                __instance.cb.ClearRenderTarget(true, true, new Color(1f, 1f, 1f, 1f));
                //__instance.cb.SetGlobalTexture("_ShmapTexture", __instance.shadowTexture);
                __instance.cb.SetGlobalTexture("_ShmapTexture", getTargetTexture(__instance));
                __instance.cb.SetGlobalFloat("_Radius", __instance.LightRadius);
                __instance.cb.SetGlobalFloat("_Column", 0f);
                __instance.cb.SetGlobalVector("_TexelSize", new Vector4(1f / num, 1f / num, num, num));
                __instance.cb.SetGlobalFloat("_DepthCompressionValue", effectiveRadius);
                __instance.cb.SetGlobalVector("_lightPosition", new Vector4(vec.x, vec.y, vec.z, 0.0f));
                //__instance.cb.DrawMesh(__instance.occluderMesh, Matrix4x4.identity, __instance.shadowCasterMaterial);
                //__instance.cb.Blit(getTargetTexture(__instance), __instance.shadowTexture);
                Graphics.ExecuteCommandBuffer(__instance.cb);

                //Graphics.Blit(getTargetTexture(__instance), __instance.shadowTexture);
                return false;
            }
        }

        [HarmonyPatch(typeof(LightSource), nameof(LightSource.UpdateOccMesh))]
        public static class UpdateOccMeshPatch
        {
            public static bool Prefix(LightSource __instance)
            {
                return isFirst;
            }
        }

    }
}
