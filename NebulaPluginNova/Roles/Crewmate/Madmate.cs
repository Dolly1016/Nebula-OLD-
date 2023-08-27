using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

public class Madmate : ConfigurableStandardRole
{
    static public Madmate MyRole = new Madmate();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "madmate";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerControl player, int[]? arguments) => new Instance(player);

    public class Instance : Crewmate.Instance
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerControl player) : base(player)
        {
        }

        public override bool CheckWins(CustomEndCondition endCondition) => endCondition == NebulaGameEnd.ImpostorWin;
        public override bool HasCrewmateTasks => false;
    }
}

