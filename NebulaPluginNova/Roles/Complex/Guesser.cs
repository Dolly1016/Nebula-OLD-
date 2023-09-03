using Nebula.Configuration;
using Nebula.Game;
using Nebula.Roles.Neutral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Complex;

static public class GuesserSystem
{
    static TextAttribute ButtonAttribute = new TextAttribute(TextAttribute.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
    public static MetaScreen LastGuesserWindow;

    static public MetaScreen OpenGuessWindow(int leftGuess,Action<AbstractRole> onSelected)
    {
        var window = MetaScreen.GenerateWindow(new(7.4f, 4.2f), HudManager.Instance.transform, new Vector3(0, 0, -50f), true, false);

        MetaContext context = new();

        MetaContext inner = new();
        inner.Append(Roles.AllRoles.Where(r => r.CanBeGuess), r => new MetaContext.Button(() => onSelected.Invoke(r), ButtonAttribute) { RawText = r.DisplayName.Color(r.RoleColor), PostBuilder = (_, renderer, _) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask }, 4, -1, 0, 0.59f);
        MetaContext.ScrollView scroller = new(new(6.6f, 3.8f), inner, true) { Alignment = IMetaContext.AlignmentOption.Center };
        context.Append(scroller);
        context.Append(new MetaContext.Text(TextAttribute.BoldAttr) { MyText = new CombinedComponent(new TranslateTextComponent("role.guesser.leftGuess"), new RawTextComponent(" : " + leftGuess.ToString())), Alignment = IMetaContext.AlignmentOption.Center });

        window.SetContext(context);

        return window;
    }

    static private SpriteLoader targetSprite = SpriteLoader.FromResource("Nebula.Resources.TargetIcon.png", 115f);
    static public void OnMeetingStart(int leftGuess,Action guessDecrementer)
    {
        List<GameObject> guessIcons = new();

        foreach (var playerVoteArea in MeetingHud.Instance.playerStates)
        {
            if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

            GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
            guessIcons.Add(targetBox);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = targetSprite.GetSprite();
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();


            var player = NebulaGameManager.Instance?.GetModPlayerInfo(playerVoteArea.TargetPlayerId);
            button.OnClick.AddListener(() =>
            {
                if (PlayerControl.LocalPlayer.Data.IsDead) return;
                if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;

                LastGuesserWindow = OpenGuessWindow(leftGuess, (r) =>
                {
                    if (PlayerControl.LocalPlayer.Data.IsDead) return;

                    if (player?.Role.Role == r)
                        PlayerControl.LocalPlayer.ModMeetingKill(player!.MyControl, true, PlayerState.Guessed, EventDetail.Guess);
                    else
                        PlayerControl.LocalPlayer.ModMeetingKill(PlayerControl.LocalPlayer, true, PlayerState.Misguessed, EventDetail.Missed);

                    //のこり推察数を減らす
                    guessDecrementer.Invoke();
                    leftGuess--;

                    if (leftGuess <= 0) foreach (var obj in guessIcons) GameObject.Destroy(obj);
                    

                    if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
                    LastGuesserWindow = null;
                });
            });
        }
    }

    static public void OnDead()
    {
        if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
        LastGuesserWindow = null;
    }
}

public class Guesser : ConfigurableStandardRole
{
    static public Guesser MyNiceRole = new(false);
    static public Guesser MyEvilRole = new(true);

    public bool IsEvil { get; private set; }
    public override RoleCategory RoleCategory => IsEvil ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole;

    public override string LocalizedName => IsEvil ? "evilGuesser" : "niceGuesser";
    public override Color RoleColor => IsEvil ? Palette.ImpostorRed : new Color(1f, 1f, 0f);
    public override Team Team => IsEvil ? Impostor.Impostor.MyTeam : Crewmate.Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => IsEvil ? new EvilInstance(player) : new NiceInstance(player);

    static public NebulaConfiguration NumOfGuessOption;

    private NebulaConfiguration? CommonEditorOption;

    public Guesser(bool isEvil)
    {
        IsEvil = isEvil;
    }

    protected override void LoadOptions()
    {
        base.LoadOptions();


        NumOfGuessOption ??= new NebulaConfiguration(null, "role.guesser.numOfGuess", null, 1, 15, 3, 3);

        var commonOptions = new NebulaConfiguration[] { NumOfGuessOption };
        foreach (var option in commonOptions) option.Title = new CombinedComponent(new TranslateTextComponent("role.general.common"), new RawTextComponent(" "), new TranslateTextComponent(option.Id));

        CommonEditorOption = new NebulaConfiguration(RoleConfig, () => {
            MetaContext context = new();
            foreach (var option in commonOptions) context.Append(option.GetEditor()!);
            return context;
        });
    }

    public class NiceInstance : Crewmate.Crewmate.Instance
    {
        public override AbstractRole Role => MyNiceRole;
        private int leftGuess = NumOfGuessOption.GetMappedInt()!.Value;
        public NiceInstance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnMeetingStart()
        {
            if (AmOwner) GuesserSystem.OnMeetingStart(leftGuess, () => leftGuess--);
        }

        public override void OnDead()
        {
            if (AmOwner) GuesserSystem.OnDead();
        }
    }

    public class EvilInstance : Crewmate.Crewmate.Instance
    {
        public override AbstractRole Role => MyEvilRole;
        private int leftGuess = NumOfGuessOption.GetMappedInt()!.Value;
        public EvilInstance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnMeetingStart()
        {
            if (AmOwner) GuesserSystem.OnMeetingStart(leftGuess, () => leftGuess--);
        }

        public override void OnDead()
        {
            if (AmOwner) GuesserSystem.OnDead();
        }
    }
}

public class GuesserModifier : ConfigurableModifier
{
    static public GuesserModifier MyRole = new GuesserModifier();

    public override string LocalizedName => "guesser";
    public override Color RoleColor => Guesser.MyNiceRole.RoleColor;

    public override ModifierInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    public class Instance : ModifierInstance
    {
        public override AbstractModifier Role => MyRole;
        private int leftGuess = Guesser.NumOfGuessOption.GetMappedInt()!.Value;

        public Instance(PlayerModInfo player) : base(player){}

        public override void OnMeetingStart()
        {
            //追加役職Guesserは役職としてのGuesserがある場合効果を発揮しない
            if (MyPlayer.Role.Role is Guesser) return;

            if (AmOwner) GuesserSystem.OnMeetingStart(leftGuess, () => leftGuess--);
        }

        public override void OnDead()
        {
            if (AmOwner) GuesserSystem.OnDead();
        }
        public override void DecoratePlayerName(ref string text, ref Color color)
        {
            if (AmOwner || NebulaGameManager.Instance.CanSeeAllInfo) text += " ⊕".Color(Jackal.MyRole.RoleColor);
        }
    }
}