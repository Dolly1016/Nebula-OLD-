using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Tasks;

[NebulaRPCHolder]
public class TimedTask
{
    public struct TimedTaskMessage
    {
        public int TaskId;
        public float LeftTime;
    }

    public static RemoteProcess<TimedTaskMessage> TimedTaskEvent = new RemoteProcess<TimedTaskMessage>(
        "TimedTask",
            (writer, message) =>
            {
                writer.Write(message.TaskId);
                writer.Write(message.LeftTime);
            },
            (reader) =>
            {
                var message = new TimedTaskMessage();
                message.TaskId = reader.ReadInt32();
                message.LeftTime = reader.ReadSingle();
                return message;
            },
            (message, isCalledByMe) =>
            {
                PlayerTask? task = null;

                switch (message.TaskId)
                {
                    case 0:
                        var hudTask = new GameObject("TimedHudOverrideTask").AddComponent<TimedHudOverrideTask>();
                        hudTask.LeftTime = message.LeftTime;
                        task = hudTask;
                        break;

                }
                if (task == null) return;

                task.transform.SetParent(PlayerControl.LocalPlayer.transform);
                task.Id = (uint)(200U + message.TaskId);
                task.Owner = PlayerControl.LocalPlayer;
                task.Initialize();
                PlayerControl.LocalPlayer.myTasks.Add(task);
            }
            );

    public static Color OnlyImpostorTextColor = new Color(1f, 0.6f, 0.6f);
}

public interface TimedSabotageTask { }

public class TimedHudOverrideTask : PlayerTask, TimedSabotageTask
{
    public float LeftTime = 10f;

    static TimedHudOverrideTask()
    {
        ClassInjector.RegisterTypeInIl2Cpp<TimedHudOverrideTask>(new RegisterTypeOptions()
        {
            Interfaces = new [] { typeof(IHudOverrideTask) } 
        });
    }


    public override int TaskStep => 0;
    public override bool IsComplete => false;
    public void FixedUpdate() {
        LeftTime -= Time.deltaTime;
        if (LeftTime < 0f) Complete();
    }

    public override bool ValidConsole(Console console) => false;

    public override void Complete()
    {
        PlayerControl.LocalPlayer.RemoveTask(this);
    }

    bool even=false;
    public override void AppendTaskText(Il2CppSystem.Text.StringBuilder sb)
    {
        even = !even;
        Color color = even ? Color.yellow : Color.red;
        sb.Append(color.ToTextColor());
        sb.Append(Language.Language.GetString("task.hudOverride.name"));
        sb.Append("</color>");

        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
        {
            sb.Append(TimedTask.OnlyImpostorTextColor.ToTextColor());
            sb.Append(" (");
            sb.Append(((int)(LeftTime + 1f)).ToString() + Language.Language.GetString("option.suffix.second"));
            sb.Append(")");
            sb.Append("</color>");
        }

        sb.AppendLine();
    }

    public override void Initialize(){
        this.TaskType = TaskTypes.FixComms;
    }
}

