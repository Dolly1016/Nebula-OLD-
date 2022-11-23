namespace Nebula.Ghost;
public enum NoiseDestination
{
    SabotageMood,
    DoorMood
}

public interface GhostAI
{
    uint Priority { get; }

    void Update(Ghost ghost);

    static protected SystemTypes? GetValidType(params SystemTypes[] systemTypes)
    {
        foreach (var room in systemTypes)
        {
            if (ShipStatus.Instance.FastRooms.ContainsKey(room)) return room;
        }
        return null;
    }

    static protected int GetCountOfAlivePlayers(SystemTypes room, int MaxPlayers)
    {
        if (!ShipStatus.Instance.FastRooms.ContainsKey(room)) return 0;
        return GetCountOfAlivePlayers(ShipStatus.Instance.FastRooms[room], MaxPlayers);
    }

    static protected int GetCountOfAlivePlayers(PlainShipRoom room, int MaxPlayers)
    {
        Collider2D roomArea = room.roomArea;
        int num = 0;

        foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (player.Data.IsDead) continue;

            if (roomArea.OverlapPoint(player.transform.position)) num++;
        }

        return num > MaxPlayers ? MaxPlayers : num;
    }

    static protected int GetCountOfDeadBodies(SystemTypes room, int MaxBodies)
    {
        if (!ShipStatus.Instance.FastRooms.ContainsKey(room)) return 0;
        return GetCountOfDeadBodies(ShipStatus.Instance.FastRooms[room], MaxBodies);
    }

    static protected int GetCountOfDeadBodies(PlainShipRoom room, int MaxBodies)
    {
        Collider2D roomArea = room.roomArea;
        int num = 0;

        foreach (DeadBody player in Helpers.AllDeadBodies())
        {
            if (roomArea.OverlapPoint(player.transform.position)) num++;
        }

        return num > MaxBodies ? MaxBodies : num;
    }

    static protected void BlendNoise(Ghost ghost, NoiseDestination destination, Func<float> generator, float weight, bool multiplyFlag)
    {
        switch (destination)
        {
            case NoiseDestination.SabotageMood:
                foreach (var room in ghost.SabotageKeys)
                {
                    if (multiplyFlag)
                        ghost.SabotageMood[room] *= weight * generator.Invoke();
                    else
                        ghost.SabotageMood[room] += weight * generator.Invoke();
                }
                break;
            case NoiseDestination.DoorMood:
                foreach (var room in ghost.DoorKeys)
                {
                    if (multiplyFlag)
                        ghost.DoorMood[room] *= weight * generator.Invoke();
                    else
                        ghost.DoorMood[room] += weight * generator.Invoke();
                }
                break;
        }
    }
}

public class GhostWeightedAI : GhostAI
{
    public float Weight { get; protected set; }
    public uint Priority { get; protected set; }

    public virtual void Update(Ghost ghost) { }

    public GhostWeightedAI(uint priority, float weight)
    {
        Priority = priority;
        Weight = weight;
    }
}