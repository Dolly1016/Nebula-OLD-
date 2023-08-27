using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Assignment;

public class RoleAssignChance
{
    public AbstractRole Role;
    public float Probability;

    public RoleAssignChance(AbstractRole role, float probability)
    {
        this.Role = role;
        this.Probability = probability;
    }
}


public interface IRoleAllocator
{
    void Assign(List<PlayerControl> impostors, List<PlayerControl> others);

    static protected void SetRole(PlayerControl player,AbstractRole role, int[]? arguments = null)
    {
        PlayerModInfo.RpcSetRole.Invoke(new PlayerModInfo.SetRoleMessage() { roleId = role.Id, playerId = player.PlayerId, arguments = arguments });
    }
}

public class AllCrewmateRoleAllocator : IRoleAllocator
{
    public void Assign(List<PlayerControl> impostors, List<PlayerControl> others)
    {
        foreach (var p in impostors) IRoleAllocator.SetRole(p,Crewmate.Crewmate.MyRole);
        foreach (var p in others) IRoleAllocator.SetRole(p, Crewmate.Crewmate.MyRole);
    }
}

public class StandardRoleAllocator : IRoleAllocator
{
    
    private void CategoryAssign(RoleCategory category,int left,List<PlayerControl> main, List<PlayerControl> others)
    {
        if (left < 0) left = 15;

        List<RoleAssignChance> rolePool = new(), preferentialPool = new();
        foreach (var r in Roles.AllRoles)
        {
            if (r.RoleCategory != category) continue;
            for (int i = 0; i < r.RoleCount; i++)
            {
                float prob = r.GetRoleChance(i);
                if (prob < 100f)
                    rolePool.Add(new(r, prob));
                else
                    preferentialPool.Add(new(r, 100f));
            }
        }

        List<RoleAssignChance> currentPool = preferentialPool;
        while (main.Count > 0 && left > 0)
        {
            currentPool.RemoveAll(c =>
            {
                int cost = c.Role.AdditionalRole?.Length ?? 0;
                if (c.Role.HasAdditionalRoleOccupancy && cost > left + 1) return true;
                if (main == others || c.Role.HasAdditionalRoleOccupancy) cost++;
                return cost > (c.Role.HasAdditionalRoleOccupancy ? main.Count : others.Count);
            });
            if (currentPool.Count == 0)
            {
                if (currentPool == rolePool) break;
                currentPool = rolePool;
                continue;
            }


            float sum = currentPool.Sum(c => c.Probability);
            float val = System.Random.Shared.NextSingle() * sum;
            RoleAssignChance? selected = null;
            foreach (var r in currentPool)
            {
                val -= r.Probability;
                if (val < 0)
                {
                    selected = r;
                    break;
                }
            }
            selected ??= currentPool[currentPool.Count - 1];

            IRoleAllocator.SetRole(main[0], selected!.Role);
            left--;
            main.RemoveAt(0);

            if (selected.Role.AdditionalRole != null)
            {
                var playerList = selected.Role.HasAdditionalRoleOccupancy ? main : others;
                foreach (var r in selected.Role.AdditionalRole)
                {
                    IRoleAllocator.SetRole(playerList[0], r);
                    if (selected.Role.HasAdditionalRoleOccupancy) left--;
                    playerList.RemoveAt(0);
                }
            }

            currentPool.Remove(selected!);
        }


    }

    public void Assign(List<PlayerControl> impostors, List<PlayerControl> others)
    {
        CategoryAssign(RoleCategory.ImpostorRole, GeneralConfigurations.AssignmentImpostorOption.GetMappedInt().Value, impostors, others);
        CategoryAssign(RoleCategory.NeutralRole, GeneralConfigurations.AssignmentNeutralOption.GetMappedInt().Value, others, others);
        CategoryAssign(RoleCategory.CrewmateRole, GeneralConfigurations.AssignmentCrewmateOption.GetMappedInt().Value, others, others);

        foreach (var p in impostors) IRoleAllocator.SetRole(p, Impostor.Impostor.MyRole);
        foreach (var p in others) IRoleAllocator.SetRole(p, Crewmate.Crewmate.MyRole);
    }
}