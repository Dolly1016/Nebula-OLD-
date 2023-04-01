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
using Nebula.Patches;
using UnityEngine.SceneManagement;
using Il2CppSystem.Xml;
using Nebula.Module;

namespace Nebula;

public static class RuntimePrefabs
{
    public static TMPro.TextMeshPro? TextPrefab = null;
    public static PlayerDisplay? PlayerDisplayPrefab = null;
}

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("Among Us.exe")]
public class NebulaPlugin : BasePlugin
{
    public static Module.Random rnd = new Module.Random();

    public const string AmongUsVersion = "2023.2.28";
    public const string PluginGuid = "jp.dreamingpig.amongus.nebula";
    public const string PluginName = "TheNebula";
    public const string PluginVersion = "2.3";
    public const bool IsSnapshot = false;

    public static string PluginVisualVersion = IsSnapshot ? "23.03.29a" : PluginVersion;
    public static string PluginStage = IsSnapshot ? "Snapshot" : "";
    
    public const string PluginVersionForFetch = "2.3";
    public byte[] PluginVersionData = new byte[] { 2, 3, 0, 0 };

    public static NebulaPlugin Instance;

    public Harmony Harmony = new Harmony(PluginGuid);

    public static Sprite ModStamp;

    public Logger.Logger Logger;

    private void InstallTools()
    {
        InstallTool("CPUAffinityEditor");
    }
    private void InstallTool(string name)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream("Nebula.Resources." + name + ".exe");
        var file = File.Create(name + ".exe");
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
        InstallTools();

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

        //ゴースト情報を読み込む
        //Ghost.GhostInfo.Load();
        //Ghost.Ghost.Load();

        // Harmonyパッチ全てを適用する
        Harmony.PatchAll();

        //RPC情報を読み込む
        RemoteProcessBase.Load();


        SceneManager.sceneLoaded += (Action<Scene,LoadSceneMode>)((scene,loadMode) =>
        {
            new GameObject("NebulaManager").AddComponent<NebulaManager>();
        });
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
        NebulaEvents.OnMapAssetLoaded();

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

[HarmonyPatch(typeof(Constants), nameof(Constants.ShouldHorseAround))]
public static class AprilFoolPatch
{
    public static bool Prefix(out bool __result)
    {
        __result = true;
        return false;
    }
}