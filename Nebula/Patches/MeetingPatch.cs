using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Hazel;

namespace Nebula.Patches
{
    [HarmonyPatch]
    class MeetingHudPatch
    {
        //最新の会議での得票数記録
        private static Dictionary<byte, int> VoteHistory = new Dictionary<byte, int>();
        private static Dictionary<byte, List<byte>> Voters = new Dictionary<byte, List<byte>>();
        private static Dictionary<byte, byte> VoteWeight = new Dictionary<byte, byte>();

        private static TMPro.TextMeshPro meetingInfoText;

        [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetVote))]
        class PlayerVoteAreaSelectPatch
        {
         
            static void Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte suspectIdx)
            {
                Helpers.RoleAction(PlayerControl.LocalPlayer,(role)=> {
                    role.OnVote(suspectIdx);
                });
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
        class MeetingCalculateVotesPatch
        {
            private static void CalculateVotes(ref Dictionary<byte, int> dictionary, MeetingHud __instance)
            {
                NebulaPlugin.Instance.Logger.Print("print");

                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.VotedFor != 252 && playerVoteArea.VotedFor != 255 && playerVoteArea.VotedFor != 254)
                    {
                        PlayerControl player = Helpers.playerById((byte)playerVoteArea.TargetPlayerId);
                        if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;

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
            }


            static bool Prefix(MeetingHud __instance)
            {
                if (__instance.playerStates.All((PlayerVoteArea ps) => ps.AmDead || ps.DidVote))
                {
                    CalculateVotes(ref VoteHistory,__instance);
                    bool tie;
                    KeyValuePair<byte, int> max = VoteHistory.MaxPair(out tie);
                    GameData.PlayerInfo exiled = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(v => !tie && v.PlayerId == max.Key && !v.IsDead);

                    int sum = 0;
                    foreach(int value in VoteHistory.Values)
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

                            NebulaPlugin.Instance.Logger.Print("weight:" + weight);
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
            static void Postfix(MeetingHud __instance)
            {
                if (meetingInfoText == null)
                {
                    meetingInfoText = UnityEngine.Object.Instantiate(HudManager.Instance.TaskText, __instance.transform);
                    meetingInfoText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
                    meetingInfoText.transform.position = Vector3.zero;
                    meetingInfoText.transform.localPosition = new Vector3(-3.07f, 3.33f, -20f);
                    meetingInfoText.transform.localScale *= 1.1f;
                    meetingInfoText.color = Palette.White;
                    meetingInfoText.gameObject.SetActive(false);
                }

                meetingInfoText.text = "";
                meetingInfoText.gameObject.SetActive(false);

                Helpers.RoleAction(PlayerControl.LocalPlayer, (role) =>
                {
                    role.MeetingUpdate(__instance,meetingInfoText);
                });
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
        class MeetingHudVotingCompletedPatch
        {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] byte[] states, [HarmonyArgument(1)] GameData.PlayerInfo exiled, [HarmonyArgument(2)] bool tie)
            {
                if (meetingInfoText != null)meetingInfoText.gameObject.SetActive(false);

                Events.Schedule.OnPostMeeting();

                VoteHistory.Clear();
                Voters.Clear();

                foreach (PlayerVoteArea player in __instance.playerStates)
                {
                    if (!VoteHistory.ContainsKey(player.VotedFor))
                    {
                        VoteHistory[player.VotedFor] = 0;
                    }

                    if (VoteWeight.ContainsKey(player.TargetPlayerId))
                    {
                        VoteHistory[player.VotedFor] += VoteWeight[player.TargetPlayerId];
                    }
                    else {
                        VoteHistory[player.VotedFor]++;
                    }

                    if (!Voters.ContainsKey(player.VotedFor))
                    {
                        Voters[player.VotedFor] = new List<byte>();
                    }
                    else
                    {
                        Voters[player.VotedFor].Add(player.TargetPlayerId);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart))]
        class MeetingServerStartPatch
        {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] byte reporter) { 
                //票の重み設定をリセット
                VoteWeight.Clear();

                Events.GlobalEvent.OnMeeting();
                Events.LocalEvent.OnMeeting();
                Events.Schedule.OnPreMeeting();

                EmergencyPatch.MeetingUpdate();

                PlayerControl.LocalPlayer.GetModData().role.SetupMeetingButton(MeetingHud.Instance);

                Game.GameData.data.myData.getGlobalData().role.OnMeetingStart();

                foreach (Game.PlayerData player in Game.GameData.data.players.Values)
                {
                    player.Speed.OnMeeting();
                }
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
        
        public static void SetVoteWeight(byte playerId,byte weight)
        {
            VoteWeight[playerId] = weight;
        }

        public static void Initialize()
        {
            VoteHistory.Clear();
            Voters.Clear();
            VoteWeight.Clear();
        }
    }
}
