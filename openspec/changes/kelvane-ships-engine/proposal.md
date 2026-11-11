# Proposal: kelvane-ships-engine

## Why

The owner, 2026-11-07, of mockup 23 (the Kelvane Institute ships, `docs/mockups/kelvane-ship-concepts.html`, v10):
"perfect, implment these ships in game". The mockup is approved for build and for implementation by those words
(recorded in `mockup-work.md`). The game drew no Institute ship: its one class, `kelvane_rv` (the Four races scene's
Patience), was a stand-in drawn as the saucer shape.

## What Changes

- **The page's ships are the Hunt's code.** The Hunt's damageable baked ships move out of its page into
  `docs/mockups/lib/baked-fleet.js` (hull shader, damage, fracture, breakup, the manifest loader), and the Kelvane page
  builds its five classes and its fighter on it, so both pages, and the game's exporter, read one copy.
- **`tools/mockups/export_ships.mjs`** exports any baked faction (the Hunt and the Institute) by one branch: the five
  classes and the Gallery fighter, each a glb of the bake's own low-poly hull with its drive, reactor, baked floods,
  windows, bays, and (the carrier) its 24 landing pads as named empties, its fracture and its signed distance field.
- **The carrier's craft** (`src/Game/View/ParkedCraft.cs`): the fighter's own glb parked on the carrier's pads, upright on
  the deck and upside down on the well's ceilings, one a fighter in its bay, as the page parks them.
- **Data**: the line as five classes (`kelvane_ff`, `kelvane_dd`, `kelvane_rv` the cruiser, `kelvane_bc`, `kelvane_cv`),
  figures placeholders at the Hunt's sizes; the carrier's fighters; The Institute and Institute wrecks from the Concord's
  and the Krai's side; the Institute a side of its own (Battle, Duel, Arming, Four races, in the Patience); its five on
  both damage ranges.
- Not yet: the Institute's own weapons and figures (its culture is the owner's), its ships' names and captains, its
  campaign. Each is a task and a line in `docs/design/mockup-sync.md`.
