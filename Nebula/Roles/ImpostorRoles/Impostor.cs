using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;

namespace Nebula.Roles.ImpostorRoles
{
    public class Impostor : Role
    {
        public static HashSet<Side> impostorSideSet = new HashSet<Side>() { Side.Impostor };
        public static HashSet<EndCondition> impostorEndSet =
           new HashSet<EndCondition>() { EndCondition.ImpostorWinByKill, EndCondition.ImpostorWinBySabotage, EndCondition.ImpostorWinByVote,EndCondition.ImpostorWinDisconnect };

        //インポスターはModで操作するFakeTaskは所持していない
        public Impostor()
                : base("Impostor", "impostor", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     impostorSideSet, impostorSideSet,impostorEndSet,
                     false,true, true, true, true)
        {
            ValidGamemode = Module.CustomGameMode.Standard;
        }
    }
}
