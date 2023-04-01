namespace Nebula.Patches;

[HarmonyPatch]
public class RitualPatch
{

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.GetTaskById))]
    class GetTaskByIdPatch
    {
        public static bool Prefix(ShipStatus __instance, ref NormalPlayerTask __result, [HarmonyArgument(0)] byte idx)
        {
            if (idx == Byte.MaxValue - 2)
            {
                var obj = new GameObject();
                obj.hideFlags |= HideFlags.HideInHierarchy;
                __result = obj.AddComponent<Tasks.OpportunistTask>();
                return false;
            }
            else if (idx == Byte.MaxValue - 3)
            {
                var obj = new GameObject();
                obj.hideFlags |= HideFlags.HideInHierarchy;
                switch (Roles.Roles.Spectre.spectreTaskOption.getSelection())
                {
                    case 1:
                        __result = obj.AddComponent<Tasks.SpectreFriedTask>();
                        break;
                    case 2:
                        __result = obj.AddComponent<Tasks.SpectreRancorTask>();
                        break;
                }
                return false;
            }
            else if (idx == Byte.MaxValue - 4)
            {
                var obj = new GameObject();
                obj.hideFlags |= HideFlags.HideInHierarchy;
                __result = obj.AddComponent<Tasks.SpectreRancorAdditionalTask>();
                return false;
            }
            return true;
        }
    }
}