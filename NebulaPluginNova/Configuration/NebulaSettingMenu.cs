using Il2CppInterop.Runtime.Injection;
using System.Text;
using Nebula.Modules;
using Nebula.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using Nebula.Roles;

namespace Nebula.Configuration;

public class NebulaSettingMenu : MonoBehaviour
{
    static NebulaSettingMenu()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaSettingMenu>();
    }

    MetaScreen LeftHolder = null!, MainHolder = null!, RightHolder = null!, SecondScreen = null!, SecondTopScreen = null!;
    GameObject FirstPage = null!, SecondPage = null!;
    Scroller FirstScroller = null!, SecondScroller = null!;
    TMPro.TextMeshPro SecondTitle = null!;
    ConfigurationTab CurrentTab = null!;

    static public NebulaSettingMenu Instance { get; private set; } = null!;

    public void Start()
    {
        Instance = this;

        CurrentTab = ConfigurationTab.Settings;

        FirstPage = UnityHelper.CreateObject("FirstPage",transform,Vector3.zero);
        LeftHolder = UnityHelper.CreateObject<MetaScreen>("LeftHolder", FirstPage.transform, new Vector3(-4.1f, 0.3f));
        RightHolder = UnityHelper.CreateObject<MetaScreen>("RightHolder", FirstPage.transform, new Vector3(2.5f, 0f));

        var MainHolderParent = UnityHelper.CreateObject("Main", FirstPage.transform, new Vector3(-1.05f, -0.4f));
        MainHolderParent.AddComponent<SortingGroup>();
        var MainHolderMask = UnityHelper.CreateObject<SpriteMask>("Mask",MainHolderParent.transform, Vector3.zero);
        MainHolderMask.sprite = VanillaAsset.FullScreenSprite;
        MainHolderMask.transform.localScale = new Vector3(6f,4.5f);
        MainHolder = UnityHelper.CreateObject<MetaScreen>("MainHolder", MainHolderParent.transform, new Vector3(0f, 0f));

        FirstScroller = VanillaAsset.GenerateScroller(new Vector2(4f, 4.5f), MainHolderParent.transform, new Vector3(2.2f, -0.05f, -1f), MainHolder.transform, new FloatRange(0f, 1f),4.6f);
        UpdateLeftTab();
        UpdateMainTab();

        SecondPage = UnityHelper.CreateObject("SecondPage", transform, Vector3.zero);
        var SecondParent = UnityHelper.CreateObject("Main", SecondPage.transform, new Vector3(-0.3f, -0.7f));
        SecondParent.AddComponent<SortingGroup>();
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
        SecondTopScreen = UnityHelper.CreateObject<MetaScreen>("SecondTopScreen", SecondPage.transform, new Vector3(0f, 1.9f, -5f));

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

    private static TextAttribute RightTextAttr = new(TextAttribute.NormalAttr) { FontSize = 1.5f, FontMaxSize = 1.5f, FontMinSize = 0.7f, Size = new(3f, 5f), Alignment = TMPro.TextAlignmentOptions.TopLeft };

    private void UpdateMainTab()
    {
        MainHolder.transform.localPosition = new Vector3(0, 0, 0);
        RightHolder.SetContext(null);

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

                    button.OnMouseOver.AddListener(() =>
                    {
                        StringBuilder builder = new();
                        copiedHolder.GetShownString(ref builder);
                        RightHolder.SetContext(new(2.2f, 3.8f),new MetaContext.Text(RightTextAttr) { RawText = builder.ToString() });
                    });
                },
                Alignment = IMetaContext.AlignmentOption.Center,
                Color = (copiedHolder.IsActivated?.Invoke() ?? true) ? Color.white : new Color(0.2f,0.2f,0.2f)
            });
            context.Append(new MetaContext.VerticalMargin(0.05f));
        }

        FirstScroller.SetYBoundsMax(MainHolder.SetContext(new Vector2(3.2f, 4.5f), context) - 4.5f);
    }

    ConfigurationHolder? CurrentHolder = null;

    private static TextAttribute RelatedButtonAttr = new TextAttribute(TextAttribute.BoldAttr) { Size = new Vector2(1.1f, 0.29f) };
    private static TextAttribute RelatedInsideButtonAttr = new TextAttribute(TextAttribute.BoldAttr) { Size = new Vector2(1.1f, 0.29f), FontMaterial = VanillaAsset.StandardMaskedFontMaterial };
    public void UpdateSecondaryPage()
    {
        if(CurrentHolder == null) return;

        SecondScroller.SetYBoundsMax(SecondScreen.SetContext(new Vector2(7.8f, 4.1f), CurrentHolder.GetContext()) - 4.1f);

        List<IMetaParallelPlacable> topContents = new();
        if(CurrentHolder.RelatedAssignable != null)
        {
            var assignable = CurrentHolder.RelatedAssignable;
            if (assignable is AbstractRole role)
            {
                //Modifierを付与されうるロール

                void OpenFilterScreen(MetaScreen? screen,AbstractRole role)
                {
                    if (!screen)
                        screen = MetaScreen.GenerateWindow(new Vector2(5f, 3.2f), HudManager.Instance.transform, Vector3.zero, true, true);

                    MetaContext inner = new();
                    inner.Append(Roles.Roles.AllIntroAssignableModifiers().Where(m => role.CanLoadDefault(m)), (m) => new MetaContext.Button(() => { role.ModifierFilter!.ToggleAndShare(m); OpenFilterScreen(screen,role); }, RelatedInsideButtonAttr)
                    {
                        RawText = m.DisplayName.Color(m.RoleColor),
                        PostBuilder = (button, renderer, text) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask,
                        Alignment = IMetaContext.AlignmentOption.Center,
                        Color = role.ModifierFilter!.Contains(m) ? new Color(0.24f, 0.24f, 0.24f) : Color.white
                    }, 3, -1, 0, 0.6f);

                    screen!.SetContext(new MetaContext.ScrollView(new(5f, 3.1f), inner, true));
                }

                if (role.ModifierFilter != null) topContents.Add(new MetaContext.Button(() => OpenFilterScreen(null, role), RelatedButtonAttr) { TranslationKey = "options.role.modifierFilter" });
            }else if(assignable is IntroAssignableModifier iam)
            {
                //付与されうるModifier

                void OpenFilterScreen(MetaScreen? screen, IntroAssignableModifier modifier)
                {
                    if (!screen)
                        screen = MetaScreen.GenerateWindow(new Vector2(5f, 3.2f), HudManager.Instance.transform, Vector3.zero, true, true);

                    MetaContext inner = new();
                    inner.Append(Roles.Roles.AllRoles.Where(r=>r.ModifierFilter != null && r.CanLoadDefault(modifier)), (role) => new MetaContext.Button(() => { role.ModifierFilter!.ToggleAndShare(iam); OpenFilterScreen(screen, modifier); }, RelatedInsideButtonAttr)
                    {
                        RawText = role.DisplayName.Color(role.RoleColor),
                        PostBuilder = (button, renderer, text) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask,
                        Alignment = IMetaContext.AlignmentOption.Center,
                        Color = role.ModifierFilter!.Contains(modifier) ? new Color(0.24f, 0.24f, 0.24f) : Color.white
                    }, 3, -1, 0, 0.6f);

                    screen!.SetContext(new MetaContext.ScrollView(new(5f, 3.1f), inner, true));
                }

                topContents.Add(new MetaContext.Button(() => OpenFilterScreen(null, iam), RelatedButtonAttr) { TranslationKey = "options.role.modifierFilter" });
            }

            foreach (var related in assignable.RelatedOnConfig()) if(related.RelatedConfig != null) topContents.Add(new MetaContext.Button(() => OpenSecondaryPage(related.RelatedConfig!), RelatedButtonAttr) { RawText = related.DisplayName.Color(related.RoleColor) });
        }

        SecondTopScreen.SetContext(new Vector2(7.8f,0.4f),new CombinedContext(0.4f, topContents.ToArray()) { Alignment = IMetaContext.AlignmentOption.Right});
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
        UpdateMainTab();
    }

    private void CloseAllPage()
    {
        FirstPage.SetActive(false);
        SecondPage.SetActive(false);
    }
}
