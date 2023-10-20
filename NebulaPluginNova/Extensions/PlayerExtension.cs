using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Nebula.Modules;
using Nebula.Utilities;
using UnityEngine.Networking.PlayerConnection;

namespace Nebula.Extensions;

[NebulaRPCHolder]
public static class PlayerExtension
{

    public static IEnumerator CoDive(this PlayerControl player)
    {

        player.MyPhysics.body.velocity = Vector2.zero;
        if (player.AmOwner) player.MyPhysics.inputHandler.enabled = true;
        player.cosmetics.skin.SetEnterVent(player.cosmetics.FlipX);
        player.moveable = false;

        yield return player.MyPhysics.Animations.CoPlayEnterVentAnimation();

        player.MyPhysics.myPlayer.Visible = false;
        player.cosmetics.skin.SetIdle(player.cosmetics.FlipX);
        player.MyPhysics.Animations.PlayIdleAnimation();
        player.moveable = true;

        player.currentRoleAnimations.ForEach((Action<RoleEffectAnimation>)((an) => an.ToggleRenderer(false)));
        if (player.AmOwner) player.MyPhysics.inputHandler.enabled = false;
    }

    public static IEnumerator CoGush(this PlayerControl player)
    {

        player.MyPhysics.body.velocity = Vector2.zero;
        if (player.AmOwner) player.MyPhysics.inputHandler.enabled = true;
        player.moveable = false;
        player.MyPhysics.myPlayer.Visible = true;
        player.cosmetics.AnimateSkinExitVent();

        yield return player.MyPhysics.Animations.CoPlayExitVentAnimation();

        player.cosmetics.AnimateSkinIdle();
        player.MyPhysics.Animations.PlayIdleAnimation();
        player.moveable = true;
        player.currentRoleAnimations.ForEach((Action<RoleEffectAnimation>)((an) => an.ToggleRenderer(true)));
        if (player.AmOwner) player.MyPhysics.inputHandler.enabled = false;
    }

    public static void StopAllAnimations(this CosmeticsLayer layer)
    {
        try
        {
            if (layer.skin.animator) layer.skin.animator.Stop();
            if (layer.currentPet.animator) layer.currentPet.animator.Stop();
        }
        catch { }
    }

    static RemoteProcess<(byte killerId, byte targetId, int stateId, int recordId, bool blink,bool showOverlay)> RpcKill = new(
        "Kill",
       (message, _) =>
       {
           var recordTag = TranslatableTag.ValueOf(message.recordId);
           if (recordTag != null)
               NebulaGameManager.Instance?.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.Kill, message.killerId, 1 << message.targetId) { RelatedTag = recordTag });

           var killer = Helpers.GetPlayer(message.killerId);
           var target = Helpers.GetPlayer(message.targetId);

           if (killer == null || target == null) return;

           // MurderPlayer ここから

           if (killer.AmOwner)
           {
               if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(killer.KillSfx, false, 0.8f, null);
               killer.SetKillTimer(AmongUsUtil.VanillaKillCoolDown);
           }

           target.gameObject.layer = LayerMask.NameToLayer("Ghost");

           if (target.AmOwner)
           {
               StatsManager.Instance.IncrementStat(StringNames.StatsTimesMurdered);
               if (Minigame.Instance)
               {
                   try
                   {
                       Minigame.Instance.Close();
                       Minigame.Instance.Close();
                   }
                   catch
                   {
                   }
               }
               if(message.showOverlay)DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(killer.Data, target.Data);
               target.cosmetics.SetNameMask(false);
               target.RpcSetScanner(false);
           }
           killer.MyPhysics.StartCoroutine(killer.KillAnimations[System.Random.Shared.Next(killer.KillAnimations.Count)].CoPerformModKill(killer, target, message.blink).WrapToIl2Cpp());

           // MurderPlayer ここまで


           var targetInfo = target.GetModInfo();
           var killerInfo = killer.GetModInfo();

           if (targetInfo != null)
           {
               targetInfo.DeathTimeStamp = NebulaGameManager.Instance!.CurrentTime;
               targetInfo.MyKiller = killerInfo;
               targetInfo.MyState = TranslatableTag.ValueOf(message.stateId);
               targetInfo?.RoleAction(role =>
               {
                   role.OnMurdered(killer!);
                   role.OnDead();
               });
           }
           if (killerInfo != null) killerInfo.RoleAction(r => r.OnKillPlayer(target));

           PlayerControl.LocalPlayer.GetModInfo()?.RoleAction(r => r.OnPlayerDeadLocal(target));
       }
       );

    static RemoteProcess<(byte killerId, byte targetId, int stateId, int recordId, bool showOverlay)> RpcMeetingKill = new(
        "NonPhysicalKill",
       (message, _) =>
       {
           var recordTag = TranslatableTag.ValueOf(message.recordId);
           if (recordTag != null)
               NebulaGameManager.Instance?.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.Kill, message.killerId, 1 << message.targetId) { RelatedTag = recordTag });

           var killer = Helpers.GetPlayer(message.killerId);
           var target = Helpers.GetPlayer(message.targetId);

           if (killer == null || target == null) return;

           if (!target.AmOwner)
               if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(killer.KillSfx, false, 0.8f, null);


           target.Die(DeathReason.Exile, false);

           if (target.AmOwner)
           {
               DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(killer.Data, target.Data);
               NebulaGameManager.Instance!.CanSeeAllInfo = true;
           }


           if (MeetingHud.Instance != null)
           {
               MeetingHud.Instance.ResetPlayerState();

               if (AmongUsClient.Instance.AmHost) MeetingHud.Instance.CheckForEndVoting();
           }


           var targetInfo = target.GetModInfo();
           var killerInfo = killer.GetModInfo();

           if (targetInfo != null)
           {
               targetInfo.DeathTimeStamp = NebulaGameManager.Instance!.CurrentTime;
               targetInfo.MyKiller = killerInfo;
               targetInfo.MyState = TranslatableTag.ValueOf(message.stateId);
               targetInfo?.RoleAction(role =>
               {
                   role.OnMurdered(killer!);
                   role.OnDead();
               });
           }
           if (killerInfo != null) killerInfo.RoleAction(r => r.OnKillPlayer(target));

           PlayerControl.LocalPlayer.GetModInfo()?.RoleAction(r => r.OnPlayerDeadLocal(target));

           if (MeetingHud.Instance)
           {
               IEnumerator CoGainDiscussionTime()
               {
                   for(int i = 0; i < 10; i++)
                   {
                       MeetingHud.Instance!.discussionTimer -= 1f;
                       MeetingHud.Instance!.lastSecond = 10;
                       yield return new WaitForSeconds(0.1f);
                   }
               }
               NebulaManager.Instance!.StartCoroutine(CoGainDiscussionTime().WrapToIl2Cpp());
           }
       }
       );

    static RemoteProcess<(byte exiledId, byte sourceId, TranslatableTag stateId, TranslatableTag recordId)> RpcMarkAsExtraVictim = new(
        "MarkAsExtraVictim",
        (message, _) => MeetingHudExtension.ExtraVictims.Add(message)
        );

    static public void ModKill(this PlayerControl killer, PlayerControl target, bool showBlink, TranslatableTag playerState, TranslatableTag? recordState, bool showOverlay = true)
    {
        RpcKill.Invoke((killer.PlayerId, target.PlayerId, playerState.Id, recordState?.Id ?? int.MaxValue, showBlink,showOverlay));
    }

    static public void ModMeetingKill(this PlayerControl killer, PlayerControl target, bool showOverlay, TranslatableTag playerState, TranslatableTag? recordState)
    {
        RpcMeetingKill.Invoke((killer.PlayerId, target.PlayerId, playerState.Id, recordState?.Id ?? int.MaxValue, showOverlay));
    }

    static public void ModMarkAsExtraVictim(this PlayerControl exiled,PlayerControl? source, TranslatableTag playerState,TranslatableTag recordState)
    {
        RpcMarkAsExtraVictim.Invoke((exiled.PlayerId, source?.PlayerId ?? byte.MaxValue, playerState, recordState));

    }

    static public void ModDive(this PlayerControl player, bool isDive = true)
    {
        RpcDive.Invoke((player.PlayerId,isDive));
    }

    static RemoteProcess<(byte sourceId, byte targetId, Vector2 revivePos, bool cleanDeadBody,bool recordEvent)> RpcRivive = new(
        "Rivive",
        (message, _) =>
        {
            var player = Helpers.GetPlayer(message.targetId);
            if (!player) return;

            player!.Revive();
            player.NetTransform.SnapTo(message.revivePos);
            player.GetModInfo()!.MyState = PlayerState.Revived;
            if (message.cleanDeadBody) foreach (var d in Helpers.AllDeadBodies()) if (d.ParentId == player.PlayerId) GameObject.Destroy(d.gameObject);

            if(message.recordEvent)NebulaGameManager.Instance?.GameStatistics.RecordEvent(new(GameStatistics.EventVariation.Revive, message.sourceId != byte.MaxValue ? message.sourceId : null, 1 << message.targetId) { RelatedTag = EventDetail.Revive });
        }
        );

    static RemoteProcess<(byte playerId, bool isDive)> RpcDive = new(
        "Dive",
        (message, _) =>
        {
            var player = Helpers.GetPlayer(message.playerId);
            if (!player) return;
            player?.StartCoroutine(message.isDive ? player.CoDive() : player.CoGush());
        }
        );

    static public void ModRevive(this PlayerControl player, Vector2 pos, bool cleanDeadBody,bool recordEvent)
    {
        RpcRivive.Invoke((byte.MaxValue, player.PlayerId, pos, cleanDeadBody,recordEvent));
    }

    static public void ModRevive(this PlayerControl player, PlayerControl healer, Vector2 pos, bool cleanDeadBody)
    {
        RpcRivive.Invoke((healer.PlayerId, player.PlayerId, pos, cleanDeadBody, true));
    }
}
