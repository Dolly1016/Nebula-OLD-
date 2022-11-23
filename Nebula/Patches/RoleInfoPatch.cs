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
                if (Input.GetKeyDown(Module.NebulaInputManager.helpInput.keyCode) && Module.MetaDialog.dialogOrder.Count==0)
                {
                    Module.MetaDialog.OpenHelpDialog(0,0);
                }
            }
        }
    }
}
