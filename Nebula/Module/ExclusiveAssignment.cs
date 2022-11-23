namespace Nebula.Module;

public class ExclusiveAssignment
{
    public List<Roles.Role> exclusiveRoles;

    public ExclusiveAssignment(params Roles.Role?[] roles)
    {
        exclusiveRoles = new List<Roles.Role>();
        foreach (var role in roles)
        {
            if (role == null) continue;

            exclusiveRoles.Add(role);
        }
    }

    public bool Exclusive(Patches.AssignRoles roles, Roles.Role role)
    {
        if (exclusiveRoles.Count == 0) return false;

        if (!exclusiveRoles.Contains(role)) return false;

        foreach (var ex in exclusiveRoles)
        {
            if (ex == role) continue;
            roles.FuzzyRemoveRole(ex);
        }
        return true;
    }
}