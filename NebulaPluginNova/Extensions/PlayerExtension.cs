using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using Nebula.Modules;
using Nebula.Utilities;
using UnityEngine.Networking.PlayerConnection;

namespace Nebula.Extensions;

[NebulaRPCHolder]
public static class PlayerExtension
{

    public static void StopAllAnimations(this CosmeticsLayer layer)
    {
        try
        {
            if (layer.skin.animator) layer.skin.animator.Stop();
            if (layer.currentPet.animator) layer.currentPet.animator.Stop();
        }
        catch { }
    }

    static RemoteProcess<(byte killerId, byte targetId, int stateId, int recordId, bool blink)> RpcKill = new(
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
               DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(killer.Data, target.Data);
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
               NebulaGameManager.Instance.CanSeeAllInfo = true;
           }


           if (MeetingHud.Instance != null)
           {
               MeetingHud.Instance.RecheckPlayerState();

               if (AmongUsClient.Instance.AmHost) MeetingHud.Instance.CheckForEndVoting();
           }


           var targetInfo = target.GetModInfo();
           var killerInfo = killer.GetModInfo();

           if (targetInfo != null)
           {
               targetInfo.DeathTimeStamp = NebulaGameManager.Instance!.CurrentTime;
               targetInfo.MyKiller = killerInfo;
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

    static RemoteProcess<(byte exiledId, byte sourceId, TranslatableTag stateId, TranslatableTag recordId)> RpcMarkAsExtraVictim = new(
        "MarkAsExtraVictim",
        (message, _) => MeetingHudExtension.ExtraVictims.Add(message)
        );

    static public void ModKill(this PlayerControl killer, PlayerControl target, bool showBlink, TranslatableTag playerState, TranslatableTag? recordState)
    {
        RpcKill.Invoke((killer.PlayerId, target.PlayerId, playerState.Id, recordState?.Id ?? int.MaxValue, showBlink));
    }

    static public void ModMeetingKill(this PlayerControl killer, PlayerControl target, bool showOverlay, TranslatableTag playerState, TranslatableTag? recordState)
    {
        RpcMeetingKill.Invoke((killer.PlayerId, target.PlayerId, playerState.Id, recordState?.Id ?? int.MaxValue, showOverlay));
    }

    static public void ModMarkAsExtraVictim(this PlayerControl exiled,PlayerControl? source, TranslatableTag playerState,TranslatableTag recordState)
    {
        RpcMarkAsExtraVictim.Invoke((exiled.PlayerId, source?.PlayerId ?? byte.MaxValue, playerState, recordState));

    }

    static RemoteProcess<(byte sourceId, byte targetId, Vector2 revivePos, bool cleanDeadBody)> RpcRivive = new(
        "Rivive",
        (message, _) =>
        {
            var player = Helpers.GetPlayer(message.targetId);
            if (!player) return;

            player!.Revive();
            player.NetTransform.SnapTo(message.revivePos);
            if (message.cleanDeadBody) foreach (var d in Helpers.AllDeadBodies()) if (d.ParentId == player.PlayerId) GameObject.Destroy(d.gameObject);

            NebulaGameManager.Instance.GameStatistics.RecordEvent(new(GameStatistics.EventVariation.Revive, message.sourceId != byte.MaxValue ? message.sourceId : null, 1 << message.targetId) { RelatedTag = EventDetail.Revive });
        }
        );

    static public void ModRevive(this PlayerControl player, Vector2 pos, bool cleanDeadBody)
    {
        RpcRivive.Invoke((byte.MaxValue, player.PlayerId, pos, cleanDeadBody));
    }

    static public void ModRevive(this PlayerControl player, PlayerControl healer, Vector2 pos, bool cleanDeadBody)
    {
        RpcRivive.Invoke((healer.PlayerId, player.PlayerId, pos, cleanDeadBody));
    }
}
