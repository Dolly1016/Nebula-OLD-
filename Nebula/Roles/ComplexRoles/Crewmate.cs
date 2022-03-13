using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Roles.ComplexRoles
{
    public class FCrewmate : Role
    {
        public Module.CustomOption isGuessableOption;
        private Module.CustomOption maxCountOfDamnedOption;
        private Module.CustomOption chanceOfDamnedOption;

        public int MaxCountOfDamned()
        {
            return (int)maxCountOfDamnedOption.getFloat();
        }

        public int ChanceOfDamned()
        {
            return chanceOfDamnedOption.getSelection();
        }

        //Complexなロールカテゴリーについてのみ呼ばれます。
        public override Patches.AssignRoles.RoleAllocation[] GetComplexAllocations()
        {
            Patches.AssignRoles.RoleAllocation[] result = new Patches.AssignRoles.RoleAllocation[(int)maxCountOfDamnedOption.getFloat()];

            int damneds= Helpers.CalcProbabilityCount(ChanceOfDamned(), result.Length);

            int chance = RoleChanceOption.getSelection();
            for(int i = 0; i < result.Length; i++)
            {
                result[i] = new Patches.AssignRoles.RoleAllocation(i < damneds ? Roles.DamnedCrew : Roles.Crewmate, chance);
            }

            return result;
        }

        public override void LoadOptionData()
        {
            isGuessableOption = CreateOption(Color.white, "isGuessable", true);
            chanceOfDamnedOption = CreateOption(Color.white, "chanceOfDamned", CustomOptionHolder.rates);
            maxCountOfDamnedOption = CreateOption(Color.white, "maxCountOfDamned", 1f, 0f, 15f, 1f);
        }

        public FCrewmate()
                : base("Crewmate", "crewmate", Palette.CrewmateBlue, RoleCategory.Complex, Side.Crewmate, Side.Crewmate,
                     new HashSet<Side>(), new HashSet<Side>(), new HashSet<Patches.EndCondition>(),
                     false, VentPermission.CanNotUse, false, false, false)
        {

        }
    }
}
