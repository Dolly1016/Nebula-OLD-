

namespace Nebula.Tasks;

public class OpportunistTask : NebulaPlayerTask
{
    public enum OpportunistTaskType
    {
        None,
        StayingNearObject
    }

    static OpportunistTask()
    {
        ClassInjector.RegisterTypeInIl2Cpp<OpportunistTask>();
    }

    public OpportunistTask()
    {
        //__Initialize();
    }

    public override void __AppendTaskText(Il2CppSystem.Text.StringBuilder sb)
    {
        bool flag = false;
        if (this.IsComplete)
        {
            sb.Append("<color=#00DD00FF>");
            flag = true;
        }
        else if (progress > 0)
        {
            if (isProcessing) sb.Append("<color=#FFFF00FF>"); else sb.Append("<color=#999900FF>");
            flag = true;
        }
        else if (progress >= maxTime)
        {
            sb.Append("<color=#00DD00FF>");
            flag = true;
        }


        sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(StartAt));
        sb.Append(": ");

        sb.Append(Language.Language.GetString("role.opportunist.task.staying").Replace("%OBJ%", Language.Language.GetString("role.opportunist.task.staying." + objName)));

        if (progress > 0f && !this.IsComplete)
        {
            sb.Append(" (");
            sb.Append((int)(1f + maxTime - progress));
            sb.Append("s)");
        }

        if (flag)
        {
            sb.Append("</color>");
        }
        sb.AppendLine();
    }

    public override bool __NextStep()
    {
        if (PlayerControl.LocalPlayer)
        {
            if (DestroyableSingleton<HudManager>.InstanceExists)
            {
                DestroyableSingleton<HudManager>.Instance.ShowTaskComplete();
                StatsManager.Instance.IncrementStat(StringNames.StatsTasksCompleted);
                DestroyableSingleton<AchievementManager>.Instance.OnTaskComplete(this.TaskType);
                if (PlayerTask.AllTasksCompleted(PlayerControl.LocalPlayer))
                {
                    StatsManager.Instance.IncrementStat(StringNames.StatsAllTasksCompleted);
                }
            }
            PlayerControl.LocalPlayer.RpcCompleteTask(base.Id);

            SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskUpdateSound, false, 1f, null);

            RPCEventInvoker.CompleteTask(PlayerControl.LocalPlayer.PlayerId);

            LocationDirty = true;
        }
        return false;
    }


    public override void __Initialize()
    {
        if (opportunistTaskType == OpportunistTaskType.None)
        {
            MaxStep = 0;
            Roles.Roles.Opportunist.InitializeOpportunistTask(this);
            LocationDirty = true;
            HasLocation = true;

            isProcessing = false;
        }
    }

    public override void __Update()
    {
        Vector3 playerPos = PlayerControl.LocalPlayer.transform.position;
        float d;

        if (__IsCompleted()) return;

        isProcessing = false;

        if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.CanMove)
        {
            playerPos = PlayerControl.LocalPlayer.transform.position;
            d = playerPos.Distance(objPos);
            if (d < distance)
            {
                if (!Helpers.AnyShadowsBetween(playerPos, (objPos - (Vector2)playerPos).normalized, d))
                {
                    taskStep = 1;
                    progress += Time.deltaTime;
                    isProcessing = true;

                    if (progress > maxTime) NextStep();
                }
            }
        }
    }

    public override bool __ValidConsole(Console console) { return false; }

    public override bool __IsCompleted()
    {
        switch (opportunistTaskType)
        {
            case OpportunistTaskType.StayingNearObject:
                return progress >= maxTime;
                break;
        }
        return false;
    }

    public override void __GetLocations(ref Il2CppSystem.Collections.Generic.List<Vector2> __result)
    {
        if (__IsCompleted()) return;

        __result = new Il2CppSystem.Collections.Generic.List<Vector2>(1);

        __result.Add(objPos);

    }

    public OpportunistTaskType opportunistTaskType;

    //Staying
    public Vector2 objPos;
    public string objName;
    public float progress;
    public float maxTime; //要求される滞在時間
    public float distance;
    public bool isProcessing;
}
