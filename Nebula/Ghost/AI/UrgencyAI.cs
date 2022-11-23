namespace Nebula.Ghost.AI;

public class AI_UrgencyCommsForDeadBodies : GhostWeightedAI
{
    private int MaxBodies;
    public override void Update(Ghost ghost)
    {
        if (!ghost.SabotageMood.ContainsKey(SystemTypes.Comms)) return;

        float value = (float)Helpers.AllDeadBodies().Length / (float)MaxBodies;
        ghost.SabotageMood[SystemTypes.Comms] += (value > 1 ? 1f : value) * Weight;
    }

    public AI_UrgencyCommsForDeadBodies(uint priority, float weight, int maxBodies) : base(priority, weight)
    {
        MaxBodies = maxBodies;
    }
}

public class AI_UrgencyCommsForAdmin : GhostWeightedAI
{
    private int MaxPlayers;

    public override void Update(Ghost ghost)
    {
        if (!ghost.SabotageMood.ContainsKey(SystemTypes.Comms)) return;

        SystemTypes? room = GhostAI.GetValidType(SystemTypes.Admin, SystemTypes.Cockpit);

        if (room == null) return;

        int num = GhostAI.GetCountOfAlivePlayers(room.Value, MaxPlayers);
        ghost.SabotageMood[SystemTypes.Comms] += Weight * (float)num / (float)MaxPlayers;
    }

    public AI_UrgencyCommsForAdmin(uint priority, float weight, int maxPlayers) : base(priority, weight)
    {
        MaxPlayers = maxPlayers;
    }
}

public class AI_UrgencyCommsForVital : GhostWeightedAI
{
    private int MaxPlayers;
    public override void Update(Ghost ghost)
    {
        if (!ghost.SabotageMood.ContainsKey(SystemTypes.Comms)) return;

        SystemTypes? room = GhostAI.GetValidType(SystemTypes.Medical, SystemTypes.Office);

        if (room == null) return;

        int num = GhostAI.GetCountOfAlivePlayers(room.Value, MaxPlayers);
        ghost.SabotageMood[SystemTypes.Comms] += Weight * (float)num / (float)MaxPlayers;
    }

    public AI_UrgencyCommsForVital(uint priority, float weight, int maxPlayers) : base(priority, weight)
    {
        MaxPlayers = maxPlayers;
    }
}

public class AI_UrgencyCommsForCamera : GhostWeightedAI
{
    private int MaxPlayers;
    public override void Update(Ghost ghost)
    {
        if (!ghost.SabotageMood.ContainsKey(SystemTypes.Comms)) return;

        SystemTypes? room = GhostAI.GetValidType(SystemTypes.Security);

        if (room == null) return;

        int num = GhostAI.GetCountOfAlivePlayers(room.Value, MaxPlayers);
        ghost.SabotageMood[SystemTypes.Comms] += Weight * (float)num / (float)MaxPlayers;
    }

    public AI_UrgencyCommsForCamera(uint priority, float weight, int maxPlayers) : base(priority, weight)
    {
        MaxPlayers = maxPlayers;
    }
}