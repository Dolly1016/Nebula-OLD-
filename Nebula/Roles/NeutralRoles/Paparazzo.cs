using Nebula.Module;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Nebula.Module.Information.PlayersIconInformation;
using static Nebula.Roles.Assignable;

namespace Nebula.Roles.NeutralRoles;

public class Paparazzo : Template.HasAlignedHologram, Template.HasWinTrigger
{
    private Texture2D TakePicture(GameObject finder,Vector2 size,int roughness)
    {
        size = size * 100f;
        Camera cam = finder.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size.y / 200f;
        cam.transform.localScale = Vector3.one;
        cam.clearFlags = CameraClearFlags.Nothing;
        cam.cullingMask = 0b1101100000000;
        cam.enabled = true;

        RenderTexture rt = new RenderTexture((int)size.x/roughness, (int)size.y/ roughness, 16);
        rt.Create();

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = cam.targetTexture;
        Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, false);
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture2D.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        GameObject.Destroy(rt);
        GameObject.Destroy(cam);

        return texture2D;
    }

    static public Color RoleColor = new Color(202f / 255f, 118f / 255f, 140f / 255f);

    static private CustomButton cameraButton;
    private GameObject finderObject;

    private Module.CustomOption finderSizeOption;
    private Module.CustomOption shootCoolDownOption;
    private Module.CustomOption canUseVentsOption;

    public bool WinTrigger { get; set; } = false;
    public byte Winner { get; set; } = Byte.MaxValue;

    private float infoUpdateCounter = 0.0f;

    public override HelpSprite[] helpSprite => new HelpSprite[] {
            new HelpSprite(cameraSprite,"role.paparazzo.help.camera",0.3f)
        };

    public override void LoadOptionData()
    {
        finderSizeOption = CreateOption(Color.white, "finderSize", 3f, 1f, 10f, 0.5f);
        finderSizeOption.suffix = "cross";

        shootCoolDownOption = CreateOption(Color.white, "shootCoolDown", 10f, 0f, 60f, 2.5f);
        shootCoolDownOption.suffix = "second";

        canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
    }


    SpriteLoader cameraSprite = new SpriteLoader("Nebula.Resources.CameraButton.png", 115f);

    public override void GlobalIntroInitialize(PlayerControl __instance)
    {
        canMoveInVents = canUseVentsOption.getBool();
        VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
    }

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);

        WinTrigger = false;
    }

    public override void CleanUp()
    {
        base.CleanUp();

        WinTrigger = false;

        if (cameraButton != null)
        {
            cameraButton.Destroy();
            cameraButton = null;
        }
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (cameraButton != null)
        {
            cameraButton.Destroy();
        }
        cameraButton = new CustomButton(
            () =>
            {
                if (finderObject)
                {
                    var texture = TakePicture(finderObject, new Vector2(3.1f, 1.9f),1);
                    //byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(texture);
                    byte[] bytes = UnityEngine.ImageConversion.EncodeToJPG(texture, 60);

                    //File.WriteAllBytes("paparazzo.png", bytes);
                    File.WriteAllBytes("paparazzo.jpg", bytes);
                }
                cameraButton.Timer = cameraButton.MaxTimer;
                //RPCEventInvoker.WinTrigger(this);
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () =>
            {
                cameraButton.Timer = cameraButton.MaxTimer;
            },
            cameraSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.film"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        cameraButton.MaxTimer = shootCoolDownOption.getFloat();
    }


    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        infoUpdateCounter += Time.deltaTime;
        if (infoUpdateCounter > 0.5f)
        {
            RPCEventInvoker.UpdatePlayersIconInfo(this, activePlayers, null);
            infoUpdateCounter = 0f;
        }

        if (!(cameraButton.Timer > 0f))
        {
            if (!finderObject)
            {
                finderObject = GameObject.Instantiate(AssetLoader.CameraFinderPrefab);
            }
            finderObject.transform.position = PlayerControl.LocalPlayer.transform.position + new Vector3(0,1);
        }
        else
        {
            if (finderObject)
            {
                GameObject.Destroy(finderObject);
                finderObject = null;
            }
        }
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Empiric);
        RelatedRoles.Add(Roles.Arsonist);
        RelatedRoles.Add(Roles.Oracle);
        RelatedRoles.Add(Roles.Morphing);
        RelatedRoles.Add(Roles.Painter);
        RelatedRoles.Add(Roles.Banshee);
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        base.GlobalInitialize(__instance);

        new Module.Information.PlayersIconInformation(Helpers.cs(RoleColor, __instance.name), __instance.PlayerId, this);
    }

    public override void OnDied(byte playerId)
    {
        Module.Information.UpperInformationManager.Remove((i) =>
        i is Module.Information.PlayersIconInformation &&
        ((Module.Information.PlayersIconInformation)i).relatedPlayerId == playerId &&
        ((Module.Information.PlayersIconInformation)i).relatedRole == this
        );
    }

    public override void GlobalFinalizeInGame(PlayerControl __instance)
    {
        Module.Information.UpperInformationManager.Remove((i) =>
        i is Module.Information.PlayersIconInformation &&
        ((Module.Information.PlayersIconInformation)i).relatedPlayerId == __instance.PlayerId &&
        ((Module.Information.PlayersIconInformation)i).relatedRole == this
        );
    }

    public Paparazzo()
        : base("Paparazzo", "paparazzo", RoleColor, RoleCategory.Neutral, Side.Arsonist, Side.Arsonist,
             new HashSet<Side>() { Side.Arsonist }, new HashSet<Side>() { Side.Arsonist },
             new HashSet<Patches.EndCondition>() { Patches.EndCondition.ArsonistWin },
             true, VentPermission.CanUseUnlimittedVent, true, false, false)
    {
        cameraButton = null;

        Patches.EndCondition.PaparazzoWin.TriggerRole = this;
    }
}
