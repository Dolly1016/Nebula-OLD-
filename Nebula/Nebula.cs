global using UnityEngine;
global using System.Collections;
global using System.Collections.Generic;
global using System.Linq;
global using HarmonyLib;
global using Nebula.Objects;
global using Nebula.Utilities;
global using AmongUs.GameOptions;
global using Nebula.Components;
global using Il2CppInterop.Runtime.Injection;
global using BepInEx.Unity.IL2CPP.Utils.Collections;

global using Il2CppInterop.Runtime;
global using Il2CppInterop.Runtime.InteropTypes;
global using Il2CppInterop.Runtime.InteropTypes.Fields;
global using Il2CppInterop.Runtime.InteropTypes.Arrays;

using BepInEx;
using System.Text;
using System.Reflection;
using BepInEx.Unity.IL2CPP;


namespace Nebula;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("Among Us.exe")]
public class NebulaPlugin : BasePlugin
{
    public static Module.Random rnd = new Module.Random();

    public const string AmongUsVersion = "2023.2.28";
    public const string PluginGuid = "jp.dreamingpig.amongus.nebula";
    public const string PluginName = "TheNebula";
    public const string PluginVersion = "2.1.1";
    public const bool IsSnapshot = true;

    public static string PluginVisualVersion = IsSnapshot ? "23.03.01a" : PluginVersion;
    public static string PluginStage = IsSnapshot ? "Snapshot" : "";
    
    public const string PluginVersionForFetch = "2.1";
    public byte[] PluginVersionData = new byte[] { 2, 1, 0, 0 };

    public static NebulaPlugin Instance;

    public Harmony Harmony = new Harmony(PluginGuid);

    public static Sprite ModStamp;

    public Logger.Logger Logger;

    private void InstallCPUAffinityEditor()
    {
       
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream("Nebula.Resources.CPUAffinityEditor.exe");
        var file = File.Create("CPUAffinityEditor.exe");
        byte[] data = new byte[stream.Length];
        stream.Read(data);
        file.Write(data);
        stream.Close();
        file.Close();
    }

    private void InitialModification()
    {
        /*
        Constants.ShadowMask = LayerMask.GetMask(new string[]
           {
                "Shadow",
                "IlluminatedBlocking"
           }) | (1 << LayerExpansion.GetShadowObjectsLayer());
        Physics.IgnoreLayerCollision(LayerExpansion.GetShadowObjectsLayer(), LayerMask.NameToLayer("Ghost"), true);
        */
    }
    override public void Load()
    {

        Logger = new Logger.Logger(true);

        Instance = this;

        //初期の変更
        InitialModification();

        //CPUAffinityEditorを生成
        InstallCPUAffinityEditor();

        //アセットバンドルを読み込む
        Module.AssetLoader.Load();

        //キー入力情報を読み込む
        Module.NebulaInputManager.Load();

        //サーバー情報を読み込む
        Patches.RegionMenuOpenPatch.Initialize();

        //クライアントオプションを読み込む
        Patches.StartOptionMenuPatch.LoadOption();

        //色データを読み込む
        Module.DynamicColors.Load();

        //ゲームモードデータを読み込む
        Game.GameModeProperty.Load();

        //マップ関連のデータを読み込む
        Map.MapEditor.Load();
        Map.MapData.Load();

        //オプションを読み込む
        CustomOptionHolder.Load();

        //GlobalEventデータを読み込む
        Events.Events.Load();

        //ヘルプを読み込む
        Module.HelpContent.Load();

        //RPC情報を読み込む
        RemoteProcessBase.Load();

        //ゴースト情報を読み込む
        //Ghost.GhostInfo.Load();
        //Ghost.Ghost.Load();

        // Harmonyパッチ全てを適用する
        Harmony.PatchAll();
    }

    public static Sprite GetModStamp()
    {
        if (ModStamp) return ModStamp;
        return ModStamp = Helpers.loadSpriteFromResources("Nebula.Resources.ModStamp.png", 150f);
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Awake))]
public static class AmongUsClientAwakePatch
{
    public static bool IsFirstFlag = true;
    public static void Postfix(AmongUsClient __instance)
    {
        if (!IsFirstFlag) return;
        IsFirstFlag = false;

        foreach (var map in Map.MapData.MapDatabase.Values)
        {
            map.LoadAssets(__instance);
        }

        __instance.PlayerPrefab.cosmetics.zIndexSpacing = 0.00001f;

        //言語データを読み込む
        Language.Language.LoadFont();
        Language.Language.LoadDefaultKey();
        Language.Language.Load();

        //テクスチャデータを読み込む
        Module.TexturePack.Load();

    }
}

// Deactivate bans
[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
public static class AmBannedPatch
{
    public static void Postfix(out bool __result)
    {
        __result = false;
    }
}


// メタコントローラ
[HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
public static class MetaControlManager
{
    private static readonly System.Random random = new System.Random((int)DateTime.Now.Ticks);
    private static List<PlayerControl> bots = new List<PlayerControl>();


    public static void SaveTexture(Texture2D texture, string fileName)
    {
        byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(texture));
        //保存
        File.WriteAllBytes(fileName + ".png", bytes);
    }

    static public IEnumerator CaptureAndSave()
    {
        yield return new WaitForEndOfFrame();
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();

        File.WriteAllBytes(Patches.NebulaOption.CreateDirAndGetPictureFilePath(out string displayPath), tex.EncodeToPNG());
    }

    public static void Postfix(KeyboardJoystick __instance)
    {
        //スクリーンショット
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (AmongUsClient.Instance)
            {
                AmongUsClient.Instance.StartCoroutine(CaptureAndSave().WrapToIl2Cpp());
            }
        }

        /* ホスト専用コマンド */
        if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
        {
            //ゲーム強制終了
            if (Input.GetKey(Module.NebulaInputManager.metaControlInput.keyCode) && Input.GetKey(Module.NebulaInputManager.noGameInput.keyCode))
            {
                Game.GameData.data.IsCanceled = true;
            }
        }

        /* 以下デバッグモード専用 */
        if (!Patches.NebulaOption.configGameControl.Value) return;

        // Spawn dummys
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);

            var i = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

            GameData.Instance.AddPlayer(playerControl);

            //playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
            playerControl.GetComponent<DummyBehaviour>().enabled = true;
            playerControl.isDummy = true;
            playerControl.SetName(Patches.RandomNamePatch.GetRandomName());
            playerControl.SetColor((byte)random.Next(15));

            bots.Add(playerControl);
            AmongUsClient.Instance.Spawn(playerControl, -2, InnerNet.SpawnFlags.None);

            GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);
        }

        // Suiside
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer, Game.PlayerData.PlayerStatus.Suicide, false, false);
        }

        // Kill nearest player
        if (Input.GetKeyDown(KeyCode.F10))
        {
            PlayerControl target = Patches.PlayerControlPatch.SetMyTarget();
            if (target == null) return;

            Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, target, Game.PlayerData.PlayerStatus.Dead, false, false);

        }
    }
}