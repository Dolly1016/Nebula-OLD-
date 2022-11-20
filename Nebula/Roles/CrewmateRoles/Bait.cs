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

        private Module.CustomOption killerCanKnowBaitKillByFlash;

        public class BaitEvent : Events.LocalEvent
        {
            byte murderId;
            public BaitEvent(byte murderId) : base(0.2f + (float)NebulaPlugin.rnd.NextDouble() * 0.2f)
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
            if (MeetingHud.Instance) return;

            //Baitが発動しない場合
            if (PlayerControl.LocalPlayer.IsMadmate() && PlayerControl.AllPlayerControls[murderId].Data.Role.Role == RoleTypes.Impostor) return;
            
            //少しの時差の後レポート
            Events.LocalEvent.Activate(new BaitEvent(murderId));
        }

        //キルしたプレイヤーにフラッシュ
        public override void OnDied(byte playerId)
        {
            if (!killerCanKnowBaitKillByFlash.getBool()) return;
            if (!Game.GameData.data.deadPlayers.ContainsKey(playerId)) return;
            if (Game.GameData.data.deadPlayers[playerId].MurderId != PlayerControl.LocalPlayer.PlayerId) return;

            //Baitが発動しない場合
            if (Helpers.playerById(playerId).IsMadmate() && PlayerControl.LocalPlayer.Data.Role.Role == RoleTypes.Impostor) return;

            Helpers.PlayQuickFlash(Color);
        }



        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Spy);
            RelatedRoles.Add(Roles.Madmate);
        }

        public override void PreloadOptionData()
        {
            defaultUnassignable.Add(Roles.Lover);
        }

        public override void LoadOptionData()
        {
            killerCanKnowBaitKillByFlash = CreateOption(Color.white, "killerCanKnowBaitKillByFlash", true);
        }

        public Bait()
            : base("Bait", "bait", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
        }
    }
}
