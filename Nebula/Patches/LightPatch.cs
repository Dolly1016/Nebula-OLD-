namespace Nebula.Patches;

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
    /*
    [HarmonyPatch(typeof(LightSource), nameof(LightSource.Update))]
    public static class LightSourceUpdatePatch
    {
        static public float FlashLightSize = 100f;
        static public float ViewDistance = 1f;
        static public float PlayerRadius = 1f;
        public static bool Prefix(LightSource __instance)
        {
            Vector3 position = __instance.transform.position;
            position.z -= 7f;
            __instance.UpdateFlashlightAngle();
            __instance.LightCutawayMaterial.SetFloat("_PlayerRadius", PlayerRadius);
            __instance.LightCutawayMaterial.SetFloat("_LightRadius", ViewDistance);
            __instance.LightCutawayMaterial.SetVector("_LightOffset", __instance.LightOffset);
            __instance.LightCutawayMaterial.SetFloat("_FlashlightSize", FlashLightSize);
            __instance.LightCutawayMaterial.SetFloat("_FlashlightAngle", PlayerControl.LocalPlayer.FlashlightAngle);
            __instance.lightChild.transform.position = position;
            __instance.renderer.Render(position);

            return false;
        }
    }
    */
}