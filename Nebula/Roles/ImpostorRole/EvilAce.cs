using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;

namespace Nebula.Roles.ImpostorRoles
{
    public class EvilAce : Role
    {
        private Module.CustomOption killCoolDownMultiplierOption;

        public override void LoadOptionData()
        {
            killCoolDownMultiplierOption = CreateOption(Color.white, "killCoolDown", 0.5f, 0.125f, 1f, 0.125f);
            killCoolDownMultiplierOption.suffix = "cross";
        }

        //インポスターはModで操作するFakeTaskは所持していない
        public EvilAce()
                : base("EvilAce", "evilAce", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     false, true, true, false, true)
        {

        }

        public override void SetKillCoolDown(ref float multiplier, ref float addition) {
            int impostorSide = 0;
            foreach(Game.PlayerData data in Game.GameData.data.players.Values)
            {
                if (!data.IsAlive)
                {
                    continue;
                }
                if (data.role.side == Side.Impostor)
                {
                    impostorSide++;
                }
            }
            if (impostorSide == 1)
            {
                multiplier = killCoolDownMultiplierOption.getFloat();
            }
        }
    }
}
