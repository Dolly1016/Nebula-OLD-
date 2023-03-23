namespace Nebula.Roles.Perk.ImpostorPerks;

public class ItWasCrewmate : Perk
{
    public override bool IsAvailable => true;

    public override void OnCompleteHnSTaskGlobal(PerkHolder.PerkInstance perkData, byte playerId, ref float additional, ref float ratio)
    {
        if (PlayerControl.LocalPlayer.PlayerId != playerId) return;

        var p = Helpers.playerById(playerId);
        if (p.Data.IsDead) return;

        Helpers.Ping(new Vector2[] { p.GetTruePosition() }, false, (p) =>
        {
            SoundManager.Instance.PlaySound(p.soundOnEnable, false, 0.75f, null).pitch = 0.4f;
        });
    }

    public ItWasCrewmate(int id) : base(id, "itWasCrewmate", false, 23, 6, new Color(0.2f, 0.5f, 0.7f))
    {
    }
}
