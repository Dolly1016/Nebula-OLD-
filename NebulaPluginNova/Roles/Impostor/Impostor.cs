using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

public class Impostor : ConfigurableStandardRole
{
    static public Impostor MyRole = new Impostor();
    static public Team MyTeam = new("teams.impostor", Palette.ImpostorRed,TeamRevealType.Teams);
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "impostor";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;
    public override bool IsDefaultRole => true;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[]? arguments) => new Instance(player);

    public class Instance : RoleInstance
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }
        public override bool CheckWins(CustomEndCondition endCondition) => endCondition == NebulaGameEnd.ImpostorWin;

        public override void DecoratePlayerName(ref string text, ref Color color)
        {
            if (MyPlayer.Role.Role.RoleCategory == RoleCategory.ImpostorRole) color = Palette.ImpostorRed;
        }
    }
}
