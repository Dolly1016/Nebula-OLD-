using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.MinigameRoles.Hunters
{
    public class Hunter : Role
    {
        public override void LoadOptionData()
        {
            base.LoadOptionData();

            TopOption.AddCustomPrerequisite(() => { return Module.CustomOption.CurrentGameMode == Module.CustomGameMode.FreePlay || CustomOptionHolder.escapeHunterOption.getRawString() == "role." + LocalizeName + ".name"; });
        }

        protected Hunter(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius) :
            base(name, localizeName, color,category,
                side,introMainDisplaySide,introDisplaySides,introInfluenceSides,
                winReasons,hasFakeTask,canUseVents,canMoveInVents,
                ignoreBlackout,useImpostorLightRadius)
        {
            ExceptBasicOption = true;
        }
    }
}
