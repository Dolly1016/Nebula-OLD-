using Nebula.Patches;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Roles.HnSImpostorRoles;

public class HnSHadar : Role 
{ 
    public override bool ShowInHelpWindow => false;

    static private CustomButton ventButton,killButton;
    private float lightRadius = 1f;
    private SpriteLoader AppearSprite = new SpriteLoader("Nebula.Resources.HadarAppearButton.png", 115f,"ui.button.hadar.appear");
    private SpriteLoader HideSprite = new SpriteLoader("Nebula.Resources.HadarHideButton.png", 115f, "ui.button.hadar.hide");
    
    /*
    private SpriteLoader ArrowSprite = new SpriteLoader("Nebula.Resources.HadarArrow.png", 115f, "role.hadar.arrow");
    
    private float searchCoolTime = 10f;
    public void ShowArrow(Vector2 position)
    {
        var dir = position - (Vector2)PlayerControl.LocalPlayer.transform.position;
        var angle = Mathf.Atan2(dir.y, dir.x);

        var obj = new GameObject("HadarArrow");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(HudManager.Instance.transform);
        obj.transform.localPosition = new Vector3(0, 0, -20f);
        obj.transform.localScale= Vector3.one;
        obj.transform.eulerAngles = new Vector3(0, 0, angle * 180 / Mathf.PI);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = ArrowSprite.GetSprite();

        IEnumerator CoShowArrow()
        {
            float t = 0f;
            while (true)
            {
                if (t > 0.25f)
                {
                    var a = 1f - (t - 0.25f) * 1.5f;
                    if (a < 0f) break;
                    renderer.color = new Color(1f, 1f, 1f, a * 0.5f);
                }
                else
                {
                    renderer.color = new Color(1f, 1f, 1f, t * 2f);
                }
                t+= Time.deltaTime;
                yield return null;
            }

            GameObject.Destroy(obj);
        }
        HudManager.Instance.StartCoroutine(CoShowArrow().WrapToIl2Cpp());
    }
    */

    public override void Initialize(PlayerControl __instance)
    {
        lightRadius = 1f;
}

    public override void ButtonInitialize(HudManager __instance)
    {
        if (killButton != null) killButton.Destroy();
        killButton = HnSImpostorSystem.GenerateKillButton(__instance);

        if (ventButton != null)
        {
            ventButton.Destroy();
        }
        ventButton = new CustomButton(
            () =>
            {
                var property = PlayerControl.LocalPlayer.GetModData().Property;

                if (property.UnderTheFloor)
                {
                    RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0, 1.25f, 0.15f, false));
                    RPCEventInvoker.EmitAttributeFactor(PlayerControl.LocalPlayer, new Game.PlayerAttributeFactor(Game.PlayerAttribute.CannotKill, 2f, 0, false));
                    Objects.SoundPlayer.PlaySound(Module.AudioAsset.HadarReappear);
                }
                else
                {
                    ventButton.Timer = 1f;

                    Objects.SoundPlayer.PlaySound(Module.AudioAsset.HadarDive);
                }
                
                ventButton.SetLabel(property.UnderTheFloor ?
                    "button.label.hadar.hide" : "button.label.hadar.appear");
                ventButton.Sprite = property.UnderTheFloor ?
                    HideSprite.GetSprite() : AppearSprite.GetSprite();
                RPCEventInvoker.UndergroundAction(!property.UnderTheFloor);
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () => { ventButton.Timer = ventButton.MaxTimer; },
            HideSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.hadar.hide"
        );
        ventButton.MaxTimer = ventButton.Timer = 0f;
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        HnSImpostorSystem.MyPlayerControlUpdate();
    }
    public override void CleanUp()
    {
        if (killButton != null) killButton.Destroy();
        if (ventButton != null) ventButton.Destroy();
        
        ventButton = null;
        killButton = null;
    }

    public override void GetLightRadius(ref float radius)
    {
        bool flashLightFlag = LightPatch.FlashlightEnabled ?? true;
        bool underFloorFlag = PlayerControl.LocalPlayer.GetModData().Property.UnderTheFloor;

        if (underFloorFlag == flashLightFlag)
        {
            LightPatch.FlashlightEnabled = flashLightFlag ? false : null;
            lightRadius = 0f;
        }
        else
        {
            if (underFloorFlag)
                lightRadius += (1f - lightRadius) * 0.01f;
            else
                lightRadius += (1f - lightRadius) * 0.3f;
        }

        radius *= lightRadius;
    }

    public HnSHadar()
            : base("HnSHadar", "hadarHnS", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorSideSet, ImpostorRoles.Impostor.impostorEndSet,
                 true, VentPermission.CanNotUse, false, true, true)
    {
        IsHideRole = true;
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        HideInExclusiveAssignmentOption = true;
        canInvokeSabotage = false;
        HideKillButtonEvenImpostor = true;
        canReport = false;
    }
}

