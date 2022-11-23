using Nebula.Patches;

namespace Nebula.Roles.NeutralRoles
{
    public class Opportunist : Role
    {
        static public Color RoleColor = new Color(106f / 255f, 252f / 255f, 45f / 255f);

        private Module.CustomOption numOfTasksOption;
        private Module.CustomOption canUseVentsOption;
        private Module.CustomOption ventCoolDownOption;
        private Module.CustomOption ventDurationOption;
        private Module.CustomOption canWinWithArsonistOption;
        private Module.CustomOption canWinWithEmpiricOption;
        private Module.CustomOption canWinWithJesterOption;
        private Module.CustomOption canWinWithVultureOption;
        private Module.CustomOption canWinWithLoversOption;
        private Module.CustomOption canWinWithAvengerOption;

        //オポチュタスク割り当て用
        private List<int> stayingTaskOrder;
        private int taskCount;

        public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
        {
            stayingTaskOrder = new List<int>(Helpers.GetRandomArray(Map.MapData.GetCurrentMapData().Objects.Count));
            taskCount = 0;

            initialTasks.Clear();
            int tasks = (int)numOfTasksOption.getFloat();
            for (int i = 0; i < tasks; i++)
            {
                initialTasks.Add(new GameData.TaskInfo(byte.MaxValue - 2, 0));
            }
        }

        public override void GlobalIntroInitialize(PlayerControl __instance)
        {
            canMoveInVents = canUseVentsOption.getBool();
            VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseLimittedVent : VentPermission.CanNotUse;
        }

        public override void LoadOptionData()
        {
            base.LoadOptionData();

            numOfTasksOption = CreateOption(Color.white, "numOfTasks", 4f,1f,6f,1f);

            canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
            ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f);
            ventCoolDownOption.suffix = "second";
            ventCoolDownOption.AddPrerequisite(canUseVentsOption);
            ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f);
            ventDurationOption.suffix = "second";
            ventDurationOption.AddPrerequisite(canUseVentsOption);

            canWinWithArsonistOption = CreateOption(Color.white, "canWinWithArsonist", true);
            canWinWithEmpiricOption = CreateOption(Color.white, "canWinWithEmpiric", true);
            canWinWithJesterOption = CreateOption(Color.white, "canWinWithJester", true);
            canWinWithVultureOption = CreateOption(Color.white, "canWinWithVulture", true);
            canWinWithLoversOption = CreateOption(Color.white, "canWinWithLovers", true);
            canWinWithAvengerOption = CreateOption(Color.white, "canWinWithAvenger", true);
        }

        public override void Initialize(PlayerControl __instance)
        {
            VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
            VentDurationMaxTimer = ventDurationOption.getFloat();
        }
        public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
        {
            if (condition == EndCondition.NoGame) return false;
            if (condition == EndCondition.NobodySkeldWin) return false;
            if (condition == EndCondition.NobodyMiraWin) return false;
            if (condition == EndCondition.NobodyPolusWin) return false;
            if (condition == EndCondition.NobodyAirshipWin) return false;

            if (player.Data.IsDead && player.GetModData().FinalData?.status != Game.PlayerData.PlayerStatus.Burned) return false;
            if (condition == EndCondition.ArsonistWin && !canWinWithArsonistOption.getBool()) return false;
            if (condition == EndCondition.EmpiricWin && !canWinWithEmpiricOption.getBool()) return false;
            if (condition == EndCondition.JesterWin && !canWinWithJesterOption.getBool()) return false;
            if (condition == EndCondition.VultureWin && !canWinWithVultureOption.getBool()) return false;
            if (condition == EndCondition.AvengerWin && !canWinWithAvengerOption.getBool()) return false;
            if (condition == EndCondition.LoversWin && !canWinWithLoversOption.getBool()) return false;
            if (condition == EndCondition.TrilemmaWin && !canWinWithLoversOption.getBool()) return false;
            

            if (player.GetModData().Tasks.AllTasks <= player.GetModData().Tasks.Completed)
            {
                EndGameManagerSetUpPatch.AddEndText(Language.Language.GetString("role.opportunist.additionalEndText"));
                return true;
            }
            
            return false;
        }

        public override bool HasExecutableFakeTask(byte playerId) => true;
       
        public Opportunist()
            : base("Opportunist", "opportunist", RoleColor, RoleCategory.Neutral, Side.Opportunist, Side.Opportunist,
                 new HashSet<Side>() { Side.Opportunist }, new HashSet<Side>() { Side.Opportunist },
                 new HashSet<Patches.EndCondition>(),
                 true, VentPermission.CanUseLimittedVent, true, false, false)
        {
            VentColor = RoleColor;

            stayingTaskOrder = new List<int>();
        }

        public void InitializeOpportunistTask(Tasks.OpportunistTask task)
        {
            task.opportunistTaskType = Tasks.OpportunistTask.OpportunistTaskType.StayingNearObject;
            Map.ObjectData objData;
            if (stayingTaskOrder.Count > 0) {
                objData = Map.MapData.GetCurrentMapData().Objects[stayingTaskOrder[0]];
                stayingTaskOrder.RemoveAt(0);
            }
            else
            {
                objData = Map.MapData.GetCurrentMapData().Objects[0];
            }
            task.objPos = objData.Position;
            task.objName = objData.Name;
            task.StartAt = objData.Room;
            task.maxTime = objData.MaxTime;
            task.distance = objData.Distance;
            task.name = "OpportunistTask" + taskCount;

            taskCount++;
        }

        
    }
}
