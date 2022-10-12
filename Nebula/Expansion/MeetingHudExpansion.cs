using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula
{
    public static class MeetingHudExpansion
    {
        public static void RecheckPlayerState(this MeetingHud meetingHud)
        {
            bool existsDeadPlayer = false;

            foreach (PlayerVoteArea pva in meetingHud.playerStates)
            {
                bool isDead=!Game.GameData.data.GetPlayerData(pva.TargetPlayerId).IsAlive;
                bool mismatch = pva.AmDead != isDead;

                if (!mismatch) continue;
                
                pva.SetDead(pva.DidReport, isDead);
                pva.Overlay.gameObject.SetActive(isDead);

                existsDeadPlayer |= isDead;
            }

            if (existsDeadPlayer)
            {
                foreach (PlayerVoteArea voter in meetingHud.playerStates)
                {
                    voter.ThumbsDown.enabled = false;

                    if (voter.DidVote) {
                        if (Helpers.playerById(voter.TargetPlayerId).AmOwner)
                        {
                            Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(),
                                (r) => r.OnVoteCanceled(Patches.MeetingHudPatch.GetVoteWeight(voter.TargetPlayerId)));
                            meetingHud.ClearVote();
                        }

                        voter.UnsetVote();
                    }
                }
            }
        }
    }
}
