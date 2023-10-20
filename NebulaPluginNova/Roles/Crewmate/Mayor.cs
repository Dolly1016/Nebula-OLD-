using Nebula.Configuration;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

public class Mayor : ConfigurableStandardRole
{
    static public Mayor MyRole = new Mayor();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "mayor";
    public override Color RoleColor => new Color(30f / 255f, 96f / 255f, 85f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration MinVoteOption = null!;
    private NebulaConfiguration MaxVoteOption = null!;
    private NebulaConfiguration MaxVoteStockOption = null!;
    private NebulaConfiguration VoteAssignmentOption = null!;
    private NebulaConfiguration FixedVotesOption = null!;

    private int MinVote => FixedVotesOption ? VoteAssignmentOption : MinVoteOption;
    private int MaxVote => FixedVotesOption ? VoteAssignmentOption : MaxVoteOption;
    private int VoteAssignment => VoteAssignmentOption;
    private int VotesStock => FixedVotesOption ? VoteAssignmentOption : MaxVoteStockOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        FixedVotesOption = new(RoleConfig, "fixedVotes", null, false, false);
        MinVoteOption = new(RoleConfig, "minVote", null, 0, 20, 1, 1) { Predicate = () => !FixedVotesOption };
        MaxVoteOption = new(RoleConfig, "maxVote", null, 0, 20, 2, 2) { Predicate = () => !FixedVotesOption };
        MaxVoteStockOption = new(RoleConfig, "maxVotesStock", null, 1, 20, 8, 8) { Predicate = () => !FixedVotesOption };
        VoteAssignmentOption = new(RoleConfig, "voteAssignment", null, 1, 20, 1, 1);
    }

    public class Instance : Crewmate.Instance
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        static private SpriteLoader leftButtonSprite = SpriteLoader.FromResource("Nebula.Resources.MeetingButtonLeft.png", 100f);
        static private SpriteLoader rightButtonSprite = SpriteLoader.FromResource("Nebula.Resources.MeetingButtonRight.png", 100f);

        private int myVote = 0;
        private int currentVote = 0;
        public override void OnMeetingStart()
        {
            if (AmOwner && !MyPlayer.IsDead)
            {
                var countText = UnityEngine.Object.Instantiate(MeetingHud.Instance.TitleText, MeetingHud.Instance.SkipVoteButton.transform);
                countText.gameObject.SetActive(true);
                countText.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
                countText.alignment = TMPro.TextAlignmentOptions.Center;
                countText.transform.localPosition = new Vector3(2.59f, 0f);
                countText.color = Palette.White;
                countText.transform.localScale *= 0.8f;
                countText.text = "";

                myVote = Mathf.Min(myVote + MyRole.VoteAssignment, MyRole.VotesStock);
                int min = Mathf.Min(MyRole.MinVote, myVote);
                int max = Mathf.Min(MyRole.MaxVote, myVote);
                currentVote = Mathf.Clamp(currentVote, min, max);
                countText.text = currentVote.ToString() + "/" + myVote;

                void UpdateVotes(bool increment)
                {
                    currentVote = Mathf.Clamp(currentVote + (increment ? 1 : -1), min, max);
                    countText.text = currentVote.ToString() + "/" + myVote;
                }

                if (min == max) return;

                var myArea = MeetingHud.Instance.playerStates.FirstOrDefault(v=>v.TargetPlayerId == MyPlayer.PlayerId);
                if (myArea is null) return;

                var leftRenderer = UnityHelper.CreateObject<SpriteRenderer>("MayorButton-Minus", MeetingHud.Instance.SkipVoteButton.transform, new Vector3(1.5f, 0f));
                leftRenderer.sprite = leftButtonSprite.GetSprite();
                var leftButton = leftRenderer.gameObject.SetUpButton(true);
                leftButton.OnMouseOver.AddListener(() => leftRenderer.color = Color.gray);
                leftButton.OnMouseOut.AddListener(() => leftRenderer.color = Color.white);
                leftButton.OnClick.AddListener(() => {
                    if (myArea.DidVote) return;
                    UpdateVotes(false);
                });
                leftRenderer.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);

                var rightRenderer = UnityHelper.CreateObject<SpriteRenderer>("MayorButton-Plus", MeetingHud.Instance.SkipVoteButton.transform, new Vector3(3.1f, 0f));
                rightRenderer.sprite = rightButtonSprite.GetSprite();
                var rightButton = rightRenderer.gameObject.SetUpButton(true);
                rightButton.OnMouseOver.AddListener(() => rightRenderer.color = Color.gray);
                rightButton.OnMouseOut.AddListener(() => rightRenderer.color = Color.white);
                rightButton.OnClick.AddListener(() => {
                    if (myArea.DidVote) return;
                    UpdateVotes(true);
                });
                rightRenderer.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);
            }
        }

        public override void OnCastVoteLocal(byte target, ref int vote)
        {
            vote = currentVote;
        }

        public override void OnEndVoting()
        {
            if (MeetingHud.Instance.playerStates.FirstOrDefault(v => v.TargetPlayerId == MyPlayer.PlayerId)?.DidVote ?? false) myVote -= currentVote;
        }
    }
}
