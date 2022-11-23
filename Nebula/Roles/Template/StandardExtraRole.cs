namespace Nebula.Roles.Template;

public class StandardExtraRole : ExtraRole
{
    private List<Tuple<Module.CustomOption, Module.CustomOption>> detailChanceOption;
    private string[] categories = { "roles.category.default", "roles.category.crewmate", "roles.category.impostor", "roles.category.neutral", "roles.category.lover" };

    private bool CheckPlayerCondition(Game.PlayerData player, int conditionIndex)
    {
        switch (conditionIndex)
        {
            case 0:
                //Default
                return true;
            case 1:
                //Crewmate
                return player.role.category == RoleCategory.Crewmate;
            case 2:
                //Impostor
                return player.role.category == RoleCategory.Impostor;
            case 3:
                //Neutral
                return player.role.category == RoleCategory.Neutral;
            case 4:
                //Lover
                return player.HasExtraRole(Roles.Lover);
        }
        return false;
    }

    private bool CheckAndAssign(Patches.AssignMap assignMap, List<byte> playerArray, int conditionIndex)
    {
        for (int i = 0; i < playerArray.Count; i++)
        {
            if (CheckPlayerCondition(Game.GameData.data.playersArray[playerArray[i]], conditionIndex))
            {
                assignMap.AssignExtraRole(playerArray[i], this.id, 0);
                playerArray.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    protected virtual bool IsAssignableTo(Role role) => role.CanHaveExtraAssignable(this);

    public override void Assignment(Patches.AssignMap assignMap)
    {
        List<byte> playerArray = new List<byte>(Helpers.GetRandomArray(Game.GameData.data.AllPlayers.Keys));
        playerArray.RemoveAll((id) => { return !IsAssignableTo(Game.GameData.data.playersArray[id].role); });

        int leftPlayers = (int)RoleCountOption.getFloat();

        float probability = ((float)RoleChanceOption.getSelection() + 1f) / 10f;

        while (leftPlayers > 0)
        {
            if (playerArray.Count == 0) break;

            if (NebulaPlugin.rnd.NextDouble() < probability)
            {
                foreach (var condition in detailChanceOption)
                {
                    if (condition.Item1.getSelection() == 0 || NebulaPlugin.rnd.NextDouble() * 10f < condition.Item2.getSelection())
                    {
                        //割り当てられたらループをぬける
                        if (CheckAndAssign(assignMap, playerArray, condition.Item1.getSelection())) break;
                    }
                }

                if (RoleChanceSecondaryOption.getSelection() != 0)
                {
                    probability = (float)RoleChanceOption.getSelection() / 10f;
                }
            }
            leftPlayers--;
        }
    }

    public override void LoadOptionData()
    {
        for (int i = 0; i < 5; i++)
        {
            detailChanceOption.Add(new Tuple<Module.CustomOption, Module.CustomOption>(
                CreateOption(Color.white, "chanceCategory" + (i + 1), categories),
                CreateOption(Color.white, "chanceRate", CustomOptionHolder.rates)
                ));

            detailChanceOption[i].Item1.name = "role.extra.chanceCategory" + (i + 1);
            detailChanceOption[i].Item2.name = "role.extra.chanceRate";

            detailChanceOption[i].Item2.SetParent(detailChanceOption[i].Item1);
            if (i != 0) detailChanceOption[i].Item1.AddPrerequisite(detailChanceOption[i - 1].Item1);

        }
    }

    protected StandardExtraRole(string name, string localizeName, Color color, byte assignmentPriority) :
        base(name, localizeName, color, assignmentPriority)
    {
        detailChanceOption = new List<Tuple<Module.CustomOption, Module.CustomOption>>();
    }
}
