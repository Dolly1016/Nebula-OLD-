using HarmonyLib;
using Nebula.Behaviour;
using Nebula.Game;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Patches;

public static class ModPreSpawnInPatch
{
    public static IEnumerator ModPreSpawnIn(Transform minigameParent)
    {
        if (NebulaPreSpawnMinigame.PreSpawnLocations.Length > 0)
        {
            NebulaPreSpawnMinigame spawnInMinigame = UnityHelper.CreateObject<NebulaPreSpawnMinigame>("PreSpawnInMinigame", minigameParent, new Vector3(0, 0, -600f), LayerExpansion.GetUILayer());
            spawnInMinigame.Begin(null);
            yield return NebulaGameManager.Instance.Syncronizer.CoSync(Modules.SynchronizeTag.PreSpawnMinigame, true, false, false);
            NebulaGameManager.Instance.Syncronizer.ResetSync(Modules.SynchronizeTag.PreSpawnMinigame);
            spawnInMinigame.Close();
        }
    }
}
public static class NebulaExileWrapUp
{
    static public IEnumerator WrapUpAndSpawn(ExileController __instance)
    {
        PlayerControl? @object = null;
        if (__instance.exiled != null)
        {
            @object = __instance.exiled.Object;
            if (@object) @object.Exiled();
            __instance.exiled.IsDead = true;
            NebulaGameManager.Instance?.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.Exile, null, 1 << __instance.exiled.PlayerId, GameStatisticsGatherTag.Spawn) { RelatedTag = EventDetail.Exiled });

            var info = @object.GetModInfo();
            if (info != null)
            {
                info.DeathTimeStamp = NebulaGameManager.Instance!.CurrentTime;
                info.RoleAction(role =>
                {
                    role.OnExiled();
                    role.OnDead();
                });

                PlayerControl.LocalPlayer.GetModInfo()?.RoleAction(r => r.OnPlayerDeadLocal(@object));
            }
        }

        NebulaGameManager.Instance?.OnMeetingEnd(__instance.exiled?.Object);

        yield return ModPreSpawnInPatch.ModPreSpawnIn(__instance.transform.parent);


        NebulaGameManager.Instance?.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.MeetingEnd, null, 0, GameStatisticsGatherTag.Spawn) { RelatedTag = EventDetail.MeetingEnd });

        NebulaGameManager.Instance?.AllRoleAction(r=>r.OnGameReenabled());

        __instance.ReEnableGameplay();
        GameObject.Destroy(__instance.gameObject);
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
public static class ExileWrapUpPatch
{
    static bool Prefix(ExileController __instance)
    {
        __instance.StartCoroutine(NebulaExileWrapUp.WrapUpAndSpawn(__instance).WrapToIl2Cpp());
        return false;
    }
}

[HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
public static class AirshipExileWrapUpPatch
{
    static bool Prefix(AirshipExileController __instance,ref Il2CppSystem.Collections.IEnumerator __result)
    {
        __result = NebulaExileWrapUp.WrapUpAndSpawn(__instance).WrapToIl2Cpp();
        return false;
    }
}
