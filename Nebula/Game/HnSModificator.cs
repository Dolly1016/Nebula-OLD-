using Il2CppSystem.Threading.Tasks;
using Nebula.Roles.Perk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Roles.NeutralRoles.Paparazzo;

namespace Nebula.Game;

[NebulaRPCHolder]
public class HnSModificator
{
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