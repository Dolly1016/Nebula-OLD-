using HarmonyLib;
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
            var p = playerInfo.GetModData();
            if(p!=null)return Tuple.Create(p.Tasks.Completed, p.Tasks.Quota);
           
            return Tuple.Create(0, 12);
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
                    GameData.PlayerInfo playerInfo = __instance.AllPlayers.get_Item(i);

                    //切断されたプレイヤーのタスクは数えない
                    if (Helpers.playerById(playerInfo.PlayerId) == null) continue;

                    if (!Helpers.HasModData(playerInfo.PlayerId)) continue;

                    bool hasFakeTask = false;
                    Helpers.RoleAction(playerInfo.PlayerId, (role) => { hasFakeTask |= !role.HasCrewmateTask(playerInfo.PlayerId); });
                    if (hasFakeTask) continue;

                    var (playerCompleted, playerTotal) = taskInfo(playerInfo);
                    __instance.TotalTasks += playerTotal;
                    __instance.CompletedTasks += playerCompleted;
                }
                return false;
            }
        }


    }
}
