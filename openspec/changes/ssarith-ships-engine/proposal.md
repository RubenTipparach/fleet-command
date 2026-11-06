# Proposal: ssarith-ships-engine

## Why

The owner, 2026-11-06, of mockup 12 (the Ssarith Hunt ships, `docs/mockups/ssarith-ship-concepts.html`, v23): "looks
good, implement in game". The mockup is approved for build and for implementation by those words (recorded in
`mockup-work.md`). The game drew no Hunt ship at all: the faction had stand-in colours and no classes.

## What Changes

- **`tools/mockups/export_ships.mjs`** exports the Hunt's six ships from the mockup page, as it does the Concord and Krai
  lines: the four frigates, the cruiser and the carrier, each a glb of the bake's own low-poly hull with its UVs (v
  flipped for glTF), a drive, a reactor, its baked floods, windows and launch bays as named empties, and its fracture
  with its signed distance field (`<id>_sdf.bin`) for the cut faces. The hull's look is not in the glb: the manifest
  names the ship's own baked maps (`assets/ships/ssarith/<id>_*.png`, painted_bake.py), so there is one copy of them.
- **`src/Game/View/baked_hull.gdshader`**: a hull drawn from its own maps (base colour, ORM, normal, emission, the hull
  lights' light map), with the battle damage of every hull (`damage.gdshaderinc`), and lights that go out when its piece
  breaks away. `ShipSkin` dresses a manifest material marked `baked_hull` with it; `ShipDamage` takes either hull shader.
- **`cutface.gdshader`** closes a break in a grown hull by its signed distance field (a 3D texture) where a loft has
  stations, so the Hunt's hulls come apart as the others do.
- **Data**: six Hunt classes (`hunt_ff_lance`, `hunt_ff_prow`, `hunt_ff_harpoon`, `hunt_ff_trident`, `hunt_ca`, `hunt_cv`)
  with their models, placeholder figures at the Krai's sizes; a scenario, The Hunt, from each side (Concord, Krai);
  the Hunt's six on both damage ranges.
- Not yet: the Hunt as a side the player commands (it has no console skin in the game), its own weapons (its culture is
  the owner's, survey H1), the rooms behind its windows and the hangars behind its bays (their places are exported),
  its strobes' and beacons' sprites. Each is a task and a line in `docs/design/mockup-sync.md`.
