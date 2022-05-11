using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.CrewmateRoles
{
    public class CrewmateWithoutTasks : Crewmate
    {
        public override bool IsGuessableRole { get => false; }

        public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
        {

        }

        public override bool CanBeLovers
        {
            get
            {
                return Roles.F_Crewmate.CanBeLovers;
            }
        }

        public override bool CanBeGuesser
        {
            get
            {
                return Roles.F_Crewmate.CanBeGuesser;
            }
        }

        public override bool CanBeDrunk
        {
            get
            {
                return Roles.F_Crewmate.CanBeDrunk;
            }
        }
        public CrewmateWithoutTasks() : base(true)
        {
            HideInExclusiveAssignmentOption = true;
            FakeTaskIsExecutable = false;
        }
    }
}