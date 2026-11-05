// The golden seed's pinned state (BattleTests). Re-pin only when the simulation is meant to change.
// Re-pinned 2026-11-04: disruptor bolts run the live line from their mount to their target (owner: "the source and
// destination of these beams must follow the ship source and ship destination"), so they land at other moments.
namespace FleetCommand.Tests;

public static class GoldenSeed
{
    public const string Hulls = "560.00,240.00,380.00,133.00,260.00,320.00";
}
