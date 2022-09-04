using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;
using Nebula.Game;

namespace Nebula.Roles.Template
{
    public class ExemptTasks : Role
    {
        private Module.CustomOption exemptTasksOption;
        //初期設定でのタスク免除数
        protected int InitialExemptTasks { get; set; } = 1;
        //最高タスク免除数
        protected int MaxExemptTasks { get; set; } = 10;
        //オプションを使用しない場合
        protected int CustomExemptTasks { get; set; } = 1;
        //タスク免除オプションを使用するかどうか
        protected bool UseExemptTasksOption { get; set; } = true;


        public override void OnSetTasks(Il2CppSystem.Collections.Generic.List<GameData.TaskInfo> tasks) {
            int exempt = UseExemptTasksOption ? (int)exemptTasksOption.getFloat() : CustomExemptTasks;
            int cutTasks = tasks.Count < exempt ? tasks.Count : exempt;
            RPCEventInvoker.ExemptTasks(cutTasks, cutTasks, tasks);
        }


        public override void LoadOptionData()
        {
            if (UseExemptTasksOption) exemptTasksOption = CreateOption(Color.white, "exemptTasks", (float)InitialExemptTasks, 0f, (float)MaxExemptTasks, 1f);
        }

        //インポスターはModで操作するFakeTaskは所持していない
        protected ExemptTasks(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius) :
            base(name, localizeName, color, category,
                side, introMainDisplaySide, introDisplaySides, introInfluenceSides,
                winReasons,
                hasFakeTask, canUseVents, canMoveInVents,
                ignoreBlackout, useImpostorLightRadius)
        {
            UseExemptTasksOption = true;
        }
    }
}
