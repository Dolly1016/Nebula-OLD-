global using BepInEx.Unity.IL2CPP.Utils.Collections;
global using Il2CppInterop.Runtime;
global using Nebula.Expansions;
global using Nebula.Utilities;
global using Nebula.Game;
global using Nebula.Player;
global using Nebula.Modules;
global using UnityEngine;
global using Nebula.Modules.ScriptComponents;
global using System.Collections;
global using Timer = Nebula.Modules.ScriptComponents.Timer;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Nebula;
using Nebula.Roles;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using Nebula.Configuration;
using System.Reflection;
using UnityEngine.Rendering;

namespace Nebula;


[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("Among Us.exe")]
public class NebulaPlugin : BasePlugin
{
    public const string AmongUsVersion = "2023.7.12";
    public const string PluginGuid = "jp.dreamingpig.amongus.nebula";
    public const string PluginName = "NebulaOnTheShip";
    public const string PluginVersion = "0.1.0";

    public const bool IsSnapshot = false;
    public const string MajorCodeName = "Experimental"/*"Haro"*/;
    public const string SnapshotVersion = "23.08.01";
    public const string VisualPluginVersion = "2";

    public static bool FinishedPreload { get; private set; } = false;

    public static string GetNebulaVersionString()
    {
        return "NoS " + MajorCodeName + " " + VisualPluginVersion;
        /*
        if (IsSnapshot)
            return "NoS Snapshot " + SnapshotVersion;
        else
            return "NoS " + MajorCodeName + " v" + VisualPluginVersion;
        */
    }

    public Harmony Harmony = new Harmony(PluginGuid);

    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern System.IntPtr FindWindow(System.String className, System.String windowName);

    override public void Load()
    {
        // Harmonyパッチ全てを適用する
        Harmony.PatchAll();

        var types = Assembly.GetAssembly(typeof(RemoteProcessBase))?.GetTypes().Where((type) => type.IsDefined(typeof(NebulaPreLoad)));
        if (types != null)
        {
            List<Type> PostLoad = new List<Type>();

            void Preload(Type type,bool isFinalize)
            {
                if (type.IsDefined(typeof(NebulaPreLoad)))
                {
                    var myPreType = type.GetCustomAttribute<NebulaPreLoad>()!;
                    var preTypes = myPreType.PreLoadType;
                    foreach (var pretype in preTypes) Preload(pretype,isFinalize);
                    if (!isFinalize && myPreType.IsFinalizer)
                    {
                        PostLoad.Add(type);
                        return;
                    }
                }

                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                Debug.Log("[Nebula] Preload " + type.Name);
                var loadMethod = type.GetMethod("Load");
                if(loadMethod != null)
                {
                    try
                    {
                        loadMethod.Invoke(null, null);
                        Debug.Log("[Nebula] Preloaded type " + type.Name + " has Load()");
                    }catch(Exception e)
                    {
                        Debug.Log("[Nebula] Preloaded type " + type.Name + " has Load with unregulated parameters.");
                    }
                }
                
                
            }

            foreach (var type in types) Preload(type, false);
            foreach (var type in PostLoad) Preload(type, true);
        }
        FinishedPreload = true;

        SetWindowText(FindWindow(null, Application.productName),"Among Us w/ " + GetNebulaVersionString());

        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)((scene, loadMode) =>
        {
            new GameObject("NebulaManager").AddComponent<NebulaManager>();
        });
    }


    private static SpriteLoader testSprite = SpriteLoader.FromResource("Nebula.Resources.LightMask.png", 100f);
    public static void Test()
    {
        /*
        var renderer = UnityHelper.CreateObject<SpriteRenderer>("Map", null, new Vector3(0, 0, -400f), LayerExpansion.GetUILayer());
        renderer.sprite = NebulaAsset.GetMapSprite(0, int.MaxValue);
        renderer.sharedMaterial = HatManager.Instance.PlayerMaterial;
        PlayerMaterial.SetColors(Color.blue, renderer);
        */

        var renderer = UnityHelper.CreateObject<SpriteRenderer>("Light",null,PlayerControl.LocalPlayer.transform.position + new Vector3(0,0,-50f),LayerExpansion.GetDrawShadowsLayer());
        renderer.sprite = testSprite.GetSprite();
        renderer.material.shader = NebulaAsset.MultiplyBackShader;
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


        //言語情報を読み込む
        Language.Load();

        __instance.StartCoroutine(VanillaAsset.CoLoadAsset().WrapToIl2Cpp());

    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class NebulaPreLoad : Attribute
{
    public Type[] PreLoadType { get; private set; }
    public bool IsFinalizer { get; private set; }
    public NebulaPreLoad(params Type[] preLoadType)
    {
        PreLoadType = preLoadType;
        IsFinalizer = false;
    }

    public NebulaPreLoad(bool isFinalizer, params Type[] preLoadType)
    {
        PreLoadType = preLoadType;
        IsFinalizer = isFinalizer;
    }

    static public bool FinishedLoading => NebulaPlugin.FinishedPreload;
}
