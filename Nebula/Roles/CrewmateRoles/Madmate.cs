using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Madmate : Role
    {
        private Module.CustomOption CanUseVentsOption;
        private Module.CustomOption HasImpostorVisionOption;
        private Module.CustomOption CanInvokeSabotageOption;

        public override void LoadOptionData()
        {
            CanUseVentsOption = CreateOption(Color.white, "canUseVents", true);
            CanInvokeSabotageOption = CreateOption(Color.white, "canInvokeSabotage", true);

            HasImpostorVisionOption = CreateOption(Color.white, "hasImpostorVision", false);
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            CanMoveInVents = CanUseVentsOption.getBool();
            VentPermission = CanUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
            canInvokeSabotage = CanInvokeSabotageOption.getBool();
            UseImpostorLightRadius = HasImpostorVisionOption.getBool();
        }

        public Madmate()
                : base("Madmate", "madmate", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, ImpostorRoles.Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, false, false)
        {
            
        }
    }
}
