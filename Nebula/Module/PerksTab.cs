namespace Nebula.Module;

[HarmonyPatch]
public class PerksTabPacth
{
    static GameObject PerksGroup;
    static GameObject PerksHeader;

    [HarmonyPatch(typeof(PlayerCustomizationMenu), nameof(PlayerCustomizationMenu.Start))]
    class AddPerksTabPatch
    {
        static Sprite perksTabSprite;
        private static Sprite GetPerksTabSprite()
        {
            if (perksTabSprite) return perksTabSprite;
            perksTabSprite = Helpers.loadSpriteFromResources("Nebula.Resources.TabIconPerks.png", 75f);
            return perksTabSprite;
        }

        public static void Postfix(PlayerCustomizationMenu __instance)
        {
            PerksGroup = new GameObject("PerksGroup");
            PerksGroup.transform.SetParent(__instance.transform);

            Transform TabsTransform = __instance.transform.FindChild("Header").FindChild("Tabs");
            PerksHeader = GameObject.Instantiate(TabsTransform.FindChild("ColorTab").gameObject, TabsTransform);
            PerksHeader.transform.localPosition -= new Vector3(1, 0, 0);

            SpriteRenderer iconRenderer = PerksHeader.transform.FindChild("ColorButton").FindChild("Icon").gameObject.GetComponent<SpriteRenderer>();
            SpriteRenderer tabRenderer = PerksHeader.transform.FindChild("ColorButton").FindChild("Tab Background").gameObject.GetComponent<SpriteRenderer>();

            iconRenderer.sprite = GetPerksTabSprite();

            TabButton tab = new TabButton();
            tab.Button = tabRenderer;
            tab.Tab = __instance.cubesTab;

            tab.Button.enabled = false;

            //新たなタブを追加
            TabButton[] tabButtons = new TabButton[__instance.Tabs.Count + 1];
            __instance.Tabs.CopyTo(tabButtons, 0);
            tabButtons[tabButtons.Length - 1] = tab;
            __instance.Tabs = new Il2CppReferenceArray<TabButton>(tabButtons);

            UpdatePerksTabPatch.Postfix(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerCustomizationMenu), nameof(PlayerCustomizationMenu.Update))]
    public class UpdatePerksTabPatch
    {
        public static void Postfix(PlayerCustomizationMenu __instance)
        {
            bool perksTabValid = CustomOption.CurrentGameMode == CustomGameMode.Ritual;
            if (PerksHeader.activeSelf != perksTabValid)
            {
                PerksHeader.SetActive(perksTabValid);
            }


        }
    }

}

public class PerksTab
{
}