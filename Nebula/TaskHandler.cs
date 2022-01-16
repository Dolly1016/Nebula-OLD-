﻿using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System;
using UnhollowerBaseLib;

namespace Nebula
{
    [HarmonyPatch]
    public static class TasksHandler
    {

        [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.FixedUpdate))]
        public static class NormalPlayerTaskPatch
        {
            public static void Postfix(NormalPlayerTask __instance)
            {
                if (__instance.IsComplete && __instance.Arrow?.isActiveAndEnabled == true)
                    __instance.Arrow?.gameObject?.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(AirshipUploadTask), nameof(AirshipUploadTask.FixedUpdate))]
        public static class AirshipUploadTaskPatch
        {
            public static void Postfix(AirshipUploadTask __instance)
            {
                if (__instance.IsComplete)
                    __instance.Arrows?.DoIf(x => x != null && x.isActiveAndEnabled, x => x.gameObject?.SetActive(false));
            }
        }

        public static Tuple<int, int> taskInfo(GameData.PlayerInfo playerInfo)
        {
            int TotalTasks = 0;
            int CompletedTasks = 0;
            if (!playerInfo.Disconnected && playerInfo.Tasks != null &&
                playerInfo.Object &&
                (PlayerControl.GameOptions.GhostsDoTasks || !playerInfo.IsDead) &&
                playerInfo.Role && playerInfo.Role.TasksCountTowardProgress &&
                !playerInfo.GetModData().role.hasFakeTask
                )
            {

                for (int j = 0; j < playerInfo.Tasks.Count; j++)
                {
                    TotalTasks++;
                    if (playerInfo.Tasks[j].Complete)
                    {
                        CompletedTasks++;
                    }
                }
            }
            return Tuple.Create(CompletedTasks, TotalTasks);
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
        private static class GameDataRecomputeTaskCountsPatch
        {
            private static bool Prefix(GameData __instance)
            {
                __instance.TotalTasks = 0;
                __instance.CompletedTasks = 0;
                for (int i = 0; i < __instance.AllPlayers.Count; i++)
                {
                    
                    GameData.PlayerInfo playerInfo = __instance.AllPlayers[i];
                    /*
                    if (playerInfo.Object &&
                        ((playerInfo.Object?.isLovers() == true && !Lovers.hasTasks) ||
                         (playerInfo.PlayerId == Shifter.shifter?.PlayerId && Shifter.isNeutral) || // Neutral shifter has tasks, but they don't count
                          playerInfo.PlayerId == Lawyer.lawyer?.PlayerId || // Tasks of the Lawyer do not count
                         (playerInfo.PlayerId == Pursuer.pursuer?.PlayerId && Pursuer.pursuer.Data.IsDead) // Tasks of the Pursuer only count, if he's alive
                        )
                    )
                        continue;
                    */
                    var (playerCompleted, playerTotal) = taskInfo(playerInfo);
                    __instance.TotalTasks += playerTotal;
                    __instance.CompletedTasks += playerCompleted;
                }
                return false;
            }
        }


    }
}
