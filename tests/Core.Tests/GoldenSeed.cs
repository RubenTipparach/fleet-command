// The golden seed's pinned state (BattleTests). Re-pin only when the simulation is meant to change.
// Re-pinned 2026-11-04: disruptor bolts run the live line from their mount to their target (owner: "the source and
// destination of these beams must follow the ship source and ship destination"), so they land at other moments; then
// missiles fly at 320 m/s with a 9.6 km reach (owner: "the missiles should move slower"), so the Vos raider's land later; then every
// faction fits missile racks by grade with flares and decoys against them (owner: "all factions can have missiles too"),
// so the cruiser fires a blue missile too; then the special weapons (owner, 2026-11-05: "Implement weapons in game"):
// the cruiser's chorus lance and the destroyer's quell torpedoes; then the reactor (owner, 2026-11-06: "please add ALL
// systems that are IN the mockup"): the coolant pumps draw from the output, hits knock coils out of alignment (the output
// falls with them), and repair energy works through crews on the repair queue, so the Vorrak takes more.
namespace FleetCommand.Tests;

public static class GoldenSeed
{
    public const string Hulls = "560.00,240.00,380.00,122.42,260.00,320.00";
}
