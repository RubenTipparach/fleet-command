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
- 1 pylon vertical, 2 pulons vertical".

## What Changes

- Version 1's three directions are withdrawn.
- **The Concord line, in the Tern family** (mockup 6, v2, `docs/mockups/concord-hull-concepts.html`):
  - **Swift-class corvette** (51 m): the Osprey's proportions, a broad command body forward, a narrow spine aft,
    nacelles slung low.
  - **Petrel-class frigate** (88 m): the Tern's proportions at the Concord's colours.
  - **Albatross-class cruiser** (131 m): the family at cruiser size, four decks, two nacelle pairs.
  - **The SCS Tern itself**, drawn from its own data at true scale, as the yardstick.
- Three pylon arrangements on every Concord class: **Level** (the family's own), **One upright** (one nacelle on the
  centreline above the hull on one vertical pylon) and **Two upright** (a nacelle pair above the hull on two vertical
  pylons), after the owner's note on the reference game's ships.
- Four liveries: Concord (ours) and the Tern's Copper, Cyan and Rescue for comparison.
- Survey A11 (pylons) and A12 (sizes) rewritten for v2.

## Impact

- Supersedes the silhouette and class shapes in `concord-ship-look` (its livery, markings, windows-as-scale and
  lighting rules stand). Mockup 5's damage system takes the chosen hull later. No game code.
