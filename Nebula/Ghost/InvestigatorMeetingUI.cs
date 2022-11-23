namespace Nebula.Ghost;

static public class InvestigatorMeetingUI
{
    private static GameObject MainUI;
    private static List<Transform> buttons = new List<Transform>();

    static public void EndMeeting()
    {
        foreach (var button in buttons)
        {
            UnityEngine.Object.Destroy(button);
        }
        buttons.Clear();
    }

    static public void UpdateMeetingUI(MeetingHud __instance)
    {
        __instance.TitleText.text = Language.Language.GetString("investigators.ui.title");

        foreach (var button in buttons)
        {
            if (__instance.state != MeetingHud.VoteStates.Animating)
            {
                if (!button.gameObject.active)
                    button.gameObject.active = true;
            }
        }
    }

    static public void FormMeetingUI(MeetingHud __instance)
    {
        Transform container = __instance.transform.FindChild("PhoneUI");
        MainUI = container.gameObject;

        int i = 0;
        var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
        var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
        var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
        var textTemplate = __instance.playerStates[0].NameText;

        buttons.Clear();

        Transform selectedButton = null;

        foreach (GhostInfo ghost in GhostInfo.AllGhostInfo)
        {
            Transform buttonParent = (new GameObject()).transform;
            buttonParent.SetParent(container);
            Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
            Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
            TMPro.TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
            button.GetComponent<SpriteRenderer>().sprite = FastDestroyableSingleton<HatManager>.Instance.AllNamePlates[0].viewData.viewData.Image;
            buttons.Add(button);
            button.gameObject.active = false;
            int row = i / 4, col = i % 4;
            buttonParent.localPosition = new Vector3(-3.28f + 2.2f * col, 1.5f - 0.53f * row, -5);
            buttonParent.localScale = new Vector3(0.73f, 0.73f, 1f);
            label.text = Helpers.cs(Color.white, Language.Language.GetString("ghost." + ghost.LocalizeName + ".name"));
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
            label.transform.localScale *= 1.7f;

            int copiedIndex = i;

            button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            button.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() =>
            {
                if (selectedButton != button)
                {
                    selectedButton = button;
                    buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Color.red : Color.white);
                }
                else
                {
                    button.GetComponent<SpriteRenderer>().color = Color.white;

                        //
                    }
            }));

            i++;
        }

        __instance.SkipVoteButton.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() =>
        {
            selectedButton = null;
            buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = Color.white);
        }));


        var players = __instance.playerStates.ToList();
        int index = 0;
        int? mask = null;

        float width = 0.578f, intercept = 3.12f;
        int alive = 0;
        foreach (var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (!player.Data.IsDead) alive++;
        }
        if (alive == 1) intercept = 0;
        else
        {
            float rate = (float)(alive + 5) / 20f;
            width *= rate * 14f / (float)(alive - 1);
            intercept *= rate;
        }

        foreach (var player in players)
        {
            if (Helpers.playerById(player.TargetPlayerId).Data.IsDead)
                player.gameObject.active = false;
            else
            {
                player.transform.localPosition = new Vector3(-intercept - 0.04f * (float)(alive - 10) + width * (float)index, -1.4f, -5);
                player.Background.enabled = false;
                player.PlayerButton.enabled = false;
                player.NameText.enabled = false;
                player.Megaphone.enabled = false;

                if (mask == null)
                {
                    mask = player.PlayerIcon.cosmetics.skin.layer.material.GetInt("_MaskLayer") - 2;
                    player.MaskArea.transform.localPosition = new Vector3(0f, 0.017f, 0.5f);
                    player.MaskArea.transform.localScale = new Vector3(10f, 0.596f, 1f);
                }
                else
                {
                    player.SetMaskLayer(mask.Value);
                }
                player.transform.FindChild("PlayerLevel").gameObject.active = false;

                index++;
            }
        }
    }
}