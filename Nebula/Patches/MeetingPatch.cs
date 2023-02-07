using Nebula.Roles.CrewmateRoles;
using UnityEngine;

namespace Nebula.Patches;

[HarmonyPatch]
class MeetingHudPatch
{
    //最新の会議での得票数記録
    private static Dictionary<byte, int> VoteHistory = new Dictionary<byte, int>();
    private static Dictionary<byte, List<byte>> Voters = new Dictionary<byte, List<byte>>();
    private static Dictionary<byte, byte> VoteWeight = new Dictionary<byte, byte>();

    private static TMPro.TextMeshPro meetingInfoText;

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Start))]
    class PlayerVoteAreaRemovePlayerLevelPatch
    {

        static void Postfix(PlayerVoteArea __instance)
        {
            try
            {
                __instance.transform.FindChild("PlayerLevel").gameObject.SetActive(false);
            }
            catch (Exception e) { }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    class CastVotePatch
    {

        static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId, [HarmonyArgument(1)] byte suspectPlayerId)
        {
            GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(srcPlayerId);
            GameData.PlayerInfo playerById2 = GameData.Instance.GetPlayerById(suspectPlayerId);
            __instance.logger.Debug(playerById.PlayerName + " has voted for " + ((playerById2 != null) ? playerById2.PlayerName : "No one"), null);
            int num = __instance.playerStates.IndexOf((Il2CppSystem.Predicate<PlayerVoteArea>)((PlayerVoteArea pv) => pv.TargetPlayerId == srcPlayerId));
            PlayerVoteArea playerVoteArea = __instance.playerStates[num];
            if (!playerVoteArea.AmDead && !playerVoteArea.DidVote)
            {
                if (PlayerControl.LocalPlayer.PlayerId == srcPlayerId)
                {
                    SoundManager.Instance.PlaySound(__instance.VoteLockinSound, false, 1f, null);
                }
                playerVoteArea.SetVote(suspectPlayerId);
                __instance.SetDirtyBit(1U);
                __instance.CheckForEndVoting();

                if (!CustomOptionHolder.hideVotedIcon.getBool() || !CustomOptionHolder.meetingOptions.getBool())
                {
                    PlayerControl.LocalPlayer.RpcSendChatNote(srcPlayerId, ChatNoteTypes.DidVote);
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.VoteForMe))]
    class PlayerVoteAreaSelectPatch
    {

        static void Prefix(PlayerVoteArea __instance)
        {
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) =>
            {
                role.OnVote(__instance.TargetPlayerId);
            });
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class MeetingCalculateVotesPatch
    {
        public static void CalculateVotes(ref Dictionary<byte, int> dictionary, MeetingHud __instance)
        {

            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];

                PlayerControl player = Helpers.playerById((byte)playerVoteArea.TargetPlayerId);
                if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;

                //無視する投票先
                if (playerVoteArea.VotedFor == 252 || playerVoteArea.VotedFor == 254 || playerVoteArea.VotedFor == 255)
                {
                    //棄権を自投票扱い
                    if (CustomOptionHolder.dealAbstentionAsSelfVote.getBool())
                        playerVoteArea.VotedFor = playerVoteArea.TargetPlayerId;
                    else
                        continue;
                }

                if (!dictionary.ContainsKey(playerVoteArea.VotedFor)) dictionary[playerVoteArea.VotedFor] = 0;

                if (VoteWeight.ContainsKey(playerVoteArea.TargetPlayerId))
                {
                    dictionary[playerVoteArea.VotedFor] += VoteWeight[playerVoteArea.TargetPlayerId];
                }
                else
                {
                    dictionary[playerVoteArea.VotedFor]++;
                }

            }
        }


        static bool Prefix(MeetingHud __instance)
        {
            if (__instance.playerStates.All((PlayerVoteArea ps) => ps.AmDead || ps.DidVote))
            {
                VoteHistory.Clear();
                CalculateVotes(ref VoteHistory, __instance);
                bool tie;
                KeyValuePair<byte, int> max = VoteHistory.MaxPair(out tie);
                GameData.PlayerInfo exiled = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(v => !tie && v.PlayerId == max.Key && !v.IsDead);

                int sum = 0;
                foreach (int value in VoteHistory.Values)
                {
                    sum += value;
                }

                List<MeetingHud.VoterState> array = new List<MeetingHud.VoterState>();

                int weight;
                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (!playerVoteArea.DidVote) continue;

                    if (VoteWeight.ContainsKey(playerVoteArea.TargetPlayerId))
                    {
                        weight = VoteWeight[playerVoteArea.TargetPlayerId];
                    }
                    else
                    {
                        weight = 1;
                    }

                    for (int w = 0; w < weight; w++)
                    {
                        array.Add(new MeetingHud.VoterState
                        {
                            VoterId = playerVoteArea.TargetPlayerId,
                            VotedForId = playerVoteArea.VotedFor
                        });
                    }
                }

                // RPCVotingComplete
                __instance.RpcVotingComplete(array.ToArray(), exiled, tie);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch
    {
        static MeetingHud.VoteStates lastState;
        static void Prefix(MeetingHud __instance)
        {
            lastState = __instance.state;
        }

        static void Postfix(MeetingHud __instance)
        {
            if (__instance.state == MeetingHud.VoteStates.NotVoted && __instance.state != lastState && lastState != MeetingHud.VoteStates.Voted)
            {
                __instance.discussionTimer += EmergencyPatch.GetPenaltyVotingTime();
            }

            if (meetingInfoText == null)
            {
                meetingInfoText = UnityEngine.Object.Instantiate(HudManager.Instance.TaskPanel.taskText, __instance.transform);
                meetingInfoText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
                meetingInfoText.transform.position = Vector3.zero;
                meetingInfoText.transform.localPosition = new Vector3(-3.07f, 3.33f, -20f);
                meetingInfoText.transform.localScale *= 1.1f;
                meetingInfoText.color = Palette.White;
                meetingInfoText.gameObject.SetActive(false);
            }

            meetingInfoText.text = "";
            meetingInfoText.gameObject.SetActive(false);

            Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (role) =>
            {
                role.MeetingUpdate(__instance, meetingInfoText);
            });

            if (Game.GameData.data.GameMode == Module.CustomGameMode.Investigators)
            {
                Ghost.InvestigatorMeetingUI.UpdateMeetingUI(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    class MeetingHudVotingCompletedPatch
    {
        static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] byte[] states, [HarmonyArgument(1)] GameData.PlayerInfo exiled, [HarmonyArgument(2)] bool tie)
        {
            if (meetingInfoText != null) meetingInfoText.gameObject.SetActive(false);

            VoteHistory.Clear();
            MeetingCalculateVotesPatch.CalculateVotes(ref VoteHistory, __instance);

            Voters.Clear();

            foreach (PlayerVoteArea player in __instance.playerStates)
            {
                if (VoteWeight.ContainsKey(player.TargetPlayerId) && VoteWeight[player.TargetPlayerId] == 0) continue;

                if (!Voters.ContainsKey(player.VotedFor))
                {
                    Voters[player.VotedFor] = new List<byte>();
                }
                Voters[player.VotedFor].Add(player.TargetPlayerId);
            }
        }
    }

    [HarmonyPatch(typeof(MeetingIntroAnimation), nameof(MeetingIntroAnimation.Init))]
    class MeetingIntroAnimationPatch
    {
        public static void Prefix(MeetingIntroAnimation __instance, [HarmonyArgument(1)] ref UnhollowerBaseLib.Il2CppReferenceArray<GameData.PlayerInfo> deadBodies)
        {
            List<GameData.PlayerInfo> dBodies = new List<GameData.PlayerInfo>();
            //既に発見されている死体
            foreach (var dBody in deadBodies)
            {
                dBodies.Add(dBody);
            }
            //遅れて発見された死体
            foreach (var dBody in Helpers.AllDeadBodies())
            {
                dBodies.Add(GameData.Instance.GetPlayerById(dBody.ParentId));
                GameObject.Destroy(dBody.gameObject);
            }
            deadBodies = new UnhollowerBaseLib.Il2CppReferenceArray<GameData.PlayerInfo>(dBodies.ToArray());

            //生死を再確認
            MeetingHud.Instance.RecheckPlayerState();
        }
    }

    /*
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.OpenMeetingRoom))]
    class OpenMeetingPatch
    {
        public static void Prefix(HudManager __instance)
        {

        }
    }
    */

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
    class StartMeetingPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo meetingTarget)
        {
            //会議前の位置を共有する
            RPCEventInvoker.SendPreMeetingPosition(PlayerControl.LocalPlayer.transform.position);

            if (meetingTarget == null) EmergencyPatch.meetingsCount++;

            //票の重み設定をリセット
            VoteWeight.Clear();
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class MeetingServerStartPatch
    {
        static private Sprite LightColorSprite;
        static private Sprite DarkColorSprite;

        static private Sprite GetLightColorSprite()
        {
            if (LightColorSprite) return LightColorSprite;
            LightColorSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ColorLight.png", 100f);
            return LightColorSprite;
        }

        static private Sprite GetDarkColorSprite()
        {
            if (DarkColorSprite) return DarkColorSprite;
            DarkColorSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ColorDark.png", 100f);
            return DarkColorSprite;
        }

        static void Postfix(MeetingHud __instance)
        {
            //スポーンミニゲームの同期設定を予めリセット
            Game.GameData.data.SynchronizeData.Reset(Game.SynchronizeTag.PreSpawnMinigame);

            Events.GlobalEvent.OnMeeting();
            Events.LocalEvent.OnMeeting();
            Events.Schedule.OnPreMeeting();

            if (Game.GameData.data.GameMode == Module.CustomGameMode.Investigators)
                Ghost.InvestigatorMeetingUI.FormMeetingUI(__instance);


            EmergencyPatch.MeetingUpdate();

            Game.GameData.data.myData.getGlobalData().role.OnMeetingStart();

            Helpers.RoleAction(PlayerControl.LocalPlayer, (role) => { role.SetupMeetingButton(MeetingHud.Instance); });


            foreach (Game.PlayerData player in Game.GameData.data.AllPlayers.Values)
            {
                player.Speed.OnMeeting();
                player.Attribute.OnMeeting();
            }

            //色の明暗を表示
            foreach (var player in __instance.playerStates)
            {
                bool isLightColor = Module.DynamicColors.IsLightColor(Palette.PlayerColors[player.TargetPlayerId]);

                GameObject template = player.Buttons.transform.Find("CancelButton").gameObject;
                GameObject targetBox = UnityEngine.Object.Instantiate(template, player.transform);
                targetBox.name = "Color";
                targetBox.transform.localPosition = new Vector3(1.2f, -0.18f, -1f);
                SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                renderer.sprite = isLightColor ? GetLightColorSprite() : GetDarkColorSprite();
                UnityEngine.GameObject.Destroy(targetBox.GetComponent<PassiveButton>());

                if (player.TargetPlayerId != PlayerControl.LocalPlayer.PlayerId && CustomOptionHolder.hideVotedIcon.getBool() && CustomOptionHolder.meetingOptions.getBool())
                {
                    player.Flag.gameObject.SetActive(false);
                }
            }

            Game.GameData.data.UtilityTimer.OnMeetingStart(__instance);
        }
    }


    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Select))]
    class SelectedPlayerVoteAreaPatch
    {
        static bool Prefix(PlayerVoteArea __instance)
        {
            if (Game.GameData.data.GetPlayerData(__instance.TargetPlayerId) != null)
            {
                return Game.GameData.data.GameMode != Module.CustomGameMode.Investigators;
            }
            //Skipは常に有効
            return true;
        }
    }

    //直近の投票結果を返します。
    public static int GetVoteResult(byte playerId)
    {
        if (VoteHistory.ContainsKey(playerId))
        {
            return VoteHistory[playerId];
        }
        return 0;
    }

    public static byte[] GetVoters(byte playerId)
    {
        if (Voters.ContainsKey(playerId))
        {
            return Voters[playerId].ToArray();
        }
        return new byte[0];
    }

    public static void SetVoteWeight(byte playerId, byte weight)
    {
        VoteWeight[playerId] = weight;
    }

    public static int GetVoteWeight(byte playerId)
    {
        if (VoteWeight.ContainsKey(playerId))
            return VoteWeight[playerId];
        else
            return 1;
    }

    public static void Initialize()
    {
        VoteHistory.Clear();
        Voters.Clear();
        VoteWeight.Clear();
    }


    //ミーティングアニメーション

    //ミーティング開始アニメーション

    [HarmonyPatch(typeof(MeetingIntroAnimation), nameof(MeetingIntroAnimation.CoRun))]
    class MeetingIntroCoRunPatch
    {
        public static void Postfix(MeetingIntroAnimation __instance, Il2CppSystem.Collections.IEnumerator __result)
        {
            
        }
    }

    /*
    //追放後アニメーション

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.CoRun))]
    class MeetingOutroPatch
    {
        public static void Postfix(MeetingIntroAnimation __instance, Il2CppSystem.Collections.IEnumerator __result)
        {

        }
    }
    */
}