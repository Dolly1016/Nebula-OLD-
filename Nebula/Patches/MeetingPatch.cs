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
        private static Dictionary<byte, int> VoteHistory=new Dictionary<byte, int>();
        private static Dictionary<byte, List<byte>> Voters = new Dictionary<byte, List<byte>>();

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Awake))]
        class MeetingCalculateVotesPatch
        {
            static void Postfix(MeetingHud __instance)
            {
                Events.GlobalEvent.OnMeeting();
                Events.LocalEvent.OnMeeting();
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Deserialize))]
        class MeetingDeserializePatch
        {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] MessageReader reader, [HarmonyArgument(1)] bool initialState)
            {
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
