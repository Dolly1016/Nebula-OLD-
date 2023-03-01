namespace Nebula.Roles.ImpostorRoles;

public class Painter : Role
{
    private CustomButton paintButton;

    private Module.CustomOption sampleCoolDownOption;
    private Module.CustomOption paintCoolDownOption;
    public Module.CustomOption changeLookImmediatelyOption;

    private Game.PlayerData.PlayerOutfitData? paintOutfit;
    private Game.PlayerData.PlayerOutfitData? myOutfit;

    private SpriteLoader sampleButtonSprite = new SpriteLoader("Nebula.Resources.SampleButton.png", 115f, "ui.button.ninja.sample");
    private SpriteLoader paintButtonSprite = new SpriteLoader("Nebula.Resources.MorphButton.png", 115f, "ui.button.ninja.paint");
    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(sampleButtonSprite,"role.painter.help.sample",0.3f),
            new HelpSprite(paintButtonSprite,"role.painter.help.paint",0.3f)
    };
    public override void LoadOptionData()
    {
        sampleCoolDownOption = CreateOption(Color.white, "sampleCoolDown", 10f, 2.5f, 30f, 2.5f);
        sampleCoolDownOption.suffix = "second";

        paintCoolDownOption = CreateOption(Color.white, "paintCoolDown", 10f, 2.5f, 30f, 2.5f);
        paintCoolDownOption.suffix = "second";

        changeLookImmediatelyOption = CreateOption(Color.white, "changeLookImmediately", true);
    }

    byte paintMode = 0;

    private void SetPaintMode(byte mode)
    {
        paintMode = (byte)(mode % 2);
        if (paintMode == 0)
        {
            paintButton.Sprite = sampleButtonSprite.GetSprite();
            paintButton.UpperText.text = "";
            paintButton.SetLabel("button.label.sample");
        }
        else
        {
            paintButton.Sprite = paintButtonSprite.GetSprite();
            paintButton.UpperText.text = paintOutfit.Name;
            paintButton.SetLabel("button.label.paint");
        }
    }

    private void ChangePaintMode()
    {
        SetPaintMode((byte)(paintMode + 1));
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        myOutfit = Game.GameData.data.myData.getGlobalData().GetOutfitData(50);
        paintOutfit = myOutfit;

        if (paintButton != null)
        {
            paintButton.Destroy();
        }
        paintButton = new CustomButton(
            () =>
            {
                if (paintMode == 0)
                {
                    paintButton.Timer = 3f;
                    paintOutfit = Game.GameData.data.myData.currentTarget.GetModData().GetOutfitData(50);
                    SetPaintMode(1);
                }
                else
                {
                    paintButton.Timer = paintCoolDownOption.getFloat();
                    RPCEventInvoker.Paint(Game.GameData.data.myData.currentTarget, paintOutfit.Clone(10));
                }
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.currentTarget != null; },
            () =>
            {
                paintButton.Timer = sampleCoolDownOption.getFloat();
            },
            sampleButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.sample"
        ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
        paintButton.MaxTimer = paintCoolDownOption.getFloat();
        paintButton.SetAidAction(Module.NebulaInputManager.changeAbilityInput.keyCode, true, ChangePaintMode);
    }

    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f);
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
    }

    public override void OnMeetingEnd()
    {
        paintOutfit = myOutfit;
        SetPaintMode(0);
    }

    public override void CleanUp()
    {
        if (paintButton != null)
        {
            paintButton.Destroy();
            paintButton = null;
        }
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Arsonist);
        RelatedRoles.Add(Roles.Morphing);
    }

    public Painter()
            : base("Painter", "painter", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        paintButton = null;
    }
}
