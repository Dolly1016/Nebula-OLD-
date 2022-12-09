namespace Nebula.Module.Information;

public class TextInformation : UpperInformation
{
    TMPro.TextMeshPro text;

    public TextInformation(string text) : base("TextInfo")
    {
        if (!HudManager.Instance.TaskPanel.taskText) return;

        height = 0.28f;

        this.text = GameObject.Instantiate(HudManager.Instance.TaskPanel.taskText, gameObject.transform);
        this.text.rectTransform.localPosition = new Vector2(0.0f, 0.0f);
        this.text.rectTransform.sizeDelta = new Vector2(0.0f, 0.0f);
        this.text.alignment = TMPro.TextAlignmentOptions.Center;
        this.text.text = text;
        this.text.fontSize = this.text.fontSizeMin = this.text.fontSizeMax = 1.25f;
        this.text.fontStyle = TMPro.FontStyles.Bold;
    }

    public override bool Update()
    {
        return Game.GameData.data.myData.CanSeeEveryoneInfo && !MeetingHud.Instance;
    }
}