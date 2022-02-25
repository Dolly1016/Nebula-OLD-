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
                    secondary = ChanceOfSecondarySide() >= 5 ? 1 : 0;
                }
                else
                {
                    secondary = (int)((float)result.Length * (float)ChanceOfSecondarySide() / 10f + 0.5f);
                }
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

    public class BilateralnessRole : Role
    {
        private bool IsSecondaryRole;
        HasBilateralness FRole;
        Func<HasBilateralness> GetFRoleFunc;

        protected BilateralnessRole(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, bool canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius, Func<HasBilateralness> bilateralness,bool isSecondary) :
            base(name, localizeName, color, category,
                side, introMainDisplaySide, introDisplaySides, introInfluenceSides,
                winReasons,
                hasFakeTask, canUseVents, canMoveInVents,
                ignoreBlackout, useImpostorLightRadius)
        {
            IsSecondaryRole = isSecondary;
            GetFRoleFunc = bilateralness;
        }

        public override void LoadOptionData()
        {
            FRole = GetFRoleFunc.Invoke();    
        }

        public override bool IsSpawnable()
        {
            if (FRole.RoleChanceOption.getSelection() == 0) return false;
            if (FRole.RoleCountOption.getFloat() == 0f) return false;

            if (FRole.definitiveAssignmentOption.getBool())
            {
                return FRole.GetComplexAllocations().Any(role => role.role == this);
            }
            else
            {
                return FRole.chanceToSpawnAsSecondarySide.getSelection() != (IsSecondaryRole ? 0 : 10);
            }

            return true;
        }
    }
}
