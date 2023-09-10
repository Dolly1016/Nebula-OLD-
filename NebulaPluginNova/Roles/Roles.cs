using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles;

[NebulaPreLoad(typeof(RemoteProcessBase),typeof(Team))]
public class Roles
{
    static public IReadOnlyList<AbstractRole> AllRoles { get; private set; }
    
    static public IReadOnlyList<AbstractModifier> AllModifiers { get; private set; }
    static public IEnumerable<IntroAssignableModifier> AllIntroAssignableModifiers()
    {
        foreach (var m in AllModifiers) if (m is IntroAssignableModifier iam) yield return iam;
    }

    static public IReadOnlyList<Team> AllTeams { get; private set; }

    static private List<AbstractRole>? allRoles = new();
    static private List<AbstractModifier>? allModifiers = new();
    static private List<Team>? allTeams = new();

    static public void Register(AbstractRole role) {
        allRoles?.Add(role);
    }
    static public void Register(AbstractModifier role)
    {
        allModifiers?.Add(role);
    }
    static public void Register(Team team) {
        allTeams?.Add(team);
    }
    static public IEnumerator CoLoad()
    {
        Patches.LoadPatch.LoadingText = "Building Roles Database";
        yield return null;

        var iroleType = typeof(AbstractRole);
        var types = Assembly.GetAssembly(typeof(AbstractRole))?.GetTypes().Where((type) => type.IsAssignableTo(typeof(IAssignableBase)) || type.IsDefined(typeof(NebulaRoleHoler)));
        if (types == null) yield break;

        foreach (var type in types)
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        

        allRoles!.Sort((role1, role2) => {
            int diff;
            
            diff = (int)role1.RoleCategory - (int)role2.RoleCategory;
            if (diff != 0) return diff;

            diff = (role1.IsDefaultRole ? -1 : 1) - (role2.IsDefaultRole ? -1 : 1);
            if (diff != 0) return diff;

            return role1.InternalName.CompareTo(role2.InternalName);
        });

        allModifiers!.Sort((role1, role2) => {
            return role1.InternalName.CompareTo(role2.InternalName);
        });

        allTeams!.Sort((team1, team2) => team1.TranslationKey.CompareTo(team2.TranslationKey));

        for (int i = 0; i < allRoles!.Count; i++) allRoles![i].Id = i; 
        for (int i = 0; i < allModifiers!.Count; i++) allModifiers![i].Id = i;

        AllRoles = allRoles!.AsReadOnly();
        AllModifiers = allModifiers!.AsReadOnly();
        AllTeams = allTeams!.AsReadOnly();

        foreach (var role in allRoles) role.Load();
        foreach (var modifier in allModifiers) modifier.Load();

        allRoles = null;
        allModifiers = null;
        allTeams = null;
    }
}
