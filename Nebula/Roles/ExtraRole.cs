namespace Nebula.Roles;

public class ExtraRole : Assignable, ExtraAssignable
{
    public byte id { get; private set; }

    //使用済みロールID
    static private byte maxId = 0;

    public virtual void Assignment(Patches.AssignMap assignMap) { }

    public byte assignmentPriority { get; protected set; }

    /*--------------------------------------------------------------------------------------*/
    /*--------------------------------------------------------------------------------------*/

    /// <summary>
    /// 正常にロールデータが更新された際に呼び出されます。
    /// </summary>
    [RoleLocalMethod]
    public virtual void OnUpdateRoleData(ulong newData) { }

    /// <summary>
    /// ロールが外されるときに呼び出されます。
    /// </summary>
    /// <param name="playerId"></param>
    [RoleGlobalMethod]
    public virtual void OnUnset(byte playerId) { }

    protected ExtraRole(string name, string localizeName, Color color, byte assignmentPriority) :
        base(name, localizeName, color)
    {
        this.id = maxId;
        maxId++;

        this.assignmentPriority = assignmentPriority;

        Roles.AllExtraAssignable.Add(this);
    }

    public static ExtraRole GetRoleById(byte id)
    {
        foreach (ExtraRole role in Roles.AllExtraRoles) if (role.id == id) return role;
        return null;
    }

    static public void LoadAllOptionData()
    {
        foreach (ExtraRole role in Roles.AllExtraRoles)
        {
            if (!role.CreateOptionFollowingRelatedRole)
                role.CreateRoleOption();
        }
    }

    /// <summary>
    /// ロールを設定します。
    /// </summary>
    /// <param name="player"></param>
    /// <param name="initializeValue"></param>
    [RoleGlobalMethod]
    public virtual void Setup(Game.PlayerData player)
    {
    }

    /// <summary>
    /// ゲーム開始時の説明文を編集します。
    /// </summary>
    /// <param name="desctiption"></param>
    /// <returns></returns>
    [RoleLocalMethod]
    public virtual void EditDescriptionString(ref string description)
    {
    }

    public virtual void EditSpawnableRoleShower(ref string roleName, Role role) { }

    sealed public override void SetupRoleOptionData()
    {
        SetupRoleOptionData(Module.CustomOptionTab.Modifiers);
    }

    public virtual Module.CustomOption? RegisterAssignableOption(Role role) => null;
}