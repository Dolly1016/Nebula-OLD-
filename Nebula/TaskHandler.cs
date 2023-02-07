namespace Nebula;

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
        if (p != null && p.Tasks != null) return Tuple.Create(p.Tasks.Completed, p.Tasks.Quota);

        return Tuple.Create(0, 0);
    }

    public static float TotalTasks { get; private set; }
    public static float CompletedTasks { get; private set; }
    public static float TotalAlivesTasks { get; private set; }
    public static float CompletedAlivesTasks { get; private set; }

    public static bool RecomputeTasks(GameData __instance)
    {
        __instance.TotalTasks = 0;
        __instance.CompletedTasks = 0;

        if (Game.GameData.data == null) return false;

        if (!Game.GameModeProperty.GetProperty(Game.GameData.data.GameMode).CountTasks) return false;

        for (int i = 0; i < __instance.AllPlayers.Count; i++)
        {
            GameData.PlayerInfo playerInfo = __instance.AllPlayers[i];

            //切断されたプレイヤーのタスクは数えない
            if (Helpers.playerById(playerInfo.PlayerId) == null) continue;

            if (!Helpers.HasModData(playerInfo.PlayerId)) continue;

            if ((playerInfo.GetModData().Tasks?.Quota ?? 0) == 0) continue;

            bool hasFakeTask = false;
            Helpers.RoleAction(playerInfo.PlayerId, (role) => { hasFakeTask |= !role.HasCrewmateTask(playerInfo.PlayerId); });
            if (hasFakeTask) continue;

            var (playerCompleted, playerTotal) = taskInfo(playerInfo);
            if (playerInfo.GetModData().Tasks.IsInfiniteCrewmateTasks) playerTotal = 9999;

            __instance.TotalTasks += playerTotal;
            __instance.CompletedTasks += playerCompleted;
            TotalTasks += playerTotal;
            CompletedTasks += playerCompleted;
            if (!playerInfo.IsDead)
            {
                TotalAlivesTasks += playerTotal;
                CompletedAlivesTasks += playerCompleted;
            }
        }
        return false;
    }

}