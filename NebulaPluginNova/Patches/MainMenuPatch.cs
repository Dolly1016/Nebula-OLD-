using Nebula.Behaviour;

namespace Nebula.Patches;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
public static class MainMenuSetUpPatch
{
    static private ISpriteLoader nebulaIconSprite = SpriteLoader.FromResource("Nebula.Resources.NebulaNewsIcon.png", 100f);
    static public GameObject? NebulaScreen = null;
    static public GameObject? AddonsScreen = null;
    static public GameObject? VersionsScreen = null;

    static public bool IsLocalGame = false;

    static void Postfix(MainMenuManager __instance)
    {
        __instance.PlayOnlineButton.OnClick.AddListener(() => IsLocalGame = false);
        __instance.playLocalButton.OnClick.AddListener(() => IsLocalGame = true);

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

        SetUpButton("title.buttons.update", () => {
            __instance.ResetScreen();
            if (!VersionsScreen) CreateVersionsScreen();
            VersionsScreen?.SetActive(true);
            __instance.screenTint.enabled = true;
        });
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
            screen.SetContext(new MetaContext.ScrollView(new Vector2(6.2f, 4.1f), inner, true) { Alignment = IMetaContext.AlignmentOption.Center });
        }

        void CreateVersionsScreen()
        {
            VersionsScreen = UnityHelper.CreateObject("Versions", __instance.accountButtons.transform.parent, new Vector3(0, 0, -1f));
            VersionsScreen.transform.localScale = NebulaScreen!.transform.localScale;

            var screen = MetaScreen.GenerateScreen(new Vector2(6.2f, 4.1f), VersionsScreen.transform, new Vector3(-0.1f, 0, 0f), false, false, false);

            TextAttribute NameAttribute = new(TextAttribute.BoldAttr)
            {
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                Size = new Vector2(2.2f, 0.3f),
                Alignment = TMPro.TextAlignmentOptions.Left
            };

            TextAttribute CategoryAttribute = new(TextAttribute.BoldAttr)
            {
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                Size = new Vector2(0.8f, 0.3f),
                Alignment = TMPro.TextAlignmentOptions.Center
            };
            CategoryAttribute.EditFontSize(1.2f,0.6f,1.2f);

            TextAttribute ButtonAttribute = new(TextAttribute.BoldAttr)
            {
                FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                Size = new Vector2(1f, 0.2f),
                Alignment = TMPro.TextAlignmentOptions.Center
            };

            Reference<MetaContext.ScrollView.InnerScreen> innerRef = new();
            List<ModUpdater.ReleasedInfo>? versions = null;
            MetaContext staticContext = new();

            MetaContext menuContext = new();
            menuContext.Append(Enum.GetValues<ModUpdater.ReleasedInfo.ReleaseCategory>(), (category) =>
            new MetaContext.Button(() => UpdateContents(category), new(TextAttribute.BoldAttr) { Size = new(0.95f, 0.28f) }) { TranslationKey = ModUpdater.ReleasedInfo.CategoryTranslationKeys[(int)category] }
                , 1, -1, 0, 0.6f);

            staticContext.Append(new ParallelContext(
                new(new MetaContext.HorizonalMargin(0.1f),0.1f),
                new(menuContext,1f),
                new(new MetaContext.HorizonalMargin(0.1f), 0.1f),
                new(new MetaContext.ScrollView(new Vector2(5f, 4f), new MetaContext(), true) { Alignment = IMetaContext.AlignmentOption.Center, InnerRef = innerRef },5f)));
            
            screen.SetContext(staticContext);

            innerRef.Value?.SetLoadingContext();

            void UpdateContents(ModUpdater.ReleasedInfo.ReleaseCategory? category = null)
            {
                if (versions == null) return;

                var inner = new MetaContext();

                foreach (var version in versions)
                {
                    if (category != null && version.Category != category) continue;

                    List<IMetaParallelPlacable> placable = new();
                    placable.Add(new MetaContext.Text(CategoryAttribute) { MyText = ITextComponent.From(ModUpdater.ReleasedInfo.CategoryTranslationKeys[(int)version.Category], ModUpdater.ReleasedInfo.CategoryColors[(int)version.Category])});
                    placable.Add(new MetaContext.HorizonalMargin(0.15f));
                    placable.Add(new MetaContext.Text(NameAttribute) { RawText = version.Version!.Replace('_', ' '),
                        PostBuilder = text => {
                            var button = text.gameObject.SetUpButton(true);
                            button.gameObject.AddComponent<BoxCollider2D>().size = text.rectTransform.sizeDelta;
                            button.OnClick.AddListener(() => Application.OpenURL("https://github.com/Dolly1016/Nebula/releases/tag/" + version.RawTag));
                            button.OnMouseOver.AddListener(() => {
                                text.color = Color.green;
                            });
                            button.OnMouseOut.AddListener(() => {
                                text.color = Color.white;
                            });
                        }
                    });
                    placable.Add(new MetaContext.HorizonalMargin(0.15f));

                    if (version.Epoch == NebulaPlugin.PluginEpoch && version.BuildNum != NebulaPlugin.PluginBuildNum)
                    {
                        placable.Add(new MetaContext.Button(()=>NebulaManager.Instance.StartCoroutine(version.CoUpdateAndShowDialog().WrapToIl2Cpp()), ButtonAttribute) { TranslationKey = "version.fetching.gainPackage", PostBuilder = (_, renderer, _) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask });
                    }
                    else
                    {
                        placable.Add(new MetaContext.HorizonalMargin(0.13f));
                        placable.Add(new MetaContext.Text(ButtonAttribute) { TranslationKey = version.Epoch == NebulaPlugin.PluginEpoch ? "version.fetching.current" : "version.fetching.mismatched", });
                    }
                    inner.Append(new CombinedContext(0.5f, placable.ToArray()) { Alignment = IMetaContext.AlignmentOption.Left });
                }

                innerRef.Value?.SetContext(inner);
            }


            NebulaManager.Instance.StartCoroutine(ModUpdater.CoFetchVersionTags((list) =>
            {
                versions = list;
                UpdateContents();
            }).WrapToIl2Cpp());
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
        if (MainMenuSetUpPatch.VersionsScreen) MainMenuSetUpPatch.VersionsScreen?.SetActive(false);
    }
}

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
class ServerVersionPatch
{
    static void Postfix(ref int __result)
    {
        if(!MainMenuSetUpPatch.IsLocalGame) __result += 25;
    }
}