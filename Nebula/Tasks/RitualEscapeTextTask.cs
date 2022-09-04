using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnhollowerRuntimeLib;
using UnhollowerBaseLib.Attributes;
using HarmonyLib;
using Nebula.Utilities;

namespace Nebula.Tasks
{
    public class RitualEscapeTextTask : NebulaPlayerTask
    {
        static RitualEscapeTextTask()
        {
            ClassInjector.RegisterTypeInIl2Cpp<RitualEscapeTextTask>();
        }

        public RitualEscapeTextTask()
        {
            __Initialize();
        }

        public override void __AppendTaskText(Il2CppSystem.Text.StringBuilder sb)
        {
            sb.Append(Language.Language.GetString("ritual.mission.escape"));
            sb.AppendLine();
        }

        public override void __NextStep()
        {
        }


        public override void __Initialize()
        {
            
        }

        public override bool __ValidConsole(Console console) { return false; }

        public override bool __IsCompleted()
        {
            return false;
        }
    }
}
