using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using HarmonyLib;
using BepInEx.IL2CPP;
using UnityEngine;
using UnityEditor;
using Hazel;

namespace Nebula
{
    public class DebugMode
    {
        private HashSet<string> DebugToken;

        public bool IsValid { get; private set; }
        public DebugMode()
        {
            DebugToken = new HashSet<string>();

            if (File.Exists("patches/DebugMode.patch")) IsValid = true;
            if (!IsValid) return;

            foreach (string token in System.IO.File.ReadLines("patches/DebugMode.patch"))
            {
                DebugToken.Add(token.Replace("\n", ""));
            }

        }

        public static implicit operator bool(DebugMode debugMode)
        {
            return debugMode.IsValid;
        }

        public bool HasToken(string token)
        {
            return DebugToken.Contains(token);
        }
    }

    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class NebulaPlugin : BasePlugin
    {
        public static Module.Random rnd = new Module.Random();

        public const string AmongUsVersion = "2022.6.21";
        public const string PluginGuid = "jp.dreamingpig.amongus.nebula";
        public const string PluginName = "TheNebula";
        public const string PluginVersion = "1.11.0.1";
        /*
        public const string PluginVisualVersion = "22.02.14a";
        public const string PluginStage = "Snapshot";
        */
        // /*
        public const string PluginVisualVersion = PluginVersion;
        public const string PluginStage = "";
        // */
        public const string PluginVersionForFetch = "1.11.0.1";
        public byte[] PluginVersionData = new byte[] { 1, 11, 0, 1 };

        public static NebulaPlugin Instance;

        public Harmony Harmony = new Harmony(PluginGuid);

        public static Sprite ModStamp;

        public static DebugMode DebugMode;

        public Logger.Logger Logger;

        override public void Load()
        {
            DebugMode = new DebugMode();

            Logger = new Logger.Logger(true);

            Instance = this;

            
            //アセットバンドルを読み込む
            Module.AssetLoader.Load();

            //サーバー情報を読み込む
            Patches.RegionMenuOpenPatch.Initialize();

            //色データを読み込む
            Module.DynamicColors.Load();

            //言語データを読み込む
            Language.Language.Load();

            //ゲームモードデータを読み込む
            Game.GameModeProperty.Load();

            //オプションを読み込む
            CustomOptionHolder.Load();

            //GlobalEventデータを読み込む
            Events.Events.Load();

            //マップ関連のデータを読み込む
            Map.MapEditor.Load();
            Map.MapData.Load();

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
        [HarmonyPrefix]
        public static void Postfix(AmongUsClient __instance)
        {
            foreach(var map in Map.MapData.MapDatabase.Values)
            {
                map.LoadAssets(__instance);
            }
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


        public static void SaveTexture(Texture2D texture , string fileName)
        {
            byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(texture));
            //保存
            File.WriteAllBytes(fileName + ".png", bytes);
        }

        public static void Postfix(KeyboardJoystick __instance)
        {
            /* ホスト専用コマンド */
            if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
            {
                //ゲーム強制終了
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.F5))
                {
                    Game.GameData.data.IsCanceled = true;
                }
            }

            /* 以下デバッグモード専用 */
            if (!NebulaPlugin.DebugMode.HasToken("GameControl")) return;

            // Spawn dummys
            if (Input.GetKeyDown(KeyCode.F1))
            {
                var playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                var i = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

                bots.Add(playerControl);
                GameData.Instance.AddPlayer(playerControl);
                AmongUsClient.Instance.Spawn(playerControl, -2, InnerNet.SpawnFlags.None);

                playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
                playerControl.GetComponent<DummyBehaviour>().enabled = false;
                playerControl.NetTransform.enabled = true;
                playerControl.SetName(Patches.RandomNamePatch.GetRandomName());
                playerControl.SetColor((byte)random.Next(Palette.PlayerColors.Length));
                GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);
            }

            // Spawn dummys
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                var i = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

                bots.Add(playerControl);
                GameData.Instance.AddPlayer(playerControl);
                AmongUsClient.Instance.Spawn(playerControl, -2, InnerNet.SpawnFlags.None);

                playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
                playerControl.GetComponent<DummyBehaviour>().enabled = true;
                playerControl.NetTransform.enabled = true;
                playerControl.SetName(Patches.RandomNamePatch.GetRandomName());
                playerControl.SetColor((byte)random.Next(Palette.PlayerColors.Length));
                GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                SpriteRenderer r = MapBehaviour.Instance.transform.FindChild("Background").GetComponent<SpriteRenderer>();
                SaveTexture(r.sprite.texture,"debug");
            }

            // Suiside
            if (Input.GetKeyDown(KeyCode.F9))
            {
                Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer,Game.PlayerData.PlayerStatus.Suicide, false, false);
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
}
