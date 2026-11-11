# Design: freighter-missions

## Encounters

An encounter is a mission file with `"kind": "encounter"` (`docs/design/mission-files.md`). It is excluded from
`CampaignData.Contracts`, so the turn's random offers never pick it. `Campaign.Meet(freighter)` offers it in the fleet's
sector, handing it `ship` (her name) and `freighter_fac` (her faction) through `takes`; a second Meet the same turn returns the
same offer. `EndTurn` drops every encounter, accepted or not, before the freighters move. Which file: `escort` when
`Campaign.Friendly(fac)` (your own faction, or standing at or above `rules.ally_standing`, 25), `raid` otherwise.

| File | Reward | Groups | Primary | Bonuses | Result |
|---|---|---|---|---|---|
| `escort.json` | 350 cr + 0-200 | the freighter (neutral, rescue), raiders of `{enemy}` at the offer's strength | she lives and the raiders are gone | half her hull; lose no ships | pay, standing +2 with her faction; failing, -1 |
| `raid.json` | 300 cr + 0-200 | the freighter (enemy, prize, 60 km), escorts of her faction (90 km) | she is destroyed or taken | taken with her cargo; lose no ships | pay, standing -4; taken, her haul too; failing, -2 |

## Freighters in battle

A group with `"hull": "freighter"` fields `CampaignData.FreighterOf(faction)` in place of warships of its tier. Every
freighter holds her course at 600 m/s (`Stance.Hold`), on whichever side, so a raid's freighter never closes to fight and an
escort's never wanders off. The figures are the freighter classes' (speed 2400 m/s at most, 90 hull, two point defence mounts;
the freighters change's design, provisional).

## Taken, carried, cashed out

- **Taken** is a prize that struck (hull at `capture_hull_frac`, 0.25) or a ship boarded and captured by marines
  (`Ship.Captured`): both now count for `taken()`, which before counted only the first.
- **Striking, not burning** (found in the playtest, 2026-11-08). Through-shield hits knock out a freighter's coolant loops long
  before her hull reaches the capture line: in the first Concord raid both loops were gone at 34 % hull and she melted down
  20 s later, and in the next she struck at 25 % and the ship that had her locked kept firing until she broke up. So a prize
  also strikes when her core goes critical (`Reactor.Melt > 0`), every lock on her is dropped as she strikes (as a meltdown
  drops them), and a struck ship's core is shut down by the boarding party (`Battle.Reactor.cs`): it cools and never melts down.
  Provisional (F4).
- **Haul.** An outcome's `haul` (a faction) and `haul_cr` (an expression; the raid's is the reward x 3, 900-1500 cr) add the
  freighter to the fleet as a `CampShip` with `Haul` credits, her hull as she struck, `Maker` her faction. With the fleet at
  `rules.fleet_max` (4) her cargo is moved across and paid at half at once.
- **Carried.** A ship with a haul is left out of the fleet's battles (`MissionRun.Plan`), unless she is all the fleet has. She
  cannot be sold as a hull.
- **Cash out** (`Campaign.CashOut`): at your own port, or a port of a faction with standing 0 or better (`IsMarket`), never to
  the faction she was taken from. Each haul pays its credits and her ship leaves the fleet; a log line records it.
- **Off her route.** The freighter met, destroyed or taken, has `Out = rules.freighter_refit_turns` (6): not drawn, not met,
  not moving, then she sails again from where she was.

## The map

`CampaignMap.ShipModel` takes a hull's glb from its schematic, else from its battle class's `model`
(`data/battle/classes.json`, handed in by `MapScreen`), so a freighter is her approved ship, dressed by `ShipSkin` and loaded
with her containers by `Cargo` (the battle's component). She is 0.9 units long, as a fleet's flagship. Shown: your own
always, anyone's in a sector `Campaign.IsVisible`. `MapPieces.Freighter`, the placeholder, is gone.

The sector panel (`MapHud.SectorPanel`) lists the freighters in the sector your ships see; with the fleet there each row has
its Escort or Raid key (GameFlow.Meet opens the briefing). With hauls aboard it shows the cargo's value and Cash out (live at
a port that deals with you).

## One copy

The encounter, the haul and the cash-out are each one function in `src/Core/Campaign/`; the HUD only calls them. The freighter
on the map and in battle is one glb and one `Cargo` component.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| F1 | Should a raid on a faction you are not at war with need a confirmation, or cost more standing? | No confirmation; -4 standing, as written |
| F2 | Should cargo sell for less at a foreign market than at your own port? | The same everywhere that deals with you |
| F3 | Should the game-flow mockup (mockup 1) show the encounters and the real freighters? | Yes, as sync tasks 3.1-3.2 |
| F4 | Should a prize whose core goes critical strike, and a struck ship's core be shut down? | Yes, both (survey V6) |
