using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using Nebula.Behaviour;
using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;


[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public class GameStartManagerUpdatePatch
{
    public static bool LastChecked = false;
    public static bool Prefix(GameStartManager __instance)
    {
        if (!GameData.Instance) return false;
        if (!GameManager.Instance) return false;

        //公開ルームではスライド使用不可 (不特定多数への画像配信を禁止)
        if (AmongUsClient.Instance.IsGamePublic) NebulaGameManager.Instance?.LobbySlideManager.Abandon();

        __instance.MinPlayers = GeneralConfigurations.CurrentGameMode.MinPlayers;

        __instance.MakePublicButton.sprite = (AmongUsClient.Instance.IsGamePublic ? __instance.PublicGameImage : __instance.PrivateGameImage);
        __instance.privatePublicText.text = (AmongUsClient.Instance.IsGamePublic ? DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PublicHeader) : DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PrivateHeader));

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(GameCode.IntToGameName(AmongUsClient.Instance.GameId));

        if (DestroyableSingleton<DiscordManager>.InstanceExists)
        {
            bool active = AmongUsClient.Instance.AmHost && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && DestroyableSingleton<DiscordManager>.Instance.CanShareGameOnDiscord() && DestroyableSingleton<DiscordManager>.Instance.HasValidPartyID();
            __instance.ShareOnDiscordButton.gameObject.SetActive(active);
        }


        //人数の調整
        __instance.LastPlayerCount = GameData.Instance.PlayerCount;
        string arg = "<color=#FF0000FF>";
        if (__instance.LastPlayerCount > __instance.MinPlayers) arg = "<color=#00FF00FF>";
        if (__instance.LastPlayerCount == __instance.MinPlayers) arg = "<color=#FFFF00FF>";

        int max = 15;
        if (AmongUsClient.Instance.NetworkMode != NetworkModes.LocalGame) max = GameManager.Instance.LogicOptions.MaxPlayers;    
        var text = string.Format("{0}{1}/{2}", arg, __instance.LastPlayerCount, max);

        if (__instance.LastPlayerCount < __instance.MinPlayers) text += $" ({__instance.MinPlayers}↑)";

        __instance.PlayerCounter.text = text;
        __instance.PlayerCounter.enableWordWrapping = false;

        bool canStart = __instance.LastPlayerCount >= __instance.MinPlayers;

        canStart &= PlayerControl.AllPlayerControls.GetFastEnumerator().All(p => !p.gameObject.TryGetComponent<UncertifiedPlayer>(out _));

        LastChecked = canStart;
        __instance.StartButton.color = canStart ? Palette.EnabledColor : Palette.DisabledClear;
        __instance.startLabelText.color = canStart ? Palette.EnabledColor : Palette.DisabledClear;
        ActionMapGlyphDisplay startButtonGlyph = __instance.StartButtonGlyph;
        
        startButtonGlyph?.SetColor(canStart ? Palette.EnabledColor : Palette.DisabledClear);
        
        if (DestroyableSingleton<DiscordManager>.InstanceExists)
        {
            if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
                DestroyableSingleton<DiscordManager>.Instance.SetInLobbyHost(__instance.LastPlayerCount, GameManager.Instance.LogicOptions.MaxPlayers, AmongUsClient.Instance.GameId);
            else
                DestroyableSingleton<DiscordManager>.Instance.SetInLobbyClient(__instance.LastPlayerCount, GameManager.Instance.LogicOptions.MaxPlayers, AmongUsClient.Instance.GameId);
        }
    
        if (AmongUsClient.Instance.AmHost)
        {
            if (__instance.startState == GameStartManager.StartingStates.Countdown)
            {
                int num = Mathf.CeilToInt(__instance.countDownTimer);
                __instance.countDownTimer -= Time.deltaTime;
                int num2 = Mathf.CeilToInt(__instance.countDownTimer);
                __instance.GameStartText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameStarting, new Il2CppReferenceArray<Il2CppSystem.Object>(new Il2CppSystem.Object[] { num2 }));
                if (num != num2) PlayerControl.LocalPlayer.RpcSetStartCounter(num2);
                
                if (num2 <= 0) __instance.FinallyBegin();
            }
            else
            {
                __instance.GameStartText.text = string.Empty;
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
public class GameStartManagerBeginGame
{
    public static bool Prefix(GameStartManager __instance)
    {
        if (!GameStartManagerUpdatePatch.LastChecked) return false;

        if (AmongUsClient.Instance.AmHost)
        {

            if (GeneralConfigurations.CurrentGameMode == CustomGameMode.FreePlay && PlayerControl.AllPlayerControls.Count == 1)
            {
                if (PlayerControl.AllPlayerControls.Count == 1)
                {
                    int num = GeneralConfigurations.NumOfDummiesOption;

                    for (int n = 0; n < num; n++) AmongUsUtil.SpawnDummy();
                }
            }
        }

        return true;
    }
}


[HarmonyPatch(typeof(GameData), nameof(GameData.AddPlayer))]
public class RequireHandshakePatch
{
    public static void Postfix(GameData __instance, [HarmonyArgument(0)] PlayerControl pc)
    {
        if (AmongUsClient.Instance.AmHost) Certification.RequireHandshake();
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Awake))]
public class SetUpCertificationPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        __instance.gameObject.AddComponent<UncertifiedPlayer>().MyControl = __instance;
    }
}
