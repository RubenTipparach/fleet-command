# Proposal: vos-ships-engine

## Why

The owner, 2026-11-11, of mockup 37 (the Vos Syndicate ships, `docs/mockups/vos-ship-concepts.html`, v15): "I woudl cut
number 12 out (ram) , everything else is good to go, implmenet it". Mockup 37 is approved for build and for implementation
by those words (recorded in `mockup-work.md`), without the 12 Ram. The game drew the Vos as the placeholder line of mockup
36 (Skiff, Corsair, Privateer, Galleon), a ladder of sizes the Syndicate does not have (mockup 37 design D11).

## What Changes

- **The Vos page builds its ships on the shared baked fleet** (`docs/mockups/lib/baked-fleet.js`), as the Hunt's, the
  Institute's and the freighters' pages do, so the exporter reads them by its one baked branch. The page's own additions
  ride on its hook: the antennas and dishes as geometry, a shader lens on each glowing mouth, the Deck runner's six Cutters'
  places on its deck as landing pads, and its containers (`MOCKUP_CARGO`).
- **`tools/mockups/export_ships.mjs`** exports the five specialists and the three fighters: each a glb of the bake's own
  low-poly hull, with its antennas as meshes, its lamps, windows, bays, lenses (`lens_*`) and the Deck runner's pads as
  named empties, its fracture and its signed distance field, and its containers in the manifest.
- **The game draws a lens** on every `lens_*` empty (`src/Game/View/lens.gdshader`, a port of the page's lens shader,
  placed by `ShipSkin`).
- **Data**: the line in `data/ships/vos/line.json`, written into the classes, the campaign and the systems files by
  `tools/factions/placeholder_classes.py` as a placeholder line is: five classes at their true lengths times 2.7, a weaker
  hull (0.75 of their templates'), the Drum's twelve mines, the Cross's tractor, the Boarder's marines, the Deck runner's six
  Cutters (the Cross and the Boarder with 2 energy off their engines, so their allocations fit their reactors). The placeholder Vos line is
  retired; its row on the damage ranges is the new line's; the old stand-in `vcr` flies the Boarder's hull.
- Not yet: the disguise (vos-ship-concepts 2.4), the Vos's own weapons and figures, the fighters' own roles in battle (the
  Twin boom and the Stack are exported but unused), its deck plans from its room plans, its campaign beyond its hulls, and a scene of its own: a named scene is
  played from every side under a sky of its own (the scene tests), so The Syndicate is four scenes and four skies, task 2.5. Each is
  a task and a line in `docs/design/mockup-sync.md`.
