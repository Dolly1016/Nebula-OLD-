﻿using AsmResolver.PE.DotNet.Metadata.Strings;
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
    static private List<TextField> allFields = new();

    public static TextField? EditFirstField()
    {
        if (allFields.Count == 0) return null;
        var field = allFields.FirstOrDefault(f=>f.gameObject.active);
        ChangeFocus(field);
        return field;
    }

    public static TextField? EditLastField()
    {
        if (allFields.Count == 0) return null;
        var field = allFields.LastOrDefault(f => f.gameObject.active);
        ChangeFocus(field);
        return field;
    }

    public static TextField? ChangeField(bool increament = true)
    {
        if (validField == null) return null;

        int index = allFields.IndexOf(validField);
        if (index == -1)
        {
            return null;
        }

        while (true)
        {
            index += increament ? 1 : -1;
            if (index < 0 || index >= allFields.Count) break;

            if (allFields[index].gameObject.active)
            {
                ChangeFocus(allFields[index]);
                return allFields[index];
            }
        }

        return null;
    }

    public TextField GainFocus()
    {
        ChangeFocus(this);
        return this;
    }

    static TextField() => ClassInjector.RegisterTypeInIl2Cpp<TextField>();

    public string Text => myInput;
    private string? hint;

    TextMeshPro myText = null!;
    TextMeshPro myCursor = null!;
    string myInput = "";
    int selectingBegin = -1;
    int cursor = 0;
    float cursorTimer = 0f;
    string lastCompoStr = "";

    public int MaxLines = 1;
    public bool AllowMultiLine => MaxLines >= 2;
    public bool AllowTab = false;

    public Predicate<char>? InputPredicate;
    public Action<string>? UpdateAction;
    public Action<string>? LostFocusAction;

    static private TextField? validField = null;

    static public bool AnyoneValid => validField?.IsValid ?? false;

    public bool IsSelecting => selectingBegin != -1;

    static readonly public Predicate<char> TokenPredicate = (c) => ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || ('0' <= c && c <= '9');
    static readonly public Predicate<char> IdPredicate = (c) => TokenPredicate(c) || c is '.';
    static readonly public Predicate<char> NameSpacePredicate = (c) => TokenPredicate(c) || c is '.' || c is ':';
    static readonly public Predicate<char> IntegerPredicate = (c) => ('0' <= c && c <= '9');
    static readonly public Predicate<char> NumberPredicate = (c) => ('0' <= c && c <= '9') || c is '.';
    static readonly public Predicate<char> JsonStringPredicate = (c) => !(c is '\\' or '"');

    private bool InputText(string input)
    {
        if (InputPredicate != null) input = new string(input.Where(c => (InputPredicate.Invoke(c))||(c is '\r' or (char)0x08)).ToArray());

        if (input.Length == 0) return false;

        {
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


            if (!AllowMultiLine) input = input.Replace("\r", "");
            if (!AllowTab) input = input.Replace("\t", " ");
            input = input.Substring(i).Replace(((char)0x08).ToString(), "").Replace(((char)0xFF).ToString(), "").Replace("\0", "").Replace("\n", "");
        }

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

        //改行文字を制限
        var strings = myInput.Split('\r');
        myInput = "";
        for(int i = 0; i < strings.Length; i++)
        {
            if (i > 0 && i < MaxLines) myInput += '\r';
            myInput += strings[i];
        }
        cursor = Math.Clamp(cursor, 0, myInput.Length);

        UpdateAction?.Invoke(myInput);

        return true;
    }

    private void RemoveAll()
    {
        myInput = "";
        cursor = 0;
        selectingBegin = -1;
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

    private void MoveCursorLine(bool moveForward,bool shift)
    {
        try
        {
            int myLineBegining = cursor;
            while (myLineBegining > 0 && myInput[myLineBegining - 1] != '\r') myLineBegining--;
            int targetLineBegining = moveForward ? cursor : myLineBegining - 1;
            while (targetLineBegining > 0 && targetLineBegining < myInput.Length && myInput[targetLineBegining - 1] != '\r') targetLineBegining += moveForward ? 1 : -1;

            int dis = cursor - myLineBegining;
            int result = targetLineBegining;
            for (int i = 0; i < dis; i++)
            {
                if (myInput[result] != '\r' && result + 1 < myInput.Length) result++;
            }

            if (IsSelecting && !shift) selectingBegin = -1;
            if (shift && !IsSelecting) selectingBegin = cursor;
            cursor = result;
            if (selectingBegin == cursor) selectingBegin = -1;
        }
        catch { }
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

    private int GetCursorLineNum(int index)
    {
        if (index >= myText.textInfo.characterInfo.Length) index = myText.textInfo.characterInfo.Length - 1;
        return myText.textInfo.characterInfo[index].lineNumber;
    }

    //改行文字を含むindex
    private float GetCursorX(int index)
    {
        //最初あるいは直前の文字と行が違う場合
        if (index <= 0 || (index < myText.textInfo.characterInfo.Count && myText.textInfo.characterInfo[index - 1].lineNumber != myText.textInfo.characterInfo[index].lineNumber))
            return myText.rectTransform.rect.min.x;
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

            str = Regex.Replace(str, "[<>]", "<noparse>$0</noparse>").Replace("\\EMK", "</mark>").Replace("\\BMK", "<mark=#5F74A5AA>").Replace("\r", "<br>");

            myText.text = str + " ";
        }
        else
        {
            myText.text = hint;
            cursor = 0;
        }
        myText.ForceMeshUpdate();

        int visualCursor = ConsiderComposition(cursor, compStr);
        int lineNum = GetCursorLineNum(visualCursor);
        myCursor.transform.localPosition = new(GetCursorX(visualCursor), myInput.Length == 0 ? 
            0f : myText.textInfo.lineInfo[lineNum].baseline - myCursor.textInfo.lineInfo[0].baseline, -1f);
        

        Vector2 compoPos = UnityHelper.WorldToScreenPoint(transform.position + new Vector3(GetCursorX(cursor), 0.15f, 0f), LayerExpansion.GetUILayer());
        compoPos.y = Screen.height - compoPos.y;
        Input.compositionCursorPos = compoPos;
    }

    public void SetHint(string hint)
    {
        this.hint = hint;
        if (myInput.Length == 0) UpdateTextMesh();
    }

    public void SetText(string text)
    {
        RemoveAll();
        InputText(text);
        ShowCursor();
        UpdateTextMesh();
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
    

    public void SetSize(Vector2 size,float fontSize,int maxLines = 1)
    {
        MaxLines = maxLines;
        GenerateAttribute(size, fontSize, AllowMultiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left).Reflect(myText);
        GenerateAttribute(new(0.3f, size.y), fontSize, AllowMultiLine ? TextAlignmentOptions.Top : TextAlignmentOptions.Center).Reflect(myCursor);
        myText.font = VanillaAsset.VersionFont;
        myCursor.font = VanillaAsset.VersionFont;

        UpdateTextMesh();
    }

    public void Awake()
    {
        allFields.Add(this);

        myText = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, transform);
        myCursor = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, transform);
        
        myText.sortingOrder = 15;
        myCursor.sortingOrder = 15;

        myText.transform.localPosition = new Vector3(0, 0, -1f);
        myCursor.transform.localPosition = new Vector3(0, 0, -1f);

        myText.outlineWidth = 0f;
        myCursor.outlineWidth = 0f;

        myText.text = "";
        myCursor.text = "|";
        myCursor.ForceMeshUpdate();

        dirtyFlag = true;

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

        if (lockedTime > 0f)
        {
            lockedTime -= Time.deltaTime;
            myCursor.gameObject.SetActive(false);
            return;
        }

        if (!AllowTab && Input.GetKeyDown(KeyCode.Tab)){
            ChangeField(!PressingShift);
            return;
        }

        if(!AllowMultiLine && Input.GetKeyDown(KeyCode.Return))
        {
            ChangeFocus(null);
            return;
        }

        bool requireUpdate = InputText(Input.inputString);
        if (dirtyFlag) { requireUpdate = true; dirtyFlag = false; }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveCursorLine(false, PressingShift);
            ShowCursor();
            requireUpdate = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveCursorLine(true, PressingShift);
            ShowCursor();
            requireUpdate = true;
        }
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
            while (cursor > 0 && myInput[cursor - 1] != '\r') cursor--;
            ShowCursor();
            requireUpdate = true;
        }
        if (Input.GetKeyDown(KeyCode.End))
        {
            if (!IsSelecting && PressingShift) selectingBegin = cursor;
            while (cursor < myInput.Length && myInput[cursor] != '\r') cursor++;
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
                InputText(Helpers.GetClipboardString());
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

    public void OnDisable()
    {
        if (validField == this) ChangeFocus(null);
    }

    public void OnDestroy()
    {
        allFields.Remove(this);
    }

    static private void ChangeFocus(TextField? field)
    {
        if (field == validField) return;
        if (validField != null) validField.LoseFocus();
        validField = field;
        field?.GetFocus();
    }

    private void LoseFocus() {
        LostFocusAction?.Invoke(myInput);
        Input.imeCompositionMode = IMECompositionMode.Off;
    }
    private void GetFocus() {
        Input.imeCompositionMode = IMECompositionMode.On;
        lockedTime = 0.1f;
        cursor = myInput.Length;
        UpdateTextMesh();
    }

    public void AsMaskedText()
    {
        myText.fontMaterial = VanillaAsset.StandardMaskedFontMaterial;
        myCursor.fontMaterial = VanillaAsset.StandardMaskedFontMaterial;
        myText.font = VanillaAsset.StandardTextPrefab.font;
        myCursor.font = VanillaAsset.StandardTextPrefab.font;
    }

    public bool IsValid => validField == this && validField;
    //有効になっても操作できない時間
    private float lockedTime = 0f;
    private bool dirtyFlag;
}