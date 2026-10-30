# Proposal: game-flow

## Why

The owner, 2026-10-30: "let start with the primary game flow in three js". Before any battle exists, the
player needs a way into it: a main menu, a skirmish that is set up in a few taps, and a campaign that
strings battles together on a map. This change designs that flow end to end, and mockup 1
(`docs/mockups/game-flow.html`, `mockup-work.md`) shows it as an interactive page. Nothing is built in
the game until the mockup is approved for build and then for implementation (CLAUDE.md section 4).

The owner's brief, in their words:

- "main menu -> campaign or skirmish, option, quit"
- "skirmish has preset scenarios (for now)"
- "skirmish gives you points to pick ship, configure loadout ->play game"
- "campaign (no story yet) -> world map made of hexagons grid, viewed at perspective angle like Civ 5,
  can zoom. ships on little standees same with planets and stations"
- "world map has various panels: shipyard, ship configure/ammo/parts buy/sell shop, recruiting center
  (only if at friendly station or planet)"
- "else -> mission: patrol, enemy station assault, planetary assault, convoy ambush, convoy defense,
  assasination, capture a ship"
- "click on deploy mission -> play game -> end back to campaign map"

## What Changes

- **Main menu**: Campaign, Skirmish, Options, Quit, over a live 3D backdrop.
- **Skirmish**: pick a preset scenario, get its point budget, build a fleet of up to four ships, refit each
  ship's loadout by hardpoint, launch, play, see the result.
- **Ship loadout**: one refit screen, used by skirmish (paid in points) and by the campaign's outfitter
  (paid in credits): hardpoints in six groups, firing arcs, buy, trade and remove, resupply of carried
  items, and an AI behaviour for each wingman (survey G3: "with ai wingmen, and behaviors configuratble
  for AI").
- **Campaign map**: a hexagon map seen at a perspective angle, pan and zoom, territory by faction colour,
  terrain, and standees for the fleet, other ships, planets and stations. The fleet moves hex to hex.
- **Campaign ports**: at a friendly station or planet, the shipyard, the outfitter (ship configuration,
  ammunition and parts, buy and sell) and the recruiting center open.
- **Missions**: seven mission types offered on map hexes, a briefing, Deploy, the battle, a results
  screen, and back to the map with rewards and damage carried over.
- The battle itself is a stand-in here; it is mockup 2 (`battle-console`).

## Capabilities

### New Capabilities

- `main-menu`: the front end: Campaign, Skirmish, Options, Quit.
- `skirmish-setup`: preset scenarios, the point budget, fleet building and launch.
- `ship-loadout`: hardpoint refit, resupply and wingman behaviour, shared by skirmish and campaign.
- `campaign-map`: the hex map, its camera, territory, terrain, standees and fleet movement.
- `campaign-ports`: shipyard, outfitter and recruiting, available at friendly ports.
- `missions`: the mission types, where they are offered, deploy, results and the return to the map.

### Modified Capabilities

None (no specs exist yet).

## Impact

- New mockup page `docs/mockups/game-flow.html`, its shots in `docs/screenshots/mockups/`, its record in
  `docs/artifacts/game-flow/`, and an entry in the mockup tracker.
- New survey questions `F1` to `F6`.
- No game code. The data shown in the mockup (scenarios, hulls, systems, missions) becomes the first
  `data/` files when implementation is approved; the engine (survey T4) does not change this design.
