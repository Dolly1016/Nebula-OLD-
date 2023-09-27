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
    private MetaScreen myScreen;
    private SpriteRenderer background;
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

    public void SetContext(PassiveUiElement? related, IMetaContext? context)
    {
        if(context == null) {
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

        Vector2 anchorPoint = new(-screenSize.x / 2f - 0.15f, screenSize.y / 2f + 0.15f);
        if (!isLeft) anchorPoint.x += (width.max - width.min) + 0.3f;
        if (isLower) anchorPoint.y -= height + 0.3f;
        
        var pos = UnityHelper.ScreenToWorldPoint(Input.mousePosition, LayerExpansion.GetUILayer());
        pos.z = -800f;
        transform.position = pos - (Vector3)anchorPoint;



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
        public KeyCode? DefaultKeyCode = null;
        public KeyCode? KeyCode => KeyAssignmentType != null ? NebulaInput.GetKeyCode(KeyAssignmentType.Value) : DefaultKeyCode;
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
    static public NebulaManager Instance { get; private set; }

    //テキスト情報表示
    private MouseOverPopup mouseOverPopup;

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
        ){ DefaultKeyCode = KeyCode.F5 });
        
        commands.Add(new("help.command.quickStart",
            () => NebulaGameManager.Instance != null && AmongUsClient.Instance && AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined && GameStartManager.Instance && GameStartManager.Instance.startState == GameStartManager.StartingStates.NotStarting,
            () =>
            {
                GameStartManager.Instance.startState = GameStartManager.StartingStates.Countdown;
                GameStartManager.Instance.FinallyBegin();
            }
        )
        { DefaultKeyCode = KeyCode.F1 });
        
        commands.Add(new("help.command.cancelStarting",
            () => NebulaGameManager.Instance != null && AmongUsClient.Instance && AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined && GameStartManager.Instance && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown,
            RpcResetGameStart.Invoke
        )
        { DefaultKeyCode = KeyCode.F2 });

        commands.Add(new("help.command.console",
            () => true,
            ()=>NebulaManager.Instance.ToggleConsole()
        )
        { DefaultKeyCode = KeyCode.Return });
    }

    private void ToggleConsole()
    {
        if (console == null) console = new CommandConsole();
        else console.IsShown = !console.IsShown;
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

    static public IEnumerator CaptureAndSave()
    {
        yield return new WaitForEndOfFrame();
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        File.WriteAllBytes(GetPicturePath(out string displayPath), tex.EncodeToPNG());
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
            if (Input.GetKeyDown(NebulaInput.GetKeyCode(KeyAssignmentType.Screenshot))) StartCoroutine(CaptureAndSave().WrapToIl2Cpp());

            //コマンド
            if (Input.GetKeyDown(NebulaInput.GetKeyCode(KeyAssignmentType.Command)))
            {
                MetaContext context = new();
                context.Append(new MetaContext.Text(new(TextAttribute.BoldAttr) { Alignment = TMPro.TextAlignmentOptions.Left }) { TranslationKey = "help.command", Alignment = IMetaContext.AlignmentOption.Left });
                string commandsStr = "";
                foreach (var command in commands)
                {
                    if (!command.Predicate.Invoke()) continue;

                    if (commandsStr.Length != 0) commandsStr += "\n";
                    commandsStr += ButtonEffect.KeyCodeInfo.GetKeyDisplayName(command.KeyCode!.Value);

                    commandsStr += " :" + Language.Translate(command.TranslationKey);
                }
                context.Append(new MetaContext.VariableText(TextAttribute.ContentAttr) { RawText = commandsStr });

                if (commandsStr.Length > 0) SetHelpContext(null, context);

            }

            //コマンド
            if (Input.GetKeyUp(NebulaInput.GetKeyCode(KeyAssignmentType.Command)))
            {
                if (HelpRelatedObject == null) HideHelpContext();
            }

            //コマンド
            if (Input.GetKey(NebulaInput.GetKeyCode(KeyAssignmentType.Command)))
            {
                foreach (var command in commands)
                {
                    if (!Input.GetKeyDown(command.KeyCode!.Value)) continue;

                    command.CommandAction.Invoke();
                    HideHelpContext();
                    break;
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
    public PassiveUiElement? HelpRelatedObject => mouseOverPopup.RelatedObject;
}