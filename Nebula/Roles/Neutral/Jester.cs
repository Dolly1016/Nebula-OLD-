using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.Neutral
{
    public class Jester : Role
    {
        static public Color Color = new Color(253f / 255f, 84f / 255f, 167f / 255f);

        public bool WinTrigger=false;

        public override bool OnExiled()
        {
            WinTrigger = true;
            return false;    
        }

        public override void Initialize(PlayerControl __instance)
        {
            WinTrigger = false;
        }

        public Jester()
            : base("Jester", "jester", Color, RoleCategory.Neutral, Side.Jester, Side.Jester,
                 new HashSet<Side>() { Side.Jester }, new HashSet<Side>() { Side.Jester },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.JesterWin },
                 true, false, false, false, false)
        {
        }
    }
}