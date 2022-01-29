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
    public class HasBilateralness : Role
    {
        public Module.CustomOption chanceToSpawnAsSecondarySide;
        public Module.CustomOption definitiveAssignmentOption;

        protected Role FirstRole=null, SecondaryRole=null;

        public int ChanceOfSecondarySide()
        {
            return chanceToSpawnAsSecondarySide.getSelection();
        }

        //Complexなロールカテゴリーについてのみ呼ばれます。
        public override Patches.AssignRoles.RoleAllocation[] GetComplexAllocations()
        {
            if (FirstRole == null || SecondaryRole == null) return null;

            Patches.AssignRoles.RoleAllocation[] result = new Patches.AssignRoles.RoleAllocation[(int)RoleCountOption.getFloat()];

            int chance = RoleChanceOption.getSelection();
            int secondary;

            if (definitiveAssignmentOption.getBool())
            {
                //決定的な割り当て
                if (result.Length == 1)
                {
                    //1人の場合確率の高い方を選択
                    result[0]= new Patches.AssignRoles.RoleAllocation(ChanceOfSecondarySide() > 0.5f ? SecondaryRole : FirstRole, chance);
                }
                secondary = (int)((float)result.Length * (float)ChanceOfSecondarySide() + 0.5f);
            }
            else
            {
                //ランダム性のある割り当て
                secondary = Helpers.CalcProbabilityCount(ChanceOfSecondarySide(), result.Length);   
            }

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Patches.AssignRoles.RoleAllocation(i < secondary ? SecondaryRole : FirstRole, chance);
            }

            return result;
        }

        public override void LoadOptionData()
        {
            chanceToSpawnAsSecondarySide = CreateOption(Color.white, "chanceToSpawnAsSecondarySide", CustomOptionHolder.rates);
            definitiveAssignmentOption=CreateOption(Color.white, "definitiveAssignment", false);
        }

        public HasBilateralness(string name, string localizeName, Color color) :
            base(name, localizeName, color, RoleCategory.Complex,
                Side.Crewmate, Side.Crewmate, new HashSet<Side>(), new HashSet<Side>(),
                new HashSet<Patches.EndCondition>(),
                false, false, false, false, false)
        {
        }
    }
}
