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

            int chance = RoleChanceOption.getSelection() + 1;
            for(int i = 0; i < result.Length; i++)
            {
                result[i] = new Patches.AssignRoles.RoleAllocation(i < damneds ? Roles.DamnedCrew : Roles.Crewmate, chance);
            }

            return result;
        }

        public override void LoadOptionData()
        {
            TopOption.showDetailForcely = true;
            var preBuilder = TopOption.preOptionScreenBuilder;
            TopOption.preOptionScreenBuilder = (refresher) => {
                if (!TopOption.getBool()) return new Module.MetaScreenContent[0][];
                else return preBuilder(refresher);
            };

            foreach(var option in extraAssignableOptions)
            {
                if (option.Value == null) continue;
                option.Value.AddPrerequisite(TopOption);
            }

            isGuessableOption = CreateOption(Color.white, "isGuessable", true);
            chanceOfDamnedOption = CreateOption(Color.white, "chanceOfDamned", CustomOptionHolder.rates).AddPrerequisite(TopOption);
            maxCountOfDamnedOption = CreateOption(Color.white, "maxCountOfDamned", 1f, 0f, 15f, 1f).AddPrerequisite(TopOption).AddPrerequisite(chanceOfDamnedOption);
        }

        public FCrewmate()
                : base("Crewmate", "crewmate", Palette.CrewmateBlue, RoleCategory.Complex, Side.Crewmate, Side.Crewmate,
                     new HashSet<Side>(), new HashSet<Side>(), new HashSet<Patches.EndCondition>(),
                     false, VentPermission.CanNotUse, false, false, false)
        {

        }
    }
}
