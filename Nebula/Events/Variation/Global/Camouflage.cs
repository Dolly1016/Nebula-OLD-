using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Events.Variation
{
    class Camouflage : GlobalEvent
    {
        public Camouflage(float duration):base(GlobalEvent.Type.Camouflage,duration)
        {
            AllowUpdateOutfit = false;
        }

        public override void OnActivate()
        {
            if (GlobalEvent.IsActive(GlobalEvent.Type.Camouflage)) return;

            //Camouflagerを確定させる
            Game.GameData.data.EstimationAI.Determine(Roles.Roles.Camouflager);

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                player.SetLook("", 38, "", "", "", "");
            }
        }

        public override void OnTerminal()
        {
            if (GlobalEvent.IsActive(GlobalEvent.Type.Camouflage)) return;

            foreach(PlayerControl player in PlayerControl.AllPlayerControls)
            {
                player.ResetOutfit();
            }
        }
    }
}
