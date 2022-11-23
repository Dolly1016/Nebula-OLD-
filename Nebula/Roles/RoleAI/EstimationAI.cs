namespace Nebula.Roles.RoleAI;

public class EstimationAI
{
    /// <summary>
    /// 全体での推測
    /// </summary>
    private Dictionary<Role, float> EstimateMap { get; }
    /// <summary>
    /// 個人ごとの推測
    /// </summary>
    private Dictionary<byte, Dictionary<Role, float>> DivEstimateMap { get; }

    /// <summary>
    /// 人数の最高予測
    /// </summary>
    private Dictionary<RoleCategory, int> CountEstimateMap { get; }

    private HashSet<Role[]> MultipleEstimates { get; }

    public EstimationAI()
    {
        EstimateMap = new Dictionary<Role, float>();
        foreach (Role role in Roles.AllRoles)
            if (role.category != RoleCategory.Complex)
                EstimateMap[role] = 0f;

        DivEstimateMap = new Dictionary<byte, Dictionary<Role, float>>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            DivEstimateMap[player.PlayerId] = new Dictionary<Role, float>();
        }

        CountEstimateMap = new Dictionary<RoleCategory, int>();
        CountEstimateMap[RoleCategory.Impostor] = 0;
        CountEstimateMap[RoleCategory.Crewmate] = 0;
        CountEstimateMap[RoleCategory.Neutral] = 0;

        MultipleEstimates = new HashSet<Role[]>();
    }

    private void InitialEliminate(RoleCategory category, ref Dictionary<Role, int> DefinitiveRoles, ref HashSet<Role> SpawnableRoles)
    {
        int count = 0;
        foreach (var entry in DefinitiveRoles)
        {
            if (entry.Key.category == category)
                count += entry.Value;
        }

        HashSet<Role> keySet = new HashSet<Role>(EstimateMap.Keys);
        bool eliminateFlag = false;
        foreach (Role key in keySet)
        {
            if (key.category != category) continue;

            eliminateFlag = false;

            if ((!DefinitiveRoles.ContainsKey(key)) && (!SpawnableRoles.Contains(key)))
                eliminateFlag = true;
            else if (CountEstimateMap[category] <= count && (!DefinitiveRoles.ContainsKey(key)))
                eliminateFlag = true;

            if (eliminateFlag)
                EstimateMap[key] = -1f;
        }
    }

    /// <summary>
    /// 出現しえない役職を排除する
    /// </summary>
    public void Initialize()
    {
        //配役人数を調べる
        foreach (var player in Game.GameData.data.AllPlayers)
            if (CountEstimateMap.ContainsKey(player.Value.role.category))
                CountEstimateMap[player.Value.role.category]++;

        //配役人数の都合で素インポスター、素クルーメイトが存在するかどうか調べる
        if (CountEstimateMap[RoleCategory.Impostor] > (int)CustomOptionHolder.impostorRolesCountMax.getFloat())
        {
            EstimateMap[Roles.Impostor] = 1f;
        }

        if (CountEstimateMap[RoleCategory.Crewmate] > (int)CustomOptionHolder.crewmateRolesCountMax.getFloat())
        {
            EstimateMap[Roles.Crewmate] = 1f;
        }

        Dictionary<Role, int> DefinitiveRoles = new Dictionary<Role, int>();
        HashSet<Role> SpawnableRoles = new HashSet<Role>();
        foreach (Role role in Roles.AllRoles)
        {
            role.SpawnableTest(ref DefinitiveRoles, ref SpawnableRoles);
        }

        InitialEliminate(RoleCategory.Crewmate, ref DefinitiveRoles, ref SpawnableRoles);
        InitialEliminate(RoleCategory.Impostor, ref DefinitiveRoles, ref SpawnableRoles);
        InitialEliminate(RoleCategory.Neutral, ref DefinitiveRoles, ref SpawnableRoles);

        Eliminate(Roles.VOID);
    }

    public float GetRoleProbability(Role role)
    {
        if (!EstimateMap.ContainsKey(role)) return -1f;

        return EstimateMap[role];
    }

    /// <summary>
    /// 全体としてロールの出現を除外する
    /// </summary>
    /// <param name="role"></param>
    public void Eliminate(Role role)
    {
        EstimateMap[role] = -1f;

        //人数をもとに弾ける役職がないか調べる
        Elimination(role.category);
    }

    /// <summary>
    /// 全体としてロールの出現を断定する
    /// </summary>
    /// <param name="role"></param>
    public void Determine(Role role)
    {
        EstimateMap[role] = 1f;

        //人数をもとに弾ける役職がないか調べる
        Elimination(role.category);
    }

    public void DetermineMultiply(params Role[] roles)
    {
        MultipleEstimates.Add(roles);

        //弾ける役職がないか調べる
        EliminationByMultipleSet();
    }

    //0.5のロールが存在する場合、確定役職数+1で人数条件をみたすなら確定させる
    public void Presume(Role role, float probability)
    {
        if (EstimateMap.ContainsKey(role)) return;

        if (EstimateMap[role] < probability)
        {
            EstimateMap[role] = probability;
        }
    }

    /// <summary>
    /// 個人のロールの出現を推定する
    /// </summary>
    /// <param name="target"></param>
    /// <param name="role"></param>
    /// <param name="probability"></param>
    public void DivPresume(PlayerControl target, Role role, float probability)
    {
        DivEstimateMap[target.PlayerId][role] = probability;
    }

    private void Elimination(RoleCategory category)
    {
        if (!CountEstimateMap.ContainsKey(category)) return;

        if (GetRoleList(category, false, true).Count >= CountEstimateMap[category])
        {
            HashSet<Role> keySet = new HashSet<Role>(EstimateMap.Keys);
            foreach (Role key in keySet)
            {
                if (key.category == category)
                {
                    if (EstimateMap[key] < 1f)
                        EstimateMap[key] = -1f;
                }
            }
        }

        EliminationByMultipleSet();
    }

    private void EliminationByMultipleSet()
    {
        bool result;

        do
        {
            result = false;

            foreach (var roles in MultipleEstimates)
            {
                Role lastRole = null;
                foreach (var role in roles)
                {
                    if (GetRoleProbability(role) < 0f) continue;

                    if (role != null) { lastRole = null; break; }

                    lastRole = role;
                }

                if (lastRole == null) continue;

                //確定させる
                EstimateMap[lastRole] = 1f;

                result = true;
            }

            //不要な条件は捨てる
            MultipleEstimates.RemoveWhere((roles) =>
            {
                int counter = 0;
                foreach (var role in roles)
                {
                    if (GetRoleProbability(role) < 0f) continue;

                    counter++;
                }
                return counter <= 1;
            });
        } while (result);
    }

    /// <summary>
    /// 該当するRoleを返す
    /// </summary>
    /// <param name="category"></param>
    /// <param name="containLikelyRoles"></param>
    /// <returns></returns>
    public List<Role> GetRoleList(RoleCategory? category, bool containLikelyRoles = true, bool containIdentifiedRoles = false)
    {
        List<Role> result = new List<Role>();
        foreach (var entry in EstimateMap)
        {
            if (category != null && entry.Key.category != category) continue;

            if (entry.Value < (containLikelyRoles ? 0f : 1f)) continue;

            if (entry.Value > 1f && !containIdentifiedRoles) continue;

            result.Add(entry.Key.oracleRole);
        }

        return result;
    }

    /// <summary>
    /// 該当するRoleを返す
    /// </summary>
    /// <param name="category">占い先カテゴリ</param>
    /// <param name="containLikelyRoles"></param>
    /// <returns></returns>
    public List<Role> GetOracleRoleList(RoleCategory? category, bool containLikelyRoles = true, bool containIdentifiedRoles = false)
    {
        List<Role> result = new List<Role>();
        foreach (var entry in EstimateMap)
        {
            if (category != null && entry.Key.oracleCategory != category) continue;

            if (entry.Value < (containLikelyRoles ? 0f : 1f)) continue;

            if (entry.Value > 1f && !containIdentifiedRoles) continue;

            result.Add(entry.Key);
        }

        return result;
    }
}
