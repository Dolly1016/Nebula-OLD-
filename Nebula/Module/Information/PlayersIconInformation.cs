namespace Nebula.Module.Information;

public class PlayersIconInformation : UpperInformation
{
    public class PlayersIcon
    {
        public TMPro.TextMeshPro text;
        public SpriteRenderer renderer;
        public GameObject gameObject;

        public PlayersIcon(GameObject gameObject, TMPro.TextMeshPro text, SpriteRenderer renderer)
        {
            this.gameObject = gameObject;
            this.text = text;
            this.renderer = renderer;
        }
    }

    Dictionary<byte, PlayersIcon> allPlayers;
    TMPro.TextMeshPro subText;

    public byte relatedPlayerId { get; private set; }
    public Roles.Assignable relatedRole { get; private set; }

    public PlayersIconInformation(string subtext, byte relatedPlayerId, Roles.Assignable relatedRole) : base("PlayersIconInfo")
    {
        height = 0.24f;

        this.relatedPlayerId = relatedPlayerId;
        this.relatedRole = relatedRole;

        allPlayers = new Dictionary<byte, PlayersIcon>();

        SpriteRenderer prefab = HudManager.Instance.MeetingPrefab.PlayerVotePrefab;

        foreach (var p in PlayerControl.AllPlayerControls)
        {
            var obj = new GameObject(p.name);
            var renderer = GameObject.Instantiate(prefab, obj.transform);
            PlayerMaterial.SetColors(p.cosmetics.bodyMatProperties.ColorId, renderer);

            var text = GameObject.Instantiate(HudManager.Instance.TaskPanel.taskText, obj.transform);
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.fontSize = text.fontSizeMin = text.fontSizeMax = 1.0f;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.rectTransform.localPosition = new Vector3(0.0f, 0.0f, -2.0f);
            text.rectTransform.sizeDelta = new Vector2(0, 0);
            text.text = "";

            obj.transform.SetParent(gameObject.transform);

            obj.SetActive(false);

            allPlayers.Add(p.PlayerId, new PlayersIcon(obj, text, renderer));
        }

        subText = GameObject.Instantiate(HudManager.Instance.TaskPanel.taskText, gameObject.transform);
        subText.alignment = TMPro.TextAlignmentOptions.Left;
        subText.fontSize = subText.fontSizeMin = subText.fontSizeMax = 1.4f;
        subText.transform.SetParent(gameObject.transform);
        subText.rectTransform.sizeDelta = new Vector2(0, 0);
        subText.rectTransform.localPosition = new Vector2(0.0f, 0.0f);
        subText.text = subtext;

        SetText(0, "0.0%");
    }

    public override bool Update()
    {
        width = 0f;
        if (subText.text != "")
        {
            width += subText.preferredWidth + 0.25f;
        }

        foreach (var icon in allPlayers)
        {
            if (icon.Value.gameObject.activeSelf)
            {
                icon.Value.gameObject.transform.localPosition = new Vector3(width, 0, 0);
                width += 0.25f;
            }
        }

        return Game.GameData.data.myData.CanSeeEveryoneInfo && !MeetingHud.Instance && PlayerControl.LocalPlayer.PlayerId != relatedPlayerId;
    }

    public void SetActive(byte playerId, bool active)
    {
        if (allPlayers.ContainsKey(playerId))
        {
            allPlayers[playerId].gameObject.SetActive(active);
        }
    }

    public void SetText(byte playerId, string text)
    {
        if (allPlayers.ContainsKey(playerId))
        {
            allPlayers[playerId].text.text = text;
        }
    }

    public void SetSemitransparent(byte playerId, bool semitransparent)
    {
        if (allPlayers.ContainsKey(playerId))
        {
            allPlayers[playerId].renderer.color = semitransparent ? new Color(1, 1, 1, 0.3f) : Color.white;
        }
    }
}