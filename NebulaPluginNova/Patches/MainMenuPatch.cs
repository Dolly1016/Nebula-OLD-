using HarmonyLib;
using Nebula.Behaviour;
using Nebula.Modules;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;

namespace Nebula.Patches;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
public static class MainMenuSetUpPatch
{
    static private ISpriteLoader nebulaIconSprite = SpriteLoader.FromResource("Nebula.Resources.NebulaNewsIcon.png", 100f);
    static public GameObject? NebulaScreen = null;
    static public GameObject? AddonsScreen = null;
    static void Postfix(MainMenuManager __instance)
    {
        var leftPanel = __instance.mainMenuUI.transform.FindChild("AspectScaler").FindChild("LeftPanel");
        leftPanel.GetComponent<SpriteRenderer>().size += new Vector2(0f,0.5f);
        var auLogo = leftPanel.FindChild("Sizer").GetComponent<AspectSize>();
        auLogo.PercentWidth = 0.14f;
        auLogo.DoSetUp();
        auLogo.transform.localPosition += new Vector3(-0.8f, 0.25f, 0f);

        float height = __instance.newsButton.transform.localPosition.y-__instance.myAccountButton.transform.localPosition.y;

        //バニラのパネルからModのパネルに切り替え
        var reworkedPanel = UnityHelper.CreateObject<SpriteRenderer>("ReworkedLeftPanel", leftPanel, new Vector3(0f, height * 0.5f, 0f));
        var oldPanel = leftPanel.GetComponent<SpriteRenderer>();
        reworkedPanel.sprite = oldPanel.sprite;
        reworkedPanel.tileMode= oldPanel.tileMode;
        reworkedPanel.drawMode = oldPanel.drawMode;
        reworkedPanel.size = oldPanel.size;
        oldPanel.enabled = false;

        
        //CreditsとQuit以外のボタンを上に寄せる
        foreach (var button in __instance.mainButtons.GetFastEnumerator())
            if (Math.Abs(button.transform.localPosition.x) < 0.1f) button.transform.localPosition += new Vector3(0f, height, 0f);
        leftPanel.FindChild("Main Buttons").FindChild("Divider").transform.localPosition += new Vector3(0f, height, 0f);

        var nebulaButton = GameObject.Instantiate(__instance.settingsButton, __instance.settingsButton.transform.parent);
        nebulaButton.transform.localPosition += new Vector3(0f, -height, 0f);
        nebulaButton.gameObject.name = "NebulaButton";
        nebulaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) => {
            var icon = obj.transform.FindChild("Icon");
            if (icon != null)
            {
                icon.localScale = new Vector3(1f, 1f, 1f);
                icon.GetComponent<SpriteRenderer>().sprite = nebulaIconSprite.GetSprite();
            }
        }));
        var nebulaPassiveButton = nebulaButton.GetComponent<PassiveButton>();
        nebulaPassiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        nebulaPassiveButton.OnClick.AddListener(() =>
        {
            SoundManager.Instance.PlaySound(VanillaAsset.SelectClip, false, 0.8f);
            __instance.ResetScreen();
            NebulaScreen?.SetActive(true);
            __instance.screenTint.enabled = true;
        });
        nebulaButton.transform.FindChild("FontPlacer").GetChild(0).GetComponent<TextTranslatorTMP>().SetModText("title.buttons.nebula");

        NebulaScreen = GameObject.Instantiate(__instance.accountButtons, __instance.accountButtons.transform.parent);
        NebulaScreen.name = "NebulaScreen";
        NebulaScreen.transform.GetChild(0).GetChild(0).GetComponent<TextTranslatorTMP>().SetModText("title.label.nebula");
        __instance.mainButtons.Add(nebulaButton);

        GameObject.Destroy(NebulaScreen.transform.GetChild(4).gameObject);

        var temp = NebulaScreen.transform.GetChild(3);
        int index = 0;
        void SetUpButton(string button,Action clickAction)
        {
            GameObject obj = temp.gameObject;
            if (index > 0) obj = GameObject.Instantiate(obj, obj.transform.parent);

            obj.transform.GetChild(0).GetChild(0).GetComponent<TextTranslatorTMP>().SetModText(button);
            var passiveButton = obj.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener(() => {
                SoundManager.Instance.PlaySound(VanillaAsset.SelectClip, false, 0.8f);
                clickAction.Invoke();
            });
            obj.transform.localPosition = new Vector3(0f, 0.66f - index * 0.68f, 0f);

            index++;
        }

        SetUpButton("title.buttons.update", () => { });
        SetUpButton("title.buttons.addons", () => {
            __instance.ResetScreen();
            if (!AddonsScreen) CreateAddonsScreen();
            AddonsScreen?.SetActive(true);
            __instance.screenTint.enabled = true;
        });
        SetUpButton("title.buttons.developersStudio", () => {
            DevStudio.Open(__instance);
        });

        void CreateAddonsScreen()
        {
            AddonsScreen = UnityHelper.CreateObject("Addons", __instance.accountButtons.transform.parent, new Vector3(0, 0, -1f));
            AddonsScreen.transform.localScale = NebulaScreen!.transform.localScale;

            var screen = MetaScreen.GenerateScreen(new Vector2(6.2f, 4.1f), AddonsScreen.transform, new Vector3(-0.1f, 0, 0f), false, false, false);

            TextAttribute NameAttribute = new(TextAttribute.BoldAttr) { 
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                Size = new Vector2(3.4f,0.3f),
                Alignment = TMPro.TextAlignmentOptions.Left
            };

            TextAttribute VersionAttribute = new TextAttribute(TextAttribute.NormalAttr)
            {
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                Size = new Vector2(0.8f, 0.3f),
                Alignment = TMPro.TextAlignmentOptions.Left
            }.EditFontSize(1.4f, 1f, 1.4f);
            TextAttribute AuthorAttribute = new TextAttribute(TextAttribute.NormalAttr)
            {
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                Size = new Vector2(1.2f, 0.3f),
                Alignment = TMPro.TextAlignmentOptions.Left
            }.EditFontSize(1.4f, 1f, 1.4f);

            TextAttribute DescAttribute = new TextAttribute(TextAttribute.NormalAttr) {
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial, 
                Alignment = TMPro.TextAlignmentOptions.TopLeft,
                Size = new Vector2(5.8f,0.4f),
                FontSize = 1.2f, FontMaxSize = 1.2f, FontMinSize = 0.7f 
            };


            var inner = new MetaContext();
            foreach (var addon in NebulaAddon.AllAddons)
            {
                inner.Append(new CombinedContext(0.5f,
                    new MetaContext.Image(addon.Icon) { Width = 0.3f },
                    new MetaContext.HorizonalMargin(0.1f),
                    new MetaContext.Text(NameAttribute) { RawText = addon.AddonName },
                    new MetaContext.Text(VersionAttribute) { RawText = addon.Version },
                    new MetaContext.Text(AuthorAttribute) { RawText = "by " + addon.Author })
                { Alignment = IMetaContext.AlignmentOption.Left });
                inner.Append(new MetaContext.Text(DescAttribute) { RawText = addon.Description });
            }
            screen.SetContext(new MetaContext.ScrollView(new Vector2(6.2f, 4.1f), inner, false) { Alignment = IMetaContext.AlignmentOption.Center });
        }

        foreach (var obj in GameObject.FindObjectsOfType<GameObject>(true)) {
            if (obj.name is "FreePlayButton" or "HowToPlayButton") GameObject.Destroy(obj);
        }

    }
}

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.ResetScreen))]
public static class MainMenuClearScreenPatch
{
    public static void Postfix(MainMenuManager __instance)
    {
        if (MainMenuSetUpPatch.NebulaScreen) MainMenuSetUpPatch.NebulaScreen?.SetActive(false);
        if (MainMenuSetUpPatch.AddonsScreen) MainMenuSetUpPatch.AddonsScreen?.SetActive(false);
    }
}