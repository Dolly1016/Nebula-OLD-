namespace Nebula.Roles.AllSideRoles;

public class Secret : AllSideRole, ExtraAssignable
{
    private int secretId;
    public override Role GetActualRole(Game.PlayerData player)
    {
        Role role;
        ParseActualRole(player, out role, out bool hasGuesser, out bool hasMadmate);
        return role;
    }

    public void ParseActualRole(Game.PlayerData player, out Role role, out bool hasGuesser, out bool hasMadmate)
    {
        int roleData = player.GetRoleData(secretId);
        int roleId = roleData & 0xFF;
        int exRoleId = roleData >> 8;

        role = Role.GetRoleById((byte)roleId);
        hasGuesser = (exRoleId & 0b01) != 0;
        hasMadmate = (exRoleId & 0b10) != 0;
    }

    public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
    {
        if (category == RoleCategory.Crewmate)
        {
            int num = (int)CustomOptionHolder.RequiredTasksForArousal.getFloat();
            actualTasks = new List<GameData.TaskInfo>();

            for (int i = 0; i < num; i++)
            {
                if (initialTasks.Count == 0) break;

                int min = GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumCommonTasks);
                if (initialTasks.Count <= min)
                {
                    min = 0;
                }

                int index = min + NebulaPlugin.rnd.Next(initialTasks.Count - min);
                actualTasks.Add(initialTasks[index]);
                initialTasks.RemoveAt(index);
            }

            GetActualRole(Game.GameData.data.myData.getGlobalData()).OnSetTasks(ref initialTasks, ref actualTasks);
        }
    }

    private void _sub_Assignment(Patches.AssignMap assignMap, PlayerControl player)
    {
        var data = Game.GameData.data.playersArray[player.PlayerId];

        int roleId = data.role.id;

        if (data.HasExtraRole(Roles.SecondaryGuesser))
        {
            roleId |= 1 << 8;
            assignMap.UnassignExtraRole(player.PlayerId, Roles.SecondaryGuesser.id);
        }
        if (data.HasExtraRole(Roles.SecondaryMadmate))
        {
            roleId |= 1 << 9;
            assignMap.UnassignExtraRole(player.PlayerId, Roles.SecondaryMadmate.id);
        }

        assignMap.AssignRole(player.PlayerId, this.id, secretId, roleId);
    }

    public void Assignment(Patches.AssignMap assignMap)
    {
        if (!CustomOptionHolder.SecretRoleOption.getBool()) return;

        List<PlayerControl> players = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
        players.RemoveAll(x => x.GetModData().role.category != category || !x.GetModData().role.CanHaveExtraAssignable(this));
        int max = 0;
        float chance = 1;

        if (category == RoleCategory.Crewmate)
        {
            max = CustomOptionHolder.NumOfSecretCrewmateOption.getSelection();
            chance = (float)CustomOptionHolder.ChanceOfSecretCrewmateOption.getSelection();
        }
        if (category == RoleCategory.Impostor)
        {
            max = CustomOptionHolder.NumOfSecretImpostorOption.getSelection();
            chance = (float)CustomOptionHolder.ChanceOfSecretImpostorOption.getSelection();
        }

        for (int i = 0; i < max; i++)
        {
            if (players.Count == 0) break;
            if (NebulaPlugin.rnd.NextDouble() * 10.0 > chance && chance < 10.0) continue;

            int rnd = NebulaPlugin.rnd.Next(players.Count);
            _sub_Assignment(assignMap, players[rnd]);
            players.RemoveAt(rnd);
        }
    }

    public byte assignmentPriority { get => 128; }

    public override bool HasInfiniteCrewTaskQuota(byte playerId)
    {
        return true;
    }

    public override bool HasExecutableFakeTask(byte playerId)
    {
        return side == Side.Crewmate;
    }

    private void RevealRole()
    {
        //役職を元に戻す
        ParseActualRole(PlayerControl.LocalPlayer.GetModData(), out Role role, out bool hasGuesser, out bool hasMadmate);
        List<Tuple<Tuple<ExtraRole, ulong>, bool>> exRoles = new List<Tuple<Tuple<ExtraRole, ulong>, bool>>();
        if (hasGuesser) exRoles.Add(new Tuple<Tuple<ExtraRole, ulong>, bool>(new Tuple<ExtraRole, ulong>(Roles.SecondaryGuesser, (ulong)Roles.F_Guesser.guesserShots.getFloat()), true));
        if (hasMadmate) exRoles.Add(new Tuple<Tuple<ExtraRole, ulong>, bool>(new Tuple<ExtraRole, ulong>(Roles.SecondaryMadmate, (ulong)0), true));

        RPCEventInvoker.ImmediatelyChangeRole(PlayerControl.LocalPlayer, role, exRoles.ToArray());
    }

    public override void OnTaskComplete(PlayerTask? task)
    {
        var taskData = Game.GameData.data.myData.getGlobalData().Tasks;

        if (taskData.Completed >= taskData.Quota)
        {
            RevealRole();
        }
    }

    public override void OnKillPlayer(byte targetId)
    {
        if (category == RoleCategory.Impostor)
        {
            int num = (int)CustomOptionHolder.RequiredNumOfKillingForArousal.getFloat();
            var data = Game.GameData.data.myData.getGlobalData();
            if (data.Tasks == null) data.Tasks = new Game.TaskData(num, num, num, false, false);
            data.Tasks.Completed++;

            if (data.Tasks.Completed >= data.Tasks.Quota)
            {
                RevealRole();
                data.Tasks = null;
            }
        }
    }

    public override void FinalizeInGame(PlayerControl __instance)
    {
        if (category == RoleCategory.Crewmate) RPCEventInvoker.ChangeTasks(Game.GameData.data.myData.InitialTasks, true);
    }

    public override void EditDisplayRoleName(byte playerId, ref string roleName, bool isIntro)
    {
        if (Game.GameData.data.myData.CanSeeEveryoneInfo) EditDisplayRoleNameForcely(playerId, ref roleName);
    }

    public override void EditDisplayRoleNameForcely(byte playerId, ref string roleName)
    {
        int roleData = Game.GameData.data.GetPlayerData(playerId).GetRoleData(secretId);
        int roleId = roleData & 0xFF;
        int exRoleId = roleData >> 8;

        var role = Role.GetRoleById((byte)roleId);
        string shortText = Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".short"));
        roleName += Helpers.cs(new Color(0.6f, 0.6f, 0.6f), $"({shortText})");
    }

    public override void SpawnableTest(ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
    {
        return;
    }

    public Module.CustomOption? RegisterAssignableOption(Role role)
    {
        if (role.category != category && role.category != RoleCategory.Complex) return null;

        Module.CustomOption option = role.CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeSecret" + (role.category == RoleCategory.Complex ? (category == RoleCategory.Crewmate ? "Crewmate" : "Impostor") : ""), role.DefaultExtraAssignableFlag(this), true).HiddenOnDisplay(true)
            .SetIdentifier("role." + role.LocalizeName + ".canBeSecret" + (role.category == RoleCategory.Complex ? (category == RoleCategory.Crewmate ? "Crewmate" : "Impostor") : ""));
        option.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
        if (role.category == RoleCategory.Crewmate)
            option.AddCustomPrerequisite(() => { return CustomOptionHolder.NumOfSecretCrewmateOption.getSelection() >= 1; });
        else if (role.category == RoleCategory.Impostor)
            option.AddCustomPrerequisite(() => { return CustomOptionHolder.NumOfSecretImpostorOption.getSelection() >= 1; });
        return option;
    }

    public void EditSpawnableRoleShower(ref string suffix, Role role)
    {

    }

    public Secret(Role templateRole)
        : base(templateRole, "Secret", "secret", templateRole.Color,
             true, templateRole.VentPermission, templateRole.CanMoveInVents, templateRole.IgnoreBlackout, templateRole.UseImpostorLightRadius)
    {
        IsHideRole = true;
        IsGuessableRole = false;

        Roles.AllExtraAssignable.Add(this);

        secretId = Game.GameData.RegisterRoleDataId("secret.role");
    }
}