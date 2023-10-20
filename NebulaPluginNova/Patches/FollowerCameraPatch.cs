using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(FollowerCamera),nameof(FollowerCamera.Update))]
static public class FollowerCameraPatch
{
    public static void Prefix(FollowerCamera __instance)
    {
        if (!__instance.Target) __instance.Target = PlayerControl.LocalPlayer;

        if (PlayerControl.LocalPlayer.lightSource)
        {
            PlayerControl.LocalPlayer.lightSource.transform.SetParent(null);
            PlayerControl.LocalPlayer.lightSource.transform.position = __instance.Target.transform.position;
        }
    }
}
