﻿using BepInEx.IL2CPP.Utils;

namespace Nebula.Expansion;

[HarmonyPatch]
static public class GridArrangeExpansion
{
    static public List<GameObject> leftSideContents = new List<GameObject>();
    static public List<GameObject> bottomContents = new List<GameObject>();
    static public List<GameObject> lineContents = new List<GameObject>();
    static public GameObject AlternativeKillButtonContent;

    [Flags]
    public enum GridArrangeParameter
    {
        None = 0x00,
        BottomContent = 0x01,
        LeftSideContent = 0x02,
        OccupyingLineContent = 0x04,
        AlternativeKillButtonContent = 0x08
    }

    static public void AddGridArrangeContent(GameObject obj, GridArrangeParameter param)
    {
        if (obj.transform.parent != HudManager.Instance.UseButton.transform.parent)
            obj.transform.SetParent(HudManager.Instance.UseButton.transform.parent);

        if ((int)(param & GridArrangeParameter.BottomContent) != 0)
            bottomContents.Add(obj);
        if ((int)(param & GridArrangeParameter.LeftSideContent) != 0)
            leftSideContents.Add(obj);
        if ((int)(param & GridArrangeParameter.OccupyingLineContent) != 0)
            lineContents.Add(obj);
        if ((int)(param & GridArrangeParameter.AlternativeKillButtonContent) != 0)
            AlternativeKillButtonContent = obj;
    }

    static public void RemoveGridArrangeContent(GameObject obj)
    {
        bottomContents.Remove(obj);
        leftSideContents.Remove(obj);
        lineContents.Remove(obj);

        if (obj == AlternativeKillButtonContent)
            AlternativeKillButtonContent = HudManager.Instance.KillButton.gameObject;
    }

    static public void OnStartGame()
    {
        bottomContents.Clear();
        leftSideContents.Clear();
        lineContents.Clear();
        AlternativeKillButtonContent = HudManager.Instance.KillButton.gameObject;
    }

    [HarmonyPatch(typeof(GridArrange), nameof(GridArrange.CheckCurrentChildren))]
    class CheckCurrentChildrenPatch
    {
        static IEnumerator GetEnumerator(GameObject obj,Vector3 dest)
        {
            float t = 0f;
            Vector3 pos = obj.transform.localPosition;
            while (t < 1f && obj)
            {
                float p = (1 - t) * (1 - t);
                obj.transform.localPosition = dest * (1 - p) + pos * p;
                t += Time.deltaTime * 1.4f;
                yield return null;
            }
            if(obj)obj.transform.localPosition = dest;
        }

        static public bool Prefix(GridArrange __instance)
        {
            __instance.GetChildsActive();

            if (__instance.cells.Count == GridArrange.currentChildren.Count)
            {
                bool flag = true;
                for (int i = 0; i < __instance.cells.Count; i++)
                {
                    if (__instance.cells[i] != GridArrange.currentChildren[i])
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag) return false;
            }

            __instance.StopAllCoroutines();

            //直前に存在していたボタン
            List<int> lastContents = new List<int>();
            foreach(var t in __instance.cells) if(t) lastContents.Add(t.gameObject.GetInstanceID());
            
            //cellsを更新
            __instance.cells.Clear();
            foreach (Transform transform in GridArrange.currentChildren)
            {
                __instance.cells.Add(transform);
            }


            Vector2 leftVec = new Vector2(0f, 0f), rightVec = new Vector2(0f, 0f);
            int nextBottomLX = 0, nextBottomRX = 0;
            foreach (var c in __instance.cells)
            {
                GameObject obj = c.gameObject;
                if (c.gameObject == AlternativeKillButtonContent) continue;

                bool isLeftSide = leftSideContents.Any((c) => c.GetInstanceID() == obj.GetInstanceID());
                bool isBottom = false;
                if ((int)((isLeftSide ? leftVec : rightVec).y) == 0 || bottomContents.Any((c) => c.GetInstanceID() == obj.GetInstanceID()))
                    isBottom = true;
                NebulaPlugin.Instance.Logger.Print(c.gameObject.name+isLeftSide.ToString());
                int x = 0, y = 0;
                if (isLeftSide)
                {
                    x = isBottom ? nextBottomLX : (int)leftVec.x;
                    y = isBottom ? 0 : (int)leftVec.y;
                }
                else
                {
                    x = isBottom ? nextBottomRX : (int)rightVec.x;
                    y = isBottom ? 0 : (int)rightVec.y;
                }

                float posX = (float)x * __instance.CellSize.x;
                float posY = (float)y * __instance.CellSize.y;

                if (isLeftSide)
                    posX -= __instance.transform.localPosition.x * 2f;
                else
                    posX *= -1f;

                if (Helpers.ShowButtons)
                {
                    if (!lastContents.Contains(c.gameObject.GetInstanceID()))
                        c.localPosition = new Vector3(posX, posY, c.localPosition.z);
                    else
                        __instance.StartCoroutine(GetEnumerator(c.gameObject, new Vector3(posX, posY, c.position.z)).WrapToIl2Cpp());
                }

                if (y == (float)((isLeftSide ? leftVec : rightVec).y))
                {
                    if (isLeftSide)
                    {
                        leftVec.x += lineContents.Any((c)=>c.GetInstanceID()==obj.GetInstanceID()) ? __instance.MaxColumns : 1f;
                        if ((int)leftVec.x >= __instance.MaxColumns)
                        {
                            leftVec.x = 0f;
                            leftVec.y += 1f;
                        }
                    }
                    else
                    {
                        rightVec.x += lineContents.Any((c) => c.GetInstanceID() == obj.GetInstanceID()) ? __instance.MaxColumns : 1f;
                        if ((int)rightVec.x >= __instance.MaxColumns)
                        {
                            rightVec.x = 0f;
                            rightVec.y += 1f;
                        }
                    }
                }
                if (isBottom)
                {
                    if (isLeftSide)
                        nextBottomLX++;
                    else
                        nextBottomRX++;
                }

                if ((int)rightVec.y == 1 && (int)rightVec.x == 0 && AlternativeKillButtonContent && AlternativeKillButtonContent.gameObject.active)
                    rightVec.x++;
            }

            if (AlternativeKillButtonContent)
            {
                AlternativeKillButtonContent.transform.localPosition = new Vector3(0f, __instance.CellSize.y, AlternativeKillButtonContent.transform.localPosition.z);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(GridArrange), nameof(GridArrange.ArrangeChilds))]
    class ArrangeChildsPatch
    {
        static public bool Prefix(GridArrange __instance)
        {
            return false;
        }
    }
}

