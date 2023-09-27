using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Modules;

public class HudGrid : MonoBehaviour
{
    static HudGrid()
    {
        ClassInjector.RegisterTypeInIl2Cpp<HudGrid>();
    }

    public List<Il2CppArgument<HudContent>>[] Contents = { new(), new() };
    private Transform ButtonsHolder = null!;

    public void Awake()
    {
        var buttonParent = HudManager.Instance.UseButton.transform.parent;
        buttonParent.localPosition= Vector3.zero;
        buttonParent.name = "Buttons";
        GameObject.Destroy(buttonParent.gameObject.GetComponent<GridArrange>());
        GameObject.Destroy(buttonParent.gameObject.GetComponent<AspectPosition>());
        ButtonsHolder = buttonParent;

        HudContent AddVanillaButtons(GameObject obj,int priority)
        {
            var content = obj.AddComponent<HudContent>();
            content.SetPriority(priority);
            RegisterContentToRight(content);

            return content;
        }

        AddVanillaButtons(HudManager.Instance.UseButton.gameObject,1000);
        AddVanillaButtons(HudManager.Instance.PetButton.gameObject,1000);
        AddVanillaButtons(HudManager.Instance.ImpostorVentButton.gameObject,998);
        AddVanillaButtons(HudManager.Instance.ReportButton.gameObject,999);
        AddVanillaButtons(HudManager.Instance.SabotageButton.gameObject, 997);
        AddVanillaButtons(HudManager.Instance.KillButton.gameObject, -1).MarkAsKillButtonContent();
        

        //ベントボタンにクールダウンテキストを設定
        HudManager.Instance.ImpostorVentButton.cooldownTimerText = GameObject.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, HudManager.Instance.ImpostorVentButton.transform);
    }

    public void LateUpdate()
    {
        for (int i = 0; i < 2; i++)
        {
            Contents[i].RemoveAll(c => !c.Value);

            Contents[i].Sort((c1, c2) => c2.Value.Priority - c1.Value.Priority);

            if (Contents[i].Count == 0) continue;

            bool killButtonPosArranged = false;

            int row = 0, column = 0;
            foreach(var c in Contents[i])
            {
                if (!c.Value.gameObject.activeSelf) continue;

                if(!killButtonPosArranged && c.Value.MarkedAsKillButtonContent)
                {
                    killButtonPosArranged = true;
                    c.Value.CurrentPos = new Vector2(0, 1);
                    continue;
                }

                c.Value.CurrentPos = new Vector2(column, row);

                if (column < 2 && !c.Value.OccupiesLine)
                    column++;
                else
                {
                    row++;
                    column = 0;
                    if (row == 1 && killButtonPosArranged) column = 1;
                }
            }
        }
    }

    public void RegisterContentToLeft(Il2CppArgument<HudContent> content) => RegisterContent(content, true);
    
    public void RegisterContentToRight(Il2CppArgument<HudContent> content) => RegisterContent(content, false);

    public void RegisterContent(Il2CppArgument<HudContent> content,bool toLeft)
    {
        Contents[toLeft ? 0 : 1].Add(content);
        content.Value.SetSide(toLeft);
    }
}

public class HudContent : MonoBehaviour
{
    static HudContent()
    {
        ClassInjector.RegisterTypeInIl2Cpp<HudContent>();
    }

    public Vector2 CurrentPos { get; set; }

    //Priorityの大きいものから配置される
    public int Priority { get => OccupiesLine ? 20000 : onKillButtonPos ? 10000 : priority; }
    private int priority;
    private bool onKillButtonPos;
    private bool isLeftSide;
    private bool isDirty = true;
    public bool OccupiesLine = false;
    public Vector3 ToLocalPos => new Vector3((4.5f - CurrentPos.x) * (isLeftSide ? -1 : 1), -2.3f + CurrentPos.y, 0f);
    public bool MarkedAsKillButtonContent => onKillButtonPos;
    public void MarkAsKillButtonContent(bool mark = true)
    {
        onKillButtonPos = mark;
    }
    public void SetPriority(int priority)
    {
        this.priority = priority;
        if (this.priority < 0) this.priority = 0;
    }
    public void SetSide(bool asLeftSide)
    {
        isLeftSide = asLeftSide;
    }

    public void OnDisable()
    {
        CurrentPos = new Vector2(-1,-1);
        isDirty = true;
    }

    public void Start()
    {
        CurrentPos = new Vector2(-1, -1);
    }

    public void LateUpdate()
    {
        if (CurrentPos.x < 0) return;
        if (isDirty)
        {
            transform.localPosition = ToLocalPos;
            isDirty = false;
        }
        else
        {
            var diff = ToLocalPos - transform.localPosition;
            transform.localPosition += diff * Time.deltaTime * 5.2f;
        }
    }

    static public HudContent InstantiateContent(string name, bool isLeftSide = true, bool occupiesLine = false,bool asKillButtonContent = false)
    {
        var obj = UnityHelper.CreateObject<HudContent>(name, HudManager.Instance.KillButton.transform.parent, Vector3.zero);
        obj.OccupiesLine= occupiesLine;
        obj.MarkAsKillButtonContent(asKillButtonContent);

        NebulaGameManager.Instance?.HudGrid.RegisterContent(obj,isLeftSide);
        return obj;
    }
}
