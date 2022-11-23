namespace Nebula.Events.Variation;

class Camouflage : GlobalEvent
{
    Game.PlayerData.PlayerOutfitData outfit;

    public Camouflage(float duration, ulong option) : base(GlobalEvent.Type.Camouflage, duration - 1f, option)
    {
    }


    public override void OnActivate()
    {
        //Camouflagerを確定させる
        Game.GameData.data.EstimationAI.Determine(Roles.Roles.Camouflager);

        outfit = new Game.PlayerData.PlayerOutfitData(100, "", 38, "", "", "", "");
        foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            player.AddOutfit(outfit);
        }
    }

    public override void OnTerminal()
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            player.RemoveOutfit(outfit);
        }
    }
}