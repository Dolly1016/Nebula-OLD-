
namespace Nebula.Tasks;

[HarmonyPatch]
public class MinigamePatch
{
    static AudioClip? uiAppearClip = null;
    static AudioClip? uiDisappearClip = null;
    public static AudioClip UIAppearClip { get {
            if (uiAppearClip == null) uiAppearClip = Helpers.FindSound("Panel_GenericAppear");
            return uiAppearClip;
        } }
    
    public static AudioClip UIDisappearClip
    {
        get
        {
            if (uiDisappearClip == null) uiDisappearClip = Helpers.FindSound("Panel_GenericDisappear");
            return uiDisappearClip;
        }
    }

    [HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
    class MinigameBeginPatch
    {
        static public void Prefix(Minigame __instance, [HarmonyArgument(0)] PlayerTask task)
        {
            if (__instance.TaskType != TaskTypes.None) return;
            NebulaMinigame t = __instance.GetComponent<NebulaMinigame>();
            if (!t) return;

            if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(UIAppearClip, false, 1f, null);
            t.CloseSound = UIDisappearClip;
            
            t.__Begin(task);
            return;
        }
    }
}


public class NebulaMinigame : Minigame
{

    static NebulaMinigame()
    {
        ClassInjector.RegisterTypeInIl2Cpp<NebulaMinigame>();
    }

    public NebulaMinigame()
    {
    }

    public virtual void __Begin(PlayerTask playerTask) { }
}