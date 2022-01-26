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

            int secondary = Helpers.CalcProbabilityCount(ChanceOfSecondarySide(), result.Length);

            int chance = RoleChanceOption.getSelection();
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Patches.AssignRoles.RoleAllocation(i < secondary ? SecondaryRole: FirstRole , chance);
            }

            return result;
        }

        public override void LoadOptionData()
        {
            chanceToSpawnAsSecondarySide = CreateOption(Color.white, "chanceToSpawnAsSecondarySide", CustomOptionHolder.rates);
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
