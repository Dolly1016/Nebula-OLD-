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

namespace Nebula.Roles.MinigameRoles.Escapees
{
    public class Escapee : Role
    {
        public override bool CheckAdditionalWin(PlayerControl player, EndCondition condition)
        {
            if (condition != EndCondition.MinigameEscapeesWin) return false;
            return !player.Data.IsDead;
        }


        protected Escapee(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius) :
            base(name, localizeName, color, category,
                side, introMainDisplaySide, introDisplaySides, introInfluenceSides,
                winReasons, hasFakeTask, canUseVents, canMoveInVents,
                ignoreBlackout, useImpostorLightRadius)
        {
            Allocation = AllocationType.None;
        }
    }
}
