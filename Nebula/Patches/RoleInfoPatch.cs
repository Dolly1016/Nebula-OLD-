using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Nebula.Utilities;

namespace Nebula.Patches
{
    [Harmony]
    public class CustomOverlays
    {
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        public static class CustomOverlayKeybinds
        {
            public static void Postfix(KeyboardJoystick __instance)
            {
                if (Input.GetKeyDown(KeyCode.H) && Module.MetaDialog.dialogOrder.Count==0)
                {
                    Module.MetaDialog.OpenHelpDialog(0,0);
                }
            }
        }
    }
}
