namespace Nebula.Module;

public class Random
{
    private ulong state;
    private ulong increment = 1442695040888963407ul;

    // This shifted to the left and or'ed with 1ul results in the default increment.
    private const ulong ShiftedIncrement = 721347520444481703ul;
    private const ulong Multiplier = 6364136223846793005ul;

    private System.Random normalRandom;

    public Random() : this(DateTime.Now.Ticks, ShiftedIncrement)
    { }

    public Random(
        long seed, ulong state = ShiftedIncrement)
    {
        initialize((ulong)seed, state);
        normalRandom = new System.Random((int)seed);
    }

    public int Next()
    {
        uint result = NextUInt();
        return (int)(result >> 1);
    }

    public int Next(int maxExclusive)
    {
        if (maxExclusive == 0) return 0;

        bool inverse = maxExclusive < 0;
        if (inverse) maxExclusive *= -1;

        uint uMaxExclusive = (uint)(maxExclusive);
        uint threshold = (uint)(-uMaxExclusive) % uMaxExclusive;

        while (true)
        {
            uint result = NextUInt();
            if (result >= threshold)
                return (int)(result % uMaxExclusive) * (inverse ? -1 : 1);
        }
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentException("MaxExclusive must be larger than MinInclusive");
        }

        uint uMaxExclusive = unchecked((uint)(maxExclusive - minInclusive));
        uint threshold = (uint)(-uMaxExclusive) % uMaxExclusive;

        while (true)
        {
            uint result = NextUInt();
            if (result >= threshold)
            {
                return (int)(unchecked((result % uMaxExclusive) + minInclusive));
            }
        }
    }

    public uint NextUInt()
    {
        ulong oldState = this.state;
        this.state = unchecked(oldState * Multiplier + this.increment);
        uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        int rot = (int)(oldState >> 59);
        uint result = (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        return result;
    }

    public double NextDouble()
    {
        return normalRandom.NextDouble();
    }

    public void SetStream(ulong sequence)
    {
        this.increment = (sequence << 1) | 1;
    }

    public static ulong GuidBasedSeed()
    {
        ulong upper = (ulong)(Environment.TickCount ^ Guid.NewGuid().GetHashCode()) << 32;
        ulong lower = (ulong)(Environment.TickCount ^ Guid.NewGuid().GetHashCode());
        return (upper | lower);
    }

    private void initialize(
        ulong seed, ulong initStete)
    {
        this.state = 0ul;
        SetStream(initStete);

        NextUInt();

        this.state += seed;

        NextUInt();

    }
}