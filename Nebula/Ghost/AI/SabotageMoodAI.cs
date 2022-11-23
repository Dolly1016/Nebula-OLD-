namespace Nebula.Ghost.AI;

public class AI_FarthestSabotageForPlayers : GhostWeightedAI
{
    public override void Update(Ghost ghost)
    {
        Map.MapData map = Map.MapData.GetCurrentMapData();

        float dis;
        int count = 0;
        foreach (var entry in map.SabotageMap)
        {
            if (!entry.Value.IsLeadingSabotage) continue;
            dis = 0;
            count = 0;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (player.Data.IsDead) continue;

                count++;
                dis += entry.Value.Position.Distance(player.transform.position);
            }

            ghost.SabotageMood[entry.Key] += Weight * (dis / (float)count) / map.MapScale;
        }
    }

    public AI_FarthestSabotageForPlayers(uint priority, float weight) : base(priority, weight) { }
}

public class AI_NearestSabotageForPlayers : GhostWeightedAI
{
    public override void Update(Ghost ghost)
    {
        Map.MapData map = Map.MapData.GetCurrentMapData();

        float dis;
        int count = 0;
        foreach (var entry in map.SabotageMap)
        {
            if (!entry.Value.IsLeadingSabotage) continue;
            dis = 0;
            count = 0;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (player.Data.IsDead) continue;

                count++;
                dis += entry.Value.Position.Distance(player.transform.position);
            }

            ghost.SabotageMood[entry.Key] += Weight * (map.MapScale - (dis / (float)count)) / map.MapScale;
        }
    }

    public AI_NearestSabotageForPlayers(uint priority, float weight) : base(priority, weight) { }
}

public class AI_FarthestSabotageForGhost : GhostWeightedAI
{
    public override void Update(Ghost ghost)
    {
        Map.MapData map = Map.MapData.GetCurrentMapData();

        float dis;
        foreach (var entry in map.SabotageMap)
        {
            if (!entry.Value.IsLeadingSabotage) continue;
            ghost.SabotageMood[entry.Key] += Weight * (ghost.Position.Distance(entry.Value.Position)) / map.MapScale;
        }
    }

    public AI_FarthestSabotageForGhost(uint priority, float weight) : base(priority, weight) { }
}

public class AI_FarthestSabotageForDeadBodies : GhostWeightedAI
{
    public override void Update(Ghost ghost)
    {
        Map.MapData map = Map.MapData.GetCurrentMapData();

        float dis;
        int count = 0;
        foreach (var entry in map.SabotageMap)
        {
            if (!entry.Value.IsLeadingSabotage) continue;

            dis = 0;
            count = 0;
            foreach (var body in Helpers.AllDeadBodies())
            {
                count++;
                dis += entry.Value.Position.Distance(body.transform.position);
            }

            ghost.SabotageMood[entry.Key] += Weight * (dis / (float)count) / map.MapScale;
        }
    }

    public AI_FarthestSabotageForDeadBodies(uint priority, float weight) : base(priority, weight) { }
}
