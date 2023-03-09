using JetBrains.Annotations;
using Nebula.Roles.Perk;
using TMPro;

namespace Nebula.Module;

public class PerksTab : InventoryTab
{
    static PerksTab()
    {
        ClassInjector.RegisterTypeInIl2Cpp<PerksTab>();
    }

    private void OpenPerkDialog(int index)
    {
        bool isCrewmatePerkSlot = !Perk.IsSeekerPerkSlot(index);
        var designer = MetaDialog.OpenDialog(new Vector2(8f, 4f),"");
        var parent = designer.screen.screen.transform;

        int x = 11, y = 3;
        int num = 0;

        designer.CustomUse(y * 0.62f + 0.5f);

        var dialogPerkNameText = new MSString(4f, "", 2.5f, 2.5f, TextAlignmentOptions.Center, FontStyles.Bold);
        var dialogPerkFlavorText = new MSMultiString(7f, 1.8f, "\n\n\n", TextAlignmentOptions.Top, FontStyles.Normal);

        designer.AddTopic(dialogPerkNameText);
        designer.CustomUse(-0.1f);
        designer.AddTopic(dialogPerkFlavorText);

        PerkDisplay GenerateDisplay()
        {
            GameObject obj = new("PerkDisplay");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3((num % x - 5f) * 0.68f, 1.3f - (float)((int)num / x) * 0.62f, -2f);
            obj.transform.localScale = Vector3.one * 0.4f;

            var display = obj.AddComponent<PerkDisplay>();

            num++;

            return display;
        }

        var emptyDisplay = GenerateDisplay();
        var emptyButton = emptyDisplay.SetUpButton(Color.yellow);
        emptyButton.OnClick.AddListener((Action)(() =>
        {
            Perk.SetEquipedPerk(index % 5, isCrewmatePerkSlot, null);
            UpdatePerk();
            MetaDialog.EraseDialogAll();
        }));
        emptyButton.OnMouseOver.AddListener((Action)(() =>
        {
            dialogPerkNameText.text.text = Language.Language.GetString("perks.unequip.name");
            dialogPerkFlavorText.text.text = Language.Language.GetString("perks.unequip.flavor");
        }));

        int unavailables = 0;
        foreach (var p in Roles.Perk.Perks.AllPerks.Values)
        {
            if (isCrewmatePerkSlot != p.IsCrewmatePerk) continue;

            if (!p.IsAvailable)
            {
                unavailables++;
                continue;
            }

            var display = GenerateDisplay();

            display.SetPerk(p);

            var perk = p;
            var button = display.SetUpButton(Color.yellow);

            bool isDuplicated = false;
            for (int i = 0; i < 5; i++) if (p == Perk.GetEquipedPerk(i, isCrewmatePerkSlot)) { isDuplicated = true;break; }

            if (isDuplicated)
            {
                display.Highlight.color = Color.white;
                display.Highlight.gameObject.SetActive(true);
                button.OnMouseOut.RemoveAllListeners();
            }
            else
            {
                button.OnClick.AddListener((Action)(() =>
                {
                    Perk.SetEquipedPerk(index % 5, isCrewmatePerkSlot, perk);
                    UpdatePerk();
                    MetaDialog.EraseDialogAll();
                }));
            }

            button.OnMouseOver.AddListener((Action)(() =>
            {
                dialogPerkNameText.text.text = perk.DisplayName;
                dialogPerkFlavorText.text.text = perk.DisplayFlavor;
            }));
        }

        for (; num < x * y && unavailables > 0; unavailables--) GenerateDisplay();
    }

    PerkDisplay[] PerkDisplays;
    TextMeshPro PerkNameText;
    TextMeshPro PerkFlavor;

    public void Start()
    {
        var headerText = GameObject.Instantiate(PlayerCustomizationMenu.Instance.transform.GetChild(4).GetChild(0).gameObject.GetComponent<TMPro.TextMeshPro>(),transform);
        headerText.text = Language.Language.GetString("perks.perk");
        headerText.transform.localPosition = new Vector3(-2.65f, 1.77f, -55f);
        GameObject.Destroy(headerText.GetComponent<TextTranslatorTMP>());

        PerkNameText = GameObject.Instantiate(headerText,transform);
        PerkNameText.transform.localPosition = new Vector3(0f,0f,-10f);
        PerkNameText.alignment = TextAlignmentOptions.Center;
        PerkNameText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        PerkNameText.text = "";
        PerkNameText.rectTransform.localScale = new Vector3(0.85f, 0.85f, 1f);

        PerkFlavor = Helpers.GenerateText(transform, "", 2f, new Vector2(7f, 1.5f), TextAlignmentOptions.Top, FontStyles.Normal);
        PerkFlavor.transform.localPosition = new Vector3(0f, -1.05f, -10f);

        PerkDisplays = new PerkDisplay[10];

        for (int i = 0; i < 10; i++){
            var obj = new GameObject("Perk");
            obj.transform.SetParent(gameObject.transform);
            obj.transform.localPosition = new Vector3(0.9f * (float)(i % 5 - 2), (i < 5 ? 0.9f : -2.3f) + (i % 2 == 1 ? 0f : 0.56f), 0f);
            obj.transform.localScale = Vector3.one * 0.75f;
            PerkDisplays[i] = obj.AddComponent<PerkDisplay>();
            var button = PerkDisplays[i].SetUpButton(Color.yellow);
            int index = i;
            button.OnClick.AddListener((Action)(()=> OpenPerkDialog(index)));
            button.OnMouseOver.AddListener((Action)(() => {
                Perk? p = Perk.GetEquipedPerk(index);
                if (p == null)
                {
                    PerkNameText.text = Language.Language.GetString("perks.unselected.name");
                    PerkFlavor.text = Language.Language.GetString("perks.unselected.flavor");
                }
                else {
                    PerkNameText.text = p.DisplayName;
                    PerkFlavor.text = p.DisplayFlavor;
                }
            }));
        }

        UpdatePerk();
    }

    public override void OnEnable(){
        PlayerCustomizationMenu.Instance.PreviewArea.transform.parent.gameObject.SetActive(false);
    }
    public override void OnDisable() {
        PlayerCustomizationMenu.Instance.PreviewArea.transform.parent.gameObject.SetActive(true);
    }

    public void UpdatePerk()
    {
        for (int i = 0; i < 10; i++) PerkDisplays[i].SetPerk(Perk.GetEquipedPerk(i));
    }
}


[HarmonyPatch]
public class PerksTabPacth
{
    static PerksTab PerksTab;
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
            var PerksTabObj = new GameObject("PerksGroup");
            PerksTabObj.transform.SetParent(__instance.transform);
            PerksTabObj.transform.localPosition = new Vector3(0f, 0f, -20f);
            PerksTabObj.SetActive(false);
            PerksTab = PerksTabObj.AddComponent<PerksTab>();

            Transform TabsTransform = __instance.transform.FindChild("Header").FindChild("Tabs");
            PerksHeader = GameObject.Instantiate(TabsTransform.FindChild("ColorTab").gameObject, TabsTransform);
            PerksHeader.transform.localPosition -= new Vector3(1, 0, 0);

            SpriteRenderer iconRenderer = PerksHeader.transform.FindChild("ColorButton").FindChild("Icon").gameObject.GetComponent<SpriteRenderer>();
            SpriteRenderer tabRenderer = PerksHeader.transform.FindChild("ColorButton").FindChild("Tab Background").gameObject.GetComponent<SpriteRenderer>();
            var tabButton = tabRenderer.gameObject.GetComponent<PassiveButton>();
            tabButton.OnClick.RemoveAllListeners();
            tabButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>  __instance.OpenTab(PerksTab)));

            iconRenderer.sprite = GetPerksTabSprite();

            TabButton tab = new TabButton();
            tab.Button = tabRenderer;
            tab.Tab = PerksTab;

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
            bool perksTabValid = CustomOption.CurrentGameMode == CustomGameMode.StandardHnS;
            if (PerksHeader.activeSelf != perksTabValid)
            {
                PerksHeader.SetActive(perksTabValid);
            }


        }
    }
    

}
