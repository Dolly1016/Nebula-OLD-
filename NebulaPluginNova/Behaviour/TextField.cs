using AsmResolver.PE.DotNet.Metadata.Strings;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Text.RegularExpressions;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nebula.Behaviour;

public class TextField : MonoBehaviour
{

    static TextField() => ClassInjector.RegisterTypeInIl2Cpp<TextField>();

    TextMeshPro myText = null!;
    TextMeshPro myCursor = null!;
    string myInput = "";
    int selectingBegin = -1;
    int cursor = 0;
    float cursorTimer = 0f;
    string lastCompoStr = "";

    static private TextField? validField = null;

    static public bool AnyoneValid => validField?.IsValid ?? false;

    public bool IsSelecting => selectingBegin != -1;

    private bool InputText(string input)
    {
        if (input.Length == 0) return false;

        int i = 0;
        while (true)
        {
            if (i == input.Length) return false;
            if (input[i] == 0x08)
                RemoveCharacter(false);
            if (input[i] == 0xFF)
                RemoveCharacter(true);
            else
                break;

            i++;
        }


        input = input.Substring(i).Replace(((char)0x08).ToString(), "").Replace(((char)0xFF).ToString(), "").Replace("\n", "").Replace("\r", "").Replace("\t", " ").Replace("\0", "");

        ShowCursor();

        if (IsSelecting)
        {
            int minIndex = Math.Min(cursor, selectingBegin);
            myInput = myInput.Remove(minIndex, Math.Abs(cursor - selectingBegin)).Insert(minIndex, input);
            selectingBegin = -1;
            cursor = minIndex + input.Length;
        }
        else
        {
            myInput = myInput.Insert(cursor, input);
            cursor += input.Length;
        }

        return true;
    }

    private void RemoveCharacter(bool isDelete)
    {
        if (IsSelecting)
        {
            myInput = myInput.Remove(Math.Min(cursor, selectingBegin), Math.Abs(cursor - selectingBegin));
            cursor = Math.Min(cursor, selectingBegin);
            selectingBegin = -1;
        }
        else
        {
            if (!isDelete && cursor > 0)
            {
                myInput = myInput.Remove(cursor - 1, 1);
                cursor--;
            }
            else if (isDelete && cursor < myInput.Length)
            {
                myInput = myInput.Remove(cursor, 1);
            }
        }
    }

    private void MoveCursor(bool moveForward, bool shift)
    {
        if (IsSelecting && !shift)
        {
            if (moveForward) cursor = Math.Max(cursor, selectingBegin);
            else cursor = Math.Min(cursor, selectingBegin);
            selectingBegin = -1;
        }
        else
        {
            if (shift && !IsSelecting) selectingBegin = cursor;
            cursor = Math.Clamp(cursor + (moveForward ? 1 : -1), 0, myInput.Length);

            if (selectingBegin == cursor) selectingBegin = -1;
        }
    }

    private int ConsiderComposition(int index,string compStr)
    {
        if (index >= cursor) return index + compStr.Length;
        return index;
    }

    private float GetCursorX(int index)
    {
        if (index == 0) return myText.rectTransform.rect.min.x;
        else return myText.textInfo.characterInfo[index - 1].xAdvance;
    }

    private void UpdateTextMesh()
    {
        lastCompoStr = Input.compositionString;
        string compStr = lastCompoStr;
        
        if (myInput.Length > 0 || compStr.Length > 0)
        {
            string str = myInput.Insert(cursor, compStr);
            if (IsSelecting) str = str.Insert(ConsiderComposition(Math.Max(cursor, selectingBegin), compStr), "\\EMK").Insert(ConsiderComposition(Math.Min(cursor, selectingBegin), compStr), "\\BMK");

            str = Regex.Replace(str,"[<>]", "<noparse>$0</noparse>").Replace("\\EMK", "</mark>").Replace("\\BMK", "<mark=#5F74A5AA>");

            myText.text = $"<font=\"Barlow-Medium SDF\">{str}";
        }
        else
        {
            myText.text = "";
        }
        myText.ForceMeshUpdate();

        int visualCursor = ConsiderComposition(cursor, compStr);
        myCursor.transform.localPosition = new(GetCursorX(visualCursor), 0, -1f);

        Vector2 compoPos = UnityHelper.WorldToScreenPoint(transform.position + new Vector3(GetCursorX(cursor), 0.15f, 0f), LayerExpansion.GetUILayer());
        compoPos.y = Screen.height - compoPos.y;
        Input.compositionCursorPos = compoPos;
    }

    private TextAttribute GenerateAttribute(Vector2 size, float fontSize, TextAlignmentOptions alignment)
    => new TextAttribute()
    {
        Alignment = alignment,
        AllowAutoSizing = false,
        Color = Color.white,
        FontSize = fontSize,
        Size = size,
        Styles = FontStyles.Normal
    };
    

    public void SetSize(Vector2 size,float fontSize)
    {
        GenerateAttribute(size, fontSize, TextAlignmentOptions.Left).Reflect(myText);
        GenerateAttribute(new(0.3f,size.y), fontSize, TextAlignmentOptions.Center).Reflect(myCursor);
        myText.font = VanillaAsset.VersionFont;
        myCursor.font = VanillaAsset.VersionFont;

        UpdateTextMesh();
    }

    public void Awake()
    {
        myText = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, transform);
        myCursor = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, transform);

        myText.transform.localPosition = new Vector3(0, 0, -1f);
        myCursor.transform.localPosition = new Vector3(0, 0, -1f);

        myText.outlineWidth = 0f;
        myCursor.outlineWidth = 0f;

        myText.text = "";
        myCursor.text = "|";

        SetSize(new Vector2(4f, 0.5f), 2f);
    }

    private void CopyText()
    {
        if (!IsSelecting) return;

        ClipboardHelper.PutClipboardString(myInput.Substring(Math.Min(cursor, selectingBegin), Math.Abs(cursor - selectingBegin)));
    }


    private bool PressingShift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public void Update()
    {
        if(this != validField)
        {
            myCursor.gameObject.SetActive(false);
            return;
        }

        bool requireUpdate = InputText(Input.inputString);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveCursor(false, PressingShift);
            ShowCursor();
            requireUpdate = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveCursor(true, PressingShift);
            ShowCursor();
            requireUpdate = true;
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            if (!IsSelecting && PressingShift) selectingBegin = cursor;
            cursor = 0;
            ShowCursor();
            requireUpdate = true;
        }
        if (Input.GetKeyDown(KeyCode.End))
        {
            if (!IsSelecting && PressingShift) selectingBegin = cursor;
            cursor = myInput.Length;
            ShowCursor();
            requireUpdate = true;
        }
        
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.A)){

                selectingBegin = 0;
                cursor = myInput.Length;
                requireUpdate = true;
            }
            if (Input.GetKeyDown(KeyCode.C)){
                CopyText();
                requireUpdate = true;
            }
            if (Input.GetKeyDown(KeyCode.X)){
                if (!IsSelecting)
                {
                    selectingBegin = 0;
                    cursor = myInput.Length;
                }
                CopyText();
                RemoveCharacter(true);
                requireUpdate = true;
            }
            if (Input.GetKeyDown(KeyCode.V)){
                InputText(ClipboardHelper.GetClipboardString());
                requireUpdate = true;
            }
        }

        if (requireUpdate || lastCompoStr != Input.compositionString) UpdateTextMesh();

        cursorTimer -= Time.deltaTime;
        if (cursorTimer < 0f)
        {
            myCursor.gameObject.SetActive(!myCursor.gameObject.activeSelf);
            cursorTimer = 0.65f;
        }
    }

    private void ShowCursor()
    {
        myCursor.gameObject.SetActive(true);
        cursorTimer = 0.8f;
    }

    public void OnEnable()
    {
        ChangeFocus(this);
    }

    public void OnDisable()
    {
        if (validField == this) ChangeFocus(null);
    }

    static private void ChangeFocus(TextField? field)
    {
        if (field == validField) return;
        if (validField != null) validField.LoseFocus();
        validField = field;
        field?.GetFocus();
    }

    private void LoseFocus() {
        Input.imeCompositionMode = IMECompositionMode.Off;
    }
    private void GetFocus() {
        Input.imeCompositionMode = IMECompositionMode.On;
    }

    public bool IsValid => validField == this && gameObject.active;
}