using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Nebula.Game;
using Nebula.Modules;
using System.Collections;

namespace Nebula;

public class NebulaManager : MonoBehaviour
{
    private List<Tuple<GameObject, PassiveButton?>> allModUi = new List<Tuple<GameObject, PassiveButton?>>();
    static public NebulaManager Instance { get; private set; }
    static NebulaManager()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaManager>();
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
        if (NebulaInput.GetKeyDown(KeyCode.P)) StartCoroutine(CaptureAndSave().WrapToIl2Cpp());

        //if (NebulaInput.GetKeyDown(KeyCode.T)) NebulaPlugin.Test();
        if (NebulaInput.GetKeyDown(KeyCode.F5) && NebulaInput.GetKey(KeyCode.LeftControl)) NebulaGameEnd.RpcSendNoGame();

        //ダイアログ管理
        allModUi.RemoveAll(tuple => !tuple.Item1);
        for (int i = 0; i < allModUi.Count; i++)
        {
            var lPos = allModUi[i].Item1.transform.localPosition;
            allModUi[i].Item1.transform.localPosition = new Vector3(lPos.x, lPos.y, -500f - i * 10f);
            allModUi[i].Item2?.gameObject.SetActive(i == allModUi.Count - 1);
        }

        if (allModUi.Count > 0 && Input.GetKeyDown(KeyCode.Escape))
            allModUi[allModUi.Count - 1].Item2?.OnClick.Invoke();

        MoreCosmic.Update();
    }

    public void Awake()
    {
        Instance = this;
    }
}