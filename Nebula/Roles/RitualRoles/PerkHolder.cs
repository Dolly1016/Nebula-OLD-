using System;
using System.Collections.Generic;
using System.Text;
using Nebula.Utilities;

namespace Nebula.Roles.RitualRoles
{
    public class PerkHolder : ExtraRole
    {

        public override void Assignment(Patches.AssignMap assignMap)
        {
            foreach(var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                assignMap.Assign(p.PlayerId, this.id, 0);
            }
        }

        public PerkHolder() : base("PerkHolder", "perkHolder", Palette.White, 0)
        {
            ValidGamemode = Module.CustomGameMode.Ritual;
            IsHideRole = true;
        }
    }
}
