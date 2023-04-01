using Nebula.Expansion;
using Nebula.Module;
using UnityEngine.Events;
using UnityEngine.UI;
using static Il2CppSystem.Uri;

namespace Nebula.Components;

public class TextCandidate
{
    public enum TextCandidateType
    {
        Suggestion,
        Hint
    }

    public string ShownText { get; private init; }
    public TextCandidateType Type { get; private init; }

    public TextCandidate(TextCandidateType type,string shownText)
    {
        ShownText= shownText;
        Type = type;
    }
}

public interface ICandidateSuggester
{
    public void GetCandidate(string text, out TextCandidate[]? candidates, out int index);
}

public class TextCandidates : MonoBehaviour
{
    static TextCandidates()
    {
        ClassInjector.RegisterTypeInIl2Cpp<TextCandidates>();
    }

    private const float Height = 0.3f;

    public Vector2 Offset { set {
            Holder.transform.localPosition = new Vector3(value.x, value.y + (Height + 0.1f) * (ExtendsDownward ? -1f : 1f), -5f);
        } }

    public bool ExtendsDownward { get; set; } = true;

    public Il2CppReferenceField<TextInputField> InputField;
    public GameObject Holder { get; private set; }
    public SpriteRenderer Background { get; private set; }
    public SpriteRenderer Cursor { get; private set; }
    private CandidateContent Attention { get; set; }

    private float width;

    public class CandidateContent
    {
        private TextCandidates Candidates;
        private TMPro.TextMeshPro Text;
        private BoxCollider2D Collider;
        public TextCandidate Candidate;
        public GameObject Content;
        
        public void Hide()
        {
            Content.gameObject.SetActive(false);
        }

        public void Show()
        {
            Content.gameObject.SetActive(true);
        }

        public float SetText(TextCandidate candidate,float y)
        {
            Content.transform.localPosition = new Vector3(0.1f, y, 0f);

            this.Candidate = candidate;
            Text.text = candidate.ShownText;
            Text.color = candidate.Type is TextCandidate.TextCandidateType.Suggestion ? Color.white : Color.gray;
            Text.ForceMeshUpdate(true, true);

            return Text.preferredWidth;
        }

        public void SetWidth(float width)
        {
            Collider.offset = new Vector2(width * 0.5f, 0f);
            Collider.size = new Vector2(width + 0.18f, Height - 0.02f);
        }

        public CandidateContent(TextCandidates holder)
        {
            Candidates = holder;

            Content = new GameObject("Candidate");
            Content.transform.SetParent(holder.Holder.transform);

            Text = GameObject.Instantiate(RuntimePrefabs.TextPrefab, Content.transform);
            Text.fontSize = Text.fontSizeMax = Text.fontSizeMin = 1.6f;
            Text.alignment = TMPro.TextAlignmentOptions.Left;
            Text.transform.localPosition = new Vector3(0, 0, -1f);
            Text.rectTransform.sizeDelta = new Vector2(5f, Height);
            Text.rectTransform.pivot = new Vector2(0f, 0.5f);
            Text.outlineWidth = 0f;
            Text.outlineColor = Color.clear;

            var obj = new GameObject("Button");
            obj.transform.SetParent(Content.transform);
            Collider = obj.AddComponent<BoxCollider2D>();
            Collider.isTrigger = true;

            var button = obj.SetUpButton(() => {
                if (Candidate.Type is TextCandidate.TextCandidateType.Suggestion) Candidates.InputField.Get().SetTextByCandidate(Candidate.ShownText);
                Candidates.InputField.Get().GetFocus(); 
            });
            button.OnMouseOver.AddListener((UnityAction)(() => Candidates.SetHighlight(this)));
            button.OnMouseOut.AddListener((UnityAction)(() => Candidates.UnsetHighlight(this)));

        }

        
    }
    private List<CandidateContent> Candidates = new();

    private void SetHighlight(CandidateContent content)
    {
        if (content.Candidate.Type is TextCandidate.TextCandidateType.Hint) Cursor.gameObject.SetActive(false);
        else
        {
            Cursor.gameObject.SetActive(true);
            Cursor.transform.localPosition = new Vector3(0.1f + width * 0.5f, content.Content.transform.localPosition.y, -0.1f);
        }
    }

    private void UnsetHighlight(CandidateContent? content = null)
    {
        if (content != null && Attention != content) return;

        Attention = null;
        Cursor.gameObject.SetActive(false);
    }

    public void SetField(Il2CppArgument<TextInputField> parentField)
    {
        InputField.Set(parentField.Value);
        transform.SetParent(parentField.Value.transform);
        transform.localPosition = new Vector3(0, 0, 0);
    }

    ISpriteLoader backgroundSprite = new AssetSpriteLoader(AssetLoader.NebulaMainAsset, "TextCandidateBack");
    ISpriteLoader highlightSprite = new AssetSpriteLoader(AssetLoader.NebulaMainAsset, "TextCandidateHighlight");
    public void Awake()
    {
        Holder = new GameObject("Holder");
        Holder.transform.SetParent(this.transform);
        
        Offset = Vector2.zero;

        var backObj = new GameObject("Background");
        backObj.layer = LayerExpansion.GetUILayer();
        backObj.transform.SetParent(Holder.transform);
        Background = backObj.AddComponent<SpriteRenderer>();
        Background.sprite = backgroundSprite.GetSprite();
        Background.drawMode = SpriteDrawMode.Sliced;
        Background.color = Color.white.RGBMultiplied(0.55f);

        var frontObj = new GameObject("Highlight");
        frontObj.layer = LayerExpansion.GetUILayer();
        frontObj.transform.SetParent(Holder.transform);
        Cursor = frontObj.AddComponent<SpriteRenderer>();
        Cursor.sprite = highlightSprite.GetSprite();
        Cursor.color = new Color(0.4f, 0.4f, 0.2f);
        Cursor.drawMode = SpriteDrawMode.Sliced;
        Cursor.gameObject.SetActive(false);
    }

    public void UpdateCandidates(TextCandidate[]? candidates)
    {
        transform.localPosition = new Vector3(0, 0, 0);

        if (candidates==null || candidates.Length == 0)
        {
            Holder.gameObject.SetActive(false);
            return;
        }

        Holder.gameObject.SetActive(true);

        UnsetHighlight();

        int i = 0;
        width = 0f;
        foreach(TextCandidate candidate in candidates)
        {
            if (Candidates.Count <= i) Candidates.Add(new CandidateContent(this));
            Candidates[i].Show();
            float num = Candidates[i].SetText(candidate, (float)i * Height * (ExtendsDownward ? -1f : 1f));
            if (num > width) width = num;
            i++;
        }
        Background.gameObject.transform.localPosition = new Vector3(0.1f + width * 0.5f, Height * (float)(i - 1) * 0.5f * (ExtendsDownward ? -1f : 1f), 0f);
        Background.size = new Vector2(width + 0.4f, Height * (float)i + 0.2f);

        Cursor.size = new Vector3(width + 0.18f, 0.28f);

        for (; i < Candidates.Count; i++)
        {
            Candidates[i].Hide();
        }

        foreach (var candidate in Candidates) candidate.SetWidth(width);
    }

}
