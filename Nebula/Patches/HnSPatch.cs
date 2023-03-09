using Nebula.Game;
using Nebula.Roles.Perk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Purchasing;

namespace Nebula.Patches;

[HarmonyPatch]
public static class HnsPatch
{
    //Mod独自のカウントプロセサを使用
    [HarmonyPatch(typeof(HideAndSeekManager), nameof(HideAndSeekManager.FinishTask)), HarmonyPrefix]
    static bool FinishTaskBlocker()
    {
        return false;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcCompleteTask)), HarmonyPrefix]
    static void HnSTaskCountProcessor(PlayerControl __instance, [HarmonyArgument(0)] uint idx)
    {
        if (!HnSModificator.IsHnSGame) return;

        var task = __instance.myTasks.Find((Il2CppSystem.Predicate<PlayerTask>)((PlayerTask p) => p.Id == idx)).TryCast<NormalPlayerTask>();
        if (task == null) return;

        float deduction = 0f;
        var manager = GameManager.Instance.CastFast<HideAndSeekManager>();

        switch (task.Length)
        {
            case NormalPlayerTask.TaskLength.None:
            case NormalPlayerTask.TaskLength.Common:
                deduction = manager.LogicOptionsHnS.GetCommonTaskTimeValue();
                break;
            case NormalPlayerTask.TaskLength.Short:
                deduction = manager.LogicOptionsHnS.GetShortTaskTimeValue();
                break;
            case NormalPlayerTask.TaskLength.Long:
                deduction = manager.LogicOptionsHnS.GetLongTaskTimeValue();
                break;
        }

        float additional = 0f, ratio = 1f;
        PerkHolder.PerkData.GeneralPerkAction((p, id) => p.Perk.OnCompleteHnSTaskGlobal(id, ref additional, ref ratio));
        PerkHolder.PerkData.GeneralPerkAction((p, id) => p.Perk.OnCompleteHnSTaskLocal(p,ref additional, ref ratio));

        if (ratio < 0f) ratio = 0f;

        deduction += additional;
        if (deduction < 0f) deduction = 0f;
        deduction *= ratio;

        HnSModificator.ProceedTimer.Invoke(new HnSModificator.HnSTaskBonusMessage() { TimeDeduction = deduction, CanProceedFinalTimer = false, IsFinishTaskBonus = true });
    }

    [HarmonyPatch(typeof(LogicOptionsHnS), nameof(LogicOptionsHnS.GetPlayerSpeedMod)), HarmonyPrefix]
    static bool GetHnSPlayerSpeedMod(LogicOptionsHnS __instance, ref float __result, [HarmonyArgument(0)] PlayerControl pc)
    {
        if (pc == null || pc.Data == null || pc.Data.Role == null)
        {
            __result = __instance.GameOptions.PlayerSpeedMod;
            return false;
        }

        if (pc.Data.IsDead)
        {
            __result = __instance.GameOptions.PlayerSpeedMod + 1f;
            return false;
        }

        if (pc.Data.Role.IsImpostor)
        {
            __result = __instance.GameOptions.PlayerSpeedMod * 1.05f;
            if (__instance.HnSManager.LogicFlowHnS.IsFinalCountdown) __result *= __instance.GameOptions.SeekerFinalSpeed;
        }
        else
        {
            __result = __instance.GameOptions.PlayerSpeedMod;
        }

        return false;
    }

   static IEnumerator GetAlignEnumerator()
    {
        int[] perkIdAry = new int[5];
        for (int i = 0; i < 5; i++) perkIdAry[i] = Perk.GetEquipedPerk(i, !PlayerControl.LocalPlayer.Data.Role.IsImpostor)?.Id ?? -1;

        Roles.Perk.PerkHolder.SharePerks.Invoke(new PerkHolder.SharePerkMessage()
        {
            playerId = PlayerControl.LocalPlayer.PlayerId,
            perks = perkIdAry
        });

        if (NebulaOption.configGameControl.Value) yield break;

        bool sync = false;

        while (!Game.GameData.data.SynchronizeData.Align(Game.SynchronizeTag.HnSInitialize, false))
        {
            if (!sync && PerkHolder.PerkData.AllPerkData.Count == PlayerControl.AllPlayerControls.Count)
            {
                RPCEventInvoker.Synchronize(Game.SynchronizeTag.HnSInitialize, PlayerControl.LocalPlayer.PlayerId);
                sync = true;
            }
            yield return null;
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin)), HarmonyPostfix]
    public static void HnSPerkSync(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (!HnSModificator.IsHnSGame) return;

        Game.HnSModificator.HideAndSeekManager = HideAndSeekManager.Instance.CastFast<HideAndSeekManager>();
        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator()) if (p.Data.Role.IsImpostor) Game.HnSModificator.Seeker = p;

        __result = Effects.Sequence(GetAlignEnumerator().WrapToIl2Cpp(), __result);
    }

    static float OrigDangerDistance1;
    static float OrigDangerDistance2;

    [HarmonyPatch(typeof(LogicHnSDangerLevel), nameof(LogicHnSDangerLevel.OnGameStart)), HarmonyPostfix]
    public static void HnSDangerLevelStartPatch(LogicHnSDangerLevel __instance)
    {
        OrigDangerDistance1 = __instance.scaryMusicDistance;
        OrigDangerDistance2 = __instance.veryScaryMusicDistance;
    }

    [HarmonyPatch(typeof(LogicHnSDangerLevel), nameof(LogicHnSDangerLevel.FixedUpdate)), HarmonyPrefix]
    public static void HnSDangerLevelUpdatePatch(LogicHnSDangerLevel __instance)
    {
        float additional = 0f, ratio = 1f;
        PerkHolder.PerkData.GeneralPerkAction((p, id) => p.Perk.EditGlobalIntimidation(id, ref additional, ref ratio));
        PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.EditLocalIntimidation(p, ref additional, ref ratio));

        __instance.scaryMusicDistance = (OrigDangerDistance1 + additional) * ratio;
        __instance.veryScaryMusicDistance = (OrigDangerDistance2 + additional) * ratio;
    }

    [HarmonyPatch(typeof(LogicOptionsHnS), nameof(LogicOptionsHnS.GetMaxPingTime)), HarmonyPostfix]
    public static void HnSGetMaxPingTimePatch(LogicOptionsHnS __instance,float __result)
    {
        float additional = 0f, ratio = 1f;
        PerkHolder.PerkData.GeneralPerkAction((p, id) => p.Perk.EditPingInterval(id, ref additional, ref ratio));

        __result = (__result + additional) * ratio;
    }

    [HarmonyPatch(typeof(LogicPingsHnS), nameof(LogicPingsHnS.SeekerPing)), HarmonyPrefix]
    public static bool ModSeekerPing(LogicPingsHnS __instance,ref Il2CppSystem.Collections.IEnumerator __result)
    {
        IEnumerator SeekerPing()
        {
            while (__instance.Manager.GameHasStarted)
            {
                foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    if (p.Data.Role.TeamType == RoleTeamTypes.Crewmate && !p.Data.IsDead && (PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor || p == PlayerControl.LocalPlayer))
                    {
                        
                        bool canPing = true;
                        PerkHolder.PerkData.AllPerkData[p.PlayerId].PerkAction(perk => canPing &= perk.Perk.CanPing(perk, p.PlayerId));
                        if (!canPing) continue;

                        PingBehaviour pingBehaviour = __instance.pingPool.Get<PingBehaviour>();
                        pingBehaviour.target = p.GetTruePosition();
                        pingBehaviour.AmSeeker = (PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor);
                        pingBehaviour.UpdatePosition();
                        pingBehaviour.gameObject.SetActive(true);
                        pingBehaviour.SetImageEnabled(true);
                    }
                }
                yield return new WaitForSeconds(__instance.options.GetShowPingTime());
                foreach (PoolableBehavior poolableBehavior in __instance.pingPool.activeChildren)
                {
                    ArrowBehaviour arrowBehaviour = (ArrowBehaviour)poolableBehavior;
                    arrowBehaviour.target = Vector3.zero;
                    arrowBehaviour.SetImageEnabled(false);
                    arrowBehaviour.gameObject.SetActive(false);
                }
                float maxPingTime = __instance.options.GetMaxPingTime();
                yield return new WaitForSeconds(maxPingTime);
            }
            yield break;
        }

        __result = SeekerPing().WrapToIl2Cpp();

        return false;
    }

    [HarmonyPatch(typeof(HideAndSeekTimerBar), nameof(HideAndSeekTimerBar.StartFinalHide)), HarmonyPostfix]
    public static void AlwaysShowChunkBar(HideAndSeekTimerBar __instance)
    {
        __instance.chunkBar.gameObject.SetActive(true);
    }
}
