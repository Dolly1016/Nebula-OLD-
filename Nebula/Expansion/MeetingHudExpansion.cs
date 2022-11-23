namespace Nebula;

public static class MeetingHudExpansion
{
    public static void RecheckPlayerState(this MeetingHud meetingHud)
    {
        bool existsDeadPlayer = false;

        foreach (PlayerVoteArea pva in meetingHud.playerStates)
        {
            bool isDead = !Game.GameData.data.GetPlayerData(pva.TargetPlayerId).IsAlive;
            bool mismatch = pva.AmDead != isDead;

            if (!mismatch) continue;

            pva.SetDead(pva.DidReport, isDead);
            pva.Overlay.gameObject.SetActive(isDead);

            if (isDead)
            {
                foreach (PlayerVoteArea voter in meetingHud.playerStates)
                {
                    if (voter.VotedFor != pva.TargetPlayerId) continue;

                    PlayerControl p = Helpers.playerById(voter.TargetPlayerId);
                    if (p.AmOwner)
                    {
                        meetingHud.ClearVote();
                        Helpers.RoleAction(p, (r) => r.OnVoteCanceled(Patches.MeetingHudPatch.GetVoteWeight(voter.TargetPlayerId)));
                    }

                    voter.ThumbsDown.enabled = false;
                    voter.UnsetVote();
                }
            }
        }
    }
}