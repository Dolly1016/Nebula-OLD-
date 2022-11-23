namespace Nebula.Roles;

public class GhostRole : Assignable
{
    public byte id { get; private set; }

    //使用済みロールID
    static private byte maxId = 0;

    protected GhostRole(string name, string localizeName, Color color) :
       base(name, localizeName, color)
    {
        this.id = maxId;
        maxId++;
    }

    public virtual bool IsAssignableTo(Game.PlayerData player) => true;

    sealed public override void SetupRoleOptionData()
    {
        SetupRoleOptionData(Module.CustomOptionTab.GhostRoles);
    }

    static public void LoadAllOptionData()
    {
        foreach (GhostRole role in Roles.AllGhostRoles)
        {
            role.CreateRoleOption();
        }
    }

    public static GhostRole? GetRoleById(byte id)
    {
        if (id == Byte.MaxValue) return null;
        foreach (GhostRole role in Roles.AllGhostRoles) if (role.id == id) return role;
        return null;
    }
}