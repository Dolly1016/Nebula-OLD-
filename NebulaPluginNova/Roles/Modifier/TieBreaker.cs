using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Modifier;



public class TieBreaker : ConfigurableStandardModifier
{
    static public TieBreaker MyRole = new TieBreaker();
    public override string LocalizedName => "tieBreaker";
    public override string CodeName => "TBR";
    public override Color RoleColor => new Color(239f / 255f, 175f / 255f, 135f / 255f);
    public override ModifierInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);
    public class Instance : ModifierInstance
    {
        public override AbstractModifier Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnTieVotes(ref List<byte> extraVotes, PlayerVoteArea myVoteArea)
        {
            if (!myVoteArea.DidVote) return;
            extraVotes.Add(myVoteArea.VotedFor);
        }
    }
}
