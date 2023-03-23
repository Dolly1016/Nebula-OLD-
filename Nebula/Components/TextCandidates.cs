namespace Nebula.Components;

public interface ICommandStructure
{
    public string[] GetCandidate(string[] inputs);
}

public class TextCandidates : MonoBehaviour
{
    static TextCandidates()
    {
        ClassInjector.RegisterTypeInIl2Cpp<TextCandidates>();
    }

    public TextInputField InputField { get; private set; }
    public Vector2 Offset { get; private set; }

    public SpriteRenderer Background { get; private set; }
    public SpriteRenderer Cursor { get; private set; }

    public class CandidateContent
    {
        TMPro.TextMeshPro Text;
        string candidate;

        public CandidateContent(GameObject holder,string candidate)
        {

        }

        public void Destroy()
        {

        }
    }
    private CandidateContent[] Candidates = new CandidateContent[0];

    public void SetField(TextInputField field,Vector2 offset,ICommandStructure structure)
    {
        InputField = field;
        transform.SetParent(field.transform);
        Offset=offset;
        transform.localPosition = new Vector3(offset.x, offset.y, 0.1f);
    }

    public void Update()
    {
        if (!InputField) return;

        string[] inputs = InputField.InputText.Split(" ");
        if (inputs.Length == 0) return;

        int length = InputField.InputText.Length;
        if (length - InputField.Cursor > inputs[inputs.Length - 1].Length) return;
    }

}
