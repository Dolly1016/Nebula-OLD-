namespace Nebula.Ghost.AI;

public class AI_Noise : GhostWeightedAI
{
    private NoiseDestination Destination;
    private Func<float> NoiseGenerator;
    private bool IsMultipleAI;

    public override void Update(Ghost ghost)
    {
        GhostAI.BlendNoise(ghost, Destination, NoiseGenerator, Weight, IsMultipleAI);
    }

    public AI_Noise(uint priority, float weight, NoiseDestination destination, Func<float> generator, bool isMultipleAI) :
        base(priority, weight)
    {
        Destination = destination;
        NoiseGenerator = generator;
        IsMultipleAI = isMultipleAI;
    }
}

public class AI_WhiteNoise : AI_Noise
{
    public AI_WhiteNoise(uint priority, float weight, NoiseDestination destination, bool isMultipleAI) :
        base(priority, weight, destination, () =>
        {
            return (float)NebulaPlugin.rnd.NextDouble();
        }, isMultipleAI)
    { }
}

public class AI_RedNoise : AI_Noise
{
    public AI_RedNoise(uint priority, float weight, NoiseDestination destination, bool isMultipleAI) :
        base(priority, weight, destination, () =>
        {
            float rnd = (float)NebulaPlugin.rnd.NextDouble();
            return rnd * rnd;
        }, isMultipleAI)
    { }
}

public class AI_UltraRedNoise : AI_Noise
{
    public AI_UltraRedNoise(uint priority, float weight, NoiseDestination destination, int level, bool isMultipleAI) :
        base(priority, weight, destination, () =>
        {
            float rnd = (float)NebulaPlugin.rnd.NextDouble();
            float result = 1f;
            for (int i = 0; i < level; i++)
                result *= rnd;
            return result;
        }, isMultipleAI)
    { }
}