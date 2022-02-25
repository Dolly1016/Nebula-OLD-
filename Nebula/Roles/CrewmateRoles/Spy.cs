using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Roles.CrewmateRoles
{
    public class Spy : Role
    {
        public bool validSpyFlag;

        private Module.CustomOption impostorCanKillImpostorOption;

        public bool CanKillImpostor()
        {
            return impostorCanKillImpostorOption.getBool() && validSpyFlag;
        }

        public override void LoadOptionData()
        {
            impostorCanKillImpostorOption = CreateOption(Color.white, "impostorCanKillImpostor", true);
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            validSpyFlag = true;
        }

        public override void StaticInitialize()
        {
            validSpyFlag = false;
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Pursuer);
            RelatedRoles.Add(Roles.Seer);
            RelatedRoles.Add(Roles.Empiric);
            RelatedRoles.Add(Roles.Bait);
            RelatedRoles.Add(Roles.Provocateur);
        }

        public Spy()
                : base("Spy", "spy", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     Crewmate.crewmateSideSet, ImpostorRoles.Impostor.impostorSideSet, Crewmate.crewmateEndSet,
                     false, true, false, false, true)
        {
            DeceiveImpostorInNameDisplay = true;
        }
    }
}
