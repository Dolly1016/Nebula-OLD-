namespace Nebula.Roles.ImpostorRoles;

public class Morphing : Role
{
    public class MorphEvent : Events.LocalEvent
    {
        public byte PlayerId { get; private set; }
        private Game.PlayerData.PlayerOutfitData outfit;

        public override void OnTerminal()
        {
            Helpers.GetModData(PlayerId).RemoveOutfit(outfit);
        }

        public override void OnActivate()
        {
            Helpers.GetModData(PlayerId).AddOutfit(outfit);
        }

        public MorphEvent(byte playerId, Game.PlayerData.PlayerOutfitData outfit) : base(Roles.Morphing.morphDurationOption.getFloat())
        {
            PlayerId = playerId;
            this.outfit = outfit;
            SpreadOverMeeting = false;
        }

    }

    private CustomButton morphButton;

    private Module.CustomOption morphCoolDownOption;
    private Module.CustomOption morphDurationOption;

    private PlayerControl? morphTarget;
    private Game.PlayerData.PlayerOutfitData morphOutfit;
    private Objects.Arrow? arrow;

    private SpriteLoader sampleButtonSprite = new SpriteLoader("Nebula.Resources.SampleButton.png", 115f, "ui.button.morphing.sample");
    private SpriteLoader morphButtonSprite = new SpriteLoader("Nebula.Resources.MorphButton.png", 115f, "ui.button.morphing.morph");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(sampleButtonSprite,"role.morphing.help.sample",0.3f),
            new HelpSprite(morphButtonSprite,"role.morphing.help.morph",0.3f)
    };

    public override void LoadOptionData()
    {
        morphCoolDownOption = CreateOption(Color.white, "morphCoolDown", 25f, 10f, 60f, 5f);
        morphCoolDownOption.suffix = "second";

        morphDurationOption = CreateOption(Color.white, "morphDuration", 15f, 5f, 40f, 2.5f);
        morphDurationOption.suffix = "second";
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        morphTarget = null;

        if (morphButton != null)
        {
            morphButton.Destroy();
        }
        morphButton = new CustomButton(
            () =>
            {
                if (morphTarget == null)
                {
                    morphButton.Timer = 3f;
                    morphButton.isEffectActive = false;
                    morphTarget = Game.GameData.data.myData.currentTarget;
                    Game.GameData.data.myData.currentTarget = null;
                    morphButton.Sprite = morphButtonSprite.GetSprite();
                    morphButton.SetLabel("button.label.morph");
                    morphOutfit = morphTarget.GetModData().GetOutfitData(50).Clone(80);
                }
                else
                {
                    RPCEventInvoker.Morph(morphOutfit.Clone(morphOutfit.Priority));
                }
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove && (morphTarget != null || Game.GameData.data.myData.currentTarget != null); },
            () =>
            {
                morphButton.Timer = morphButton.MaxTimer;
                morphButton.isEffectActive = false;
                morphButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                RPCEventInvoker.MorphCancel();
            },
            sampleButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            morphDurationOption.getFloat(),
            () => { morphButton.Timer = morphButton.MaxTimer; },
            "button.label.sample"
        ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
        morphButton.MaxTimer = morphCoolDownOption.getFloat();
        morphButton.EffectDuration = morphDurationOption.getFloat();
        morphButton.SetSuspendAction(() =>
        {
            morphButton.Timer = morphButton.MaxTimer;
            morphButton.isEffectActive = false;
            morphButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
            RPCEventInvoker.MorphCancel();
        });
    }

    SpriteLoader arrowSprite = new SpriteLoader("role.morphing.arrow");
    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f);
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

        RoleSystem.TrackSystem.PlayerTrack_MyControlUpdate(ref arrow, morphTarget, Color.red, arrowSprite);
    }

    public override void OnMeetingEnd()
    {
        morphTarget = null;
        morphButton.Sprite = sampleButtonSprite.GetSprite();
        morphButton.SetLabel("button.label.sample");
    }

    public override void CleanUp()
    {
        if (morphButton != null)
        {
            morphButton.Destroy();
            morphButton = null;
        }
        if (arrow != null)
        {
            GameObject.Destroy(arrow.arrow);
            arrow = null;
        }
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Arsonist);
    }

    public Morphing()
            : base("Morphing", "morphing", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        morphButton = null;
    }
}
