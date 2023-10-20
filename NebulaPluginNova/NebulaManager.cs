using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Nebula.Behaviour;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Utilities;
using Rewired.UI.ControlMapper;
using System.Collections;
using UnityEngine;

namespace Nebula;

public class MouseOverPopup : MonoBehaviour
{
    private MetaScreen myScreen = null!;
    private SpriteRenderer background = null!;
    private Vector2 screenSize;
    private PassiveUiElement? relatedButton;

    public PassiveUiElement? RelatedObject => relatedButton;
    static MouseOverPopup()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MouseOverPopup>();
    }

    public void Awake()
    {
        background = UnityHelper.CreateObject<SpriteRenderer>("Background", transform, Vector3.zero, LayerExpansion.GetUILayer());
        background.sprite = NebulaAsset.SharpWindowBackgroundSprite.GetSprite();
        background.drawMode = SpriteDrawMode.Sliced;
        background.tileMode = SpriteTileMode.Continuous;
        background.color = new Color(0.17f, 0.17f, 0.17f, 1f);

        screenSize = new Vector2(7f, 4f);
        myScreen = MetaScreen.GenerateScreen(screenSize,transform,Vector3.zero,false,false,false);

        gameObject.SetActive(false);

        NebulaGameManager.Instance?.OnSceneChanged();
    }

    public void Irrelevantize()
    {
        relatedButton = null;
    }

    public void SetContext(PassiveUiElement? related, IMetaContext? context)
    {
        myScreen.SetContext(null);

        if (context == null) {
            gameObject.SetActive(false);
            relatedButton = null;
            return;
        }

        gameObject.SetActive(true);

        relatedButton = related;
        transform.SetParent(UnityHelper.FindCamera(LayerExpansion.GetUILayer())!.transform);

        bool isLeft = Input.mousePosition.x < Screen.width / 2f;
        bool isLower = Input.mousePosition.y < Screen.height / 2f;

        float height = myScreen.SetContext(context, out var width);

        if (width.min > width.max)
        {
            gameObject.SetActive(false);
            return;
        }

        float[] xRange = new float[2], yRange = new float[2];
        xRange[0] = -screenSize.x / 2f - 0.15f;
        yRange[1] = screenSize.y / 2f + 0.15f;
        xRange[1] = xRange[0] + (width.max - width.min) + 0.3f;
        yRange[0] = yRange[1] - height - 0.3f;

        Vector2 anchorPoint = new(xRange[isLeft ? 0 : 1], yRange[isLower ? 0 : 1]);

        var pos = UnityHelper.ScreenToWorldPoint(Input.mousePosition, LayerExpansion.GetUILayer());
        pos.z = -800f;
        transform.position = pos - (Vector3)anchorPoint;

        //範囲外にはみ出た表示の是正
        {
            var lower = UnityHelper.ScreenToWorldPoint(new(10f, 10f), LayerExpansion.GetUILayer());
            var upper = UnityHelper.ScreenToWorldPoint(new(Screen.width - 10f, Screen.height - 10f), LayerExpansion.GetUILayer());
            float diff;

            diff = (transform.position.x + xRange[0]) - lower.x;
            if (diff < 0f) transform.position -= new Vector3(diff, 0f);

            diff = (transform.position.y + yRange[0]) - lower.y;
            if (diff < 0f) transform.position -= new Vector3(0f, diff);

            diff = (transform.position.x + xRange[1]) - upper.x;
            if (diff > 0f) transform.position -= new Vector3(diff, 0f);

            diff = (transform.position.y + yRange[1]) - upper.y;
            if (diff > 0f) transform.position -= new Vector3(0f, diff);
        }


        background.transform.localPosition = new Vector3((width.min + width.max) / 2f, screenSize.y / 2f - height / 2f, 1f);
        background.size = new Vector2((width.max - width.min) + 0.22f, height + 0.1f);

        Update();
    }

    public void Update()
    {
        if(relatedButton is not null && !relatedButton)
        {
            SetContext(null, null);
        }

    }
}

[NebulaPreLoad]
[NebulaRPCHolder]
public class NebulaManager : MonoBehaviour
{
    public class MetaCommand
    {
        public KeyAssignmentType? KeyAssignmentType = null;
        public VirtualInput? DefaultKeyInput = null;
        public VirtualInput? KeyInput => KeyAssignmentType != null ? NebulaInput.GetInput(KeyAssignmentType.Value) : DefaultKeyInput;
        public string TranslationKey;
        public Func<bool> Predicate;
        public Action CommandAction;

        public MetaCommand(string translationKey, Func<bool> predicate,Action commandAction)
        {
            TranslationKey = translationKey;
            Predicate = predicate;
            CommandAction = commandAction;
        }
    }

    private List<Tuple<GameObject, PassiveButton?>> allModUi = new();
    static private List<MetaCommand> commands = new();
    static public NebulaManager Instance { get; private set; } = null!;

    //テキスト情報表示
    private MouseOverPopup mouseOverPopup = null!;

    //コンソール
    private CommandConsole? console = null;

    static NebulaManager()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaManager>();
    }

    static private RemoteProcess RpcResetGameStart = new(
        "ResetStarting",
        (_) =>
        {
            if(GameStartManager.Instance) GameStartManager.Instance.ResetStartState();
        }
        );
    static public void Load()
    {
        commands.Add(new( "help.command.nogame",
            () => NebulaGameManager.Instance != null && AmongUsClient.Instance &&  AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started,
            () => NebulaGameManager.Instance?.RpcInvokeForcelyWin(NebulaGameEnd.NoGame, 0)
        ){ DefaultKeyInput = new(KeyCode.F5) });
        
        commands.Add(new("help.command.quickStart",
            () => NebulaGameManager.Instance != null && AmongUsClient.Instance && AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined && GameStartManager.Instance && GameStartManager.Instance.startState == GameStartManager.StartingStates.NotStarting,
            () =>
            {
                GameStartManager.Instance.startState = GameStartManager.StartingStates.Countdown;
                GameStartManager.Instance.FinallyBegin();
            }
        )
        { DefaultKeyInput = new(KeyCode.F1) });
        
        commands.Add(new("help.command.cancelStarting",
            () => NebulaGameManager.Instance != null && AmongUsClient.Instance && AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined && GameStartManager.Instance && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown,
            RpcResetGameStart.Invoke
        )
        { DefaultKeyInput = new(KeyCode.F2) });

        commands.Add(new("help.command.console",
            () => true,
            ()=>NebulaManager.Instance.ToggleConsole()
        )
        { DefaultKeyInput = new(KeyCode.Return) });
    }

    private void ToggleConsole()
    {
        if (console == null) console = new CommandConsole();
        else console.IsShown = !console.IsShown;

        if (console.IsShown) console.GainFocus();
    }

    public void CloseAllUI()
    {
        foreach (var ui in allModUi) GameObject.Destroy(ui.Item1);
        allModUi.Clear();
    }

    public void RegisterUI(GameObject uiObj,PassiveButton? closeButton)
    {
        allModUi.Add(new Tuple<GameObject, PassiveButton?>(uiObj,closeButton));
    }

    public bool HasSomeUI => allModUi.Count > 0;

    static private string GetCurrentTimeString()
    {
        return DateTime.Now.ToString("yyyyMMddHHmmss");
    }

    static public string GetPicturePath(out string displayPath)
    {
        string dir = "Screenshots";
        displayPath = "ScreenShots";
        string currentTime = GetCurrentTimeString();
        displayPath += "\\" + currentTime + ".png";
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        return dir + "\\" + currentTime + ".png";
    }

    static public IEnumerator CaptureAndSave(bool fillBackColor)
    {
        yield return new WaitForEndOfFrame();


        var tex = ScreenCapture.CaptureScreenshotAsTexture();

        if (fillBackColor)
        {
            var backColor = Camera.main.backgroundColor;
            var pixels = tex.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i] * pixels[i].a + backColor * (1f - pixels[i].a);
                c.a = 1f;
                pixels[i] = c;

                if (i % 10000 == 0) yield return null;
            }

            tex.SetPixels(pixels);
            tex.Apply();
        }

        File.WriteAllBytesAsync(GetPicturePath(out string displayPath), tex.EncodeToPNG());
    }

    public void Update()
    {

        /*
        if (Input.GetKeyDown(KeyCode.T))
        {
            UnityHelper.CreateObject<TextField>("TextField", null, new Vector3(0, 0, -200f), LayerExpansion.GetUILayer());
        }
        */


        if (NebulaPlugin.FinishedPreload)
        {
            //スクリーンショット
            if (NebulaInput.GetInput(KeyAssignmentType.Screenshot).KeyDown) StartCoroutine(CaptureAndSave(NebulaInput.GetInput(KeyAssignmentType.Command).KeyState).WrapToIl2Cpp());

            if (AmongUsClient.Instance && AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.NotJoined)
            {
                //コマンド
                if (NebulaInput.GetInput(KeyAssignmentType.Command).KeyDown)
                {
                    MetaContext context = new();
                    context.Append(new MetaContext.Text(new(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Left }) { TranslationKey = "help.command", Alignment = IMetaContext.AlignmentOption.Left });
                    string commandsStr = "";
                    foreach (var command in commands)
                    {
                        if (!command.Predicate.Invoke()) continue;

                        if (commandsStr.Length != 0) commandsStr += "\n";
                        commandsStr += ButtonEffect.KeyCodeInfo.GetKeyDisplayName(command.KeyInput!.TypicalKey);

                        commandsStr += " :" + Language.Translate(command.TranslationKey);
                    }
                    context.Append(new MetaContext.VariableText(TextAttribute.ContentAttr) { RawText = commandsStr });

                    if (commandsStr.Length > 0) SetHelpContext(null, context);

                }

                //コマンド
                if (NebulaInput.GetInput(KeyAssignmentType.Command).KeyUp)
                {
                    if (HelpRelatedObject == null) HideHelpContext();
                }

                //コマンド
                if (NebulaInput.GetInput(KeyAssignmentType.Command).KeyState)
                {
                    foreach (var command in commands)
                    {
                        if (!command.Predicate.Invoke()) continue;

                        if (!command.KeyInput!.KeyDown) continue;

                        command.CommandAction.Invoke();
                        HideHelpContext();
                        break;
                    }
                }
            }
        }

        //ダイアログ管理
        allModUi.RemoveAll(tuple => !tuple.Item1);
        for (int i = 0; i < allModUi.Count; i++)
        {
            var lPos = allModUi[i].Item1.transform.localPosition;
            allModUi[i].Item1.transform.localPosition = new Vector3(lPos.x, lPos.y, -500f - i * 50f);
            allModUi[i].Item2?.gameObject.SetActive(i == allModUi.Count - 1);
        }

        if (allModUi.Count > 0 && Input.GetKeyDown(KeyCode.Escape))
            allModUi[allModUi.Count - 1].Item2?.OnClick.Invoke();

        MoreCosmic.Update();
    }

    public void Awake()
    {
        Instance = this;
        gameObject.layer = LayerExpansion.GetUILayer();

        mouseOverPopup = UnityHelper.CreateObject<MouseOverPopup>("MouseOverPopup",transform,Vector3.zero);
    }

    public void SetHelpContext(PassiveUiElement? related, IMetaContext? context) => mouseOverPopup.SetContext(related, context);
    public void HideHelpContext() => mouseOverPopup.SetContext(null, null);
    public void HideHelpContextIf(PassiveUiElement? related)
    {
        if(HelpRelatedObject == related) mouseOverPopup.SetContext(null, null);
    }
    public PassiveUiElement? HelpRelatedObject => mouseOverPopup.RelatedObject;
    public bool ShowingAnyHelpContent => mouseOverPopup.isActiveAndEnabled;
    public void HelpIrrelevantize() => mouseOverPopup.Irrelevantize();

    public Coroutine ScheduleDelayAction(Action action)
    {
        return StartCoroutine(Effects.Sequence(
            Effects.Action((Il2CppSystem.Action)(() => { })),
            Effects.Action(action)
            ));
    }
}