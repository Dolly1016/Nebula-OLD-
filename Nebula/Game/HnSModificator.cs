using Il2CppSystem.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Roles.NeutralRoles.Paparazzo;

namespace Nebula.Game;

public class HnSModificator
{
    public struct HnSTaskBonusMessage
    {
        public float TimeDeduction;
        public bool CanProceedFinalTimer;
        public bool IsFinishTaskBonus;
    }

    public static bool IsHnSGame => GameOptionsManager.Instance.currentGameMode == GameModes.HideNSeek;
    public static HideAndSeekManager HideAndSeekManager;
    public static PlayerControl Seeker;

    public static RemoteProcess<HnSTaskBonusMessage> ProceedTimer = new RemoteProcess<HnSTaskBonusMessage>(
            (writer, message) =>
            {
                writer.Write(message.CanProceedFinalTimer);
                writer.Write(message.IsFinishTaskBonus);
                writer.Write(message.TimeDeduction);
            },
            (reader) =>
            {
                HnSTaskBonusMessage message = new HnSTaskBonusMessage();
                message.CanProceedFinalTimer = reader.ReadBoolean();
                message.IsFinishTaskBonus = reader.ReadBoolean();
                message.TimeDeduction = reader.ReadSingle();
                return message;
            },
            (message, isCalledByMe) =>
            {
                var manager = HideAndSeekManager.Instance.CastFast<HideAndSeekManager>();

                if (manager.LogicFlowHnS.TimerBar != null) manager.LogicFlowHnS.TimerBar.StartChunkCoroutine();

                if (manager.LogicFlowHnS.IsFinalCountdown)
                    manager.LogicFlowHnS.AdjustFinalEscapeTimer(message.TimeDeduction);
                else
                    manager.LogicFlowHnS.AdjustEscapeTimer(message.TimeDeduction, true);

                if (message.IsFinishTaskBonus) SoundManager.Instance.PlaySoundImmediate(manager.TaskFinishedSound, false, 1f, 1f, null);
            }
            );
}