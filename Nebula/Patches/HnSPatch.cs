using Nebula.Expansion;
using Nebula.Game;
using Nebula.Map;
using Nebula.Module;
using Nebula.Roles;
using Nebula.Roles.Perk;
using Nebula.Utilities;
using PowerTools;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using static Nebula.Roles.Perk.PerkHolder;
using static Rewired.Data.Mapping.CustomCalculation_Accelerometer;

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
        PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.OnCompleteHnSTaskLocal(p, ref additional, ref ratio));

        if (ratio < 0f) ratio = 0f;

        deduction += additional;
        if (deduction < 0f) deduction = 0f;
        deduction *= ratio;

        HnSModificator.ProceedTimer.Invoke(new HnSModificator.HnSTaskBonusMessage() { TimeDeduction = deduction, CanProceedFinalTimer = false, IsFinishTaskBonus = true, ContributorId = PlayerControl.LocalPlayer.PlayerId });
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
            __result = __instance.GameOptions.PlayerSpeedMod * 1.2f;
            if (__instance.HnSManager.LogicFlowHnS.IsFinalCountdown) __result *= __instance.GameOptions.SeekerFinalSpeed;
        }
        else
        {
            __result = __instance.GameOptions.PlayerSpeedMod;
        }

        return false;
    }

   static IEnumerator GetAlignEnumerator(IntroCutscene __instance)
    {
        int[] perkIdAry = new int[5];
        for (int i = 0; i < 5; i++) perkIdAry[i] = PerkSaver.GetEquipedAbilityPerk(i, !PlayerControl.LocalPlayer.Data.Role.IsImpostor)?.Id ?? -1;
        var seekerRole = PerkSaver.GetEquipedRolePerk(0, false)?.RelatedRole ?? null;
        if (seekerRole == null)
        {
            var ary = Perks.AllRolePerks.Values.Where((p) => p.IsAvailable).ToArray();
            seekerRole = ary[NebulaPlugin.rnd.Next(ary.Length)].RelatedRole;
        }
        Roles.Perk.PerkHolder.SharePerks.Invoke(new PerkHolder.SharePerkMessage()
        {
            playerId = PlayerControl.LocalPlayer.PlayerId,
            perks = perkIdAry,
            choicedSeekerRole= seekerRole.id
        });
        
        bool sync = false;

        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator()) if(p.isDummy) PerkHolder.PerkData.AllPerkData[p.PlayerId] = new PerkData(p.PlayerId, new int[0]);

        while (!Game.GameData.data.SynchronizeData.Align(Game.SynchronizeTag.HnSInitialize, true,true,false))
        {
            if (!sync && PerkHolder.PerkData.AllPerkData.Count == PlayerControl.AllPlayerControls.Count)
            {
                RPCEventInvoker.Synchronize(Game.SynchronizeTag.HnSInitialize, PlayerControl.LocalPlayer.PlayerId);
                sync = true;
            }
            yield return null;
        }
        __instance.ImpostorTitle.GetComponent<TMPro.TextMeshPro>().text = Language.Language.GetString("role." + HnSModificator.Seeker.GetModData().role.LocalizeName + ".name");
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin)), HarmonyPostfix]
    public static void HnSPerkSync(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (!HnSModificator.IsHnSGame) return;

        if (CustomOptionHolder.GetCustomGameMode() == Module.CustomGameMode.FreePlayHnS)
        {
            void ReassignRole(bool playImpostor)
            {
                byte impostorId = playImpostor ? PlayerControl.LocalPlayer.PlayerId : PlayerControl.AllPlayerControls.Find((Il2CppSystem.Predicate<PlayerControl>)((p) => p.PlayerId != PlayerControl.LocalPlayer.PlayerId)).PlayerId;
                foreach(var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    var isImpostor = p.PlayerId == impostorId;
                    Game.GameData.data.GetPlayerData(p.PlayerId).role =
                        isImpostor ? (PerkSaver.GetEquipedRolePerk(0, false)?.RelatedRole ?? Roles.Roles.HnSReaper) : Roles.Roles.HnSCrewmate;
                    RoleManager.Instance.SetRole(p, isImpostor ? RoleTypes.Impostor : RoleTypes.Crewmate);
                    p.MyPhysics.SetBodyType(isImpostor ? PlayerBodyTypes.Seeker : PlayerBodyTypes.Normal);
                }
            }
            IEnumerator CoBeginFreeplay()
            {
                var crewPanel = GameObject.Instantiate(RuntimePrefabs.PlayerDisplayPrefab, __instance.transform);
                var seekPanel = GameObject.Instantiate(RuntimePrefabs.PlayerDisplayPrefab, __instance.transform);
                var panels = new[] { crewPanel, seekPanel };
                crewPanel.transform.localPosition = new Vector3(-1.5f,0.2f,-50f);
                seekPanel.transform.localPosition = new Vector3(1.5f, 0.2f, -50f);

                bool breakFlag = false;

                foreach (var p in panels)
                {
                    p.gameObject.SetActive(true);
                    p.SetLayer(LayerExpansion.GetUILayer());
                }

                crewPanel.SetBodyType(PlayerBodyTypes.Normal);
                seekPanel.SetBodyType(PlayerBodyTypes.Seeker);

                var textPrefab = MapData.MapDatabase[4].Assets.CastFast<AirshipStatus>().SpawnInGame.LocationButtons[0].GetComponentInChildren<TextMeshPro>();

                foreach (var p in panels)
                {
                    p.gameObject.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                    var text = GameObject.Instantiate(textPrefab,p.transform);
                    text.transform.localPosition = new Vector3(0f, -0.8f, -10f);
                    text.transform.localScale = new Vector3(1f / 1.2f, 1f / 1.2f, 1f);
                    text.text = Language.Language.GetString(p.Cosmetics.bodyType == PlayerBodyTypes.Normal ? "role.hider.name" : "role.seeker.name");


                    p.UpdateFromPlayerOutfit(PlayerControl.LocalPlayer,false,false);

                    var group = p.Animations.animationGroups.Find((Il2CppSystem.Predicate<PlayerAnimationGroup>)((group) => group.BodyType == p.Cosmetics.bodyType));

                    group.SpriteAnimator.Play(group.IdleAnim, 1f);

                    p.gameObject.AddComponent<BoxCollider2D>().size=new Vector2(1f,1f);
                    var myPanel = p;
                    var button = p.gameObject.SetUpButton(() =>
                    {
                        ReassignRole(myPanel.Cosmetics.bodyType == PlayerBodyTypes.Seeker);
                        SoundManager.Instance.PlaySound(MetaDialog.getSelectClip(), false, 0.8f);
                        breakFlag = true;
                    });

                    button.OnMouseOver.AddListener((UnityAction)(() => {
                        group.SpriteAnimator.Play(group.RunAnim, 1f);
                        
                        myPanel.Cosmetics.AnimateSkinRun();
                        text.color = Palette.AcceptedGreen;
                        SoundManager.Instance.PlaySound(MetaDialog.getHoverClip(), false, 0.8f);
                    }));
                    button.OnMouseOut.AddListener((UnityAction)(() => {
                        group.SpriteAnimator.Play(group.IdleAnim, 1f);
                        myPanel.Cosmetics.AnimateSkinIdle();
                        text.color = Color.white;
                    }));

                    p.Cosmetics.ToggleName(false);
                }
                seekPanel.Cosmetics.SetBodyCosmeticsVisible(false);
                crewPanel.Cosmetics.SetBodyCosmeticsVisible(true);

                while (!breakFlag) yield return null;

                Game.HnSModificator.Initialize();

                yield return GetAlignEnumerator(__instance);

                GameObject.Destroy(__instance.gameObject);

                yield break;
            }

            __result = CoBeginFreeplay().WrapToIl2Cpp();
        }
        else
        {
            Game.HnSModificator.Initialize();

            __instance.ImpostorTitle.GetComponent<TextTranslatorTMP>().enabled = false;

            __result = Effects.Sequence(GetAlignEnumerator(__instance).WrapToIl2Cpp(), __result);
        }
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
        if (HnSModificator.Seeker.GetModData().Property.UnderTheFloor)
        {
            __instance.scaryMusicDistance = 0f;
            __instance.veryScaryMusicDistance = 0f;
            return;
        }

        bool unconditional = false;
        PerkHolder.PerkData.GeneralPerkAction((p, id) => unconditional |= p.Perk.UnconditionalIntimidationGlobal(p));
        PerkHolder.PerkData.MyPerkData.PerkAction((p) => unconditional |= p.Perk.UnconditionalIntimidationLocal(p));
        if (unconditional)
        {
            __instance.scaryMusicDistance = 2000f;
            __instance.veryScaryMusicDistance = 1000f;
        }
        else
        {
            float additional = 0f, ratio = 1f;
            PerkHolder.PerkData.GeneralPerkAction((p, id) => p.Perk.EditGlobalIntimidation(p, ref additional, ref ratio));
            PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.EditLocalIntimidation(p, ref additional, ref ratio));

            __instance.scaryMusicDistance = (OrigDangerDistance1 + additional) * ratio;
            __instance.veryScaryMusicDistance = (OrigDangerDistance2 + additional) * ratio;
        }
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

    [HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlowHnS.OnFinalCountdownTriggered)), HarmonyPrefix]
    public static bool HnSFinalCountdownPatch(LogicGameFlowHnS __instance)
    {
        //最終盤面でもタスクをこなせる

        __instance.timerBar.StartFinalHide();
        SoundManager.Instance.PlaySound(__instance.hideAndSeekManager.FinalHideAlertSFX, false, 1f, null);
        DestroyableSingleton<HudManager>.Instance.SetAlertFlash(true);

        return false;
    }
}
