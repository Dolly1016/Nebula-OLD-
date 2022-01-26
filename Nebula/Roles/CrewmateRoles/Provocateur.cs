using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;

namespace Nebula.Roles.CrewmateRoles
{
    public class Provocateur : Role
    {
        static public Color Color = new Color(112f / 255f, 255f / 255f, 89f / 255f);

        public override void OnMurdered(byte murderId)
        {
            //相手も殺す
            if (PlayerControl.LocalPlayer.PlayerId == murderId) return;
            if (Helpers.playerById(murderId).Data.IsDead) return;
            RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId, murderId,false);
        }

        public override void OnExiledPre(byte[] voters)
        {
            if (voters.Length == 0) return;

            //ランダムに相手を選んで追放する
            RPCEventInvoker.UncheckedExilePlayer(voters[NebulaPlugin.rnd.Next(voters.Length)]);
        }

        public Provocateur()
            : base("Provocateur", "provocateur", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, false, false, false, false)
        {
        }
    }
}
