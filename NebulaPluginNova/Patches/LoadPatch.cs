using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nebula.Utilities;
using TMPro;
using Unity.Services.Core.Internal;

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

        while (coloadRoutine.MoveNext())
        {
            if(NebulaPlugin.LastException != null)
            {
                (var excep, var type) = NebulaPlugin.LastException.Value;

                string errorText = $"[{excep.GetType().Name}] {excep.Message}\n on {type.FullName}\n{excep.StackTrace}";
                {
                    var inner = excep;
                    while (inner.InnerException != null) { 
                        inner = inner.InnerException;
                        errorText += $"\ncaused by [{inner.GetType().Name}] {inner.Message}\n on {type.FullName}\n{inner.StackTrace}";
                    }
                }


                GameObject.Destroy(logo.gameObject);
                GameObject.Destroy(logoGlow.gameObject);
                GameObject.Destroy(loadText.gameObject);

                __instance.errorPopup.gameObject.SetActive(true);

                //__instance.errorPopup.transform.GetChild(2).GetComponent<TextMeshPro>().text = "Sorry! But the error occurred on loading.";
                __instance.errorPopup.transform.GetChild(2).gameObject.SetActive(false);
                __instance.errorPopup.transform.GetChild(3).gameObject.SetActive(false);
                __instance.errorPopup.transform.GetChild(4).transform.localPosition += new Vector3(0f,-0.8f,0f);

                var button = __instance.errorPopup.transform.GetChild(6).GetComponent<PassiveButton>();
                button.transform.localPosition += new Vector3(0f, -1f, 0f);
                
                var infoText =__instance.errorPopup.transform.GetChild(4).GetComponent<TextMeshPro>();
                infoText.alignment = TMPro.TextAlignmentOptions.TopLeft;
                infoText.rectTransform.sizeDelta = new Vector2(11f, 4f);
                infoText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                infoText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                infoText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                infoText.text = errorText;
                infoText.color = Color.white;

                var infoTextHolder = UnityHelper.CreateObject("InfoTextHolder",__instance.errorPopup.transform,infoText.transform.localPosition);
                infoText.transform.SetParent(infoTextHolder.transform, true);
                var textButton = infoTextHolder.SetUpButton(true);
                textButton.OnMouseOut.AddListener(() => infoText.color = Color.white);
                textButton.OnMouseOver.AddListener(() => infoText.color = Color.green);
                textButton.OnClick.AddListener(() => ClipboardHelper.PutClipboardString(infoText.text));
                textButton.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(11f,4f);

                //停止
                while (true) yield return null;
            }
            yield return null;
        }

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
