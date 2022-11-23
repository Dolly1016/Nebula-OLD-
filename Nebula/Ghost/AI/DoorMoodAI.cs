namespace Nebula.Ghost.AI;

public class AI_PlayerDoorMood : GhostWeightedAI
{
    private int MaxPlayer;

    public override void Update(Ghost ghost)
    {
        foreach (var room in ghost.DoorKeys)
        {
            ghost.DoorMood[room] += Weight * (float)GhostAI.GetCountOfAlivePlayers(room, MaxPlayer) / (float)MaxPlayer;
        }
    }

    public AI_PlayerDoorMood(uint priority, float weight, int maxPlayer) : base(priority, weight)
    {
        MaxPlayer = maxPlayer;
    }
}

public class AI_HideDeadBodyDoorMood : GhostWeightedAI
{
    private int MaxBodies;

    public override void Update(Ghost ghost)
    {
        foreach (var room in ghost.DoorKeys)
        {
            ghost.DoorMood[room] += Weight * (float)GhostAI.GetCountOfDeadBodies(room, MaxBodies) / (float)MaxBodies;
        }
    }

    public AI_HideDeadBodyDoorMood(uint priority, float weight, int maxBodies) : base(priority, weight)
    {
        MaxBodies = maxBodies;
    }
}

public class AI_RandomDoorMoodWithoutSkeld : AI_WhiteNoise
{
    public override void Update(Ghost ghost)
    {
        if (Map.MapData.GetCurrentMapData().DoorHackingCanBlockSabotage) return;

        base.Update(ghost);
    }

    public AI_RandomDoorMoodWithoutSkeld(uint priority, float weight) : base(priority, weight, NoiseDestination.DoorMood, false)
    {
    }
}