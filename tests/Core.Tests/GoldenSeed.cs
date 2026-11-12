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
// the cruiser's chorus lance and the destroyer's quell torpedoes; then the reactor (owner, 2026-11-06: "please add ALL
// systems that are IN the mockup"): the coolant pumps draw from the output, hits knock coils out of alignment (the output
// falls with them), and repair energy works through crews on the repair queue. Merged with the chopping block (both
// packages, 2026-11-06), with a burn's ticks not rolling on the reactor (they are not hits), the Vorrak ends at 150.07.
// Re-pinned 2026-11-08: an AI captain whose fire has not landed for ai_close_after_s (20 s) closes to half its fighting range, half again each further 20 s
// (Mac playtest: a Vos corsair kited a Krai corvette at 49 km for a whole battle), so the escort closes sooner and the
// Vorrak ends at 142.84.
namespace FleetCommand.Tests;

public static class GoldenSeed
{
    public const string Hulls = "555.02,240.00,380.00,127.32,260.00,320.00";
}
