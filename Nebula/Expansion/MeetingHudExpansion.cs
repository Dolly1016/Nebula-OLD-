using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula
{
    public static class MeetingHudExpansion
    {
        public static void RecheckPlayerState(this MeetingHud meetingHud)
        {
            foreach (PlayerVoteArea pva in meetingHud.playerStates)
            {
                bool isDead=!Game.GameData.data.GetPlayerData(pva.TargetPlayerId).IsAlive;
                bool mismatch = pva.AmDead != isDead;

                if (!mismatch) continue;
                
                pva.SetDead(pva.DidReport, isDead);
                pva.Overlay.gameObject.SetActive(isDead);


                if (isDead)
                {
                    foreach(PlayerVoteArea voter in meetingHud.playerStates)
                    {
                        if (voter.VotedFor != pva.TargetPlayerId) continue;

                        voter.UnsetVote();

                        if (Helpers.playerById(voter.TargetPlayerId).AmOwner) meetingHud.ClearVote();
                    }
                }
            }
        }
    }
}
