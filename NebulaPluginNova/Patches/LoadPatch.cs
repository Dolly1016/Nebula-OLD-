using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nebula.Utilities;

namespace Nebula.Patches;

[HarmonyPatch(typeof(SplashManager),nameof(SplashManager.Update))]
public static class LoadPatch
{
    static SpriteLoader logoSprite = SpriteLoader.FromResource("Nebula.Resources.NebulaLogo.png", 100f);
    static SpriteLoader logoGlowSprite = SpriteLoader.FromResource("Nebula.Resources.NebulaLogoGlow.png", 100f);
    static TMPro.TextMeshPro loadText = null!;
    static public string LoadingText { set { loadText.text = value; } }
    static IEnumerator CoLoadNebula(SplashManager __instance)
    {
        var logo = UnityHelper.CreateObject<SpriteRenderer>("NebulaLogo", null, new Vector3(0, 0.2f, -5f));
        var logoGlow = UnityHelper.CreateObject<SpriteRenderer>("NebulaLogoGlow", null, new Vector3(0, 0.2f, -5f));
        logo.sprite = logoSprite.GetSprite();
        logoGlow.sprite = logoGlowSprite.GetSprite();

        StackfullCoroutine coloadRoutine = new(NebulaPlugin.MyPlugin.CoLoad());

        

        float p = 1f;
        while (p > 0f)
        {
            p -= Time.deltaTime * 2.8f;
            float alpha = 1 - p;
            logo.color = Color.white.AlphaMultiplied(alpha);
            logoGlow.color = Color.white.AlphaMultiplied(Mathf.Min(1f, alpha * (p * 2)));
            logo.transform.localScale = Vector3.one * (p * p * 0.012f + 1f);
            logoGlow.transform.localScale = Vector3.one * (p * p * 0.012f + 1f);
            yield return null;
        }
        logo.color = Color.white;
        logoGlow.gameObject.SetActive(false);
        logo.transform.localScale = Vector3.one;

        loadText = GameObject.Instantiate(__instance.errorPopup.InfoText, null);
        loadText.transform.localPosition = new(0f, -0.28f, -10f);
        loadText.fontStyle = TMPro.FontStyles.Bold;
        loadText.text = "Loading...";
        loadText.color = Color.white.AlphaMultiplied(0.3f);

        while (coloadRoutine.MoveNext()) yield return null;

        loadText.text = "Loading Completed";
        for(int i = 0; i < 3; i++)
        {
            loadText.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.03f);
            loadText.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.03f);
        }

        GameObject.Destroy(loadText.gameObject);

        p = 1f;
        while (p > 0f)
        {
            p -= Time.deltaTime * 1.2f;
            logo.color = Color.white.AlphaMultiplied(p);
            yield return null;
        }
        logo.color = Color.clear;
        

        __instance.sceneChanger.AllowFinishLoadingScene();
        __instance.startedSceneLoad = true;
    }

    static bool loadedNebula = false;
    public static bool Prefix(SplashManager __instance)
    {
        if (__instance.doneLoadingRefdata && !__instance.startedSceneLoad && Time.time - __instance.startTime > __instance.minimumSecondsBeforeSceneChange && !loadedNebula)
        {
            loadedNebula = true;
            __instance.StartCoroutine(CoLoadNebula(__instance).WrapToIl2Cpp());
        }

        return false;
    }
}
