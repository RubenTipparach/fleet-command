// Rng: the simulation's seeded random numbers (CLAUDE.md section 6.3). SplitMix64, so a battle replays exactly from its
// seed on every platform; forked per subsystem by name, so adding a draw in one subsystem never shifts another's. The
// global generator (Random.Shared, GD.Randf) is never used by the simulation.
namespace FleetCommand.Sim;

public sealed class Rng
{
    private ulong _state;

    public Rng(ulong seed) { _state = seed; }

    /// <summary>The next 64 random bits.</summary>
    public ulong NextU64()
    {
        ulong z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A double in [0, 1), from the top 53 bits.</summary>
    public double Next() => (NextU64() >> 11) * (1.0 / 9007199254740992.0);

    /// <summary>A whole number in [0, n).</summary>
    public int Below(int n) => n <= 1 ? 0 : (int)(Next() * n);

    /// <summary>An independent stream for one subsystem, named so its seed does not depend on draw order.</summary>
    public static Rng Fork(ulong seed, string subsystem)
    {
        ulong h = 0xCBF29CE484222325UL; // FNV-1a over the name
        foreach (char c in subsystem) { h ^= c; h *= 0x100000001B3UL; }
        var r = new Rng(seed ^ h);
        r.NextU64();
        return r;
    }
}
