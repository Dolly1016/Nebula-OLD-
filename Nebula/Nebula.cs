﻿using System;
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
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class NebulaPlugin : BasePlugin
    {
        public static System.Random rnd = new System.Random((int)DateTime.Now.Ticks);

        public const string AmongUsVersion = "2021.12.15";
        public const string PluginGuid = "jp.dreamingpig.amongus.nebula";
        public const string PluginName = "TheNebula";
        public const string PluginVersion = "1.2.5";
        public const string PluginStage = "ALPHA";
        public readonly byte[] PluginVersionData = new byte[] { 0, 1, 2, 1 };

        public static NebulaPlugin Instance;

        public Harmony Harmony = new Harmony(PluginGuid);

        public static Sprite ModStamp;

        public static bool DebugMode=false;

        override public void Load()
        {
            if (File.Exists("patches/DebugMode.patch"))DebugMode = true;

            Instance = this;

            //言語データを読み込む
            Language.Language.Load();

            //オプションを読み込む
            CustomOptionHolder.Load();

            //GlobalEventデータを読み込む
            Events.Events.Load();

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
            if (Input.GetKeyDown(KeyCode.J))
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
            if (Input.GetKeyDown(KeyCode.K))
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

            
            // Terminate round
            if (Input.GetKeyDown(KeyCode.L))
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ForceEnd, Hazel.SendOption.Reliable, -1);
                writer.WritePacked(PlayerControl.LocalPlayer.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCEvents.ForceEnd(PlayerControl.LocalPlayer.PlayerId);
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
