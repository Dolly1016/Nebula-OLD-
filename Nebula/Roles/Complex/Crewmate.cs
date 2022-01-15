using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Roles.Complex
{
    public class Crewmate : Role
    {
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
            Patches.AssignRoles.RoleAllocation[] result = new Patches.AssignRoles.RoleAllocation[(int)roleCountOption.getFloat()];

            int damneds = 0;
            double rnd = NebulaPlugin.rnd.NextDouble();
            double rate = (double)ChanceOfDamned() / 10.0;
            while (rnd < rate)
            {
                damneds++;
                rate *= (double)ChanceOfDamned() / 10.0;
                if (damneds >= MaxCountOfDamned() || damneds>=result.Length)
                {
                    break;
                }
            }

            int chance = roleChanceOption.getSelection();
            for(int i = 0; i < result.Length; i++)
            {
                result[i] = new Patches.AssignRoles.RoleAllocation(i < damneds ? Roles.DamnedCrew : Roles.Crewmate, chance);
            }

            return result;
        }

        public override void LoadOptionData()
        {
            chanceOfDamnedOption = CreateOption(Color.white, "chanceOfDamned", CustomOptionHolder.rates);
            maxCountOfDamnedOption = CreateOption(Color.white, "maxCountOfDamned", 1f, 0f, 15f, 1f);
        }

        public Crewmate()
                : base("Crewmate", "crewmate", Palette.CrewmateBlue, RoleCategory.Complex, Side.Crewmate, Side.Crewmate,
                     new HashSet<Side>(), new HashSet<Side>(), new HashSet<Patches.EndCondition>(),
                     false, false, false, false, false)
        {

        }
    }
}
