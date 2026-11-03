# Proposal: concord-hull-concepts

## Why

The owner, 2026-11-03, on mockup 5's Concord ships: "ship designs suck, I want somethign more original ,check out star
crew and adventure con repos for how ships are designed". Then, on this change's first version (Halo, Lantern spine,
Light sail): "those concepts are all bad. didnt you take a look at the otehr star crew repos?", "they dont fit my star
crrew ships atall", "star crew bridge sim", "star crew 64 and star raster are irrelevant".

So the reference is one project: **Star Crew Bridge Simulator** (`RubenTipparach/star-crew-bridge-simulator`) and its
ships, the SCS Tern (Heron-class frigate, the player ship) and its two companions, the Osprey courier and the Shrike
raider (`openspec/changes/ship-exteriors/`, `data/ships/exteriors.json`, `data/ships/tern/layout.json`, the captures
in `docs/screenshots/exteriors-*`). The Concord's ships are built as members of that family.

Then, on v2 while it was being built: "try some vertically aligned pulons too, i thnkn those from sfc was interesting
- 1 pylon vertical, 2 pulons vertical". And on v3: "swift having 1 below would be better, petrel having 1 above and
below", "albatross is a bid long, I would prefer it to have like a secondary hull where pulons extend outr of", then
"instead of opptions. please just apply the variants for both petrel and swift and include horizontal aligned ones aas
well". And on v4: "remove petrel with 1 pylon, add detail textures, norrmal maps, fix up windows, need to bring lighting
standards up to what we had before, and damage systems".

## What Changes

- Version 1's three directions are withdrawn.
- **The Concord line, in the Tern family** (mockup 6, v2, `docs/mockups/concord-hull-concepts.html`):
  - **Swift-class corvette** (51 m): the Osprey's proportions, a broad command body forward, a narrow spine aft.
  - **Petrel-class frigate** (88 m): the Tern's proportions at the Concord's colours.
  - **Albatross-class cruiser** (112 m): a shorter primary hull with a secondary hull under it on a neck; the nacelle
    pylons sweep out of the secondary hull.
  - **Condor-class battleship** (184 m): stepped triple-gun turrets fore and aft, broadside cannon sponsons, two nacelle pairs.
  - **Pelican-class carrier** (132 m): a catamaran of a spine and two hangar pods, fighter bays down the pods, a flight deck and runway.
  - **The SCS Tern itself**, drawn from its own data at true scale, as the yardstick.
- The Swift shown as three ships, one per nacelle layout: **one below** (one nacelle under the centreline on an upright
  pylon), **above and below** (one over, one under) and **level** (the family's pair on level swept pylons); the Petrel two
  ways, above and below and level. The line-up shows all seven at true scale.
- **Mockup 5's standards** on every ship (v5): its light presets, environment reflections, floodlight pools and
  navigation lights, and its whole damage system (a signature per subsystem, scorch, holes cut or drawn by interior mapping,
  venting fire, coolant and plasma, nacelles snapping off, random fracture with cut faces, wires, the staged core breach,
  weapon and blast lighting). New: **detail and normal maps**, and **real windows**: a copper rim, reflecting glass and a
  room behind each pane drawn by interior mapping.
- Four liveries: Concord (ours) and the Tern's Copper, Cyan and Rescue for comparison.
- Survey A11 (which layout each class keeps) and A12 (sizes).

## Impact

- Supersedes the silhouette and class shapes in `concord-ship-look` (its livery, markings, windows-as-scale and
  lighting rules stand). Mockup 5's damage system takes the chosen hull later. No game code.
