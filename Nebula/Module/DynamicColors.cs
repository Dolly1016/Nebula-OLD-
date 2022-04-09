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
            return (color.r + color.g + color.b > 1.7);
        }

        public class CustomColor
        {
            private ConfigEntry<float> EntryR;
            private ConfigEntry<float> EntryG;
            private ConfigEntry<float> EntryB;
            //ShadowType
            private ConfigEntry<byte> EntryS;
            //Luminosity 
            private ConfigEntry<float> EntryL;
            //ColorName
            private ConfigEntry<byte> EntryH;
            private ConfigEntry<byte> EntryD;

            public Color OriginalColor { get; private set; }
            public Color Color { get; private set; }
            public Color ShadowColor { get; private set; }

            public CustomColor(string saveCategory)
            {
                EntryR = NebulaPlugin.Instance.Config.Bind(saveCategory, "R", (float)1f);
                EntryG = NebulaPlugin.Instance.Config.Bind(saveCategory, "G", (float)0f);
                EntryB = NebulaPlugin.Instance.Config.Bind(saveCategory, "B", (float)0f);

                EntryL = NebulaPlugin.Instance.Config.Bind(saveCategory, "L", (float)1f);

                //陰タイプ
                EntryS = NebulaPlugin.Instance.Config.Bind(saveCategory, "S", (byte)0);

                //色名判別
                EntryH = NebulaPlugin.Instance.Config.Bind(saveCategory, "H", (byte)0);
                EntryD = NebulaPlugin.Instance.Config.Bind(saveCategory, "D", (byte)8);

                OriginalColor = new Color(EntryR.Value,EntryG.Value,EntryB.Value,1f);
                Color = GetColor(OriginalColor, EntryL.Value);
                ShadowColor = GetShadowColor(Color, EntryS.Value);
            }

            public void SetColor(Color color,byte h,byte d,byte shadowType,float luminosity)
            {
                EntryR.Value = color.r;
                EntryG.Value = color.g;
                EntryB.Value = color.b;

                EntryS.Value = shadowType;

                EntryL.Value = luminosity;

                if (h < 80)
                    EntryH.Value = (byte)(h % 64);
                else
                    EntryH.Value = (byte)h;
                if (d < 24)
                    EntryD.Value = d;
                else
                    EntryD.Value = 23;

                OriginalColor = color;
                Color = GetColor(OriginalColor,EntryL.Value);
                ShadowColor = GetShadowColor(Color, EntryS.Value);
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

            public float GetLuminosity()
            {
                return EntryL.Value;
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
                    PlayerControl.SetPlayerMaterialColors(RefBodyId, PlayerCustomizationMenu.Instance.PreviewArea.CurrentBodySprite.BodySprite);
                    if (Color != null)
                    {
                        PlayerCustomizationMenu.Instance.SetItemName(GetColorName(Color.GetHue(),Color.GetDistance()));
                    }
                }));

                PassiveButton.OnClick.RemoveAllListeners();
                PassiveButton.OnClick.AddListener(onClick);
                PassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    PlayerControl.SetPlayerMaterialColors(SaveManager.BodyColor, PlayerCustomizationMenu.Instance.PreviewArea.CurrentBodySprite.BodySprite);
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

        static private ColorButton[] VanillaVariations = null;
        static private ColorButton[] ShadowVariations=null;
        static private ColorButton[] SaveVariations = null;
        static private SaveButton[] WriteSaveVariations = null;

        static private Sprite ModeVanillaSprite,ModeDynamicSprite;
        static private Sprite GetModeChangeSprite()
        {
            if (ShowVanillaColorFlag)
            {
                if (ModeDynamicSprite) return ModeDynamicSprite;
                ModeDynamicSprite = Helpers.loadSpriteFromResources(
                    Helpers.loadTextureFromResources("Nebula.Resources.PaletteChangeButton.png"), 100f,
                    new Rect(64f,0f,64f,-64f));
                return ModeDynamicSprite;
            }
            else
            {
                if (ModeVanillaSprite) return ModeVanillaSprite;
                ModeVanillaSprite = Helpers.loadSpriteFromResources(
                    Helpers.loadTextureFromResources("Nebula.Resources.PaletteChangeButton.png"), 100f,
                    new Rect(0f, 0f, 64f, -64f));
                return ModeVanillaSprite;
            }
        }

        static private Sprite PaletteSprite;
        static private Sprite GetPalleteSprite()
        {
            if (PaletteSprite) return PaletteSprite;
            PaletteSprite = Helpers.loadSpriteFromResources("Nebula.Resources.Palette.png", 100f);
            return PaletteSprite;
        }

        static private Sprite LPaletteSprite;
        static private Sprite GetLPalleteSprite()
        {
            if (LPaletteSprite) return LPaletteSprite;
            LPaletteSprite = Helpers.loadSpriteFromResources("Nebula.Resources.PaletteBrightness.png", 100f);
            return LPaletteSprite;
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

        static private Sprite ColorTargetSprite;
        static private Sprite GetColorTargetSprite()
        {
            if (ColorTargetSprite) return ColorTargetSprite;
            ColorTargetSprite = Helpers.loadSpriteFromResources("Nebula.Resources.TargetIcon.png", 140f);
            return ColorTargetSprite;
        }

        static private Sprite LTargetSprite;
        static private Sprite GetLTargetSprite()
        {
            if (LTargetSprite) return LTargetSprite;
            LTargetSprite = Helpers.loadSpriteFromResources("Nebula.Resources.PaletteKnob.png", 100f);
            return LTargetSprite;
        }

        static private GameObject PaletteObject = null;
        static private SpriteRenderer PaletteRenderer = null;
        static private PassiveButton PalettePassiveButton = null;
        static private CircleCollider2D PaletteCollider = null;

        static private GameObject LPaletteObject = null;
        static private SpriteRenderer LPaletteRenderer = null;
        static private PassiveButton LPalettePassiveButton = null;
        static private BoxCollider2D LPaletteCollider = null;

        static private GameObject ChangeModeObject = null;
        static private SpriteRenderer ChangeModeRenderer = null;
        static private PassiveButton ChangeModePassiveButton = null;
        static private BoxCollider2D ChangeModeCollider = null;

        static private GameObject ColorTargetObject = null;
        static private SpriteRenderer ColorTargetRenderer = null;

        static private GameObject LTargetObject = null;
        static private SpriteRenderer LTargetRenderer = null;

        static private string ColorName;

        static public CustomColor MyColor;
        static private CustomColor[] SaveColor;
        static private CustomColor[] VanillaColor;

        static private bool ShowVanillaColorFlag=false;

        static public Color GetColor(Color originalColor,float l)
        {
            originalColor = new Color(
                originalColor.r > 1f ? 1f : originalColor.r,
                originalColor.g > 1f ? 1f : originalColor.g,
                originalColor.b > 1f ? 1f : originalColor.b);
            return originalColor.RGBMultiplied(l * 0.5f + 0.5f);
        }

        static public Color GetShadowColor(Color myColor,byte shadowType)
        {
            switch (shadowType)
            {
                case 0:
                    return myColor.RGBMultiplied(0.6f);
                case 1:
                    return myColor.RGBMultiplied(0.8f) * myColor * myColor * myColor;
                case 8:
                    for(int i = 0; i < 18; i++)
                    {
                        if(((Color32)myColor).rgba == Palette.PlayerColors[32 + i].rgba)
                        {
                            return Palette.ShadowColors[32 + i];
                        }
                    }
                    return myColor.RGBMultiplied(0.6f);
            }
            return Color.black;
        }

        static public void Load()
        {
            var PlayerColors = Enumerable.ToList<Color32>(Palette.PlayerColors);
            var ShadowColors = Enumerable.ToList<Color32>(Palette.ShadowColors);
            
            while (PlayerColors.Count < 50)
            {
                PlayerColors.Add(new Color32());
            }
            
            while (ShadowColors.Count < 50)
            {
                ShadowColors.Add(new Color32());
            }

            //Camo Color
            PlayerColors[31] = PlayerColors[6];

            for (int i = 0; i < 18; i++)
            {
                PlayerColors[32 + i] = PlayerColors[i];
                ShadowColors[32 + i] = ShadowColors[i];
            }
            
            Palette.PlayerColors = PlayerColors.ToArray();
            Palette.ShadowColors = ShadowColors.ToArray();
            
            MyColor = new CustomColor("Color");
            SaveColor = new CustomColor[5];
            VanillaColor = new CustomColor[18];

            for (int i = 0; i < VanillaColor.Length; i++)
            {
                VanillaColor[i] = new CustomColor("VanillaColor"+i);
                VanillaColor[i].SetColor(Palette.PlayerColors[i], 100, (byte)i, 8,1f);
            }

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

        static public void SaveAndSetColor(Color color,float l,byte h, byte d,byte shadowType,byte playerId)
        {
            if (h < 80) { h = (byte)(h % 64); }
            MyColor.SetColor(color, h, d >= 24 ? (byte)23 : d, shadowType,l);

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
            SaveColor[ColorId - 20].SetColor(MyColor.OriginalColor,MyColor.GetHue(),MyColor.GetDistance(),MyColor.GetShadowType(),MyColor.GetLuminosity());
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
            return Language.Language.GetString("color." + h + "." + d);
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

        private static void DetectColor(Color color,float l,byte h,byte d,byte shadowType,bool setFlag)
        {
            if (setFlag)
            {
                if (PlayerControl.LocalPlayer != null)
                {
                    SaveAndSetColor(color, l, h, d, shadowType, PlayerControl.LocalPlayer.PlayerId);
                    PlayerControl.LocalPlayer.SetColor(PlayerControl.LocalPlayer.PlayerId);
                    RPCEventInvoker.SetMyColor();
                }
                else
                {
                    SaveAndSetColor(color, l, h, d, shadowType, SaveManager.BodyColor);
                }
            }
            else
            {
                var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (PaletteCollider.OverlapPoint(pos))
                {
                    Palette.PlayerColors[15] = GetColor(color, MyColor.GetLuminosity());
                    Palette.ShadowColors[15] = GetShadowColor(Palette.PlayerColors[15], MyColor.GetShadowType());
                    PlayerCustomizationMenu.Instance.SetItemName(GetColorName(h, d));
                }
                else if (LPaletteCollider.OverlapPoint(pos))
                {
                    Palette.PlayerColors[15] = GetColor(MyColor.OriginalColor,l);
                    Palette.ShadowColors[15] = GetShadowColor(Palette.PlayerColors[15], MyColor.GetShadowType());
                }
                else { return; }

                PlayerControl.SetPlayerMaterialColors(15, PlayerCustomizationMenu.Instance.PreviewArea.CurrentBodySprite.BodySprite);                
            }
        }

        private static void DetectColor(bool setFlag)
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - PaletteObject.transform.position;
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

            pos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - LPaletteObject.transform.position;
            float l = pos.y / LPaletteCollider.size.y + 0.5f;

            pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (PaletteCollider.OverlapPoint(pos))
                DetectColor(color, MyColor.GetLuminosity(), (byte)((h * 64) % 64), (byte)((dis / 2.1f) * 24), MyColor.GetShadowType(), setFlag);
            else if (LPaletteCollider.OverlapPoint(pos))
                DetectColor(MyColor.OriginalColor, l, MyColor.GetHue(), MyColor.GetDistance(), MyColor.GetShadowType(), setFlag);
            else
                DetectColor(MyColor.OriginalColor, MyColor.GetLuminosity(), MyColor.GetHue(), MyColor.GetDistance(), MyColor.GetShadowType(), setFlag);
        }

        [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.Update))]
        private static class PlayerTabUpdatePatch
        {
            public static void Postfix(PlayerTab __instance)
            {
                if (!__instance.enabled) return;

                if (!ShowVanillaColorFlag)
                {
                    DetectColor(false);
                }

                if (ShadowVariations != null)
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

                if (PaletteRenderer.color.r != MyColor.GetLuminosity() *0.5f + 0.5f)
                {
                    float l = MyColor.GetLuminosity() * 0.5f + 0.5f;
                    PaletteRenderer.color = new Color(l,l,l);
                }
                if (LPaletteRenderer.color!=MyColor.OriginalColor)
                {
                    LPaletteRenderer.color = MyColor.OriginalColor;
                }

                {
                    float angle = MyColor.GetHue() / 64f * 2f*(float)System.Math.PI;
                    float r = MyColor.GetDistance() / 10f *0.88f;
                    ColorTargetObject.transform.localPosition=new Vector3(-MathF.Sin(angle) * r, MathF.Cos(angle) * r,-200f);

                    float l = (MyColor.GetLuminosity() - 0.5f) * 2f;
                    LTargetObject.transform.localPosition = new Vector3(0f, l * 1.35f, -200f);
                }

                //Vanillaカラーは変化しないのでUpdateは不要

                if (PaletteObject.active != !ShowVanillaColorFlag)
                {
                    PaletteObject.SetActive(!ShowVanillaColorFlag);
                    LPaletteObject.SetActive(!ShowVanillaColorFlag);
                }
                else
                {
                    foreach (ColorButton button in VanillaVariations)
                    {
                        if (button.ButtonObject.active != ShowVanillaColorFlag)
                            button.ButtonObject.SetActive(ShowVanillaColorFlag);
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
                    PaletteRenderer = PaletteObject.AddComponent<SpriteRenderer>();
                    PaletteCollider = PaletteObject.AddComponent<CircleCollider2D>();
                    PalettePassiveButton = PaletteObject.AddComponent<PassiveButton>();

                    LPaletteObject = new GameObject("LuminosityPalette");
                    LPaletteRenderer = LPaletteObject.AddComponent<SpriteRenderer>();
                    LPaletteCollider = LPaletteObject.AddComponent<BoxCollider2D>();
                    LPalettePassiveButton = LPaletteObject.AddComponent<PassiveButton>();

                    ChangeModeObject = new GameObject("ChangeButton");
                    ChangeModeRenderer = ChangeModeObject.AddComponent<SpriteRenderer>();
                    ChangeModeCollider = ChangeModeObject.AddComponent<BoxCollider2D>();
                    ChangeModePassiveButton = ChangeModeObject.AddComponent<PassiveButton>();

                    ColorTargetObject = new GameObject("ColorTarget");
                    ColorTargetRenderer = ColorTargetObject.AddComponent<SpriteRenderer>();

                    LTargetObject = new GameObject("LTarget");
                    LTargetRenderer = LTargetObject.AddComponent<SpriteRenderer>();

                    ShadowVariations = new ColorButton[2];
                    for (int i = 0; i < ShadowVariations.Length; i++)
                    {
                        int index = i;
                        ShadowVariations[i] =
                            new ColorButton(__instance, new Vector3(1.4f + (float)i * 0.8f, -2.2f, -75f), 16 + i,
                            () =>
                            {
                                DetectColor(MyColor.OriginalColor,MyColor.GetLuminosity(),MyColor.GetHue(),MyColor.GetDistance(), (byte)index,true);
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
                                DetectColor(SaveColor[index].OriginalColor, SaveColor[index].GetLuminosity(),SaveColor[index].GetHue(), SaveColor[index].GetDistance(), SaveColor[index].GetShadowType(), true);
                            },SaveColor[i]);
                        WriteSaveVariations[i]=new SaveButton(__instance, SaveVariations[i]);
                    }

                    VanillaVariations = new ColorButton[18];
                    for (int i = 0; i < VanillaVariations.Length; i++)
                    {
                        int index = i;
                        VanillaVariations[i] =
                            new ColorButton(__instance, new Vector3(1.0f + (float)(i%3) * 0.8f, 1.3f-0.5f*(float)(i/3), -75f), 32+i,
                            () =>
                            {
                                DetectColor(VanillaColor[index].OriginalColor, 1f,100, (byte)index, VanillaColor[index].GetShadowType(), true);
                            },VanillaColor[i]);
                        VanillaVariations[i].Update();
                    }
                }

                PaletteObject.transform.SetParent(__instance.ColorChips[0].transform.parent);
                PaletteObject.transform.localPosition = new Vector3(1.8f, 0.35f, -40f);
                PaletteObject.layer = __instance.ColorChips[0].gameObject.layer;

                PaletteRenderer.sprite = GetPalleteSprite();
                PaletteCollider.radius = 2.1f;
                PalettePassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                PalettePassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                PalettePassiveButton.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(()=>{
                    PlayerControl.SetPlayerMaterialColors(SaveManager.BodyColor, PlayerCustomizationMenu.Instance.PreviewArea.CurrentBodySprite.BodySprite);
                    PlayerCustomizationMenu.Instance.SetItemName(GetColorName());
                }));
                PalettePassiveButton.OnClick.RemoveAllListeners();
                PalettePassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    DetectColor(true);
                }));
                PalettePassiveButton.enabled = true;

                /* ------------------------------------------------------ */

                LPaletteObject.transform.SetParent(__instance.ColorChips[0].transform.parent);
                LPaletteObject.transform.localPosition = new Vector3(4.5f, 0.35f, -40f);
                LPaletteObject.layer = __instance.ColorChips[0].gameObject.layer;

                LPaletteRenderer.sprite = GetLPalleteSprite();
                LPaletteCollider.size = new Vector2(0.3f, 2.7f);
                LPalettePassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                LPalettePassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                LPalettePassiveButton.OnClick.RemoveAllListeners();
                LPalettePassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    DetectColor(true);
                }));
                LPalettePassiveButton.enabled = true;

                /* ------------------------------------------------------ */


                ChangeModeObject.transform.SetParent(__instance.ColorChips[0].transform.parent);
                ChangeModeObject.transform.localPosition = new Vector3(3.8f, -1.4f, -40f);
                ChangeModeObject.layer = __instance.ColorChips[0].gameObject.layer;

                ChangeModeRenderer.sprite = GetModeChangeSprite();
                ChangeModeCollider.size = new Vector2(0.55f, 0.55f);
                ChangeModePassiveButton.ClickSound = __instance.ColorChips[0].Button.ClickSound;
                ChangeModePassiveButton.OnMouseOut = __instance.ColorChips[0].Button.OnMouseOut;
                ChangeModePassiveButton.OnClick.RemoveAllListeners();
                ChangeModePassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    ShowVanillaColorFlag = !ShowVanillaColorFlag;
                    ChangeModeRenderer.sprite = GetModeChangeSprite();
                }));
                ChangeModePassiveButton.enabled = true;

                /* ------------------------------------------------------ */

                ColorTargetObject.transform.SetParent(PaletteObject.transform);
                ColorTargetObject.layer = __instance.ColorChips[0].gameObject.layer;
                ColorTargetRenderer.sprite = GetColorTargetSprite();

                LTargetObject.transform.SetParent(LPaletteObject.transform);
                LTargetObject.layer = __instance.ColorChips[0].gameObject.layer;
                LTargetRenderer.sprite = GetLTargetSprite();

                foreach (ColorButton button in ShadowVariations)
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

                foreach (ColorButton button in VanillaVariations)
                {
                    button.OnEnable(__instance);
                }
            }
        }
    }
    
}
