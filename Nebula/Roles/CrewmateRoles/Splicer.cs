using Nebula.Map;
using Nebula.Module;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.CrewmateRoles;

public class Splicer : Role
{
    static public Color RoleColor = new Color(58f / 255f, 127f / 255f, 190f / 255f);

    CustomOption warpCoolDownOption;
    CustomOption warpMaxDistanceOption;

    public override void LoadOptionData()
    {
        warpCoolDownOption = CreateOption(Color.white, "warpCoolDown", 20f, 10f, 60f, 2.5f);
        warpCoolDownOption.suffix = "second";
        warpMaxDistanceOption = CreateOption(Color.white, "maxWarpDistance", 4f, 1f, 6f, 0.5f);
        warpMaxDistanceOption.suffix = "cross";
    }

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.WarpButton.png", 115f, "ui.button.splicer.warp");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.splicer.help.warp",0.3f)
    };


    static private CustomButton WarpButton;

    private void TryWarp()
    {
        float angle = PlayerControl.LocalPlayer.FlashlightAngle;
        Vector2 vector=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
        Vector2 truePos = PlayerControl.LocalPlayer.GetTruePosition();

        bool result = false;
        float maxDistance = warpMaxDistanceOption.getFloat();
        float minDistance = maxDistance;
        int num = Physics2D.RaycastNonAlloc(truePos, vector, PhysicsHelpers.castHits, minDistance, Constants.ShipAndAllObjectsMask);
        for (int i = 0; i < num; i++)
        {
            if (PhysicsHelpers.castHits[i].collider.isTrigger) continue;

            result = true;
            float temp = (PhysicsHelpers.castHits[i].point - truePos).magnitude;
            if (temp < minDistance) minDistance = temp;
        }

        if (!result) return;

        float d=minDistance;
        var data = MapData.GetCurrentMapData();
        Vector2 tempVec;
        while (true)
        {
            d += 0.1f;
            if (d > maxDistance) break;

            tempVec = truePos + (vector * d);
            if (data.isOnTheShip(tempVec))
            {
                RPCEventInvoker.ObjectInstantiate(CustomObject.Type.TeleportEvidence, PlayerControl.LocalPlayer.GetTruePosition());
                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(tempVec);
                break;
            }
        }
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        Game.GameData.data.myData.currentTarget = null;

        if (WarpButton != null)
        {
            WarpButton.Destroy();
        }
        WarpButton = new CustomButton(
            () =>
            {
                RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer,new Game.SpeedFactor(0,3f,0f,false));
                PlayerControl.LocalPlayer.lightSource.StartCoroutine(RoleSystem.WarpSystem.CoOrient(PlayerControl.LocalPlayer.lightSource, 0.6f, 2.4f,
                    (p) =>
                    {
                    }, () =>
                    {
                        TryWarp();
                    }).WrapToIl2Cpp());
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () => { WarpButton.Timer = WarpButton.MaxTimer; },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
        3.1f,
        () => {
            WarpButton.Timer = WarpButton.MaxTimer;
        }, "button.label.warp"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        WarpButton.MaxTimer = warpCoolDownOption.getFloat();
    }
    public override void CleanUp()
    {
        if (WarpButton != null)
        {
            WarpButton.Destroy();
            WarpButton = null;
        }
    }


    public Splicer()
            : base("Splicer", "splicer", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, false, false)
    {
    }
}

