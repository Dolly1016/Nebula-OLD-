using Il2CppSystem.Net;
using Nebula.Expansion;
using Rewired.HID;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Xml.Schema.FacetsChecker.FacetsCompiler;

namespace Nebula.Objects;

public class PerkDisplay : MonoBehaviour
{
    static PerkDisplay()
    {
        PerkBackSprite = new SpriteLoader[9];
        PerkFrontSprite = new SpriteLoader[41];

        for (int i = 0; i < PerkBackSprite.Length; i++) PerkBackSprite[i] = new SpriteLoader("Nebula.Resources.Perks.Back"+i+".png",100f);
        for (int i = 0; i < PerkFrontSprite.Length; i++) PerkFrontSprite[i] = new SpriteLoader("Nebula.Resources.Perks.Front" + i + ".png", 100f);

        ClassInjector.RegisterTypeInIl2Cpp<PerkDisplay>();
    }

    private static SpriteLoader BackSprite = new("Nebula.Resources.Perks.FrameBack.png",100f);
    private static SpriteLoader FrameSprite = new("Nebula.Resources.Perks.Frame.png", 100f);
    private static SpriteLoader FrameRoleSprite = new("Nebula.Resources.Perks.RoleFrame.png", 100f);
    private static SpriteLoader FrameRoleGemSprite = new("Nebula.Resources.Perks.RoleGem.png", 100f);
    private static SpriteLoader HighlightSprite = new("Nebula.Resources.Perks.Highlight.png", 100f);
    private static SpriteLoader[] PerkBackSprite;
    private static SpriteLoader[] PerkFrontSprite;

    public SpriteRenderer Frame,FrameSub,PerkBack,PerkFront,Background;
    private SpriteRenderer? highlight = null;
    private bool hasButtonMaterial = false;

    public void SetCool(float percentage)
    {
        percentage = Mathf.Clamp01(percentage);

        if (!hasButtonMaterial)
        {
            PerkBack.material = new Material(HudManager.Instance.KillButton.graphic.material);
            PerkBack.material.SetFloat("_Desat", 0f);
            PerkFront.material = new Material(HudManager.Instance.KillButton.graphic.material);
            PerkFront.material.SetFloat("_Desat", 0f);
            hasButtonMaterial = true;
        }

        float backHeight = PerkBack.sprite.rect.height;
        float frontHeight = PerkFront.sprite.rect.height;

        float margin = (1f - frontHeight / backHeight) / 2f;
        float fPercentage = Mathf.Clamp((percentage - margin) * backHeight / frontHeight, 0f, 1f);

        PerkBack.material.SetFloat("_Percent", percentage);
        PerkFront.material.SetFloat("_Percent", fPercentage);

        CooldownHelpers.SetCooldownNormalizedUvs(PerkBack);
        CooldownHelpers.SetCooldownNormalizedUvs(PerkFront);
    }

    public SpriteRenderer Highlight { get
        {
            if(highlight == null)
            {
                var obj = new GameObject("Highlight");
                obj.layer = LayerExpansion.GetUILayer();
                obj.transform.SetParent(gameObject.transform);
                obj.transform.localPosition = new Vector3(0, 0, -2f);
                obj.transform.localScale = Vector3.one;
                obj.SetActive(false);
                highlight = obj.AddComponent<SpriteRenderer>();
                highlight.sprite = HighlightSprite.GetSprite();
            }
            return highlight;
        } }

    public void Awake()
    {
        var frameObj = new GameObject("Frame");
        frameObj.layer = LayerExpansion.GetUILayer();
        frameObj.transform.SetParent(gameObject.transform);
        frameObj.transform.localPosition= new Vector3(0,0,-1f);
        frameObj.transform.localScale=Vector3.one;
        Frame = frameObj.AddComponent<SpriteRenderer>();
        Frame.sprite = FrameSprite.GetSprite();
        Frame.color = Color.white.RGBMultiplied(0.3f);

        var frameSub = new GameObject("FrameSub");
        frameSub.layer = LayerExpansion.GetUILayer();
        frameSub.transform.SetParent(gameObject.transform);
        frameSub.transform.localPosition = new Vector3(0, 0, -0.95f);
        frameSub.transform.localScale = Vector3.one;
        FrameSub = frameSub.AddComponent<SpriteRenderer>();
        FrameSub.sprite = null;
        FrameSub.color = Color.white.RGBMultiplied(0.3f);

        var backObj = new GameObject("Back");
        backObj.layer = LayerExpansion.GetUILayer();
        backObj.transform.SetParent(gameObject.transform);
        backObj.transform.localPosition = new Vector3(0, 0, 0f);
        backObj.transform.localScale = Vector3.one;
        Background = backObj.AddComponent<SpriteRenderer>();
        Background.sprite = BackSprite.GetSprite();
        Background.color = Color.white.RGBMultiplied(0.3f).AlphaMultiplied(0.4f);

        var perkBObj = new GameObject("PerkBack");
        perkBObj.layer = LayerExpansion.GetUILayer();
        perkBObj.transform.SetParent(gameObject.transform);
        perkBObj.transform.localPosition = new Vector3(0, 0, -0.5f);
        perkBObj.transform.localScale = Vector3.one;
        PerkBack = perkBObj.AddComponent<SpriteRenderer>();

        var perkFObj = new GameObject("PerkFront");
        perkFObj.layer = LayerExpansion.GetUILayer();
        perkFObj.transform.SetParent(gameObject.transform);
        perkFObj.transform.localPosition = new Vector3(0, 0, -0.75f);
        perkFObj.transform.localScale = Vector3.one;
        PerkFront = perkFObj.AddComponent<SpriteRenderer>();
    }

    public void SetType(bool isNormal = true,Color? gemColor=null)
    {
        if (isNormal)
        {
            Frame.sprite = FrameSprite.GetSprite();
            Frame.color = Color.white.RGBMultiplied(0.3f);

            FrameSub.sprite = null;
            FrameSub.color = Color.white.RGBMultiplied(0.3f);
        }
        else
        {
            Frame.sprite = FrameRoleSprite.GetSprite();
            Frame.color = Color.white.RGBMultiplied(0.3f);

            FrameSub.sprite = FrameRoleGemSprite.GetSprite();
            FrameSub.color = gemColor ?? Color.red;
        }
    }

    public void SetPerk(Roles.Perk.DisplayPerk? perk)
    {
        if (perk == null) Inactivate();
        else SetPerk(perk.VisualFrontSpriteId, perk.VisualBackSpriteId, perk.VisualBackSpriteColor);
    }

    public void SetPerk(int frontId,int backId,Color backColor)
    {
        if (frontId < 0 || backId < 0) Inactivate();

        PerkFront.sprite = PerkFrontSprite[frontId].GetSprite();
        PerkBack.sprite = PerkBackSprite[backId].GetSprite();
        PerkBack.color = backColor;
    }

    public void Inactivate()
    {
        PerkFront.sprite = null;
        PerkBack.sprite = null;
    }

    public PassiveButton SetUpButton(Color? highlightColor)
    {
        if (highlightColor.HasValue) Highlight.color = highlightColor.Value;
        
        var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.7f;
        var button = gameObject.SetUpButton(null);
        button.OnMouseOver.AddListener((Action)(() => {
            Highlight.gameObject.SetActive(true);
        }));
        button.OnMouseOut.AddListener((Action)(() => {
            Highlight.gameObject.SetActive(false);
        }));

        return button;
    }
}
