// The golden seed's pinned state (BattleTests). Re-pin only when the simulation is meant to change.
// Re-pinned 2026-11-04: disruptor bolts run the live line from their mount to their target (owner: "the source and
// destination of these beams must follow the ship source and ship destination"), so they land at other moments; then
// missiles fly at 320 m/s with a 9.6 km reach (owner: "the missiles should move slower"), so the Vos raider's land later; then every
// faction fits missile racks by grade with flares and decoys against them (owner: "all factions can have missiles too"),
// so the cruiser fires a blue missile too; then the special weapons (owner, 2026-11-05: "Implement weapons in game"):
// the cruiser's chorus lance and the destroyer's quell torpedoes.
// Re-pinned 2026-11-06: where a hit lands follows the mockup's chopping block (openspec/changes/weapon-targeting: the facing
// struck exposes the subsystems and mounts on its side, precise weapons aim), point defence and missiles charge from their own
// power, and weapons control stops the rest charging once it is disabled; so subsystems and mounts are hurt where the
// mockup hurts them and the fourth ship's hull ends lower.
namespace FleetCommand.Tests;

public static class GoldenSeed
{
    public const string Hulls = "560.00,240.00,380.00,122.93,260.00,320.00";
}
