using Il2CppSystem.Text.Json;
using Nebula.Configuration;
using Nebula.Modules;
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


public abstract class IRoleAllocator
{
    public class RoleTable
    {
        public List<(AbstractRole role, int[] arguments, byte playerId)> roles = new();
        public List<(AbstractModifier modifier, int[] arguments, byte playerId)> modifiers = new();

        public void SetRole(PlayerControl player, AbstractRole role, int[]? arguments = null)
        {
            roles.Add(new(role, arguments ?? Array.Empty<int>(), player.PlayerId));
        }

        public void SetModifier(PlayerControl player, AbstractModifier role, int[]? arguments = null)
        {
            modifiers.Add(new(role, arguments ?? Array.Empty<int>(), player.PlayerId));
        }

        public void Determine()
        {
            List<NebulaRPCInvoker> allInvokers = new();
            foreach (var role in roles) allInvokers.Add(PlayerModInfo.RpcSetAssignable.GetInvoker((role.playerId, role.role.Id, role.arguments, true )));
            foreach (var modifier in modifiers) allInvokers.Add(PlayerModInfo.RpcSetAssignable.GetInvoker((modifier.playerId, modifier.modifier.Id, modifier.arguments, false)));

            allInvokers.Add(NebulaGameManager.RpcStartGame.GetInvoker());

            CombinedRemoteProcess.CombinedRPC.Invoke(allInvokers.ToArray());
        }
    }

    public abstract void Assign(List<PlayerControl> impostors, List<PlayerControl> others);
}

public class FreePlayRoleAllocator : IRoleAllocator
{
    public override void Assign(List<PlayerControl> impostors, List<PlayerControl> others)
    {
        RoleTable table = new();

        foreach (var p in impostors) table.SetRole(p,Crewmate.Crewmate.MyRole);
        foreach (var p in others) table.SetRole(p, Crewmate.Crewmate.MyRole);

        foreach (var p in PlayerControl.AllPlayerControls) table.SetModifier(p, Modifier.MetaRole.MyRole);

        table.Determine();
    }
}

public class StandardRoleAllocator : IRoleAllocator
{
    
    private void CategoryAssign(RoleTable table, RoleCategory category,int left,List<PlayerControl> main, List<PlayerControl> others)
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

            table.SetRole(main[0], selected!.Role);
            left--;
            main.RemoveAt(0);

            if (selected.Role.AdditionalRole != null)
            {
                var playerList = selected.Role.HasAdditionalRoleOccupancy ? main : others;
                foreach (var r in selected.Role.AdditionalRole)
                {
                    table.SetRole(playerList[0], r);
                    if (selected.Role.HasAdditionalRoleOccupancy) left--;
                    playerList.RemoveAt(0);
                }
            }

            currentPool.Remove(selected!);
        }


    }

    public override void Assign(List<PlayerControl> impostors, List<PlayerControl> others)
    {
        RoleTable table = new();

        CategoryAssign(table, RoleCategory.ImpostorRole, GeneralConfigurations.AssignmentImpostorOption.GetMappedInt().Value, impostors, others);
        CategoryAssign(table, RoleCategory.NeutralRole, GeneralConfigurations.AssignmentNeutralOption.GetMappedInt().Value, others, others);
        CategoryAssign(table, RoleCategory.CrewmateRole, GeneralConfigurations.AssignmentCrewmateOption.GetMappedInt().Value, others, others);

        foreach (var p in impostors) table.SetRole(p, Impostor.Impostor.MyRole);
        foreach (var p in others) table.SetRole(p, Crewmate.Crewmate.MyRole);

        table.Determine();
    }
}