using UnityEngine;

namespace Nebula.Roles.Crewmate;

public class Mayor : Role
{
    static public Color RoleColor = new Color(3f / 255f, 79f / 255f, 66f / 255f);

    public int votesId { get; private set; }
    public int canCallEmergencyId { get;private set; }
    public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(votesId, "Vote Stock", 0, 20), new RelatedRoleData(canCallEmergencyId, "Can Call Special Meeting", 0, 1,new string[] { "False","True"}) }; }

    private Module.CustomOption voteAssignmentOption;
    private Module.CustomOption minVoteOption;
    private Module.CustomOption maxVoteOption;
    private Module.CustomOption maxVoteStockOption;
    private Module.CustomOption haveRemoteEmergencyOption;

    private CustomButton emergencyButton;

    private static SpriteLoader emergencySprite = new SpriteLoader("Nebula.Resources.EmergencyButton.png", 115f, "ui.button.mayor.emergency");

    //投じる票数の表示
    private TMPro.TextMeshPro countText;

    //今投票したときに投じる票数
    private byte numOfVote = 1;

    public override void GlobalInitialize(PlayerControl __instance)
    {
        Game.GameData.data.playersArray[__instance.PlayerId].SetRoleData(votesId, 0);
    }

    public override void OnMeetingStart()
    {
        RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, votesId, (int)voteAssignmentOption.getFloat());
    }

    public override void OnVote(byte targetId)
    {
        RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, votesId, -numOfVote);
    }

    public override void OnVoteCanceled(int weight)
    {
        RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, votesId, weight);
    }

    public override void SetupMeetingButton(MeetingHud __instance)
    {
        numOfVote = 1;

        if ((int)minVoteOption.getFloat() >= (int)maxVoteOption.getFloat())
        {
            //入れうる票が固定になる場合
            numOfVote = (byte)maxVoteOption.getFloat();
            if (numOfVote > Game.GameData.data.myData.getGlobalData().GetRoleData(votesId))
            {
                numOfVote = (byte)Game.GameData.data.myData.getGlobalData().GetRoleData(votesId);
            }
        }
        else
        {

            if (!PlayerControl.LocalPlayer.Data.IsDead)
            {
                GameObject template, button;
                PassiveButton passiveButton;
                SpriteRenderer renderer;

                template = __instance.SkipVoteButton.Buttons.transform.Find("CancelButton").gameObject;
                button = UnityEngine.Object.Instantiate(template, __instance.SkipVoteButton.transform);
                button.SetActive(true);
                button.name = "MayorButton";
                button.transform.position += new Vector3(1.5f, 0f);
                renderer = button.GetComponent<SpriteRenderer>();
                renderer.sprite = Images.GlobalImage.GetMeetingButtonLeft();
                passiveButton = button.GetComponent<PassiveButton>();
                passiveButton.OnClick.RemoveAllListeners();
                passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    if (numOfVote > 0)
                    {
                        numOfVote--;
                        RPCEventInvoker.MultipleVote(PlayerControl.LocalPlayer, numOfVote);
                    }
                }));

                template = __instance.SkipVoteButton.Buttons.transform.Find("CancelButton").gameObject;
                button = UnityEngine.Object.Instantiate(template, __instance.SkipVoteButton.transform);
                button.SetActive(true);
                button.name = "MayorButton";
                button.transform.position += new Vector3(2.7f, 0f);
                renderer = button.GetComponent<SpriteRenderer>();
                renderer.sprite = Images.GlobalImage.GetMeetingButtonRight();
                passiveButton = button.GetComponent<PassiveButton>();
                passiveButton.OnClick.RemoveAllListeners();
                passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    if (numOfVote < maxVoteOption.getFloat() && numOfVote < Game.GameData.data.myData.getGlobalData().GetRoleData(votesId))
                    {
                        numOfVote++;
                        RPCEventInvoker.MultipleVote(PlayerControl.LocalPlayer, numOfVote);
                    }
                }));



                countText = UnityEngine.Object.Instantiate(__instance.TitleText, __instance.SkipVoteButton.transform);
                countText.gameObject.SetActive(true);
                countText.alignment = TMPro.TextAlignmentOptions.Center;
                countText.transform.position = __instance.SkipVoteButton.CancelButton.transform.position;
                countText.transform.position += new Vector3(1.54f, 0f);
                countText.color = Palette.White;
                countText.transform.localScale *= 0.8f;
                countText.text = "";
            }
        }

        RPCEventInvoker.MultipleVote(PlayerControl.LocalPlayer, numOfVote);
    }

    public override void MeetingUpdate(MeetingHud __instance, TMPro.TextMeshPro meetingInfo)
    {

        int count = Game.GameData.data.myData.getGlobalData().GetRoleData(votesId);

        countText.text = numOfVote.ToString() + "/" + count;

    }

    public override void LoadOptionData()
    {
        voteAssignmentOption = CreateOption(Color.white, "voteAssignment", 1f, 1f, 5f, 1f);
        minVoteOption = CreateOption(Color.white, "minVote", 0f, 0f, 20f, 1f);
        maxVoteOption = CreateOption(Color.white, "maxVote", 5f, 0f, 20f, 1f);
        maxVoteStockOption = CreateOption(Color.white, "maxVoteStock", 5f, 0f, 20f, 1f);
        haveRemoteEmergencyOption = CreateOption(Color.white, "haveRemoteEmergencyButton", true);
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Opportunist);
    }

    public override void OnCallSpecialMeeting()
    {
        Game.GameData.data.myData.getGlobalData().SetRoleData(canCallEmergencyId, 0);
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        RPCEventInvoker.UpdateRoleData(PlayerControl.LocalPlayer.PlayerId, canCallEmergencyId, haveRemoteEmergencyOption.getBool() ? 1 : 0);
        if (emergencyButton != null)
        {
            emergencyButton.Destroy();
        }
        emergencyButton = new CustomButton(
            () =>
            {
                Patches.EmergencyPatch.isSpecialEmergency = true;

                var console = Map.MapData.MapDatabase[0].Assets.transform.GetChild(4).GetChild(0).GetChild(4).gameObject.GetComponent<SystemConsole>();
                Minigame minigame = GameObject.Instantiate<Minigame>(console.MinigamePrefab);
                minigame.transform.SetParent(Camera.main.transform, false);
                minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
                minigame.Begin(null);
            },
            () =>
            {
                return !PlayerControl.LocalPlayer.Data.IsDead && Game.GameData.data.myData.getGlobalData().GetRoleData(canCallEmergencyId) > 0;
            },
            () =>
            {
                int remain = Game.GameData.data.myData.getGlobalData().GetRoleData(canCallEmergencyId);
                return remain == 1 && PlayerControl.LocalPlayer.CanMove;
            },
            () => { emergencyButton.Timer = emergencyButton.MaxTimer; },
            emergencySprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.emergency"
        ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
        emergencyButton.UsesText.text = "1";
        emergencyButton.MaxTimer = 10f;
    }

    public override void CleanUp()
    {

        if (emergencyButton != null)
        {
            emergencyButton.Destroy();
            emergencyButton = null;
        }
    }


    public Mayor()
        : base("Mayor", "mayor", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, false, false)
    {
        votesId = Game.GameData.RegisterRoleDataId("mayor.votes");
        canCallEmergencyId = Game.GameData.RegisterRoleDataId("mayor.canCallEmergency");
    }
}
