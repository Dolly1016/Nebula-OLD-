using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Seer : Role
    {
        static public Color Color = new Color(60f / 255f, 181f / 255f, 101f / 255f);

        static public HashSet<Objects.Ghost> Ghosts=new HashSet<Objects.Ghost>();

        public override void OnAnyoneMurdered(byte murderId,byte targetId)
        {
            if (targetId == PlayerControl.LocalPlayer.PlayerId) return;

            Objects.Ghost ghost = new Objects.Ghost(Helpers.playerById(targetId).transform.position);
            Ghosts.Add(ghost);

            Helpers.PlayFlash(Color);
        }

        static public Module.CustomOption GhostDurationOption;

        public override void LoadOptionData()
        {
            GhostDurationOption = CreateOption(Color.white, "ghostDuration", 120f, 10f, 300f, 10f);
            GhostDurationOption.suffix = "second";
        }

        public override void Initialize(PlayerControl __instance)
        {
            foreach(Objects.Ghost g in Ghosts)
            {
                g.Remove();
            }
            Ghosts.Clear();
        }

        public override void MyUpdate()
        {
            foreach (Objects.Ghost g in Ghosts)
            {
                if (g.Timer > GhostDurationOption.getFloat())
                {
                    if (!g.IsInFadePhase) g.Fade();
                }
            }

            Ghosts.RemoveWhere((g) =>{ return g.IsRemoved; });
        }

        public Seer()
            : base("Seer", "seer", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, false, false, false, false)
        {
        }
    }
}
