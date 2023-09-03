using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

public class Crewmate : ConfigurableStandardRole
{
    static public Crewmate MyRole = new Crewmate();
    static public Team MyTeam = new("teams.crewmate", Palette.CrewmateBlue, TeamRevealType.Everyone);

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "crewmate";
    public override Color RoleColor => Palette.CrewmateBlue;
    public override bool IsDefaultRole => true;
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    public class Instance : RoleInstance
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override bool CheckWins(CustomEndCondition endCondition) => endCondition == NebulaGameEnd.CrewmateWin;
    }
}
