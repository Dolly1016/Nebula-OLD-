using Il2CppInterop.Runtime.Injection;
using JetBrains.Annotations;
using Nebula.Modules;
using Nebula.Utilities;
using System.IO;
using System;
using static Il2CppSystem.Linq.Expressions.Interpreter.InitializeLocalInstruction;
using static Nebula.Modules.NebulaAddon;
using System.Text;
using System.IO.Compression;

namespace Nebula.Behaviour;

public class DevStudio : MonoBehaviour
{
    static DevStudio() => ClassInjector.RegisterTypeInIl2Cpp<DevStudio>();
    static public MainMenuManager? MainMenu;

    private MetaScreen myScreen = null!;

    private List<Func<(IMetaContext context, Action? postAction,Func<bool>? confirm)>> screenLayer = new();
    private Func<bool>? currentConfirm = null;
    private const float ScreenWidth = 9f;

    private bool HasContent => screenLayer.Count > 0;

    private void ChangeScreen(bool reopen, bool surely = false)
    {
        if (screenLayer.Count == 0) return;
        var content = screenLayer[screenLayer.Count - 1];

        //falseを返す場合は画面を遷移させない
        if (!surely && !(currentConfirm?.Invoke() ?? true)) return;

        if (!reopen)
        {
            screenLayer.RemoveAt(screenLayer.Count - 1);
            currentConfirm = null;
            NebulaManager.Instance.HideHelpContext();
        }

        if (HasContent)
            OpenFrontScreen();
        else
            Close();

    }

    private void CloseScreen(bool surely = false) => ChangeScreen(false, surely);
    private void ReopenScreen(bool surely = false) => ChangeScreen(true, surely);

    private void OpenFrontScreen()
    {
        if (screenLayer.Count == 0) return;
        var content = screenLayer[screenLayer.Count - 1].Invoke();


        myScreen.SetContext(new Vector2(ScreenWidth, 5.5f), content.context);
        content.postAction?.Invoke();
        currentConfirm = content.confirm;
    }

    private void OpenScreen(Func<(IMetaContext context, Action? postAction, Func<bool>? confirm)> content)
    {
        screenLayer.Add(content);
        OpenFrontScreen();
    }

    protected void Close()
    {
        TransitionFade.Instance.DoTransitionFade(gameObject, null!, () => MainMenu?.mainMenuUI.SetActive(true), () => GameObject.Destroy(gameObject));
    }

    static public void Open(MainMenuManager mainMenu)
    {
        MainMenu = mainMenu;

        var obj = UnityHelper.CreateObject<DevStudio>("DevStudioMenu", Camera.main.transform, new Vector3(0, 0, -30f));
        TransitionFade.Instance.DoTransitionFade(null!, obj.gameObject, () => { mainMenu.mainMenuUI.SetActive(false); }, () => { obj.OnShown(); });
    }

    public void OnShown() => OpenScreen(ShowMainScreen);

    public void Awake()
    {
        if (MainMenu != null)
        {
            var backBlackPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(1);
            GameObject.Instantiate(backBlackPrefab.gameObject, transform);
            var backGroundPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(2);
            var backGround = GameObject.Instantiate(backGroundPrefab.gameObject, transform);
            GameObject.Destroy(backGround.transform.GetChild(2).gameObject);

            var closeButtonPrefab = MainMenu.playerCustomizationPrefab.transform.GetChild(0).GetChild(0);
            var closeButton = GameObject.Instantiate(closeButtonPrefab.gameObject, transform);
            GameObject.Destroy(closeButton.GetComponent<AspectPosition>());
            var button = closeButton.GetComponent<PassiveButton>();
            button.gameObject.SetActive(true);
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.OnClick.AddListener(()=>CloseScreen());
            button.transform.localPosition = new Vector3(-4.9733f, 2.6708f, -50f);
        }

        myScreen = UnityHelper.CreateObject<MetaScreen>("Screen", transform, new Vector3(0, -0.1f, -10f));
    }
    
    public (IMetaContext context, Action? postAction, Func<bool>? confirm) ShowMainScreen()
    {
        void CheckAndGenerateAddon(Il2CppArgument<MetaScreen> editScreen, Il2CppArgument<TextField> id, Il2CppArgument<TextField> name, Il2CppArgument<TextField> author, Il2CppArgument<TextField> desc)
        {
            if (id.Value.Text.Length < 1 || name.Value.Text.Length < 1)
            {
                id.Value.SetHint(Language.Translate("devStudio.ui.hint.requiredText").Color(Color.red * 0.7f));
                name.Value.SetHint(Language.Translate("devStudio.ui.hint.requiredText").Color(Color.red * 0.7f));
                return;
            }
            if (Directory.Exists("Addons/" + id.Value.Text))
            {
                id.Value.SetText("");
                id.Value.SetHint(Language.Translate("devStudio.ui.hint.duplicatedId").Color(Color.red * 0.7f));
                return;
            }

            Directory.CreateDirectory("Addons/" + id.Value.Text);
            using (var stream = new StreamWriter(File.Create("Addons/" + id.Value.Text + "/addon.meta"), Encoding.UTF8))
            {
                stream.Write(JsonStructure.Serialize(new AddonMeta() { Id = id.Value.Text, Name = name.Value.Text, Author = author.Value.Text, Version = "1.0", Description = desc.Value.Text }));
            }
            editScreen.Value.CloseScreen();

            OpenScreen(() => ShowAddonScreen(new DevAddon(name.Value.Text, "Addons/" + id.Value.Text)));
        }

        MetaContext context = new();

        context.Append(new MetaContext.Text(new TextAttribute(TextAttribute.TitleAttr) { Font = VanillaAsset.BrookFont, Styles = TMPro.FontStyles.Normal, Size = new(3f, 0.45f) }.EditFontSize(5.2f)) { TranslationKey = "devStudio.ui.main.title" });
        context.Append(new MetaContext.VerticalMargin(0.2f));

        //Add-ons
        context.Append(new MetaContext.Button(() => 
        {
            var screen = MetaScreen.GenerateWindow(new(5.9f, 3.1f), transform, Vector3.zero, true, false);
            MetaContext context = new();

            CombinedContext GenerateContext(Reference<TextField> reference, string rawText,bool isMultiline,Predicate<char> predicate)=> 
            new CombinedContext(
               new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Left, Size = new(1.5f, 0.3f) }) { RawText = rawText },
               new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new(0.2f, 0.3f) }) { RawText = ":" },
               new MetaContext.TextInput(isMultiline ? 2 : 1, 2f, new(3.7f, isMultiline ? 0.58f : 0.3f)) { TextFieldRef = reference, TextPredicate = predicate }
                   );

            Reference<TextField> refId = new(), refName = new(), refAuthor = new(), refDesc = new();
            context.Append(GenerateContext(refId, "Add-on ID", false, TextField.IdPredicate));
            context.Append(GenerateContext(refName, "Name", false, TextField.JsonStringPredicate));
            context.Append(GenerateContext(refAuthor, "Author", false, TextField.JsonStringPredicate));
            context.Append(GenerateContext(refDesc, "Description", true, TextField.JsonStringPredicate));
            context.Append(new MetaContext.VerticalMargin(0.16f));
            context.Append(new MetaContext.Button(() => 
            {
                CheckAndGenerateAddon(screen, refId.Value!, refName.Value!, refAuthor.Value!, refDesc.Value!);
            }, new(TextAttribute.BoldAttr) { Size = new(1.8f, 0.3f) }) { TranslationKey = "devStudio.ui.common.generate", Alignment = IMetaContext.AlignmentOption.Center });

            screen.SetContext(context);
            refId.Value!.InputPredicate = TextField.TokenPredicate;
            TextField.EditFirstField();

        }, new TextAttribute(TextAttribute.BoldAttr) { Size = new(0.34f, 0.18f) }.EditFontSize(2.4f)) { RawText = "+" });

        Reference<MetaContext.ScrollView.InnerScreen> addonsRef = new();
        context.Append(new MetaContext.ScrollView(new Vector2(9f, 4f), addonsRef));

        IEnumerator CoLoadAddons()
        {
            yield return addonsRef.Wait();
            
            addonsRef.Value?.SetLoadingContext();

            var task = DevAddon.SearchDevAddonsAsync();
            yield return task.WaitAsCoroutine();
            
            MetaContext inner = new();
            foreach (var addon in task.Result)
            {
                inner.Append(
                    new CombinedContext(
                        new MetaContext.Text(
                            new(TextAttribute.NormalAttr)
                            {
                                FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                                Size = new(4.6f, 0.27f),
                                Alignment = TMPro.TextAlignmentOptions.Left
                            })
                        { RawText = addon.Name },
                        new MetaContext.VerticalMargin(0.3f),
                        new MetaContext.Button(() => OpenScreen(() => ShowAddonScreen(addon)),
                         new(TextAttribute.NormalAttr)
                         {
                             FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                             Size = new(1f, 0.2f),
                         })
                        { TranslationKey = "devStudio.ui.common.edit" }.SetAsMaskedButton(),
                        new MetaContext.HorizonalMargin(0.1f),
                        new MetaContext.Button(() => ShowConfirmWindow(() => { Helpers.DeleteDirectoryWithInnerFiles(addon.FolderPath); ReopenScreen(true); }, () => { }, Language.Translate("devStudio.ui.common.confirmDeletingAddon") + $"<br>\"{addon.Name}\""),
                         new(TextAttribute.NormalAttr)
                         {
                             FontMaterial = VanillaAsset.StandardMaskedFontMaterial,
                             Size = new(1f, 0.2f),
                         })
                        { Text = ITextComponent.From("devStudio.ui.common.delete", Color.red.RGBMultiplied(0.7f)), Color = Color.red.RGBMultiplied(0.7f) }.SetAsMaskedButton()

                         )
                    { Alignment = IMetaContext.AlignmentOption.Left }
                    );
            }

            addonsRef.Value?.SetContext(inner);
        }

        return (context, ()=>StartCoroutine(CoLoadAddons().WrapToIl2Cpp()), null);
    }

    public (IMetaContext context, Action? postAction, Func<bool>? confirm) ShowAddonScreen(DevAddon addon)
    {
        MetaContext context = new();

        void ShowNameEditWindow() {
            var screen = MetaScreen.GenerateWindow(new(3.9f, 1.14f), transform, Vector3.zero, true, false);
            MetaContext context = new();
            Reference<TextField> refName = new();

            context.Append(new MetaContext.TextInput(1, 2f, new(3.7f, 0.3f)) { TextFieldRef = refName, DefaultText = addon.Name, TextPredicate = TextField.JsonStringPredicate });
            context.Append(new MetaContext.Button(() =>
            {
                addon.MetaSetting.Name = refName.Value!.Text;
                UpdateMetaInfo();
                addon.SaveMetaSetting();
                screen.CloseScreen();
                ReopenScreen(true);
            }
            , new(TextAttribute.BoldAttr) { Size = new(1.8f, 0.3f) })
            { TranslationKey = "devStudio.ui.common.save", Alignment = IMetaContext.AlignmentOption.Center });

            screen.SetContext(context);
        }

        Reference<TextField> authorRef = new();
        Reference<TextField> versionRef = new();
        Reference<TextField> descRef = new();

        //Addon Name
        context.Append(
            new CombinedContext(
                new MetaContext.Button(ShowNameEditWindow, new(TextAttribute.BoldAttr) { Size = new(0.5f, 0.22f) }) { TranslationKey = "devStudio.ui.common.edit" },
                new MetaContext.HorizonalMargin(0.14f),
                new MetaContext.Text(new TextAttribute(TextAttribute.TitleAttr) { Styles = TMPro.FontStyles.Normal, Size = new(3f, 0.45f) }.EditFontSize(2.7f)) { RawText = addon.Name }
            ){ Alignment = IMetaContext.AlignmentOption.Left});

        //Author & Version
        context.Append( new CombinedContext(
            new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Left, Size = new(1.5f, 0.3f) }) { RawText = "Author" },
            new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new(0.2f, 0.3f) }) { RawText = ":" },
            new MetaContext.TextInput(1, 2f, new(2.5f, 0.3f)) { TextFieldRef = authorRef, DefaultText = addon.MetaSetting.Author, TextPredicate = TextField.JsonStringPredicate },

            new MetaContext.HorizonalMargin(0.4f),

            new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Left, Size = new(1.45f, 0.3f) }) { RawText = "Version" },
            new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new(0.2f, 0.3f) }) { RawText = ":" },
            new MetaContext.TextInput(1, 2f, new(1.5f, 0.3f)) { TextFieldRef = versionRef, DefaultText = addon.MetaSetting.Version, TextPredicate = TextField.NumberPredicate }
               )
        { Alignment = IMetaContext.AlignmentOption.Left });

        //Description
        context.Append(new CombinedContext(
            new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Left, Size = new(1.5f, 0.3f) }) { RawText = "Description" },
            new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new(0.2f, 0.3f) }) { RawText = ":" },
            new MetaContext.TextInput(2, 2f, new(6.2f, 0.58f)) { TextFieldRef = descRef, DefaultText = addon.MetaSetting.Description.Replace("<br>","\r"), TextPredicate = TextField.JsonStringPredicate }
               ){ Alignment = IMetaContext.AlignmentOption.Left });

        bool MetaInfoDirty() => addon.MetaSetting.Author != authorRef.Value!.Text || addon.MetaSetting.Version != versionRef.Value!.Text || addon.MetaSetting.Description != descRef.Value!.Text.Replace("\r", "<br>");

        void UpdateMetaInfo()
        {
            addon.MetaSetting.Author = authorRef.Value!.Text;
            addon.MetaSetting.Version = versionRef.Value!.Text;
            addon.MetaSetting.Description = descRef.Value!.Text.Replace("\r", "<br>");
        }

        //Contents of add-on
        (string translationKey,Func<DevAddon,(IMetaContext context, Action? postAction, Func<bool>? confirm)>)[] edtiors = {
            ("devStudio.ui.addon.document",ShowDocumentScreen)
        };

        context.Append(new MetaContext.VerticalMargin(0.21f));
        context.Append(edtiors, (entry) => new MetaContext.Button(() => { UpdateMetaInfo(); addon.SaveMetaSetting(); OpenScreen(() => entry.Item2.Invoke(addon)); }, new(TextAttribute.BoldAttr) { Size = new(2.4f, 0.55f) }) { TranslationKey = entry.translationKey }, 3, 3, 0, 0.85f, true);
        context.Append(new MetaContext.VerticalMargin(0.2f));

        //Build
        context.Append(new MetaContext.Button(() => addon.BuildAddon(), new TextAttribute(TextAttribute.BoldAttr)) { TranslationKey = "devStudio.ui.addon.build", Alignment = IMetaContext.AlignmentOption.Right });

        return (context, null, () => {
            if (!MetaInfoDirty()) return true;
            ShowConfirmWindow(()=> { UpdateMetaInfo(); addon.SaveMetaSetting(); CloseScreen(true); }, () => { CloseScreen(true); },  Language.Translate("devStudio.ui.common.confirmSaving"));
            return false;
        }
        );
    }

    private (IMetaContext context, Action? postAction, Func<bool>? confirm) ShowDocumentEditorScreen(DevAddon addon, string path, string id, SerializableDocument doc)
    {
        void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, doc.Serialize(), Encoding.UTF8);
        }

        void NewContentEditor(SerializableDocument targetContainer)
        {
            MetaContext context = new();

            (string id, Func<SerializableDocument> generator)[] variations = {
                ("contents", ()=>new SerializableDocument(){ Contents = new() }),
                ("aligned", ()=>new SerializableDocument(){ Aligned = new() }),
                ("localizedText", ()=>new SerializableDocument(){ TranslationKey = "undefined", IsVariable = true }),
                ("rawText", ()=>new SerializableDocument(){ RawText = "Text", IsVariable = true }),
                ("image", ()=>new SerializableDocument(){ Image = "Nebula::NebulaImage", Width = 0.25f }),
                ("vertical", ()=>new SerializableDocument(){ VSpace = 0.5f }),
                ("horizontal", ()=>new SerializableDocument(){ HSpace = 0.5f }),
                ("documentRef", ()=>new SerializableDocument(){ Document = new(){ Id = "", Arguments = new() } })
            };

            context.Append(variations, (entry) =>
            new MetaContext.Button(() =>
            {
                targetContainer.AppendContent(entry.generator.Invoke());
                NebulaManager.Instance.HideHelpContext();
                ReopenScreen(true);
            }, new(TextAttribute.BoldAttr) { Size = new(1.2f, 0.24f) })
            { TranslationKey = "devStudio.ui.document.element." + entry.id, Alignment = IMetaContext.AlignmentOption.Left },
            2, -1, 0, 0.52f, false, IMetaContext.AlignmentOption.Left);

            NebulaManager.Instance.SetHelpContext(null, context);
        }

        void ShowContentEditor(PassiveButton editorButton, SerializableDocument doc, SerializableDocument? parent)
        {
            MetaContext context = new();

            MetaContext.Button GetButton(Action clickAction, string rawText, bool reopenScreen = true, bool useBoldFont = false, bool asMasked = false)
            {
                var attr = new TextAttribute(useBoldFont ? TextAttribute.BoldAttr : TextAttribute.NormalAttr) { Size = new(0.2f, 0.2f) };
                if (asMasked) attr.FontMaterial = VanillaAsset.StandardMaskedFontMaterial;
                var button = new MetaContext.Button(() => { clickAction.Invoke(); if (reopenScreen) ReopenScreen(true); }, attr) { RawText = rawText };
                if (asMasked) button.SetAsMaskedButton();
                return button;
            }

            List<IMetaParallelPlacable> buttons = new();
            void AppendMargin(bool wide = false) { if (buttons.Count > 0) buttons.Add(new MetaContext.HorizonalMargin(wide ? 0.35f : 0.2f)); }
            void AppendButton(Action clickAction, string rawText, bool reopenScreen = true, bool useBoldFont = false) => buttons.Add(GetButton(clickAction,rawText,reopenScreen,useBoldFont));
            

            if (doc.Contents != null || doc.Aligned != null)
            {
                AppendMargin();
                AppendButton(() => NewContentEditor(doc), "+", false, true);
            }

            if (parent != null)
            {
                bool isVertical = parent.Contents != null;

                AppendMargin();
                AppendButton(() => parent.ReplaceContent(doc, true), isVertical ? "▲" : "◀", true, false);
                AppendButton(() => parent.ReplaceContent(doc, false), isVertical ? "▼" : "▶", true, false);

                AppendMargin(true);
                AppendButton(() => { parent.RemoveContent(doc); NebulaManager.Instance.HideHelpContext(); }, "×".Color(Color.red), true, true);
            }

            context.Append(new CombinedContext(buttons.ToArray()) { Alignment = IMetaContext.AlignmentOption.Left });

            MetaContext.TextInput GetTextFieldContent(bool isMultiline, float width, string defaultText, Action<string> updateAction, Predicate<char>? textPredicate,bool withMaskMaterial = false)
            {
                return new MetaContext.TextInput(isMultiline ? 7 : 1, isMultiline ? 1.2f : 1.8f, new(width, isMultiline ? 1.2f : 0.23f))
                {
                    DefaultText = isMultiline ? defaultText.Replace("<br>", "\r") : defaultText,
                    TextPredicate = textPredicate,
                    PostBuilder = field =>
                    {
                        field.LostFocusAction = (myInput) =>
                        {
                            if (isMultiline) myInput = myInput.Replace("\r", "<br>");
                            updateAction.Invoke(myInput);
                            ReopenScreen(true);
                        };
                    },
                    WithMaskMaterial = withMaskMaterial
                };
            }

            List<IMetaParallelPlacable> parallelPool = new();
            void AppendParallel(IMetaParallelPlacable content) => parallelPool.Add(content);
            void AppendParallelMargin(float margin) => AppendParallel(new MetaContext.HorizonalMargin(margin));
            void OutputParallelToContext()
            {
                if (parallelPool.Count == 0) return;
                context.Append(new CombinedContext(parallelPool.ToArray()) { Alignment = IMetaContext.AlignmentOption.Left });
                parallelPool.Clear();
            }

            void AppendTextField(bool isMultiline, float width, string defaultText, Action<string> updateAction, Predicate<char>? textPredicate)
            {
                context.Append(GetTextFieldContent(isMultiline, width, defaultText, updateAction, textPredicate));
            }

            void AppendTopTag(string translateKey)
            {
                AppendParallel(GetLocalizedTagContent(translateKey));
                AppendParallelMargin(0.05f);
                AppendParallel(GetRawTagContent(":"));
                AppendParallelMargin(0.1f);
            }

            MetaContext.VariableText GetRawTagContent(string rawText, bool asMasked = false) => new MetaContext.VariableText(asMasked ? new(TextAttribute.BoldAttrLeft) { FontMaterial = VanillaAsset.StandardMaskedFontMaterial } : TextAttribute.BoldAttrLeft) { RawText = rawText };
            MetaContext.VariableText GetLocalizedTagContent(string translationKey) => new MetaContext.VariableText(TextAttribute.BoldAttrLeft) { TranslationKey = translationKey };

            if (doc.RawText != null || doc.TranslationKey != null)
            {
                if (doc.RawText != null)
                    AppendTextField(true, 7.5f, doc.RawText, (input) => doc.RawText = input, TextField.JsonStringPredicate);
                else
                    AppendTextField(false, 3f, doc.TranslationKey, (input) => doc.TranslationKey = input, TextField.JsonStringPredicate);


                AppendParallel(MetaContext.StateButton.TopLabelCheckBox("devStudio.ui.document.editor.isBold", null, true, doc.IsBold ?? false, (val) => { doc.IsBold = val; ReopenScreen(true); }));
                AppendParallelMargin(0.2f);

                AppendTopTag("devStudio.ui.document.editor.fontSize");
                AppendParallel(GetTextFieldContent(false, 0.4f, doc.FontSize.ToString() ?? "", (input) => { if (float.TryParse(input, out var val)) doc.FontSize = val; else doc.FontSize = null; }, TextField.NumberPredicate));
                AppendParallelMargin(0.2f);

                AppendTopTag("devStudio.ui.document.editor.color");
                if (doc.Color == null || doc.Color.Style != null)
                {
                    AppendParallel(GetTextFieldContent(false, 1.8f, doc.Color?.Style ?? "", (input) =>
                    {
                        if (input.Length == 0) doc.Color = null;
                        else doc.Color = new() { Style = input };
                    }, TextField.IdPredicate));
                }
                else
                {
                    AppendParallel(GetRawTagContent("R"));
                    AppendParallel(GetTextFieldContent(false, 0.4f, doc.Color.R?.ToString() ?? "255", (input) => { if (byte.TryParse(input, out var val)) doc.Color.R = val; }, TextField.IntegerPredicate));

                    AppendParallel(GetRawTagContent("G"));
                    AppendParallel(GetTextFieldContent(false, 0.4f, doc.Color.G?.ToString() ?? "255", (input) => { if (byte.TryParse(input, out var val)) doc.Color.G = val; }, TextField.IntegerPredicate));

                    AppendParallel(GetRawTagContent("B"));
                    AppendParallel(GetTextFieldContent(false, 0.4f, doc.Color.B?.ToString() ?? "255", (input) => { if (byte.TryParse(input, out var val)) doc.Color.B = val; }, TextField.IntegerPredicate));
                }

                OutputParallelToContext();
            }
            else if (doc.Image != null)
            {
                AppendTopTag("devStudio.ui.document.editor.image");
                AppendParallel(GetTextFieldContent(false, 3.2f, doc.Image, (input) =>
                {
                    doc.Image = input;
                }, TextField.NameSpacePredicate));

                AppendParallelMargin(0.2f);


                AppendTopTag("devStudio.ui.document.editor.width");
                AppendParallel(GetTextFieldContent(false, 0.8f, doc.Width?.ToString() ?? "0.25", (input) =>
                {
                    if (float.TryParse(input, out var val)) doc.Width = val;
                }, TextField.NumberPredicate));

                OutputParallelToContext();
            }
            else if (doc.HSpace != null || doc.VSpace != null)
            {
                bool isHorizontal = doc.HSpace != null;
                AppendTopTag("devStudio.ui.document.editor." + (isHorizontal ? "width" : "height"));
                AppendParallel(GetTextFieldContent(false, 0.8f, (isHorizontal ? doc.HSpace : doc.VSpace)?.ToString() ?? "0.5", (input) =>
                {
                    if (float.TryParse(input, out var val))
                    {
                        if (isHorizontal) doc.HSpace = val;
                        else doc.VSpace = val;
                    }
                }, TextField.NumberPredicate));

                OutputParallelToContext();
            }else if(doc.Document != null)
            {
                Reference<MetaContext.ScrollView.InnerScreen> innerRef = new();
                void UpdateInner()
                {
                    if (innerRef.Value == null) return;
                    if (!innerRef.Value.IsValid) return;

                    MetaContext inner = new();
                    foreach (var arg in doc.Document!.Arguments!)
                    {
                        inner.Append(new CombinedContext(
                            GetTextFieldContent(false, 1.4f, arg.Key, (input) =>
                            {
                                if (arg.Key != input)
                                {
                                    doc.Document.Arguments.Remove(arg.Key);
                                    doc.Document.Arguments[input] = arg.Value;
                                    NebulaManager.Instance.ScheduleDelayAction(UpdateInner);
                                }
                            }, TextField.IdPredicate, true),
                            new MetaContext.HorizonalMargin(0.1f),
                            GetRawTagContent(":"),
                            new MetaContext.HorizonalMargin(0.1f),
                            GetTextFieldContent(false, 3.1f, arg.Value, (input) =>
                            {
                                if (arg.Value != input)
                                {
                                    doc.Document.Arguments[arg.Key] = input;
                                    NebulaManager.Instance.ScheduleDelayAction(UpdateInner);
                                }
                            }, TextField.JsonStringPredicate, true),
                            new MetaContext.HorizonalMargin(0.1f),
                            GetButton(() => {
                                doc.Document.Arguments.Remove(arg.Key);
                                NebulaManager.Instance.ScheduleDelayAction(UpdateInner);
                            }, "×".Color(Color.red), true, true, true)
                            )
                        { Alignment = IMetaContext.AlignmentOption.Left });
                    }

                    try
                    {
                        innerRef.Value!.SetContext(inner);
                    }
                    catch { }
                }

                AppendTopTag("devStudio.ui.document.editor.document");
                AppendParallel(GetTextFieldContent(false, 2.6f, doc.Document.Id.ToString() ?? "", (input) =>
                {
                    doc.Document.Id = input;
                    var refDoc = DocumentManager.GetDocument(input);
                    if(refDoc?.Arguments != null)
                    {
                        foreach (var entry in doc.Document.Arguments) if (!refDoc!.Arguments!.Contains(entry.Key)) doc.Document.Arguments.Remove(entry.Key);
                        foreach (var arg in refDoc!.Arguments!) if (!doc.Document.Arguments.ContainsKey(arg)) doc.Document.Arguments[arg] = "";
                        NebulaManager.Instance.ScheduleDelayAction(UpdateInner);
                    }
                }, TextField.IdPredicate));
                OutputParallelToContext();
                AppendParallel(GetButton(() => {
                    int index = 0;
                    while (true) {
                        string str = "argument" + (index == 0 ? "" : index.ToString());
                        if (!doc.Document!.Arguments!.ContainsKey(str))
                        {
                            doc.Document.Arguments[str] = "";
                            break;
                        }
                        index++;
                        continue;
                    }
                    NebulaManager.Instance.ScheduleDelayAction(UpdateInner);
                }, "+", false, true));
                OutputParallelToContext();

                context.Append(new MetaContext.ScrollView(new Vector2(6.1f, 2.6f), new MetaContext(), true) { Alignment = IMetaContext.AlignmentOption.Left, InnerRef = innerRef, PostBuilder = UpdateInner });
            }

            NebulaManager.Instance.SetHelpContext(editorButton, context);
            TextField.EditFirstField();
        }

        void BuildContentEditor(PassiveButton editorButton, SerializableDocument doc, SerializableDocument? parent)
        {
            editorButton.OnMouseOver.AddListener(() =>
            {
                if ((!NebulaManager.Instance.ShowingAnyHelpContent)) ShowContentEditor(editorButton, doc, parent);
            });
            editorButton.OnMouseOut.AddListener(() =>
            {
                if (NebulaManager.Instance.HelpRelatedObject == editorButton) NebulaManager.Instance.HideHelpContext();
            });
            editorButton.OnClick.AddListener(() =>
            {
                if (NebulaManager.Instance.HelpRelatedObject != editorButton)
                    ShowContentEditor(editorButton, doc, parent);
                NebulaManager.Instance.HelpIrrelevantize();
            });

        }

        MetaContext context = new();
        context.Append(
            new CombinedContext(
                new MetaContext.Text(new TextAttribute(TextAttribute.TitleAttr) { Styles = TMPro.FontStyles.Normal, Size = new(3f, 0.45f) }.EditFontSize(2.7f)) { RawText = id, Alignment = IMetaContext.AlignmentOption.Left },
                new MetaContext.Button(() => { NebulaManager.Instance.HideHelpContext(); Save(); }, TextAttribute.BoldAttr) { TranslationKey = "devStudio.ui.common.save" },
                new MetaContext.Button(() =>
                {
                    NebulaManager.Instance.HideHelpContext();
                    var screen = MetaScreen.GenerateWindow(new(7f, 4.5f), transform, Vector3.zero, true, true, true);
                    Reference<MetaContext.ScrollView.InnerScreen> innerRef = new();
                    screen.SetContext(new MetaContext.ScrollView(new Vector2(7f, 4.5f), doc.Build(innerRef, nameSpace: addon) ?? new MetaContext()) { InnerRef = innerRef });
                }, TextAttribute.BoldAttr)
                { TranslationKey = "devStudio.ui.common.preview" }
            )
            { Alignment = IMetaContext.AlignmentOption.Left }
        );

        context.Append(new MetaContext.VerticalMargin(0.1f));
        context.Append(new MetaContext.ScrollView(new Vector2(ScreenWidth - 0.4f, 4.65f), doc.BuildForDev(BuildContentEditor) ?? new MetaContext(), true) { ScrollerTag = "DocumentEditor" });

        return (context, null, () =>
        {
            NebulaManager.Instance.HideHelpContext();
            ShowConfirmWindow(() => { Save(); CloseScreen(true); }, () => { CloseScreen(true); }, Language.Translate("devStudio.ui.common.confirmSaving"), true);
            return false;
        }
        );
    }

    //Documents
    private (IMetaContext context, Action? postAction, Func<bool>? confirm) ShowDocumentScreen(DevAddon addon)
    {
        void CheckAndGenerateDocument(Il2CppArgument<MetaScreen> editScreen, Il2CppArgument<TextField> id, string? originalId = null)
        {
            if (id.Value.Text.Length < 1)
            {
                id.Value.SetHint(Language.Translate("devStudio.ui.hint.requiredText").Color(Color.red * 0.7f));
                return;
            }

            if (File.Exists(addon.FolderPath + "/Documents/" + id.Value.Text + ".json"))
            {
                id.Value.SetText("");
                id.Value.SetHint(Language.Translate("devStudio.ui.hint.duplicatedId").Color(Color.red * 0.7f));
                return;
            }

            MetaContext.ScrollView.RemoveDistHistory("DocumentEditor");
            editScreen.Value.CloseScreen();
            SerializableDocument? doc = null;
            if(originalId != null)
                doc = JsonStructure.Deserialize<SerializableDocument>(File.ReadAllText(addon.FolderPath + "/Documents/" + originalId + ".json"));
            doc ??= new SerializableDocument() { Contents = new() };
            doc.RelatedNamespace = addon;
            OpenScreen(() => ShowDocumentEditorScreen(addon, addon.FolderPath + "/Documents/" + id.Value.Text + ".json", id.Value.Text, doc));
        }

        MetaContext context = new();

        context.Append(new MetaContext.Text(new TextAttribute(TextAttribute.TitleAttr) { Font = VanillaAsset.BrookFont, Styles = TMPro.FontStyles.Normal, Size = new(3f, 0.45f) }.EditFontSize(5.2f)) { TranslationKey = "devStudio.ui.addon.document" });
        context.Append(new MetaContext.VerticalMargin(0.2f));

        (string path, string id, SerializableDocument doc)[]? docs = null;

        void OpenGenerateWindow(string? original = null)
        {
            var screen = MetaScreen.GenerateWindow(new(5.9f, original != null ? 1.8f : 1.5f), transform, Vector3.zero, true, false);
            MetaContext context = new();

            if (original != null) context.Append(new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new(1.5f, 0.3f) }) { RawText = Language.Translate("devStudio,ui.common.original") + " : " + original });

            Reference<TextField> refId = new();
            TMPro.TextMeshPro usingInfoText = null!;


            context.Append(new CombinedContext(
               new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Left, Size = new(1.5f, 0.3f) }) { RawText = "ID" },
               new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new(0.2f, 0.3f) }) { RawText = ":" },
               new MetaContext.TextInput(1, 2f, new(3.7f, 0.3f)) { TextFieldRef = refId, TextPredicate = TextField.IdPredicate }
                   ));
            context.Append(new MetaContext.Text(new TextAttribute(TextAttribute.NormalAttr) { Alignment = TMPro.TextAlignmentOptions.Right, Size = new(5.6f, 0.14f) }.EditFontSize(1.2f, 0.6f, 1.2f)) { PostBuilder = t => usingInfoText = t });
            context.Append(new MetaContext.VerticalMargin(0.16f));
            context.Append(new MetaContext.Button(() =>
            {
                CheckAndGenerateDocument(screen, refId.Value!,original);
            }, new(TextAttribute.BoldAttr) { Size = new(1.8f, 0.3f) })
            { TranslationKey = original != null ? "devStudio.ui.common.clone" : "devStudio.ui.common.generate", Alignment = IMetaContext.AlignmentOption.Center });

            screen.SetContext(context);
            TextField.EditFirstField();

            usingInfoText.fontStyle |= TMPro.FontStyles.Italic;
            refId.Value!.UpdateAction = (id) =>
            {
                if (docs?.Any(entry => entry.id == id) ?? false)
                {
                    usingInfoText.color = Color.red;
                    usingInfoText.text = Language.Translate("devStudio.ui.document.isDuplicatedId");
                }
                else if (DocumentManager.GetAllUsingId().Any(str => str == id))
                {
                    usingInfoText.color = Color.green;
                    usingInfoText.text = Language.Translate("devStudio.ui.document.isUsingId");
                }
                else
                {
                    usingInfoText.text = "";
                }
            };
        }

        //Add Button
        context.Append(new MetaContext.Button(() =>
        {
            OpenGenerateWindow();
        }, new TextAttribute(TextAttribute.BoldAttr) { Size = new(0.34f, 0.18f) }.EditFontSize(2.4f))
        { RawText = "+" });

        //Scroller
        Reference<MetaContext.ScrollView.InnerScreen> inner = new();
        context.Append(new MetaContext.ScrollView(new(ScreenWidth, 4f), inner) { Alignment = IMetaContext.AlignmentOption.Center });

        //Shower
        IEnumerator CoShowDocument()
        {
            yield return inner.Wait();
            inner.Value?.SetLoadingContext();

            var task = addon.LoadDocumentsAsync();
            yield return task.WaitAsCoroutine();

            MetaContext context = new();
            docs = task.Result;
            foreach (var entry in docs)
            {
                context.Append(new CombinedContext(
                    new MetaContext.Text(new(TextAttribute.NormalAttr) { FontMaterial = VanillaAsset.StandardMaskedFontMaterial,  Alignment = TMPro.TextAlignmentOptions.Left, Size = new(3f, 0.27f) }) { RawText = entry.id },
                    new MetaContext.Button(() =>
                    {
                        MetaContext.ScrollView.RemoveDistHistory("DocumentEditor");
                        var doc = JsonStructure.Deserialize<SerializableDocument>(File.ReadAllText(entry.path));

                        if (doc != null)
                        {
                            doc.RelatedNamespace = addon;
                            OpenScreen(() => ShowDocumentEditorScreen(addon, entry.path, entry.id, doc));
                        }
                    }, new(TextAttribute.BoldAttr) { FontMaterial = VanillaAsset.StandardMaskedFontMaterial, Size = new(0.8f, 0.22f) })
                    { TranslationKey = "devStudio.ui.common.edit" }.SetAsMaskedButton(),
                    new MetaContext.HorizonalMargin(0.2f),
                    new MetaContext.Button(() =>
                    {
                        ShowConfirmWindow(() => { File.Delete(entry.path); ReopenScreen(true); }, () => { }, Language.Translate("devStudio.ui.common.confirmDeleting") + $"<br>\"{entry.id}\"");
                    }, new(TextAttribute.BoldAttr) { FontMaterial = VanillaAsset.StandardMaskedFontMaterial, Size = new(0.8f, 0.22f)  })
                    { Text = ITextComponent.From("devStudio.ui.common.delete", Color.red) }.SetAsMaskedButton(),
                    new MetaContext.HorizonalMargin(0.2f),
                    new MetaContext.Button(() =>
                    {
                        OpenGenerateWindow(entry.id);
                    }, new(TextAttribute.BoldAttr) { FontMaterial = VanillaAsset.StandardMaskedFontMaterial, Size = new(0.8f, 0.22f) })
                    { Text = ITextComponent.From("devStudio.ui.common.clone", Color.white) }.SetAsMaskedButton()
                    )
                { Alignment = IMetaContext.AlignmentOption.Left });
            }

            inner.Value?.SetContext(context);
        }

        return (context, () => StartCoroutine(CoShowDocument().WrapToIl2Cpp()), null);
    }

    private void ShowConfirmWindow(Action yesAction,Action noAction,string text,bool canCancelClickOutside = false)
    {
        var screen = MetaScreen.GenerateWindow(new(3.9f, 1.14f), transform, Vector3.zero, true, canCancelClickOutside);
        MetaContext context = new();
        Reference<TextField> refName = new();

        context.Append(new MetaContext.Text(new TextAttribute(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new(3f, 0.6f) }) { RawText = text, Alignment = IMetaContext.AlignmentOption.Center });

        context.Append(new CombinedContext(
            new MetaContext.Button(() => { noAction.Invoke(); screen.CloseScreen(); }, new(TextAttribute.BoldAttr) { Size = new(0.5f, 0.3f) })
            { TranslationKey = "ui.dialog.no", Alignment = IMetaContext.AlignmentOption.Center },
            new MetaContext.HorizonalMargin(0.3f),
            new MetaContext.Button(() => { yesAction.Invoke(); screen.CloseScreen(); }, new(TextAttribute.BoldAttr) { Size = new(0.5f, 0.3f) })
            { TranslationKey = "ui.dialog.yes", Alignment = IMetaContext.AlignmentOption.Center }
            ));

        screen.SetContext(context);
    }
}

public class DevAddon : INameSpace
{
    public string Name { get; private set; }
    public string FolderPath { get; private set; }
    public string Id { get; private set; }
    private AddonMeta? addonMeta;
    public AddonMeta MetaSetting { get {
            if (addonMeta == null)
            {
                addonMeta = (AddonMeta?)JsonStructure.Deserialize(File.ReadAllText(FolderPath+"/addon.meta"), typeof(AddonMeta)) ??
                new() { Name = Name, Version = "1.0", Author = "Unknown", Description = "" };
            }
            return addonMeta;
        } }

    static public IEnumerable<DevAddon> SearchDevAddons()
    {
        foreach (var dir in Directory.GetDirectories("Addons", "*"))
        {
            string metaFile = $"{dir}/addon.meta";
            if (File.Exists(metaFile))
            {
                AddonMeta? meta = (AddonMeta?)JsonStructure.Deserialize(File.ReadAllText(metaFile), typeof(AddonMeta));
                if (meta == null) continue;
                yield return new DevAddon(meta.Name, dir) { addonMeta = meta };
            }
        }
    }

    static public async Task<DevAddon[]> SearchDevAddonsAsync()
    {
        List<DevAddon> result = new();
        foreach (var dir in Directory.GetDirectories("Addons", "*"))
        {
            string metaFile = $"{dir}/addon.meta";
            if (File.Exists(metaFile))
            {
                AddonMeta? meta = (AddonMeta?)JsonStructure.Deserialize(await File.ReadAllTextAsync(metaFile), typeof(AddonMeta));
                if (meta == null) continue;
                result.Add(new DevAddon(meta.Name, dir) { addonMeta = meta });
            }
        }
        return result.ToArray();
    }

    public DevAddon(string name,string folderPath)
    {
        Name = name;
        FolderPath = folderPath;
        Id = Path.GetFileName(folderPath);
    }

    public void BuildAddon()
    {
        string zipPath = FolderPath + "/" + Id + ".zip";
        string tempPath = "TempAddon.zip";
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(FolderPath, tempPath);
        File.Move(tempPath,zipPath);
    }

    public void SaveMetaSetting()
    {
        Name = MetaSetting.Name;
        File.WriteAllText(FolderPath + "/addon.meta", MetaSetting.Serialize());
    }

    public async Task<(string path,string id,SerializableDocument doc)[]> LoadDocumentsAsync()
    {
        if (!Directory.Exists(FolderPath + "/Documents")) return new (string, string, SerializableDocument)[0];

        List<(string path, string id, SerializableDocument doc)> result = new();
        foreach (var path in Directory.GetFiles(FolderPath + "/Documents"))
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var doc = JsonStructure.Deserialize<SerializableDocument>(await File.ReadAllTextAsync(path));
            if(doc == null) continue;

            doc.RelatedNamespace = this;
            result.Add((path, id, doc));
        }
        return result.ToArray();
    }

    private Stream? OpenRead(string folder, string innerAddress)
    {
        if (File.Exists(folder + "/" + innerAddress)) return File.OpenRead(folder + "/" + innerAddress);
        

        foreach (var dir in Directory.GetDirectories(folder))
        {
            string lowestDir = dir.Substring(folder.Length + 1);
            if (innerAddress.Length > (lowestDir.Length) && innerAddress[lowestDir.Length] is '.' && innerAddress.StartsWith(lowestDir))
            {
                var stream = OpenRead(dir, innerAddress.Substring(lowestDir.Length + 1));
                if (stream != null) return stream;
            }
        }

        return null;
    }

    public Stream? OpenRead(string innerAddress)
    {
        try
        {
            return OpenRead(FolderPath,innerAddress);
        }
        catch
        {
            return null;
        }
        
    }
    
}