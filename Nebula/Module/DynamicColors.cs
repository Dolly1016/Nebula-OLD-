using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Il2CppSystem;
using HarmonyLib;
using UnhollowerBaseLib;
using Assets.CoreScripts;
using BepInEx.Configuration;

namespace Nebula.Module
{
    

    [HarmonyPatch]
    public static class DynamicColors
    {
        static public bool IsLightColor(Color color) {
            return (color.r + color.g + color.b > 1.5);
        }

        public class CustomColor
        {
            private ConfigEntry<float> EntryR;
            private ConfigEntry<float> EntryG;
            private ConfigEntry<float> EntryB;
            //ShadowType
            private ConfigEntry<byte> EntryS;
            //ColorName
            private ConfigEntry<byte> EntryH;
            private ConfigEntry<byte> EntryD;

            public Color Color { get; private set; }
            public Color ShadowColor { get; private set; }

            public CustomColor(string saveCategory)
            {
                EntryR = NebulaPlugin.Instance.Config.Bind(saveCategory, "R", (float)1f);
                EntryG = NebulaPlugin.Instance.Config.Bind(saveCategory, "G", (float)0f);
                EntryB = NebulaPlugin.Instance.Config.Bind(saveCategory, "B", (float)0f);

                //陰タイプ
                EntryS = NebulaPlugin.Instance.Config.Bind(saveCategory, "S", (byte)0);

                //色名判別
                EntryH = NebulaPlugin.Instance.Config.Bind(saveCategory, "H", (byte)0);
                EntryD = NebulaPlugin.Instance.Config.Bind(saveCategory, "D", (byte)8);

                Color = new Color(EntryR.Value,EntryG.Value,EntryB.Value,1f);
                ShadowColor = GetShadowColor(Color, EntryS.Value);
            }

            public void SetColor(Color color,byte h,byte d,byte shadowType)
            {
                EntryR.Value = color.r;
                EntryG.Value = color.g;
                EntryB.Value = color.b;

                EntryS.Value = shadowType;

                EntryH.Value = (byte)(h%64);
                if (d < 24)
                    EntryD.Value = d;
                else
                    EntryD.Value = 23;

                Color = color;
                ShadowColor= GetShadowColor(Color, EntryS.Value);
            }

            public byte GetHue()
            {
                return EntryH.Value;
            }

            public byte GetDistance()
            {
                return EntryD.Value;
            }

            public byte GetShadowType()
            {
                return EntryS.Value;
            }
        }

        public class SaveButton
        {
            GameObject ButtonObject;
            PassiveButton PassiveButton;
            BoxCollider2D Collider;
            SpriteRenderer Renderer;
            int RefBodyId;
            ColorButton RelateButton;

            public SaveButton(PlayerTab __instance,  ColorButton relateButton)
            {
                RefBodyId = relateButton.RefBodyId;

                ButtonObject = new GameObject("SaveButton");
                ButtonObject.transform.SetParent(relateButton.ButtonObject.transform);

                Collider = ButtonObject.AddComponent<BoxCollider2D>();
                PassiveButton = ButtonObject.AddComponent<PassiveButton>();

                Renderer = ButtonObject.AddComponent<SpriteRenderer>();
                Renderer.sprite = GetSaveButtonSprite();
                Renderer.transform.localPosition = new Vector3(0.6f, -0.01f, 0);

                Collider.size = new Vector2(0.45f, 0.45f);

                PassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                PassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                PassiveButton.OnClick.RemoveAllListeners();
                PassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    SaveCurrentColor(RefBodyId);
                }));
                PassiveButton.enabled = true;

                OnEnable(__instance);
            }

            public void OnEnable(PlayerTab __instance)
            {
                ButtonObject.layer = __instance.ColorChips[0].gameObject.layer;
            }
        }

        public class ColorButton
        {
            public GameObject ButtonObject { get; private set; }
            GameObject SubObject;
            PassiveButton PassiveButton;
            BoxCollider2D Collider;
            SpriteRenderer BaseRenderer;
            SpriteRenderer ShadowRenderer;
            public int RefBodyId { get; private set; }
            Vector3 Position;

            CustomColor Color;

            public ColorButton(PlayerTab __instance,Vector3 position, int refBodyId,System.Action onClick, CustomColor color=null)
            {
                RefBodyId = refBodyId;
                Position = position;
                Color = color;

                ButtonObject = new GameObject("ColorButton");

                Collider = ButtonObject.AddComponent<BoxCollider2D>();
                PassiveButton = ButtonObject.AddComponent<PassiveButton>();

                ShadowRenderer = ButtonObject.AddComponent<SpriteRenderer>();
                ShadowRenderer.sprite = GetButtonSprite();

                SubObject = new GameObject("ColorButtonBase");
                SubObject.transform.SetParent(ButtonObject.transform);
                BaseRenderer = SubObject.AddComponent<SpriteRenderer>();
                BaseRenderer.sprite = GetBaseButtonSprite();
                BaseRenderer.transform.position += new Vector3(0, 0, -1f);

                Collider.size = new Vector2(0.68f,0.45f);

                PassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                PassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                PassiveButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
                PassiveButton.OnMouseOver.AddListener((System.Action)(() =>
                {
                    PlayerControl.SetPlayerMaterialColors(RefBodyId, PlayerCustomizationMenu.Instance.PreviewArea.Body);
                    if (Color != null)
                    {
                        PlayerCustomizationMenu.Instance.SetItemName(GetColorName(Color.GetHue(),Color.GetDistance()));
                    }
                }));

                PassiveButton.OnClick.RemoveAllListeners();
                PassiveButton.OnClick.AddListener(onClick);
                PassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    PlayerControl.SetPlayerMaterialColors(SaveManager.BodyColor, PlayerCustomizationMenu.Instance.PreviewArea.Body);
                    PlayerCustomizationMenu.Instance.SetItemName(GetColorName());
                }));
                PassiveButton.enabled = true;

                OnEnable(__instance);
            }

            public void OnEnable(PlayerTab __instance)
            {
                ButtonObject.transform.SetParent(__instance.ColorChips[0].transform.parent);
                ButtonObject.transform.localPosition = Position;
                ButtonObject.layer = __instance.ColorChips[0].gameObject.layer;

                SubObject.layer = __instance.ColorChips[0].gameObject.layer;
            }

            public void Update()
            {
                BaseRenderer.color = Palette.PlayerColors[RefBodyId];
                ShadowRenderer.color = Palette.ShadowColors[RefBodyId];
            }
        }

        static private ColorButton[] ShadowVariations=null;
        static private ColorButton[] SaveVariations = null;
        static private SaveButton[] WriteSaveVariations = null;

        static private Sprite PaletteSprite;
        static private Sprite GetPalleteSprite()
        {
            if (PaletteSprite) return PaletteSprite;
            PaletteSprite = Helpers.loadSpriteFromResources("Nebula.Resources.Palette.png", 100f);
            return PaletteSprite;
        }

        static private Sprite BaseButtonSprite;
        static private Sprite GetBaseButtonSprite()
        {
            if (BaseButtonSprite) return BaseButtonSprite;
            BaseButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ColorHalfButton.png", 100f);
            return BaseButtonSprite;
        }

        static private Sprite ButtonSprite;
        static private Sprite GetButtonSprite()
        {
            if (ButtonSprite) return ButtonSprite;
            ButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ColorFullBase.png", 100f);
            return ButtonSprite;
        }

        static private Sprite SaveButtonSprite;
        static private Sprite GetSaveButtonSprite()
        {
            if (SaveButtonSprite) return SaveButtonSprite;
            SaveButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ColorSaveButton.png", 100f);
            return SaveButtonSprite;
        }

        static private GameObject PaletteObject = null;
        static private SpriteRenderer Renderer = null;
        static private PassiveButton PassiveButton = null;
        static private CircleCollider2D Collider = null;

        static private string ColorName;

        static public CustomColor MyColor;
        static private CustomColor[] SaveColor;

        static public Color GetShadowColor(Color myColor,byte shadowType)
        {
            switch (shadowType)
            {
                case 0:
                    return myColor.RGBMultiplied(0.5f);
                case 1:
                    return myColor.RGBMultiplied(0.8f) * myColor * myColor * myColor;
            }
            return Color.black;
        }

        static public void Load()
        {
            var PlayerColors = Enumerable.ToList<Color32>(Palette.PlayerColors);
            var ShadowColors = Enumerable.ToList<Color32>(Palette.ShadowColors);
            
            while (PlayerColors.Count < 25)
            {
                PlayerColors.Add(new Color32());
            }
            
            while (ShadowColors.Count < 25)
            {
                ShadowColors.Add(new Color32());
            }
            
            Palette.PlayerColors = PlayerColors.ToArray();
            Palette.ShadowColors = ShadowColors.ToArray();
            
            MyColor = new CustomColor("Color");
            SaveColor = new CustomColor[5];

            for (int i = 0; i < SaveColor.Length; i++)
            {
                SaveColor[i] = new CustomColor("SaveColor" + i);
                Palette.PlayerColors[20 + i] = SaveColor[i].Color;
                Palette.ShadowColors[20 + i] = SaveColor[i].ShadowColor;
            }
            
            if (SaveManager.BodyColor >= Palette.PlayerColors.Count) SaveManager.BodyColor = 0;
            Palette.PlayerColors[SaveManager.BodyColor] = MyColor.Color;
            Palette.ShadowColors[SaveManager.BodyColor] = MyColor.ShadowColor;
            
            for (int i = 0; i < 2; i++)
            {
                Palette.PlayerColors[16 + i] = MyColor.Color;
                Palette.ShadowColors[16 + i] = GetShadowColor(MyColor.Color,(byte)i);
            }
            
        }

        static public void SaveAndSetColor(Color color,byte h, byte d,byte shadowType,byte playerId)
        {
            MyColor.SetColor(color, (byte)(h % 64), d >= 24 ? (byte)23 : d, shadowType);

            Palette.PlayerColors[playerId] = MyColor.Color;
            Palette.ShadowColors[playerId] = MyColor.ShadowColor;

            for (int i = 0; i < 2; i++)
            {
                Palette.PlayerColors[16 + i] = MyColor.Color;
                Palette.ShadowColors[16 + i] = GetShadowColor(MyColor.Color, (byte)i);
            }
        }

        static public void SaveCurrentColor(int ColorId)
        {
            SaveColor[ColorId - 20].SetColor(MyColor.Color,MyColor.GetHue(),MyColor.GetDistance(),MyColor.GetShadowType());
            Palette.PlayerColors[ColorId] = SaveColor[ColorId-20].Color;
            Palette.ShadowColors[ColorId] = SaveColor[ColorId - 20].ShadowColor;
        }

        static public void SetOthersColor(Color color, Color shadowColor, byte playerId)
        {
            Palette.PlayerColors[playerId] = color;
            Palette.ShadowColors[playerId] = shadowColor;
        }

        private static string GetColorName()
        {
            return Language.Language.GetString("color." + (MyColor.GetHue()) + "." + MyColor.GetDistance());
        }

        private static string GetColorName(byte h,byte d)
        {
            return Language.Language.GetString("color." + (h % 64) + "." + d);
        }

        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), new[] {
                typeof(StringNames),
                typeof(Il2CppReferenceArray<Il2CppSystem.Object>)
            })]
        private class ColorStringPatch
        {
            public static bool Prefix(ref string __result, [HarmonyArgument(0)] StringNames name)
            {
                if (((int)StringNames.ColorOrange<=(int)name&& (int)StringNames.ColorLime >= (int)name)||
                    ((int)StringNames.ColorMaroon <= (int)name && (int)StringNames.ColorSunset >= (int)name)||
                    StringNames.ColorCoral==name)
                {
                    __result = GetColorName();
                    return false;
                }
                return true;
            }
        }

        private static void DetectColor(Color color,byte h,byte d,byte shadowType,bool setFlag)
        {
            if (setFlag)
            {
                if (PlayerControl.LocalPlayer != null)
                {
                    SaveAndSetColor(color, h, d, shadowType, PlayerControl.LocalPlayer.PlayerId);
                    PlayerControl.LocalPlayer.SetColor(PlayerControl.LocalPlayer.PlayerId);
                    RPCEventInvoker.SetMyColor();
                }
                else
                {
                    SaveAndSetColor(color, h, d, shadowType, SaveManager.BodyColor);
                }
            }
            else
            {
                Palette.PlayerColors[15] = color;
                Palette.ShadowColors[15] = GetShadowColor(color, MyColor.GetShadowType());

                if (d < 24f)
                {
                    PlayerControl.SetPlayerMaterialColors(15, PlayerCustomizationMenu.Instance.PreviewArea.Body);
                    PlayerCustomizationMenu.Instance.SetItemName(GetColorName(h, d));
                }
                else if (d < 26f)
                {
                    PlayerControl.SetPlayerMaterialColors(SaveManager.BodyColor, PlayerCustomizationMenu.Instance.PreviewArea.Body);
                    PlayerCustomizationMenu.Instance.SetItemName(GetColorName());
                }
            }
        }

        private static void DetectColor(bool setFlag)
        {
            Vector3 pos = Camera.current.ScreenToWorldPoint(Input.mousePosition) - PaletteObject.transform.position;
            float dis = Mathf.Sqrt(pos.x * pos.x + pos.y * pos.y);

            float angle = Mathf.Atan2(pos.y, pos.x) - (float)System.Math.PI / 2f;
            if (angle < 0) angle += (float)(System.Math.PI * 2f);
            Color color;
            float h = angle / (float)(2f * System.Math.PI);
            color = Color.HSVToRGB(h, 1f, 1f);
            if (dis < 0.45)
            {
                float v = 1f - (dis / 0.45f);
                color += new Color(v, v, v);
            }
            else if (dis > 1.1f)
            {
                float s = dis - 1.1f;
                float sum = (color.r + color.g + color.b) / 3f;



                color = new Color(sum, sum, sum) * s + color * (1 - s);
            }
            DetectColor(color, (byte)(h * 64), (byte)((dis / 2.1f) * 24), MyColor.GetShadowType(),setFlag);
        }

        [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.Update))]
        private static class PlayerTabUpdatePatch
        {
            public static void Postfix(PlayerTab __instance)
            {
                if (!__instance.enabled) return;
                DetectColor(false);

                if (ShadowVariations!=null)
                {
                    foreach (ColorButton button in ShadowVariations)
                    {
                        button.Update();
                    }
                }

                if (SaveVariations != null)
                {
                    foreach (ColorButton button in SaveVariations)
                    {
                        button.Update();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
        private static class PlayerTabEnablePatch
        {
            public static void Postfix(PlayerTab __instance)
            {
                //全てのColorChipを無効化
                foreach (ColorChip colorChip in __instance.ColorChips)
                {
                    colorChip.transform.localScale *= 0f;
                    colorChip.enabled = false;
                    colorChip.Button.enabled = false;
                    colorChip.Button.OnClick.RemoveAllListeners();
                }

                //パレット
                if (PaletteObject == null)
                {
                    PaletteObject = new GameObject("DynamicPalette");
                    Renderer = PaletteObject.AddComponent<SpriteRenderer>();
                    Collider = PaletteObject.AddComponent<CircleCollider2D>();
                    PassiveButton = PaletteObject.AddComponent<PassiveButton>();

                    ShadowVariations = new ColorButton[2];
                    for (int i = 0; i < ShadowVariations.Length; i++)
                    {
                        int index = i;
                        ShadowVariations[i] =
                            new ColorButton(__instance, new Vector3(1.4f + (float)i * 0.8f, -2.2f, -75f), 16 + i,
                            () =>
                            {
                                DetectColor(MyColor.Color,MyColor.GetHue(),MyColor.GetDistance(), (byte)index,true);
                            });
                    }

                    SaveVariations = new ColorButton[5];
                    WriteSaveVariations = new SaveButton[5];
                    Vector3 pos;
                    for (int i = 0; i < SaveVariations.Length; i++)
                    {
                        int index = i;
                        pos = new Vector3(-1.5f, 1.2f - (float)i * 0.6f, -75f);
                        SaveVariations[i] =
                            new ColorButton(__instance, pos, 20 + i,
                            () =>
                            {
                                DetectColor(SaveColor[index].Color, SaveColor[index].GetHue(), SaveColor[index].GetDistance(), SaveColor[index].GetShadowType(), true);
                            },SaveColor[i]);
                        WriteSaveVariations[i]=new SaveButton(__instance, SaveVariations[i]);
                    }
                }

                PaletteObject.transform.SetParent(__instance.ColorChips[0].transform.parent);
                PaletteObject.transform.localPosition = new Vector3(1.8f, 0.35f, -40f);
                PaletteObject.layer = __instance.ColorChips[0].gameObject.layer;


                Renderer.sprite = GetPalleteSprite();
                Collider.radius = 2.1f;
                PassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                PassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                PassiveButton.OnClick.RemoveAllListeners();
                PassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    DetectColor(true);
                }));
                PassiveButton.enabled = true;

                foreach(ColorButton button in ShadowVariations)
                {
                    button.OnEnable(__instance);
                }

                foreach (ColorButton button in SaveVariations)
                {
                    button.OnEnable(__instance);
                }

                foreach (SaveButton button in WriteSaveVariations)
                {
                    button.OnEnable(__instance);
                }
            }
        }
    }
    
}
