namespace Nebula.Roles.Template;

public class ExemptTasks : Role
{
    private Module.CustomOption exemptTasksOption;
    //初期設定でのタスク免除数
    protected int InitialExemptTasks { get; set; } = 1;
    //最高タスク免除数
    protected int MaxExemptTasks { get; set; } = 10;
    //オプションを使用しない場合
    protected int CustomExemptTasks { get; set; } = 1;
    //タスク免除オプションを使用するかどうか
    protected bool UseExemptTasksOption { get; set; } = true;


    public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
    {
        int exempt = UseExemptTasksOption ? (int)exemptTasksOption.getFloat() : CustomExemptTasks;
        int cutTasks = initialTasks.Count < exempt ? initialTasks.Count : exempt;

        for (int i = 0; i < cutTasks; i++)
        {
            if (initialTasks.Count == 0) break;
            initialTasks.RemoveAt(NebulaPlugin.rnd.Next(initialTasks.Count));
        }
    }

    public override void LoadOptionData()
    {
        if (UseExemptTasksOption) exemptTasksOption = CreateOption(Color.white, "exemptTasks", (float)InitialExemptTasks, 0f, (float)MaxExemptTasks, 1f);
    }

    //インポスターはModで操作するFakeTaskは所持していない
    protected ExemptTasks(string name, string localizeName, Color color, RoleCategory category,
        Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
        HashSet<Patches.EndCondition> winReasons,
        bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
        bool ignoreBlackout, bool useImpostorLightRadius) :
        base(name, localizeName, color, category,
            side, introMainDisplaySide, introDisplaySides, introInfluenceSides,
            winReasons,
            hasFakeTask, canUseVents, canMoveInVents,
            ignoreBlackout, useImpostorLightRadius)
    {
        UseExemptTasksOption = true;
    }
}