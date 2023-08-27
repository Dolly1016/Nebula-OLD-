using Il2CppInterop.Runtime.Injection;
using Nebula.Modules;
using UnityEngine;

namespace Nebula.Configuration;

public class NebulaSettingMenu : MonoBehaviour
{
    static NebulaSettingMenu()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaSettingMenu>();
    }

    MetaScreen LeftHolder, MainHolder, SecondScreen;
    GameObject FirstPage, SecondPage;
    Scroller FirstScroller, SecondScroller;
    TMPro.TextMeshPro SecondTitle;
    ConfigurationTab CurrentTab;

    static public NebulaSettingMenu Instance { get;private set; }

    public void Start()
    {
        Instance = this;

        CurrentTab = ConfigurationTab.Settings;

        FirstPage = UnityHelper.CreateObject("FirstPage",transform,Vector3.zero);
        LeftHolder = UnityHelper.CreateObject<MetaScreen>("LeftHolder", FirstPage.transform, new Vector3(-4.1f, 0.3f));

        var MainHolderParent = UnityHelper.CreateObject("Main", FirstPage.transform, new Vector3(-1.05f, -0.4f));
        var MainHolderMask = UnityHelper.CreateObject<SpriteMask>("Mask",MainHolderParent.transform, Vector3.zero);
        MainHolderMask.sprite = VanillaAsset.FullScreenSprite;
        MainHolderMask.transform.localScale = new Vector3(6f,4.5f);
        MainHolder = UnityHelper.CreateObject<MetaScreen>("MainHolder", MainHolderParent.transform, new Vector3(0f, 0f));

        FirstScroller = VanillaAsset.GenerateScroller(new Vector2(4f, 4.5f), MainHolderParent.transform, new Vector3(2.2f, -0.05f, -1f), MainHolder.transform, new FloatRange(0f, 1f),4.6f);
        UpdateLeftTab();
        UpdateMainTab();

        SecondPage = UnityHelper.CreateObject("SecondPage", transform, Vector3.zero);
        var SecondParent = UnityHelper.CreateObject("Main", SecondPage.transform, new Vector3(-0.3f, -0.7f));
        var SecondMask = UnityHelper.CreateObject<SpriteMask>("Mask", SecondParent.transform, Vector3.zero);
        SecondMask.sprite = VanillaAsset.FullScreenSprite;
        SecondMask.transform.localScale = new Vector3(8f, 4.1f);
        SecondScreen = UnityHelper.CreateObject<MetaScreen>("SecondScreen", SecondParent.transform, new Vector3(0f, 0f, -5f));
        SecondScroller = VanillaAsset.GenerateScroller(new Vector2(8f, 4.1f), SecondParent.transform, new Vector3(4.2f, -0.05f, -1f), SecondScreen.transform, new FloatRange(0f, 1f), 4.2f);

        //左上タイトルと戻るボタン
        SecondTitle = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, SecondPage.transform);
        TextAttribute.TitleAttr.Reflect(SecondTitle);
        SecondTitle.text = "Configuration Title";
        SecondTitle.transform.localPosition = new Vector3(-2.8f, 1.9f, -10f);
        new MetaContext.Button(() => OpenFirstPage(), new(TextAttribute.BoldAttr) { Size = new Vector2(0.4f, 0.28f) }) { RawText = "<<" }.Generate(SecondPage, new Vector2(-4.8f, 1.9f));

        OpenFirstPage();
    }

    private void UpdateLeftTab()
    {
        MetaContext context = new();

        context.Append(new MetaContext.Text(new(TextAttribute.BoldAttr)) { RawText = Language.Translate("options.gamemode"), Alignment = IMetaContext.AlignmentOption.Center });
        context.Append(new MetaContext.Button(() => {
            GeneralConfigurations.GameModeOption.ChangeValue(true);
            UpdateMainTab();
            UpdateLeftTab();
        }, new(TextAttribute.BoldAttr) { Size = new(1.5f, 0.3f) }) { RawText = Language.Translate(GeneralConfigurations.CurrentGameMode.TranslateKey),Alignment = IMetaContext.AlignmentOption.Center});
        context.Append(new MetaContext.VerticalMargin(0.2f));
        foreach (var tab in ConfigurationTab.AllTab)
        {
            ConfigurationTab copiedTab = tab;
            context.Append(
                new MetaContext.Button(() => {
                    CurrentTab = copiedTab;
                    UpdateMainTab();
                }, new(TextAttribute.BoldAttr) { Size = new(1.7f, 0.26f) })
                {
                    RawText = tab.DisplayName,
                    Alignment = IMetaContext.AlignmentOption.Center,
                    PostBuilder = (button, renderer, text) =>
                    {
                        renderer.color = tab.Color;
                        button.OnMouseOut.AddListener(() => { renderer.color = tab.Color; });
                    }
                }
                );
        }
        LeftHolder.SetContext(new Vector2(2f, 3f), context);
    }

    private void UpdateMainTab()
    {
        MainHolder.transform.localPosition = new Vector3(0, 0, 0);

        MetaContext context = new();

        TextAttribute mainTextAttr = new(TextAttribute.BoldAttr)
        {
            FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
            Size = new Vector2(3.2f, 0.62f),
            FontMaxSize = 2.5f,
            FontSize=2.5f,
            Alignment = TMPro.TextAlignmentOptions.TopLeft
        };
        TextAttribute subTextAttr = new(TextAttribute.NormalAttr) {
            FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
            Size = new Vector2(3.1f,0.3f),
            FontMaxSize = 1.2f,
            FontSize = 1.2f,
            Alignment = TMPro.TextAlignmentOptions.BottomRight
        };

        foreach (var holder in ConfigurationHolder.AllHolders)
        {
            var copiedHolder = holder;

            if (!holder.IsShown || ((holder.TabMask & CurrentTab) == 0) || (holder.GameModeMask & GeneralConfigurations.CurrentGameMode) == 0) continue;

            context.Append(new MetaContext.Button(() => OpenSecondaryPage(copiedHolder), mainTextAttr)
            {
                RawText = holder.Title.Text,
                PostBuilder = (button, renderer, text) => { 
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    text.transform.localPosition += new Vector3(0.03f, -0.03f, 0f);

                    var subTxt = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, renderer.transform);
                    subTextAttr.Reflect(subTxt);
                    subTxt.text = Language.Translate(holder.Id + ".detail");
                    subTxt.transform.localPosition = new Vector3(0f, -0.15f, -0.5f);
                    subTxt.sortingOrder = 30;
                },
                Alignment = IMetaContext.AlignmentOption.Center
            });
            context.Append(new MetaContext.VerticalMargin(0.05f));

        }

        FirstScroller.SetYBoundsMax(MainHolder.SetContext(new Vector2(3.2f, 4.5f), context) - 4.5f);
    }

    ConfigurationHolder? CurrentHolder = null;
    public void UpdateSecondaryPage()
    {
        if(CurrentHolder == null) return;

        SecondScroller.SetYBoundsMax(SecondScreen.SetContext(new Vector2(7.8f, 4.1f), CurrentHolder.GetContext()) - 4.1f);
        SecondTitle.text = CurrentHolder.Title.Text;
    }

    private void OpenSecondaryPage(ConfigurationHolder holder) 
    {
        CloseAllPage();
        SecondPage.SetActive(true);

        CurrentHolder = holder;
        UpdateSecondaryPage();
    }

    private void OpenFirstPage()
    {
        CloseAllPage();
        FirstPage.SetActive(true);
    }

    private void CloseAllPage()
    {
        FirstPage.SetActive(false);
        SecondPage.SetActive(false);
    }
}
