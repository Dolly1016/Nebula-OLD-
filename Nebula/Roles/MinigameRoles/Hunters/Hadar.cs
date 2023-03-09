using Nebula.Patches;

namespace Nebula.Roles.MinigameRoles.Hunters;

public class Hadar : Role
{
    private SpriteRenderer FS_PlayersSensor = null;

    private void UpdateFullScreen()
    {
        if (!PlayerControl.LocalPlayer) return;
        if (PlayerControl.LocalPlayer.GetModData() == null) return;

        if (FS_PlayersSensor == null)
        {
            FS_PlayersSensor = GameObject.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.transform);
            FS_PlayersSensor.color = Palette.ImpostorRed.AlphaMultiplied(0f);
            FS_PlayersSensor.enabled = true;
            FS_PlayersSensor.gameObject.SetActive(true);
        }

        if (!PlayerControl.LocalPlayer.GetModData().Property.UnderTheFloor)
            FS_PlayersSensor.color = Palette.ClearWhite;
        else
        {
            float sum = 0f;
            var center = PlayerControl.LocalPlayer.transform.position;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == PlayerControl.LocalPlayer) continue;
                if (player.Data.IsDead) continue;
                float dis = player.transform.position.Distance(center);
                if (dis < DamageRadiusOption.getFloat() + 3f)
                {
                    sum += 1f - (dis / (DamageRadiusOption.getFloat() + 3f));
                }
            }
            sum *= 0.25f;
            if (sum > 1f) sum = 1f;

            FS_PlayersSensor.color = Palette.ImpostorRed.AlphaMultiplied(sum * 0.75f);
        }
    }

    static private CustomButton ventButton;
    private float lightRadius = 1f;

    private Sprite ventAppearButtonSprite = null, ventHideButtonSprite = null, auraButtonSprite = null;
    public Sprite GetVentAppearButtonSprite()
    {
        if (ventAppearButtonSprite) return ventAppearButtonSprite;
        ventAppearButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.HadarAppearButton.png", 115f);
        return ventAppearButtonSprite;
    }

    public Sprite GetVentHideButtonSprite()
    {
        if (ventHideButtonSprite) return ventHideButtonSprite;
        ventHideButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.HadarHideButton.png", 115f);
        return ventHideButtonSprite;
    }

    public Sprite GetAuraButtonSprite()
    {
        if (auraButtonSprite) return auraButtonSprite;
        auraButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ArrestButton.png", 115f);
        return auraButtonSprite;
    }

    private Module.CustomOption DamageRadiusOption;
    private Module.CustomOption DamageLevelOption;
    private Module.CustomOption ReappearanceOption;

    public override void LoadOptionData()
    {
        base.LoadOptionData();

        DamageRadiusOption = CreateOption(Color.white, "damageRadius", 5f, 1f, 15f, 0.5f);
        DamageRadiusOption.suffix = "cross";

        DamageLevelOption = CreateOption(Color.white, "damageLevel", 2f, 0.5f, 5f, 0.5f);
        DamageLevelOption.suffix = "cross";

        ReappearanceOption = CreateOption(Color.white, "reappearance", 5f, 3f, 15f, 1f);
        ReappearanceOption.suffix = "second";
    }


    public override void Initialize(PlayerControl __instance)
    {
        lightRadius = 1f;
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (ventButton != null)
        {
            ventButton.Destroy();
        }
        ventButton = new CustomButton(
            () =>
            {
                var property = PlayerControl.LocalPlayer.GetModData().Property;

                    //ダメージを与える
                    if (property.UnderTheFloor)
                {
                    var center = PlayerControl.LocalPlayer.transform.position;
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        if (player == PlayerControl.LocalPlayer) continue;
                        if (player.Data.IsDead) continue;
                        float dis = player.transform.position.Distance(center);
                        if (dis < DamageRadiusOption.getFloat())
                        {
                            float damage = (DamageRadiusOption.getFloat() - dis) / DamageRadiusOption.getFloat();
                            damage *= DamageLevelOption.getFloat() * 0.6f;
                            RPCEventInvoker.DeathGuage(PlayerControl.LocalPlayer.PlayerId, player.PlayerId, damage);
                        }
                    }
                    ventButton.Timer = 3f;
                }
                else
                {
                    ventButton.Timer = ReappearanceOption.getFloat();
                }

                ventButton.SetLabel(property.UnderTheFloor ?
                    "button.label.hadar.hide" : "button.label.hadar.appear");
                ventButton.Sprite = property.UnderTheFloor ?
                    GetVentHideButtonSprite() : GetVentAppearButtonSprite();
                RPCEventInvoker.UndergroundAction(!property.UnderTheFloor);
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () => { ventButton.Timer = ventButton.MaxTimer; },
            GetVentHideButtonSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.hadar.hide"
        );
        ventButton.MaxTimer = ventButton.Timer = 0f;
    }

    public override void CleanUp()
    {
        if (ventButton != null)
        {
            ventButton.Destroy();
            ventButton = null;
        }

        if (FS_PlayersSensor)
        {
            GameObject.Destroy(FS_PlayersSensor);
            FS_PlayersSensor = null;
        }
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();
        UpdateFullScreen();
    }

    public override void GetLightRadius(ref float radius)
    {
        if (PlayerControl.LocalPlayer.GetModData().Property.UnderTheFloor)
            lightRadius = 0f;
        else
            lightRadius += (1f - lightRadius) * 0.3f;

        radius *= lightRadius;
    }


    public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
    {
        displayColor = Palette.ImpostorRed;
    }

    public Hadar()
            : base("Hadar", "hadar", Palette.ImpostorRed, RoleCategory.Crewmate, Side.GamePlayer, Side.GamePlayer,
                 Player.minigameSideSet, Player.minigameSideSet, ImpostorRoles.Impostor.impostorEndSet,
                 true, VentPermission.CanNotUse, false, false, true)
    {
        ValidGamemode = Module.CustomGameMode.StandardHnS;
        CanCallEmergencyMeeting = false;

        ventButton = null;
    }
}