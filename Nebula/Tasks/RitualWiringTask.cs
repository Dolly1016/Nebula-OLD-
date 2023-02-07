using UnhollowerRuntimeLib;

namespace Nebula.Tasks;

[HarmonyPatch]
public class RitualWiringMinigamePatch
{
    [HarmonyPatch(typeof(WireMinigame), nameof(WireMinigame.Begin))]
    class BeginPatch
    {
        static public void Postfix(WireMinigame __instance, [HarmonyArgument(0)] PlayerTask task)
        {
            if (task.TaskType != TaskTypes.None) return;
            RitualWiringTask t = task.GetComponent<RitualWiringTask>();
            if (!t) return;


            int[] colorArray = { 0, 1, 2, 3 }, randomArray = Helpers.GetRandomArray(4);
            int rnd;
            for (int i = 0; i < 2; i++)
            {
                while (true)
                {
                    rnd = NebulaPlugin.rnd.Next(4);
                    if (rnd != randomArray[i]) break;
                }

                //既に置き換え先の色が存在していない場合
                if (Array.IndexOf(randomArray, rnd) < i)
                {
                    colorArray[randomArray[i]] = colorArray[rnd];
                }
                else
                {
                    colorArray[randomArray[i]] = rnd;
                }
            }

            for (int i = 0; i < __instance.LeftNodes.Length; i++)
            {
                int num = (int)__instance.ExpectedWires[i];
                __instance.LeftNodes[i].SetColor(WireMinigame.colors[colorArray[num]], __instance.Symbols[colorArray[num]]);
                __instance.RightNodes[i].SetColor(WireMinigame.colors[colorArray[i]], __instance.Symbols[colorArray[i]]);
            }

            return;
        }
    }


    [HarmonyPatch(typeof(WireMinigame), nameof(WireMinigame.CheckTask))]
    class CheckTaskPatch
    {
        static private IEnumerator GetEnumrator(SpriteRenderer consoleRenderer)
        {
            float a = 1f;
            while (true)
            {
                a -= Time.deltaTime * 0.7f;
                if (a < 0f) break;
                consoleRenderer.color = new Color(1f, 1f, 1f, a);
                yield return null;

            }
            GameObject.Destroy(consoleRenderer.gameObject);
            yield break;
        }

        static public bool Prefix(WireMinigame __instance)
        {
            if (__instance.MyNormTask.TaskType != TaskTypes.None) return true;
            RitualWiringTask t = __instance.MyNormTask.GetComponent<RitualWiringTask>();
            if (!t) return true;

            bool flag = true;
            for (int i = 0; i < __instance.ActualWires.Length; i++)
            {
                if (__instance.ActualWires[i] != __instance.ExpectedWires[i])
                {
                    flag = false;
                    break;
                }
            }
            if (flag)
            {
                //使用済みのコンソール
                int mask = ~(1 << t.ValidRooms.IndexOf(__instance.Console.Room));
                t.ExistingConsoles &= mask;

                __instance.Console.ConsoleId = 1;
                __instance.Console.TaskTypes = new UnhollowerBaseLib.Il2CppStructArray<TaskTypes>(0);
                HudManager.Instance.StartCoroutine(GetEnumrator(__instance.Console.GetComponent<SpriteRenderer>()).WrapToIl2Cpp());
                __instance.MyNormTask.NextStep();
                __instance.Close();
            }

            return false;
        }
    }
}

public class RitualWiringTask : NebulaPlayerTask
{
    public static Sprite[] TaskSprites = new Sprite[] { null, null, null };

    public static Sprite GetTaskSprite()
    {
        int rnd = NebulaPlugin.rnd.Next(3);
        if (TaskSprites[rnd]) return TaskSprites[rnd];
        TaskSprites[rnd] = Helpers.loadSpriteFromResources("Nebula.Resources.WiringRift" + rnd + ".png", 100f);
        return TaskSprites[rnd];
    }

    static RitualWiringTask()
    {
        ClassInjector.RegisterTypeInIl2Cpp<RitualWiringTask>();
    }

    public RitualWiringTask()
    {
        __Initialize();
    }

    public bool CanKnowAllRooms()
    {
        return PlayerControl.LocalPlayer.Data.Role.IsImpostor;
    }

    public override void __AppendTaskText(Il2CppSystem.Text.StringBuilder sb)
    {
        bool flag = false;
        if (this.IsComplete)
        {
            sb.Append("<color=#00DD00FF>");
            flag = true;
        }
        else if (NebulaData[2] > 0)
        {
            sb.Append("<color=#FFFF00FF>");
            flag = true;
        }
        else if (NebulaData[2] == NebulaData[1])
        {
            sb.Append("<color=#00DD00FF>");
            flag = true;
        }


        bool appendFlag = false;
        bool redFlag = false;
        for (int i = 0; i < ValidRooms.Count; i++)
        {
            if (!CanKnowAllRooms() && ShowRooms <= i) break;

            if (i != 0) sb.Append(", ");

            redFlag = false;
            if (ShowRooms <= i)
            {
                sb.Append("<color=#FF8080FF>");
                redFlag = true;
            }

            appendFlag = false;
            if (ValidRooms.Count > i + 1 && (i + 1 < ShowRooms || (CanKnowAllRooms() && ShowRooms != i + 1)))
            {

                if (((ValidRooms[i] == SystemTypes.UpperEngine && ValidRooms[i + 1] == SystemTypes.LowerEngine) ||
                (ValidRooms[i + 1] == SystemTypes.UpperEngine && ValidRooms[i] == SystemTypes.LowerEngine)))
                {
                    sb.Append(Language.Language.GetString("ritual.rooms.bothEngines"));
                    appendFlag = true;
                }
                else if (((ValidRooms[i] == SystemTypes.Decontamination2 && ValidRooms[i + 1] == SystemTypes.Decontamination3) ||
                (ValidRooms[i + 1] == SystemTypes.Decontamination2 && ValidRooms[i] == SystemTypes.Decontamination3)))
                {
                    sb.Append(Language.Language.GetString("ritual.rooms.bothDecontaminations"));
                    appendFlag = true;
                }
            }

            if (appendFlag) i++;
            else
            {
                sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(ValidRooms[i]));
            }

            if (redFlag) sb.Append("</color>");
        }

        if (ShowRooms < ValidRooms.Count && !CanKnowAllRooms())
        {
            if (ShowRooms != 0) sb.Append(", ");
            sb.Append("... ");
        }
        sb.Append(": ");

        sb.Append(Language.Language.GetString("ritual.mission.formularize"));

        if (NebulaData[2] > 0)
        {
            sb.Append(" (");
            sb.Append(NebulaData[2]);
            sb.Append("/");
            sb.Append(NebulaData[1]);
            sb.Append(")");
        }

        if (flag)
        {
            sb.Append("</color>");
        }
        sb.AppendLine();
    }

    public override bool __NextStep()
    {
        RPCEventInvoker.RitualUpdateTaskProgress((int)Id);

        if (NebulaData[2] < NebulaData[1] && Constants.ShouldPlaySfx())
        {
            SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskUpdateSound, false, 1f);
        }

        return false;
    }


    public override void __Initialize()
    {
        this.MinigamePrefab = Map.MapData.MapDatabase[0].Assets.CommonTasks[1].MinigamePrefab;
        this.Arrow = null;
        this.Length = NormalPlayerTask.TaskLength.Short;
        this.taskStep = 0;
        this.MaxStep = 1;
        this.ShowTaskStep = true;
        this.ShowTaskTimer = false;
        this.NebulaData = new byte[3];
        NebulaData[0] = 0;//固定(WireMinigameの制約)
        NebulaData[1] = 10;
        NebulaData[2] = 0;

        ExistingConsoles = 0;

        //特に使わない(NormalPlayerTask.FixedUpdateで使用している)
        this.arrowSuspended = false;

        this.StartAt = SystemTypes.Admin;

        //マップにアイコンを表示しない
        this.HasLocation = false;
        this.LocationDirty = false;

        ShowRooms = 0;
        ValidRooms = new List<SystemTypes>();
        NextLocations = new List<Vector2>();
    }

    public override bool __ValidConsole(Console console) { return console.ConsoleId == 0 && console.TaskTypes.Contains((TaskTypes)10000) && ValidRooms.Contains(console.Room); }

    public override bool __IsCompleted()
    {
        return NebulaData[1] <= NebulaData[2];
    }

    public void SetRooms(UnhollowerBaseLib.Il2CppStructArray<SystemTypes> rooms, int showRooms = 0)
    {
        ValidRooms = new List<SystemTypes>(rooms);
        ShowRooms = showRooms;
    }

    public void ShowdownRoom()
    {
        ShowRooms++;
    }

    public void SearchNextLocation()
    {
        NextLocations.Clear();
        foreach (SystemTypes s in ValidRooms)
        {
            if (!Map.MapData.GetCurrentMapData().RitualMissionPositions.ContainsKey(s))
            {
                NextLocations.Add(new Vector2());
                continue;
            }
            var list = Map.MapData.GetCurrentMapData().RitualMissionPositions[s];
            var pos = list[NebulaPlugin.rnd.Next(list.Count)];
            NextLocations.Add(pos.GetVector());
        }
    }

    //公開される対象部屋数
    int ShowRooms;
    //有効部屋
    public List<SystemTypes> ValidRooms;
    //次のミッション場所
    public List<Vector2> NextLocations;
    public int ExistingConsoles;
}
