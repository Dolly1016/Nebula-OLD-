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
using Hazel;

namespace Nebula
{
    public class DebugMode
    {
        HashSet<string> DebugToken;

        public bool IsValid { get; private set; }
        public DebugMode()
        {
            DebugToken = new HashSet<string>();

            if (File.Exists("patches/DebugMode.patch")) IsValid = true;
        }

        public static implicit operator bool(DebugMode debugMode)
        {
            return debugMode.IsValid;
        }
    }

    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class NebulaPlugin : BasePlugin
    {
        public static System.Random rnd = new System.Random((int)DateTime.Now.Ticks);

        public const string AmongUsVersion = "2021.12.15";
        public const string PluginGuid = "jp.dreamingpig.amongus.nebula";
        public const string PluginName = "TheNebula";
        public const string PluginVersion = "1.8.6";
        public const string PluginStage = "ALPHA";
        public const string PluginVersionForFetch = "0.1.8.6";
        public byte[] PluginVersionData = new byte[] { 0, 1, 8, 6 };

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

            //色データを読み込む
            Module.CustomColors.Load();

            //言語データを読み込む
            Language.Language.Load();

            //オプションを読み込む
            CustomOptionHolder.Load();

            

            //GlobalEventデータを読み込む
            Events.Events.Load();

            //マップ関連のデータを読み込む
            Map.MapEditor.Load();
            Map.MapData.Load();

            // Harmonyパッチ全てを適用する
            Harmony.PatchAll();
        }

        public static Sprite GetModStamp()
        {
            if (ModStamp) return ModStamp;
            return ModStamp = Helpers.loadSpriteFromResources("Nebula.Resources.ModStamp.png", 150f);
        }
    }


    // Debugging tools
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    public static class DebugManager
    {
        private static readonly System.Random random = new System.Random((int)DateTime.Now.Ticks);
        private static List<PlayerControl> bots = new List<PlayerControl>();

        public static void Postfix(KeyboardJoystick __instance)
        {
            if (!NebulaPlugin.DebugMode) return;

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
                playerControl.SetName(RandomString(10));
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
                playerControl.SetName(RandomString(10));
                playerControl.SetColor((byte)random.Next(Palette.PlayerColors.Length));
                GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);
            }

            // オブジェクトチェック
            if (Input.GetKeyDown(KeyCode.F5))
            {
                //Input.GetKeyDownInt(KeyCode.Y)
                var objects = GameObject.FindObjectsOfType<SabotageTask>();
                string message = "";
                foreach (SabotageTask obj in objects)
                {
                    message += obj.name + "\n";
                    obj.Complete();
                }

                Objects.CustomMessage.Create(new Vector3(-2, 0), false, message, 5, 0, 1f, 1f, Color.white);
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

            // Terminate round
            if (Input.GetKeyDown(KeyCode.F11))
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ForceEnd, Hazel.SendOption.Reliable, -1);
                writer.WritePacked(PlayerControl.LocalPlayer.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCEvents.ForceEnd(PlayerControl.LocalPlayer.PlayerId);
            }

            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Objects.CustomMessage.Create("test1",5f,1f,2f,Color.white);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Objects.CustomMessage message = Objects.CustomMessage.Create(1.3f, 3f, 2.3f, null, "test1", 1f, 0.5f, 2f, (float)NebulaPlugin.rnd.NextDouble() * 3.5f +0.8f, new Color(1f, 1f, 1f, 0.4f));
                message.textSwapDuration = 0.1f;
                message.textSwapGain = 5;

                float rand = (float)NebulaPlugin.rnd.NextDouble() * 0.2f +0.1f;
                message.textSizeVelocity = new Vector3(rand,rand);
            }

        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
