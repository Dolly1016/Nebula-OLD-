namespace Nebula.Patches;

[Harmony]
public class CustomOverlays
{
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    public static class CustomOverlayKeybinds
    {
        public static void Postfix(KeyboardJoystick __instance)
        {
            if (!Components.TextInputField.ValidField && Input.GetKeyDown(Module.NebulaInputManager.helpInput.keyCode) && Module.MetaDialog.dialogOrder.Count == 0)
            {
                Module.MetaDialog.OpenHelpDialog(0, 0);
            }
        }
    }

    [HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
    class TaskTextPatch
    {

        public static void Postfix(TaskPanelBehaviour __instance)
        {
            try
            {
                var data = PlayerControl.LocalPlayer.GetModData();
                if (data == null) return;
                if (data.role == null) return;

                var role = (Roles.Assignable)(data.ShouldBeGhostRole ? data.ghostRole! : data.role);

                var text =
                    Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name") + ": " +
                     Language.Language.GetString("role." + role.LocalizeName + ".hint"))
                    + "\n" + __instance.taskText.text;
                string? append = data.role.GetCustomTaskText();
                if (append != null) { text += "\n"+append; }
                __instance.taskText.text = text;
            }
            catch { }
        }
    }
}