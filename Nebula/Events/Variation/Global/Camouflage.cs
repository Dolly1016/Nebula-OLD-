using Nebula.Utilities;

namespace Nebula.Events.Variation
{
    class Camouflage : GlobalEvent
    {
        public Camouflage(float duration, ulong option) :base(GlobalEvent.Type.Camouflage,duration,option)
        {
            AllowUpdateOutfit = false;
        }

        public override void OnActivate()
        {
            if (GlobalEvent.IsActive(GlobalEvent.Type.Camouflage)) return;

            //Camouflagerを確定させる
            Game.GameData.data.EstimationAI.Determine(Roles.Roles.Camouflager);

            foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                player.SetLook("", 38, "", "", "", "");
            }
        }

        public override void OnTerminal()
        {
            if (GlobalEvent.IsActive(GlobalEvent.Type.Camouflage)) return;

            foreach(PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                player.ResetOutfit();
            }
        }
    }
}
