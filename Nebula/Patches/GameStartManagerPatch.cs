using System.Reflection;
using Hazel;

namespace Nebula.Patches;

public class GameStartManagerPatch
{
    public static Dictionary<int, PlayerVersion> playerVersions = new Dictionary<int, PlayerVersion>();
    private static float timer = 600f;
    private static float kickingTimer = 0f;
    private static bool versionSent = false;
    private static string lobbyCodeText = "";

    public class PlayerVersion
    {
        public readonly byte[] version;
        public readonly Guid guid;

        public PlayerVersion(byte[] version, Guid guid)
        {
            this.version = version;
            this.guid = guid;
        }

        public bool Matches()
        {
            if (version.All((b) => b == 0)) return true;

            if (!Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.Equals(this.guid))
            {
                return false;
            }
            if (NebulaPlugin.Instance.PluginVersionData.Length != version.Length)
            {
                return false;
            }
            for (int i = 0; i < version.Length; i++)
            {
                if (version[i] != NebulaPlugin.Instance.PluginVersionData[i])
                {
                    return false;
                }
            }
            return true;
        }
    }

    //[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoSpawnPlayer))]
    public class AmongUsClientOnPlayerJoinedPatch
    {
        public static void Postfix()
        {
            if (PlayerControl.LocalPlayer != null)
            {
                Helpers.shareGameVersion();
                PlayerControl.LocalPlayer.SetColor(PlayerControl.LocalPlayer.PlayerId);
                RPCEventInvoker.SetMyColor();
            }


            foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                player.SetColor(player.PlayerId);
            }

        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public class GameStartManagerStartPatch
    {
        public static void Postfix(GameStartManager __instance)
        {
            // Trigger version refresh
            versionSent = false;
            // Reset lobby countdown timer
            timer = 600f;
            // Reset kicking timer
            kickingTimer = 0f;
            // Copy lobby code
            string code = InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId);
            GUIUtility.systemCopyBuffer = code;
            lobbyCodeText = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.RoomCode, new Il2CppReferenceArray<Il2CppSystem.Object>(0)) + "\r\n" + code;
        }
    }


    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public class GameStartManagerUpdatePatch
    {
        private static bool update = false;
        private static string currentText = "";

        public static void Prefix(GameStartManager __instance)
        {
            try
            {
                if (!GameData.Instance) return;

                GameData.Instance.HandleDisconnect();

                foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    if (player != null && player.PlayerId != player.Data.DefaultOutfit.ColorId)
                    {
                        player.SetColor(player.PlayerId);
                    }
                }
                if (!AmongUsClient.Instance.AmHost) return; // Not host or no instance
                update = GameData.Instance.PlayerCount != __instance.LastPlayerCount;
            }
            catch { }
        }

        public static void Postfix(GameStartManager __instance)
        {

            try
            {
                // Send version as soon as PlayerControl.LocalPlayer exists
                if (PlayerControl.LocalPlayer != null && !versionSent)
                {
                    versionSent = true;
                    Helpers.shareGameVersion();

                    PlayerControl.LocalPlayer.SetColor(PlayerControl.LocalPlayer.PlayerId);
                    AmongUs.Data.DataManager.Player.Customization.Color = PlayerControl.LocalPlayer.PlayerId;
                    RPCEventInvoker.SetMyColor();
                }

                if (!AmongUsClient.Instance) return;

                // Host update with version handshake infos
                if (AmongUsClient.Instance.AmHost)
                {
                    
                    int minPlayers= Game.GameModeProperty.GetProperty(CustomOptionHolder.GetCustomGameMode()).MinPlayers;
                    int maxPlayers = Game.GameModeProperty.GetProperty(CustomOptionHolder.GetCustomGameMode()).MaxPlayers ?? 15;
                    __instance.MinPlayers = minPlayers;

                    bool blockStart = false;
                    string message = "";
                    foreach (InnerNet.ClientData client in AmongUsClient.Instance.allClients.ToArray())
                    {
                        if (client.Character == null) continue;
                        var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                        if (dummyComponent != null && dummyComponent.enabled)
                            continue;
                        else if (!playerVersions.ContainsKey(client.Id))
                        {
                            blockStart = true;
                            message += $"<color=#FF0000FF>{Language.Language.GetString("lobby.hasNoNebula").Replace("%NAME%", client.Character.Data.PlayerName)}</color>\n";

                        }
                        else if(!NebulaOption.configDontCareMismatchedNoS.Value)
                        {
                            PlayerVersion version = playerVersions[client.Id];
                            if (!version.Matches())
                            {
                                message += $"<color=#FF0000FF>{Language.Language.GetString("lobby.hasDifferentNebula").Replace("%NAME%", client.Character.Data.PlayerName)}</color>\n";
                                blockStart = true;
                            }
                        }
                    }
                    if (blockStart)
                    {
                        __instance.StartButton.color = __instance.startLabelText.color = Palette.DisabledClear;
                        __instance.GameStartText.text = message;
                        __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition + Vector3.up * 2;
                    }
                    else
                    {
                        __instance.StartButton.color = __instance.startLabelText.color = ((__instance.LastPlayerCount >= minPlayers && __instance.LastPlayerCount <= maxPlayers) ? Palette.EnabledColor : Palette.DisabledClear);
                        __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition;
                    }
                }

                // Client update with handshake infos
                if (!AmongUsClient.Instance.AmHost)
                {
                    if (!playerVersions.ContainsKey(AmongUsClient.Instance.HostId) || !playerVersions[AmongUsClient.Instance.HostId].Matches())
                    {
                        kickingTimer += Time.deltaTime;
                        if (kickingTimer > 10)
                        {
                            kickingTimer = 0;
                            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                            SceneChanger.ChangeScene("MainMenu");
                        }

                        __instance.GameStartText.text = $"<color=#FF0000FF>{Language.Language.GetString("lobby.willEliminatedByMismatchVersion").Replace("%STAY%", Math.Round(10 - kickingTimer).ToString())}</color>\n";
                        __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition + Vector3.up * 2;
                    }
                    else
                    {
                        __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition;
                        if (__instance.startState != GameStartManager.StartingStates.Countdown)
                        {
                            __instance.GameStartText.text = String.Empty;
                        }
                    }
                }

                // Lobby code replacement
                //__instance.GameRoomName.text = TheOtherRolesPlugin.StreamerMode.Value ? $"<color={TheOtherRolesPlugin.StreamerModeReplacementColor.Value}>{TheOtherRolesPlugin.StreamerModeReplacementText.Value}</color>" : lobbyCodeText;

                // Lobby timer
                if (!AmongUsClient.Instance.AmHost || !GameData.Instance) return; // Not host or no instance

                if (update) currentText = __instance.PlayerCounter.text;

                timer = Mathf.Max(0f, timer -= Time.deltaTime);
                int minutes = (int)timer / 60;
                int seconds = (int)timer % 60;
                string suffix = $" ({minutes:00}:{seconds:00})";

                __instance.PlayerCounter.text = currentText + suffix;
                __instance.PlayerCounter.autoSizeTextContainer = true;
            }
            catch (Exception e) { }

        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    public class GameStartManagerBeginGame
    {
        public static bool Prefix(GameStartManager __instance)
        {
            // Block game start if not everyone has the same mod version
            bool continueStart = true;

            if (AmongUsClient.Instance.AmHost)
            {
                // Reset Settings
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ResetVaribles, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCEvents.ResetVaribles();

                if (PlayerControl.AllPlayerControls.Count > (Game.GameModeProperty.GetProperty(CustomOptionHolder.GetCustomGameMode()).MaxPlayers ?? 15)) continueStart = false;

                foreach (InnerNet.ClientData client in AmongUsClient.Instance.allClients)
                {
                    if (client.Character == null) continue;
                    var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                    if (dummyComponent != null && dummyComponent.enabled)
                        continue;

                    if (!playerVersions.ContainsKey(client.Id))
                    {
                        continueStart = false;
                        break;
                    }

                    if (!playerVersions[client.Id].Matches())
                    {
                        continueStart = false;
                        break;
                    }
                }


                if (CustomOptionHolder.dynamicMap.getBool() && CustomOptionHolder.mapOptions.getBool())
                {
                    // 0 = Skeld
                    // 1 = Mira HQ
                    // 2 = Polus
                    // 3 = Dleks - deactivated
                    // 4 = Airship
                    List<byte> possibleMaps = new List<byte>();
                    if (!CustomOptionHolder.exceptSkeld.getBool()) possibleMaps.Add(0);
                    if (!CustomOptionHolder.exceptMIRA.getBool()) possibleMaps.Add(1);
                    if (!CustomOptionHolder.exceptPolus.getBool()) possibleMaps.Add(2);
                    if (!CustomOptionHolder.exceptAirship.getBool()) possibleMaps.Add(4);

                    //候補が無い場合はSkeldにする
                    if (possibleMaps.Count == 0) possibleMaps.Add(0);

                    RPCEventInvoker.SetRandomMap(possibleMaps[NebulaPlugin.rnd.Next(possibleMaps.Count)]);
                }

                if (CustomOptionHolder.GetCustomGameMode() is Module.CustomGameMode.FreePlay or Module.CustomGameMode.FreePlayHnS)
                {
                    if (PlayerControl.AllPlayerControls.Count == 1)
                    {
                        int num = 6;
                        if (CustomOptionHolder.GetCustomGameMode() is Module.CustomGameMode.FreePlay)
                            num = (int)CustomOptionHolder.CountOfDummiesOption.getFloat();
                        
                        for (int n = 0; n < num; n++)
                        {
                            var playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                            var i = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

                            GameData.Instance.AddPlayer(playerControl);

                            playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
                            playerControl.GetComponent<DummyBehaviour>().enabled = true;
                            playerControl.isDummy = true;
                            playerControl.SetName(Patches.RandomNamePatch.GetRandomName());
                            playerControl.SetColor(i);

                            AmongUsClient.Instance.Spawn(playerControl, -2, InnerNet.SpawnFlags.None);
                            GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);

                            //playerControl.StartCoroutine(playerControl.CoPlayerAppear().WrapToIl2Cpp());
                        }
                    }
                }
            }

            return continueStart;
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.SetStartCounter))]
    public static class SetStartCounterPatch
    {
        public static void Postfix(GameStartManager __instance, sbyte sec)
        {
            if (sec > 0)
            {
                __instance.startState = GameStartManager.StartingStates.Countdown;
            }

            if (sec <= 0)
            {
                __instance.startState = GameStartManager.StartingStates.NotStarting;
            }
        }
    }
}