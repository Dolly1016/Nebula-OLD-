using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;

namespace Nebula.Roles.CrewmateRoles
{
    public class Bait : Role
    {
        static public Color RoleColor = new Color(0f / 255f, 247f / 255f, 255f / 255f);

        public class BaitEvent : Events.LocalEvent
        {
            byte murderId;
            public BaitEvent(byte murderId) : base(0.1f + (float)NebulaPlugin.rnd.NextDouble() * 0.2f)
            {
                this.murderId = murderId;
            }

            public override void OnTerminal()
            {
                RPCEventInvoker.UncheckedCmdReportDeadBody(murderId, PlayerControl.LocalPlayer.PlayerId);
            }
        }
    
        public override void OnMurdered(byte murderId)
        {
            //少しの時差の後レポート
            Events.LocalEvent.Activate(new BaitEvent(murderId));
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Spy);
            RelatedRoles.Add(Roles.Madmate);
        }

        public Bait()
            : base("Bait", "bait", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
        }
    }
}
