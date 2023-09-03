using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
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
        background.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        screenSize = new Vector2(7f, 4f);
        myScreen = MetaScreen.GenerateScreen(screenSize,transform,Vector3.zero,false,false,false);

        gameObject.SetActive(false);
    }

    public void SetContext(Func<IMetaContext.AlignmentOption, MetaContext?>? context)
    {
        if(context == null) {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        transform.SetParent(UnityHelper.FindCamera(LayerExpansion.GetUILayer())!.transform);

        var pos = UnityHelper.ScreenToWorldPoint(Input.mousePosition, LayerExpansion.GetUILayer());

        pos.z = -800f;

        bool isLeft = Input.mousePosition.x < Screen.width / 2f;
        float height = myScreen.SetContext(context.Invoke(isLeft ? IMetaContext.AlignmentOption.Left : IMetaContext.AlignmentOption.Right), out var width);

        if (width.min > width.max)
        {
            gameObject.SetActive(false);
            return;
        }
        Vector2 anchorPoint = new(screenSize.x / 2f + 0.15f, screenSize.y / 2f + 0.15f);
        if (isLeft) anchorPoint.x *= -1f;
        transform.position = pos - (Vector3)anchorPoint;

        background.transform.localPosition = new Vector3((width.min + width.max) / 2f, screenSize.y / 2f - height / 2f, 1f);
        background.size = new Vector2((width.max - width.min) + 0.22f, height + 0.1f);

        
    }
}

public class NebulaManager : MonoBehaviour
{
    private List<Tuple<GameObject, PassiveButton?>> allModUi = new List<Tuple<GameObject, PassiveButton?>>();
    static public NebulaManager Instance { get; private set; }

    //テキスト情報表示
    private MouseOverPopup mouseOverPopup;

    static NebulaManager()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaManager>();
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
        //スクリーンショット
        if (Input.GetKeyDown(KeyCode.P)) StartCoroutine(CaptureAndSave().WrapToIl2Cpp());

        if (Input.GetKeyDown(KeyCode.T)) NebulaPlugin.Test();

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

    public void SetContext(Func<IMetaContext.AlignmentOption, MetaContext?>? context) => mouseOverPopup.SetContext(context);
}