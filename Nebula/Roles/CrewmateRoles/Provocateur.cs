namespace Nebula.Roles.CrewmateRoles;

public class Provocateur : Role
{
    static public Color RoleColor = new Color(112f / 255f, 255f / 255f, 89f / 255f);


    private CustomButton embroilButton;

    private Module.CustomOption embroilCoolDownOption;
    private Module.CustomOption embroilCoolDownAdditionOption;
    private Module.CustomOption embroilDurationOption;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.EmbroilButton.png", 115f, "ui.button.provocateur.embroil");
    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.provocateur.help.embroil",0.3f)
    };

    public override void OnMurdered(byte murderId)
    {
        //相手も殺す
        if (PlayerControl.LocalPlayer.PlayerId == murderId) return;
        if (Helpers.playerById(murderId).Data.IsDead) return;
        if (!embroilButton.isEffectActive) return;
        RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId, murderId, Game.PlayerData.PlayerStatus.Embroiled.Id, false);
    }

    public override void OnExiledPre(byte[] voters)
    {
        if (voters.Length == 0) return;

        //ランダムに相手を選んで追放する
        List<byte> v = new List<byte>(voters);
        v.RemoveAll((voter) => voter == PlayerControl.LocalPlayer.PlayerId);
        RPCEventInvoker.UncheckedExilePlayer(v[NebulaPlugin.rnd.Next(v.Count)], Game.PlayerData.PlayerStatus.Embroiled.Id);
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        base.ButtonInitialize(__instance);

        if (embroilButton != null)
        {
            embroilButton.Destroy();
        }
        embroilButton = new CustomButton(
            () => { },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                return PlayerControl.LocalPlayer.CanMove;
            },
            () =>
            {
                embroilButton.Timer = embroilButton.MaxTimer;
                embroilButton.isEffectActive = false;
            },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            embroilDurationOption.getFloat(),
            () =>
            {
                embroilButton.MaxTimer += embroilCoolDownAdditionOption.getFloat();
                embroilButton.Timer = embroilButton.MaxTimer;
            },
            "button.label.embroil"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        embroilButton.MaxTimer = embroilCoolDownOption.getFloat();
    }

    public override void CleanUp()
    {
        if (embroilButton != null)
        {
            embroilButton.Destroy();
            embroilButton = null;
        }
    }

    public override void PreloadOptionData()
    {
        defaultUnassignable.Add(Roles.Lover);
    }

    public override void LoadOptionData()
    {
        embroilCoolDownOption = CreateOption(Color.white, "embroilCoolDown", 25f, 10f, 60f, 5f);
        embroilCoolDownOption.suffix = "second";

        embroilCoolDownAdditionOption = CreateOption(Color.white, "embroilCoolDownAddition", 10f, 0f, 60f, 2.5f);
        embroilCoolDownAdditionOption.suffix = "second";

        embroilDurationOption = CreateOption(Color.white, "embroilDuration", 5f, 2f, 20f, 1f);
        embroilDurationOption.suffix = "second";
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Spy);
        RelatedRoles.Add(Roles.Madmate);
    }

    public Provocateur()
        : base("Provocateur", "provocateur", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, false, false)
    {
    }
}