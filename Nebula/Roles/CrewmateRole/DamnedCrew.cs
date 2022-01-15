using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Roles.CrewmateRoles
{
    public class DamnedCrew : Crewmate
    {
        int guardLeftId;
        bool changeTrigger;
        public override void GlobalInitialize(PlayerControl __instance)
        {
            Game.GameData.data.players[__instance.PlayerId].SetRoleData(guardLeftId, 1);
        }

        //変身トリガは個人で持つ(役職を交換されても引き継がない)
        public override void Initialize(PlayerControl __instance)
        {
            changeTrigger = false;
        }

        public override Helpers.MurderAttemptResult OnMurdered(byte murderId, byte playerId) {
            if (Game.GameData.data.players[playerId].GetRoleData(guardLeftId) > 0)
            {
                RPCEventInvoker.AddAndUpdateRoleData(playerId,guardLeftId,-1);
                return Helpers.MurderAttemptResult.SuppressKill;
            }
            return Helpers.MurderAttemptResult.PerformKill; 
        }

        public override void OnUpdateRoleData(int dataId, int newData)
        {
            if (dataId == guardLeftId && newData >= 0) {
                Helpers.
                    PlayQuickFlash(Palette.ImpostorRed);
                Objects.CustomMessage.Create(new Vector3(0, 0, 0), false, Language.Language.GetString("role.damned.message.killed"), 0.2f, 4f, 1f, 1.5f, Color.yellow, Color.red);
                //変身トリガ
                changeTrigger = true;
            }
        }

        public override void OnMeetingStart()
        {
            if (!changeTrigger) return;
            RPCEventInvoker.ChangeRole(PlayerControl.LocalPlayer,Roles.Damned);
        }

        public DamnedCrew():base()
        {
            guardLeftId = Game.GameData.RegisterRoleDataId("damnedCrew.guardLeft");
            IsGuessableRole = false;
        }
    }
}
