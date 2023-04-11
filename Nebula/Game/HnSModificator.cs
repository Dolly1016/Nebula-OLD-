using Il2CppSystem.Threading.Tasks;
using Nebula.Roles.Perk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Roles.NeutralRoles.Paparazzo;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Game;

[NebulaRPCHolder]
public class HnSModificator
{
    public static float GetDefaultCoolDown()
    {
        return GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
    }
    public static float StartKillCoolDown(bool onFailed = false)
    {
        float cool = GetDefaultCoolDown();
        float additional = 0f, ratio = 1f;
        PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetKillCoolDown(p, !onFailed, ref additional, ref ratio));
        if (ratio < 0f) ratio = 0f;
        cool = (cool + additional) * ratio;
        if (cool < 0f) cool = 0f;

        if (onFailed)
        {
            float sa = 0f, sr = 1f;
            float ta = 0f, tr = 1f;
            PerkHolder.PerkData.MyPerkData.PerkAction((p) => p.Perk.SetFailedKillPenalty(p, ref sa, ref sr, ref ta, ref tr));
            sr = Mathf.Min(0, sr);
            tr = Mathf.Min(0, tr);
            RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, (cool + ta) * tr, (0.25f + sa) * sr, false));
        }

        return cool;
    }

    public static RemoteProcess NoticeSeekerEvent = new RemoteProcess(
        "PingSeeker",
           (isCalledByMe) =>
           {
               if (PlayerControl.LocalPlayer.Data.Role.IsImpostor) return;

               Helpers.Ping(new Vector2[] { Game.HnSModificator.Seeker.GetTruePosition() }, false, (p) =>
               {
                   SoundManager.Instance.PlaySound(p.soundOnEnable, false, 0.75f, null).pitch = 0.4f;
               });
           }
           );


    public struct HnSTaskBonusMessage
    {
        public float TimeDeduction;
        public bool CanProceedFinalTimer;
        public bool IsFinishTaskBonus;
        public byte ContributorId;
    }

    public static bool IsHnSGame => GameOptionsManager.Instance.currentGameMode == GameModes.HideNSeek;
    public static HideAndSeekManager HideAndSeekManager;
    public static PlayerControl Seeker;

    public static void Initialize()
    {
        HideAndSeekManager = HideAndSeekManager.Instance.CastFast<HideAndSeekManager>();
        foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator()) if (p.Data.Role.IsImpostor) Seeker = p;
    }

    public static RemoteProcess<HnSTaskBonusMessage> ProceedTimer = new RemoteProcess<HnSTaskBonusMessage>(
        "HnSTaskBonus",
            (writer, message) =>
            {
                writer.Write(message.CanProceedFinalTimer);
                writer.Write(message.IsFinishTaskBonus);
                writer.Write(message.TimeDeduction);
                writer.Write(message.ContributorId);
            },
            (reader) =>
            {
                HnSTaskBonusMessage message = new HnSTaskBonusMessage();
                message.CanProceedFinalTimer = reader.ReadBoolean();
                message.IsFinishTaskBonus = reader.ReadBoolean();
                message.TimeDeduction = reader.ReadSingle();
                message.ContributorId = reader.ReadByte();
                return message;
            },
            (message, isCalledByMe) =>
            {
                var manager = HideAndSeekManager.Instance.CastFast<HideAndSeekManager>();

                if (message.IsFinishTaskBonus)
                {
                    SoundManager.Instance.PlaySoundImmediate(manager.TaskFinishedSound, false, 1f, 1f, null);

                    float additional = 0f, ratio = 1f;
                    PerkHolder.PerkData.GeneralPerkAction((p, id) => p.Perk.OnCompleteHnSTaskGlobal(p, message.ContributorId, ref additional, ref ratio));
                    if (ratio < 0f) ratio = 0f;
                    message.TimeDeduction = (message.TimeDeduction + additional) * ratio;
                }


                if (manager.LogicFlowHnS.timerBar != null) manager.LogicFlowHnS.timerBar.StartChunkCoroutine();

                if (manager.LogicFlowHnS.IsFinalCountdown)
                    manager.LogicFlowHnS.AdjustFinalEscapeTimer(message.TimeDeduction);
                else
                    manager.LogicFlowHnS.AdjustEscapeTimer(message.TimeDeduction, true);
            }
            );
}