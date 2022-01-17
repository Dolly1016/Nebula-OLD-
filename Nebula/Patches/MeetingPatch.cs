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

        private static TMPro.TextMeshPro meetingInfoText;

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
                if (meetingInfoText != null)
                    meetingInfoText.gameObject.SetActive(false);
            }
        }
        
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Awake))]
        class MeetingCalculateVotesPatch
        {
            static void Postfix(MeetingHud __instance)
            {
                Events.GlobalEvent.OnMeeting();
                Events.LocalEvent.OnMeeting();
                Events.Schedule.OnPreMeeting();

                Game.GameData.data.myData.getGlobalData().role.OnMeetingStart();
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart))]
        class MeetingServerStartPatch
        {
            static void Postfix(MeetingHud __instance)
            {
                PlayerControl.LocalPlayer.GetModData().role.SetupMeetingButton(__instance);
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Deserialize))]
        class MeetingDeserializePatch
        {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] MessageReader reader, [HarmonyArgument(1)] bool initialState)
            {
                Events.Schedule.OnPostMeeting();

                VoteHistory.Clear();
                Voters.Clear();

                foreach(PlayerVoteArea player in __instance.playerStates)
                {
                    if (!VoteHistory.ContainsKey(player.VotedFor))
                    {
                        VoteHistory[player.VotedFor] = 1;
                    }
                    else
                    {
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
        
        public static void Initialize()
        {
            VoteHistory.Clear();
            Voters.Clear();
        }
    }
}
