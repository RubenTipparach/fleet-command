# Proposal: placeholder-fleets

## Why

Nine factions now have a battle console (mockups 25-33) but no ships and no emblem: the Axiom, the Exponent, the Red
Tithe, the Vos Syndicate, the Kethran Hegemony, the Tarsine Remnant, the Aelith Conclave, the Ombrai and the Vhorr
Brood. The owner, 2026-11-08: "generate place holders for all of their ships, class design is your discretion, and a
place holder logo for the factions too".

## What Changes

- **A placeholder line for each of the nine factions**: four classes, frigate to capital, 36 in all. The class names,
  roles, lengths and armament sketches are ours, chosen from each faction's written culture. They are in one data file,
  `data/ships/placeholders.json`, written by `tools/factions/placeholder_fleets.py`.
- **One hull generator for all of them**, `docs/mockups/lib/placeholder-hulls.js`. A hull is a body loft with limb lofts
  grown from inside it. No primitive is stretched into a part. The drives are glowing recesses, and the light strips are
  stripes on the skin.
- **A placeholder emblem for each faction**, `assets/emblems/<faction>/placeholder.svg`, plus one JSON of the same SVGs
  (`assets/emblems/placeholders.json`). Both are written by `tools/factions/placeholder_emblems.py`.
- **Mockup 36, Placeholder fleets** (`docs/mockups/placeholder-fleets.html`). Each line is shown at true scale beside a
  100 m bar, orbitable, with its emblem and class table.

## Impact

- Nothing in the game changes until the owner approves the mockup for implementation. After that the hulls can be
  exported to glb by `tools/mockups/export_ships.mjs` and added to `data/battle/classes.json` and `factions.json`.
- Each placeholder is replaced when that faction's ship mockup and emblem go through their own design.
