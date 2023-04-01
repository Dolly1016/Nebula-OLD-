using Nebula.Module;
using Nebula.Tasks;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nebula.Components;

public class TextInputField : MonoBehaviour
{
    static TextInputField()
    {
        ClassInjector.RegisterTypeInIl2Cpp<TextInputField>();
    }

    private void ReflectTextProperty()
    {
        if (Background) Background.size = Text.rectTransform.sizeDelta + new Vector2(0.2f, 0.2f);
        if (Collider) Collider.size = Text.rectTransform.sizeDelta;
    }

    public void SetTextProperty(Vector2 size,float fontSize,TMPro.TextAlignmentOptions alignmentOptions,TMPro.FontStyles fontStyle)
    {
        if(!Text)Text = GameObject.Instantiate(RuntimePrefabs.TextPrefab);
        Text.transform.SetParent(gameObject.transform);
        Text.transform.localScale = new Vector3(1f, 1f, 1f);
        Text.transform.localPosition = new Vector3(0f, 0f, -1f);
        Text.enableAutoSizing = false;

        Text.alignment = alignmentOptions;
        Text.fontStyle = fontStyle;
        Text.rectTransform.sizeDelta = size;
        Text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        Text.text = "";
        Text.fontSize = Text.fontSizeMax = Text.fontSizeMin = fontSize;
        Text.outlineWidth = 0f;
        Text.outlineColor = Color.clear;

        if (!Pipe) Pipe = GameObject.Instantiate(RuntimePrefabs.TextPrefab);
        Pipe.transform.SetParent(gameObject.transform);
        Pipe.transform.localScale = new Vector3(1f, 1f, 1f);
        Pipe.transform.localPosition = new Vector3(0f, 0f, -1f);
        Pipe.alignment = TMPro.TextAlignmentOptions.Center;
        Pipe.fontStyle = fontStyle;
        Pipe.text = "";
        Pipe.fontSize = Text.fontSizeMax = Text.fontSizeMin = fontSize;
        Pipe.outlineWidth = 0f;
        Pipe.outlineColor = Color.clear;
        Pipe.enableAutoSizing = false;

        ReflectTextProperty();
    }

    public void Start()
    {
        gameObject.layer = LayerExpansion.GetUILayer();

        Background = gameObject.AddComponent<SpriteRenderer>();
        Background.drawMode = SpriteDrawMode.Tiled;
        Background.sprite = MetaScreen.GetButtonBackSprite();

        Collider = gameObject.AddComponent<BoxCollider2D>();

        Button = gameObject.AddComponent<PassiveButton>();
        Button.OnClick.RemoveAllListeners();
        Button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
            GetFocus();
        }));

        Button.OnMouseOver = new UnityEngine.Events.UnityEvent();
        Button.OnMouseOut = new UnityEngine.Events.UnityEvent();
        Button.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)(() => {
            if (ValidField != this) Background.color = Color.green;
        }));
        Button.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => {
            Background.color = Color.white;
        }));

        if (!Text) SetTextProperty(new Vector2(4f, 0.5f), 1.5f, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Normal);
        else ReflectTextProperty();
    }

    public void ResetPipe()
    {
        CaretTimer = 0.5f;
        CaretFlag = true;
    }

    public void AcceptText(string text)
    {
        if (text.Length == 0) return;

        string result = InputText;
        foreach (var c in text)
        {
            if (c == '\b')
            {
                if (Cursor > 0 && result.Length > 0)
                {
                    result = result.Remove(Cursor - 1, 1);
                    Cursor--;
                }
            }
            else if (c == '\n' || c == '\r')
            {
                if (DecisionAction != null) DecisionAction(InputText);
                LoseFocus();
                break;
            }
            else if (!AllowCharacters?.Invoke(c) ?? false)
                continue;
            else
            {
                if (Cursor < result.Length)
                    result = result.Insert(Cursor, c.ToString());
                else
                    result += c;
                Cursor++;
            }
        }
        SetText(result);

        ResetPipe();
    }

    public void Update()
    {
        bool isFocused = ValidField == this;

        if (!isFocused)
        {
            Pipe.text = "";
            if (InputText.Length == 0 && HintText != null)
                Text.text = Helpers.cs(Color.white * 0.6f, HintText);
            else
                Text.text = InputText;

            if (CandidatesShower) CandidatesShower.gameObject.SetActive(false);
            return;
        }

        if (!(lastFocusTime > 0f) && Input.GetMouseButtonUp(0)) LoseFocus();
        if (lastFocusTime > 0f) lastFocusTime -= Time.deltaTime;

        if (AllowPaste && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
        {
            string clipboardString = ClipboardHelper.GetClipboardString();
            if (!string.IsNullOrWhiteSpace(clipboardString)) AcceptText(clipboardString);
        }

        if (Cursor > 0 && Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Cursor--;
            ResetPipe();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Cursor++;
            ResetPipe();
        }
        if (Cursor > InputText.Length) Cursor = InputText.Length;

        //入力を受け付ける
        AcceptText(Input.inputString);

        //縦棒
        CaretTimer -= Time.deltaTime;
        if (CaretTimer < 0f)
        {
            CaretTimer = 0.5f;
            CaretFlag = !CaretFlag;
        }

        Pipe.text = CaretFlag ? "|" : "";

        string firstHalf = (Cursor > 0 ? InputText.Substring(0, Cursor) : "") + Input.compositionString;
        string latterHalf= InputText.Substring(Cursor);
        if (InputText.Length > 0) {
            Text.text = firstHalf + latterHalf;
            Text.ForceMeshUpdate();
            float x = 0f;
            if (firstHalf.Length > 0)
                x = Text.textInfo.characterInfo[firstHalf.Length - 1].bottomRight.x;
            else
                x = Text.textInfo.characterInfo[0].bottomLeft.x;
            Pipe.transform.localPosition = new Vector3(x, 0f, -1.5f);
        }
        else
        {
            Text.text = firstHalf + latterHalf;
            Pipe.transform.localPosition = new Vector3(-Text.rectTransform.sizeDelta.x / 2f, 0f, -1.5f);
        }

        if (Cursor >= CandidateIndex)
        {
            if (CandidatesShower)
            {
                CandidatesShower.gameObject.SetActive(true);
                CandidatesShower.Offset = new(CandidateIndex > 0 ? Text.textInfo.characterInfo[CandidateIndex - 1].bottomRight.x : -Text.rectTransform.sizeDelta.x / 2f, 0f);
            }
        }
        else if (CandidatesShower)
            CandidatesShower.gameObject.SetActive(false);

    }

    public void LoseFocus()
    {
        if (ValidField != this) return;
        Input.imeCompositionMode = IMECompositionMode.Off;
        ValidField = null;
        if (LoseFocusAction != null) LoseFocusAction.Invoke(InputText);

        if(CandidatesShower != null) CandidatesShower.gameObject.SetActive(false);
        
    }

    public void GetFocus()
    {
        if (ValidField) ValidField!.LoseFocus();
        ValidField = this;

        if(UseIME) Input.imeCompositionMode = IMECompositionMode.On;
        Cursor= InputText.Length;

        lastFocusTime = 0.5f;

        OnSetText();
    }

    public void OnDisable()
    {
        LoseFocus();
    }

    private TMPro.TextMeshPro Text;
    private TMPro.TextMeshPro Pipe;
    public SpriteRenderer Background;
    public BoxCollider2D Collider;
    public PassiveButton Button;
    public string InputText { get; private set; } = "";
    public string? HintText = null;
    private float CaretTimer = 0f;
    private bool CaretFlag = false;
    public bool UseIME = false;
    public float lastFocusTime = 0f;

    public ICandidateSuggester? Suggester = null;
    private int CandidateIndex = -1;

    public void SetTextByCandidate(string text)
    {
        if(CandidateIndex>=0)SetText(InputText.Substring(0, CandidateIndex) + text);
    }

    public void SetText(string text)
    {
        if (InputText == text) return;

        InputText = text;
        OnSetText();
    }

    private void OnSetText()
    {
        if (ValidField != this) return;

        if (Suggester != null)
        {
            Suggester.GetCandidate(InputText, out var candidates, out CandidateIndex);

            if (CandidatesShower == null)
            {
                CandidatesShower = new GameObject("Candidates").AddComponent<TextCandidates>();
                CandidatesShower.SetField(this);
                CandidatesShower.Offset = Vector2.zero;
            }
            CandidatesShower.gameObject.SetActive(true);
            CandidatesShower.UpdateCandidates(candidates);
        }
        else
        {
            CandidateIndex = -1;
        }
    }



    public TextCandidates CandidatesShower;

    public int Cursor { get; private set; } = 0;

    public bool AllowPaste = true;
    public Predicate<char>? AllowCharacters = null;

    public Action<string>? DecisionAction = null;
    public Action<string>? LoseFocusAction = null;

    public static TextInputField? ValidField = null;
    
    
}
