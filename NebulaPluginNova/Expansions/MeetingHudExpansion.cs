using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Expansions;

public static class MeetingHudExpansion
{
    public static void RecheckPlayerState(this MeetingHud meetingHud)
    {
        bool existsDeadPlayer = false;

        foreach (PlayerVoteArea pva in meetingHud.playerStates)
        {
            bool isDead = NebulaGameManager.Instance?.GetModPlayerInfo(pva.TargetPlayerId)?.IsDead ?? true;

            if (pva.AmDead == isDead) continue;

            pva.SetDead(pva.DidReport, isDead);
            pva.Overlay.gameObject.SetActive(isDead);

            if (isDead)
            {
                foreach (PlayerVoteArea voter in meetingHud.playerStates)
                {
                    if (voter.VotedFor != pva.TargetPlayerId) continue;

                    var p = NebulaGameManager.Instance?.GetModPlayerInfo(voter.TargetPlayerId);
                    if (p.AmOwner)
                    {
                        meetingHud.ClearVote();
                    }

                    voter.ThumbsDown.enabled = false;
                    voter.UnsetVote();
                }
            }
        }
    }
}
