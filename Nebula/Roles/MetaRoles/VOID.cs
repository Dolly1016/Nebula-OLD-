using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.MetaRoles
{
    public class VOID : Role
    {
        static public Color RoleColor = new Color(173f / 255f, 173f / 255f, 198f / 255f);

        private Module.CustomOption killerCanKnowBaitKillByFlash;

        public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
        {
            initialTasks.Clear();
            actualTasks = null;
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            __instance.Die(DeathReason.Exile);
        }

        public override void Initialize(PlayerControl __instance)
        {
            Game.GameData.data.myData.CanSeeEveryoneInfo = true;
            
        }

       
        public override void LoadOptionData()
        {
            
        }

        public VOID()
            : base("VOID", "void", RoleColor, RoleCategory.Neutral, Side.Extra, Side.Extra,
                 new HashSet<Side>(), new HashSet<Side>(), new HashSet<Patches.EndCondition>(),
                 true, VentPermission.CanNotUse, false, false, false)
        {
            DefaultCanBeLovers = false;
            DefaultCanBeDrunk = false;
            DefaultCanBeGuesser = false;
            DefaultCanBeMadmate = false;
            DefaultCanBeSecret = false;

            Allocation = AllocationType.Switch;
            FixedRoleCount = true;

            IsGuessableRole = false;
        }
    }
}
